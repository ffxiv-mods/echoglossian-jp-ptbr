// <copyright file="LuminaJapaneseQuestDataReader.cs" company="lokinmodar">
// Copyright (c) lokinmodar. All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International Public License license.
// </copyright>

using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

namespace Echoglossian;

/// <summary>
///     Reads Japanese quest texts through the Dalamud data manager. Lumina
///     keeps the loaded sheets cached, so this reader holds no cache itself.
/// </summary>
internal sealed class LuminaJapaneseQuestDataReader : IJapaneseQuestDataReader
{
    /// <inheritdoc />
    public IReadOnlyList<QuestTextRow> ReadQuestTextRows(
        string questSheetName,
        CancellationToken cancellationToken)
    {
        var sheet = TryGetJapaneseQuestTextSheet(questSheetName);
        if (sheet == null)
        {
            return [];
        }

        var rows = new List<QuestTextRow>();
        var rowCount = Convert.ToInt32(sheet.Count, CultureInfo.InvariantCulture);
        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = sheet.GetRow((uint)rowIndex);
            var rowKey = row.ReadStringColumn(0).ExtractText();
            var text = row.ReadStringColumn(1).ExtractText();
            if (rowKey.Length == 0 || text.Length == 0)
            {
                continue;
            }

            rows.Add(new QuestTextRow(rowIndex, rowKey, text));
        }

        return rows;
    }

    /// <inheritdoc />
    public async Task<string?> EvaluateQuestTextRowAsync(
        string questSheetName,
        int rowIndex,
        string expectedRowKey,
        CancellationToken cancellationToken)
    {
        var sheet = TryGetJapaneseQuestTextSheet(questSheetName);
        if (sheet == null ||
            rowIndex < 0 ||
            rowIndex >= Convert.ToInt32(sheet.Count, CultureInfo.InvariantCulture))
        {
            return null;
        }

        var row = sheet.GetRow((uint)rowIndex);
        if (!string.Equals(
                row.ReadStringColumn(0).ExtractText(),
                expectedRowKey,
                StringComparison.Ordinal))
        {
            return null;
        }

        ReadOnlySeString rawText = row.ReadStringColumn(1);
        var extracted = rawText.ExtractText();
        var evaluator = Echoglossian.SeStringEvaluator;
        var framework = Echoglossian.FrameworkInterface;
        if (evaluator == null || framework == null)
        {
            return extracted;
        }

        string? evaluated = null;
        await framework.RunOnFrameworkThread(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    evaluated = evaluator.Evaluate(
                            rawText,
                            language: ClientLanguage.Japanese)
                        .ExtractText();
                }
                catch (Exception)
                {
                    evaluated = null;
                }
            }).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return string.IsNullOrWhiteSpace(evaluated) ? extracted : evaluated;
    }

    /// <inheritdoc />
    public string? ReadQuestName(uint questId)
    {
        var dataManager = Echoglossian.DManager;
        if (dataManager == null)
        {
            return null;
        }

        try
        {
            var questSheet = dataManager.GetExcelSheet<Quest>(ClientLanguage.Japanese);
            if (questSheet == null ||
                !QuestProgressResolver.TryResolveQuestRow(questSheet, questId, out _, out var questRow))
            {
                return null;
            }

            var name = QuestLuminaResolver.GetQuestNameText(questRow);
            return name.Length == 0 ? null : name;
        }
        catch (Exception ex)
        {
            PluginRuntimeLog.Debug(
                "SourceText",
                $"Japanese quest name unavailable for {questId}: {ex.GetType().Name}");
            return null;
        }
    }

    private static ExcelSheet<RawRow>? TryGetJapaneseQuestTextSheet(string questSheetName)
    {
        var dataManager = Echoglossian.DManager;
        if (dataManager == null || string.IsNullOrWhiteSpace(questSheetName))
        {
            return null;
        }

        try
        {
            var sheet = dataManager.GetExcelSheet<RawRow>(ClientLanguage.Japanese, questSheetName);
            return sheet == null || sheet.Count == 0 ? null : sheet;
        }
        catch (Exception ex)
        {
            PluginRuntimeLog.Debug(
                "SourceText",
                $"Japanese quest sheet unavailable ({questSheetName}): {ex.GetType().Name}");
            return null;
        }
    }
}
