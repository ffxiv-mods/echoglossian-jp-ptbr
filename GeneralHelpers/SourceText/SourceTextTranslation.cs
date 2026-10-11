// <copyright file="SourceTextTranslation.cs" company="lokinmodar">
// Copyright (c) lokinmodar. All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International Public License license.
// </copyright>

using System.Threading.Tasks;

using Echoglossian.Translators;

namespace Echoglossian;

/// <summary>
///     Translates an alternate source text (for example the Japanese original)
///     when one is available and falls back to the client-language text when
///     the alternate text is missing or its translation is unusable.
/// </summary>
/// <remarks>
///     Only the engine input changes: callers keep persisting and looking up
///     the client-language original, so DB semantics stay unchanged.
/// </remarks>
public static class SourceTextTranslation
{
    /// <summary>
    ///     Translates the alternate text, falling back to the client text.
    /// </summary>
    /// <param name="clientText">The text captured in the client language.</param>
    /// <param name="clientSource">The client source language.</param>
    /// <param name="alternateText">The alternate source text, if resolved.</param>
    /// <param name="alternateSource">The alternate source language, if any.</param>
    /// <param name="targetLanguage">The target language code.</param>
    /// <param name="translate">Translates a text from a source language.</param>
    /// <returns>The translated text.</returns>
    public static string TranslatePreferringAlternate(
        string clientText,
        SourceClientLanguage clientSource,
        string? alternateText,
        SourceClientLanguage? alternateSource,
        string targetLanguage,
        Func<string, SourceClientLanguage, string> translate)
    {
        ArgumentNullException.ThrowIfNull(translate);
        if (alternateSource is { } alternate &&
            !string.IsNullOrWhiteSpace(alternateText))
        {
            try
            {
                var alternateResult = translate(alternateText, alternate);
                if (IsUsable(alternateText, alternateResult, alternate, targetLanguage))
                {
                    return alternateResult;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                PluginRuntimeLog.Debug(
                    "SourceText",
                    $"Alternate source translation failed; using client text. {ex.GetType().Name}");
            }
        }

        return translate(clientText, clientSource);
    }

    /// <summary>
    ///     Asynchronously translates the alternate text, falling back to the
    ///     client text.
    /// </summary>
    /// <param name="clientText">The text captured in the client language.</param>
    /// <param name="clientSource">The client source language.</param>
    /// <param name="alternateText">The alternate source text, if resolved.</param>
    /// <param name="alternateSource">The alternate source language, if any.</param>
    /// <param name="targetLanguage">The target language code.</param>
    /// <param name="translateAsync">Translates a text from a source language.</param>
    /// <returns>The translated text.</returns>
    public static async Task<string> TranslatePreferringAlternateAsync(
        string clientText,
        SourceClientLanguage clientSource,
        string? alternateText,
        SourceClientLanguage? alternateSource,
        string targetLanguage,
        Func<string, SourceClientLanguage, Task<string>> translateAsync)
    {
        ArgumentNullException.ThrowIfNull(translateAsync);
        if (alternateSource is { } alternate &&
            !string.IsNullOrWhiteSpace(alternateText))
        {
            try
            {
                var alternateResult = await translateAsync(alternateText, alternate)
                    .ConfigureAwait(false);
                if (IsUsable(alternateText, alternateResult, alternate, targetLanguage))
                {
                    return alternateResult;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                PluginRuntimeLog.Debug(
                    "SourceText",
                    $"Alternate source translation failed; using client text. {ex.GetType().Name}");
            }
        }

        return await translateAsync(clientText, clientSource).ConfigureAwait(false);
    }

    /// <summary>
    ///     Translates a field batch from the alternate source, re-translating
    ///     from the client source only the fields whose alternate value is
    ///     missing or unusable.
    /// </summary>
    /// <param name="clientFields">The fields captured in the client language.</param>
    /// <param name="clientSource">The client source language.</param>
    /// <param name="alternateFields">
    ///     The alternate source fields keyed by the same field names, if any.
    /// </param>
    /// <param name="alternateSource">The alternate source language, if any.</param>
    /// <param name="targetLanguage">The target language code.</param>
    /// <param name="translateFieldsAsync">Translates a field batch from a source language.</param>
    /// <returns>The merged translated field batch in client field order.</returns>
    public static async Task<TranslationFieldBatchResult> TranslateFieldsPreferringAlternateAsync(
        IReadOnlyList<TranslationField> clientFields,
        SourceClientLanguage clientSource,
        IReadOnlyList<TranslationField>? alternateFields,
        SourceClientLanguage? alternateSource,
        string targetLanguage,
        Func<IReadOnlyList<TranslationField>, SourceClientLanguage, Task<TranslationFieldBatchResult>> translateFieldsAsync)
    {
        ArgumentNullException.ThrowIfNull(clientFields);
        ArgumentNullException.ThrowIfNull(translateFieldsAsync);
        if (alternateSource is not { } alternate ||
            alternateFields is null ||
            alternateFields.Count == 0 ||
            alternateFields.All(field => string.IsNullOrWhiteSpace(field.Text)))
        {
            return await translateFieldsAsync(clientFields, clientSource).ConfigureAwait(false);
        }

        var alternateByName = alternateFields
            .Where(field => !string.IsNullOrWhiteSpace(field.Text))
            .GroupBy(field => field.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Text, StringComparer.Ordinal);
        var requested = clientFields
            .Where(field => alternateByName.ContainsKey(field.Name))
            .Select(field => new TranslationField(field.Name, alternateByName[field.Name]))
            .ToArray();
        if (requested.Length == 0)
        {
            return await translateFieldsAsync(clientFields, clientSource).ConfigureAwait(false);
        }

        TranslationFieldBatchResult? alternateResult = null;
        try
        {
            alternateResult = await translateFieldsAsync(requested, alternate).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            PluginRuntimeLog.Debug(
                "SourceText",
                $"Alternate source field batch failed; using client text. {ex.GetType().Name}");
        }

        var merged = new Dictionary<string, string>(StringComparer.Ordinal);
        var missing = new List<TranslationField>();
        foreach (var field in clientFields)
        {
            if (alternateResult is not null &&
                alternateByName.TryGetValue(field.Name, out var alternateText) &&
                alternateResult.Translations.TryGetValue(field.Name, out var translated) &&
                IsUsable(alternateText, translated, alternate, targetLanguage))
            {
                merged[field.Name] = translated;
            }
            else
            {
                missing.Add(field);
            }
        }

        var usedIndividualFallback = alternateResult?.UsedIndividualFallback ?? false;
        if (missing.Count > 0)
        {
            var fallback = await translateFieldsAsync(missing, clientSource).ConfigureAwait(false);
            usedIndividualFallback |= fallback.UsedIndividualFallback;
            foreach (var field in missing)
            {
                merged[field.Name] = fallback.Translations.TryGetValue(field.Name, out var value)
                    ? value
                    : string.Empty;
            }
        }

        return new TranslationFieldBatchResult(
            clientFields.Select(field => new TranslationField(field.Name, merged[field.Name])),
            usedIndividualFallback);
    }

    private static bool IsUsable(
        string alternateText,
        string? translated,
        SourceClientLanguage alternate,
        string targetLanguage)
    {
        return TranslationPersistenceGuard.IsUsableDialogueTranslation(
            alternateText,
            translated,
            alternate.PersistenceCode,
            targetLanguage);
    }
}
