// <copyright file="JapaneseQuestSourceTextProvider.cs" company="lokinmodar">
// Copyright (c) lokinmodar. All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International Public License license.
// </copyright>

using QuestManager = FFXIVClientStructs.FFXIV.Client.Game.QuestManager;

namespace Echoglossian;

/// <summary>
///     Maps visible client-language quest texts to their quest sheet rows and
///     reads the same rows from the Japanese game data. Row keys and indexes
///     are identical across game-data languages.
/// </summary>
public sealed class JapaneseQuestSourceTextProvider : IJapaneseQuestSourceTextProvider
{
    private readonly IJapaneseQuestDataReader reader;
    private readonly Func<string, CancellationToken, Task<IReadOnlyList<QuestDialogueRowMatch>>> findDialogueRowsAsync;
    private readonly Func<CancellationToken, Task<IReadOnlyList<(uint QuestId, byte QuestSequence)>>> captureAcceptedQuestsAsync;
    private readonly Func<uint, byte, CancellationToken, QuestProgressSnapshot?> resolveClientQuestProgress;

    /// <summary>
    ///     Initializes a new instance of the <see cref="JapaneseQuestSourceTextProvider" /> class
    ///     bound to the live game data.
    /// </summary>
    /// <param name="dialogueRowResolver">The resolver that matches visible dialogue to quest rows.</param>
    public JapaneseQuestSourceTextProvider(DialogueInterlocutorMetadataResolver dialogueRowResolver)
        : this(
            new LuminaJapaneseQuestDataReader(),
            dialogueRowResolver.FindAcceptedQuestDialogueRowsAsync,
            CaptureAcceptedQuestsAsync,
            ResolveClientQuestProgress)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="JapaneseQuestSourceTextProvider" /> class.
    /// </summary>
    /// <param name="reader">The Japanese quest data reader.</param>
    /// <param name="findDialogueRowsAsync">Matches a visible dialogue line to quest rows.</param>
    /// <param name="captureAcceptedQuestsAsync">Captures accepted quests and sequences.</param>
    /// <param name="resolveClientQuestProgress">Resolves a client-language quest snapshot.</param>
    internal JapaneseQuestSourceTextProvider(
        IJapaneseQuestDataReader reader,
        Func<string, CancellationToken, Task<IReadOnlyList<QuestDialogueRowMatch>>> findDialogueRowsAsync,
        Func<CancellationToken, Task<IReadOnlyList<(uint QuestId, byte QuestSequence)>>> captureAcceptedQuestsAsync,
        Func<uint, byte, CancellationToken, QuestProgressSnapshot?> resolveClientQuestProgress)
    {
        this.reader = reader;
        this.findDialogueRowsAsync = findDialogueRowsAsync;
        this.captureAcceptedQuestsAsync = captureAcceptedQuestsAsync;
        this.resolveClientQuestProgress = resolveClientQuestProgress;
    }

    /// <inheritdoc />
    public async Task<string?> TryResolveQuestDialogueAsync(
        string visibleText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(visibleText))
        {
            return null;
        }

        try
        {
            var rows = await this.findDialogueRowsAsync(visibleText, cancellationToken)
                .ConfigureAwait(false);
            var candidates = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                var text = await this.reader.EvaluateQuestTextRowAsync(
                    row.QuestSheetName,
                    row.SourceOrder,
                    row.RowKey,
                    cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    candidates.Add(text);
                }
            }

            return SelectUnique(candidates);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            PluginRuntimeLog.Debug(
                "SourceText",
                $"Japanese dialogue lookup failed: {ex.GetType().Name}");
            return null;
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> ReadQuestRows(
        string questSheetName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(questSheetName))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var rows = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in this.reader.ReadQuestTextRows(questSheetName, cancellationToken))
        {
            rows.TryAdd(row.RowKey, row.Text);
        }

        return rows;
    }

    /// <inheritdoc />
    public string? TryGetQuestName(uint questId)
    {
        return this.reader.ReadQuestName(questId);
    }

    /// <inheritdoc />
    public async Task<string?> TryResolveQuestTextAsync(
        string visibleText,
        uint? questId,
        CancellationToken cancellationToken = default)
    {
        var normalizedVisible = visibleText?.Trim() ?? string.Empty;
        if (normalizedVisible.Length == 0)
        {
            return null;
        }

        try
        {
            IReadOnlyList<(uint QuestId, byte QuestSequence)> quests = questId.HasValue
                ? [(questId.Value, (byte)0)]
                : await this.captureAcceptedQuestsAsync(cancellationToken).ConfigureAwait(false);
            var candidates = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (candidateQuestId, questSequence) in quests)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (this.resolveClientQuestProgress(candidateQuestId, questSequence, cancellationToken)
                    is not { } snapshot)
                {
                    continue;
                }

                if (string.Equals(snapshot.QuestName, normalizedVisible, StringComparison.Ordinal) &&
                    this.reader.ReadQuestName(snapshot.QuestId) is { Length: > 0 } japaneseName)
                {
                    candidates.Add(japaneseName);
                }

                var matchingKeys = snapshot.QuestSeqTexts
                    .Concat(snapshot.QuestSteps)
                    .Concat(snapshot.QuestSystemTexts)
                    .Where(entry => string.Equals(entry.Text.Trim(), normalizedVisible, StringComparison.Ordinal))
                    .Select(entry => entry.KeyText)
                    .ToArray();
                if (matchingKeys.Length == 0)
                {
                    continue;
                }

                var japaneseRows = this.ReadQuestRows(snapshot.QuestSheetName, cancellationToken);
                foreach (var key in matchingKeys)
                {
                    if (japaneseRows.TryGetValue(key, out var japaneseText) &&
                        !string.IsNullOrWhiteSpace(japaneseText))
                    {
                        candidates.Add(japaneseText);
                    }
                }
            }

            return SelectUnique(candidates);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            PluginRuntimeLog.Debug(
                "SourceText",
                $"Japanese quest text lookup failed: {ex.GetType().Name}");
            return null;
        }
    }

    private static string? SelectUnique(IReadOnlyCollection<string> candidates)
    {
        return candidates.Count == 1 ? candidates.First() : null;
    }

    private static async Task<IReadOnlyList<(uint QuestId, byte QuestSequence)>> CaptureAcceptedQuestsAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<(uint QuestId, byte QuestSequence)> quests = [];
        var framework = Echoglossian.FrameworkInterface;
        if (framework == null)
        {
            return quests;
        }

        await framework.RunOnFrameworkThread(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (QuestProgressResolver.TryCollectAcceptedQuestIds(out var questIds))
                {
                    quests = questIds
                        .Select(id => (id, QuestManager.GetQuestSequence((ushort)(id & 0xFFFF))))
                        .ToArray();
                }
            }).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return quests;
    }

    private static QuestProgressSnapshot? ResolveClientQuestProgress(
        uint questId,
        byte questSequence,
        CancellationToken cancellationToken)
    {
        return QuestProgressResolver.TryResolveQuestProgress(
            new QuestProgressCapture(
                questId,
                questSequence,
                Echoglossian.ClientStateInterface.ClientLanguage),
            cancellationToken,
            out var snapshot) ? snapshot : null;
    }
}
