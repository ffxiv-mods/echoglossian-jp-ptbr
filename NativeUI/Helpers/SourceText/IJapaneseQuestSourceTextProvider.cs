// <copyright file="IJapaneseQuestSourceTextProvider.cs" company="lokinmodar">
// Copyright (c) lokinmodar. All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International Public License license.
// </copyright>

namespace Echoglossian;

/// <summary>
///     Resolves the Japanese original of quest texts captured in the client
///     language. Every method returns <see langword="null" /> or an empty
///     result when the original cannot be resolved unambiguously, so callers
///     keep translating from the client-language text.
/// </summary>
public interface IJapaneseQuestSourceTextProvider
{
    /// <summary>
    ///     Resolves the Japanese original of one visible quest dialogue line
    ///     from the accepted quests in their current sequence.
    /// </summary>
    /// <param name="visibleText">The visible client-language dialogue line.</param>
    /// <param name="cancellationToken">The token that cancels the lookup.</param>
    /// <returns>The Japanese line, or <see langword="null" />.</returns>
    Task<string?> TryResolveQuestDialogueAsync(
        string visibleText,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Reads one quest text sheet in Japanese keyed by row key.
    /// </summary>
    /// <param name="questSheetName">The quest text sheet name.</param>
    /// <param name="cancellationToken">The token that cancels sheet traversal.</param>
    /// <returns>The Japanese texts keyed by row key.</returns>
    IReadOnlyDictionary<string, string> ReadQuestRows(
        string questSheetName,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Reads the Japanese title of one quest.
    /// </summary>
    /// <param name="questId">The runtime or row quest id.</param>
    /// <returns>The Japanese title, or <see langword="null" />.</returns>
    string? TryGetQuestName(uint questId);

    /// <summary>
    ///     Resolves the Japanese original of one visible quest title, journal
    ///     summary, objective, or system text.
    /// </summary>
    /// <param name="visibleText">The visible client-language quest text.</param>
    /// <param name="questId">
    ///     The quest id when known; otherwise the accepted quests are searched.
    /// </param>
    /// <param name="cancellationToken">The token that cancels the lookup.</param>
    /// <returns>The Japanese text, or <see langword="null" />.</returns>
    Task<string?> TryResolveQuestTextAsync(
        string visibleText,
        uint? questId,
        CancellationToken cancellationToken = default);
}
