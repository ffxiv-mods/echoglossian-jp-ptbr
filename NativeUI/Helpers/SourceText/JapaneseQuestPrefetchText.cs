// <copyright file="JapaneseQuestPrefetchText.cs" company="lokinmodar">
// Copyright (c) lokinmodar. All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International Public License license.
// </copyright>

namespace Echoglossian;

/// <summary>
///     Carries the Japanese rows and title of one quest for a prefetch work
///     item, keyed by the language-independent row key.
/// </summary>
/// <param name="Source">The Japanese source language.</param>
/// <param name="RowsByKey">The Japanese row texts keyed by row key.</param>
/// <param name="QuestName">The Japanese quest title, if any.</param>
internal sealed record JapaneseQuestPrefetchText(
    SourceClientLanguage Source,
    IReadOnlyDictionary<string, string> RowsByKey,
    string? QuestName)
{
    /// <summary>
    ///     Finds the Japanese text of one row.
    /// </summary>
    /// <param name="rowKey">The row key.</param>
    /// <returns>The Japanese text, or <see langword="null" />.</returns>
    public string? FindRow(string? rowKey)
    {
        return !string.IsNullOrWhiteSpace(rowKey) &&
               this.RowsByKey.TryGetValue(rowKey, out var text)
            ? text
            : null;
    }
}
