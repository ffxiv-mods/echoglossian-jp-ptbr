// <copyright file="IJapaneseQuestDataReader.cs" company="lokinmodar">
// Copyright (c) lokinmodar. All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International Public License license.
// </copyright>

namespace Echoglossian;

/// <summary>
///     Reads quest texts from the Japanese game data regardless of the client
///     language.
/// </summary>
internal interface IJapaneseQuestDataReader
{
    /// <summary>
    ///     Reads every populated row of one quest text sheet in Japanese.
    /// </summary>
    /// <param name="questSheetName">The quest text sheet name.</param>
    /// <param name="cancellationToken">The token that cancels sheet traversal.</param>
    /// <returns>The populated rows in sheet order.</returns>
    IReadOnlyList<QuestTextRow> ReadQuestTextRows(
        string questSheetName,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Evaluates one Japanese quest text row, including player-name and
    ///     gender macros, on the framework thread.
    /// </summary>
    /// <param name="questSheetName">The quest text sheet name.</param>
    /// <param name="rowIndex">The zero-based row index.</param>
    /// <param name="expectedRowKey">The row key the index must still carry.</param>
    /// <param name="cancellationToken">The token that cancels the evaluation.</param>
    /// <returns>The evaluated Japanese text, or <see langword="null" />.</returns>
    Task<string?> EvaluateQuestTextRowAsync(
        string questSheetName,
        int rowIndex,
        string expectedRowKey,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Reads the Japanese quest title.
    /// </summary>
    /// <param name="questId">The runtime or row quest id.</param>
    /// <returns>The Japanese title, or <see langword="null" />.</returns>
    string? ReadQuestName(uint questId);
}

/// <summary>
///     Represents one quest text sheet row.
/// </summary>
/// <param name="Index">The zero-based row index.</param>
/// <param name="RowKey">The language-independent row key.</param>
/// <param name="Text">The row text with macros removed.</param>
internal readonly record struct QuestTextRow(
    int Index,
    string RowKey,
    string Text);
