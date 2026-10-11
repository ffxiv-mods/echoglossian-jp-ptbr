// <copyright file="JapaneseSourceTextPolicy.cs" company="lokinmodar">
// Copyright (c) lokinmodar. All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International Public License license.
// </copyright>

namespace Echoglossian;

/// <summary>
///     Prefers the Japanese original text as translation input when the user
///     enabled <see cref="Config.UseJapaneseSourceText" /> and neither the
///     client nor the target language is already Japanese.
/// </summary>
public sealed class JapaneseSourceTextPolicy : ISourceTextLanguagePolicy
{
    private readonly Func<bool> isEnabled;

    /// <summary>
    ///     Initializes a new instance of the <see cref="JapaneseSourceTextPolicy" /> class
    ///     bound to the live plugin configuration.
    /// </summary>
    /// <param name="configuration">The live plugin configuration.</param>
    public JapaneseSourceTextPolicy(Config configuration)
        : this(() => configuration.UseJapaneseSourceText)
    {
        ArgumentNullException.ThrowIfNull(configuration);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="JapaneseSourceTextPolicy" /> class.
    /// </summary>
    /// <param name="isEnabled">Reads whether the option is currently enabled.</param>
    public JapaneseSourceTextPolicy(Func<bool> isEnabled)
    {
        ArgumentNullException.ThrowIfNull(isEnabled);
        this.isEnabled = isEnabled;
    }

    /// <inheritdoc />
    public bool TryGetAlternateSource(
        SourceClientLanguage clientSource,
        string? targetLanguageCode,
        out SourceClientLanguage alternateSource)
    {
        alternateSource = default;
        if (!this.isEnabled())
        {
            return false;
        }

        if (!RuntimeLanguageHelper.TryResolveSourceLanguage(
                ClientLanguage.Japanese,
                out var japanese))
        {
            return false;
        }

        if (RuntimeLanguageHelper.LanguagesMatch(
                clientSource.PersistenceCode,
                japanese.PersistenceCode) ||
            RuntimeLanguageHelper.LanguagesMatch(
                targetLanguageCode,
                japanese.PersistenceCode))
        {
            return false;
        }

        alternateSource = japanese;
        return true;
    }
}
