// <copyright file="ISourceTextLanguagePolicy.cs" company="lokinmodar">
// Copyright (c) lokinmodar. All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International Public License license.
// </copyright>

namespace Echoglossian;

/// <summary>
///     Decides whether a translation operation should send an alternate
///     source-language text (read from game data) to the translation engine
///     instead of the text captured in the client language.
/// </summary>
public interface ISourceTextLanguagePolicy
{
    /// <summary>
    ///     Resolves the alternate source language for one operation.
    /// </summary>
    /// <param name="clientSource">
    ///     The source language captured from the client for the operation.
    /// </param>
    /// <param name="targetLanguageCode">The operation target language code.</param>
    /// <param name="alternateSource">
    ///     The alternate source language when the policy applies.
    /// </param>
    /// <returns>
    ///     <see langword="true" /> when the operation should prefer the
    ///     alternate source text; otherwise <see langword="false" />.
    /// </returns>
    bool TryGetAlternateSource(
        SourceClientLanguage clientSource,
        string? targetLanguageCode,
        out SourceClientLanguage alternateSource);
}
