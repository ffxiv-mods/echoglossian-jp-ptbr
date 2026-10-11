// <copyright file="JapaneseSourceTextWiring.cs" company="lokinmodar">
// Copyright (c) lokinmodar. All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International Public License license.
// </copyright>

namespace Echoglossian;

/// <summary>
///     Plugin-owned wiring that hands the Japanese source-text option to the
///     addon handlers and prefetch runtimes.
/// </summary>
public partial class Echoglossian
{
  private ISourceTextLanguagePolicy? sourceTextLanguagePolicy;
  private IJapaneseQuestSourceTextProvider? japaneseQuestSourceText;

  /// <summary>
  ///     Gets the policy that decides when the Japanese original is preferred.
  /// </summary>
  internal ISourceTextLanguagePolicy SourceTextLanguagePolicy =>
      this.sourceTextLanguagePolicy ??= new JapaneseSourceTextPolicy(
          () => this.configuration.UseJapaneseSourceText);

  /// <summary>
  ///     Gets the provider that reads Japanese quest texts from game data.
  /// </summary>
  internal IJapaneseQuestSourceTextProvider JapaneseQuestSourceText =>
      this.japaneseQuestSourceText ??= new JapaneseQuestSourceTextProvider(
          new DialogueInterlocutorMetadataResolver(this));

  /// <summary>
  ///     Decides whether a sheet-backed prefetch should also read the
  ///     Japanese row for the current client and target languages.
  /// </summary>
  /// <param name="japaneseSource">The Japanese source language when it applies.</param>
  /// <returns><see langword="true" /> when the Japanese row should be read.</returns>
  private bool TryGetJapanesePrefetchSource(out SourceClientLanguage japaneseSource)
  {
    japaneseSource = default;
    return RuntimeLanguageHelper.TryResolveCurrentSourceLanguage(out var clientSource) &&
           this.SourceTextLanguagePolicy.TryGetAlternateSource(
               clientSource,
               RuntimeLanguageHelper.GetConfiguredTargetLanguageCode(this.configuration.Lang),
               out japaneseSource);
  }

  /// <summary>
  ///     Translates one prefetched sheet text, preferring its Japanese
  ///     original when one was read for the same sheet row.
  /// </summary>
  /// <param name="clientText">The client-language sheet text.</param>
  /// <param name="clientSource">The captured client source language.</param>
  /// <param name="japaneseText">The Japanese sheet text, if any.</param>
  /// <param name="japaneseSource">The Japanese source language, if it applies.</param>
  /// <param name="targetLanguage">The target language code.</param>
  /// <param name="originContext">The translation origin label.</param>
  /// <returns>The translated text.</returns>
  private static string TranslatePrefetchPreferringAlternate(
      string clientText,
      SourceClientLanguage clientSource,
      string? japaneseText,
      SourceClientLanguage? japaneseSource,
      string targetLanguage,
      string originContext)
  {
    var translationService = TranslationService;
    return SourceTextTranslation.TranslatePreferringAlternate(
        clientText,
        clientSource,
        japaneseText,
        japaneseSource,
        targetLanguage,
        (text, source) => translationService.Translate(
            text,
            source,
            targetLanguage,
            originContext: originContext));
  }

  /// <summary>
  ///     Resolves the Japanese original of one visible quest dialogue line
  ///     when the option applies to the operation.
  /// </summary>
  /// <param name="originalText">The visible client-language dialogue line.</param>
  /// <param name="clientSource">The captured client source language.</param>
  /// <param name="targetLanguageCode">The operation target language.</param>
  /// <param name="cancellationToken">The handler-owned cancellation token.</param>
  /// <returns>The Japanese source text, or <see langword="null" />.</returns>
  private async Task<AlternateSourceText?> ResolveJapaneseDialogueSourceTextAsync(
      string originalText,
      SourceClientLanguage clientSource,
      string targetLanguageCode,
      CancellationToken cancellationToken)
  {
    if (!this.SourceTextLanguagePolicy.TryGetAlternateSource(
            clientSource,
            targetLanguageCode,
            out var japaneseSource))
    {
      return null;
    }

    var japaneseText = await this.JapaneseQuestSourceText
        .TryResolveQuestDialogueAsync(originalText, cancellationToken)
        .ConfigureAwait(false);
    return string.IsNullOrWhiteSpace(japaneseText)
        ? null
        : new AlternateSourceText(japaneseText, japaneseSource);
  }

  /// <summary>
  ///     Resolves the Japanese original of one visible quest title, journal
  ///     summary, objective, or system text when the option applies.
  /// </summary>
  /// <param name="visibleText">The visible client-language quest text.</param>
  /// <param name="questId">The quest id when known.</param>
  /// <param name="clientSource">The captured client source language.</param>
  /// <param name="targetLanguageCode">The operation target language.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>The Japanese source text, or <see langword="null" />.</returns>
  private async Task<AlternateSourceText?> ResolveJapaneseQuestSourceTextAsync(
      string visibleText,
      uint? questId,
      SourceClientLanguage clientSource,
      string targetLanguageCode,
      CancellationToken cancellationToken)
  {
    if (!this.SourceTextLanguagePolicy.TryGetAlternateSource(
            clientSource,
            targetLanguageCode,
            out var japaneseSource))
    {
      return null;
    }

    var japaneseText = await this.JapaneseQuestSourceText
        .TryResolveQuestTextAsync(visibleText, questId, cancellationToken)
        .ConfigureAwait(false);
    return string.IsNullOrWhiteSpace(japaneseText)
        ? null
        : new AlternateSourceText(japaneseText, japaneseSource);
  }
}
