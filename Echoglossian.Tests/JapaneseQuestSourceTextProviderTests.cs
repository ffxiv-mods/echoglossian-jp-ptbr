// <copyright file="JapaneseQuestSourceTextProviderTests.cs" company="lokinmodar">
// Copyright (c) lokinmodar. All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International Public License license.
// </copyright>

using System.Threading.Tasks;

using Lumina.Text.ReadOnly;

using Xunit;

namespace Echoglossian.Tests;

/// <summary>
///     Covers mapping client-language quest texts to Japanese quest rows.
/// </summary>
public class JapaneseQuestSourceTextProviderTests
{
    private const string SheetName = "quest/032/LucKbb111_03263";

    /// <summary>
    ///     A matched dialogue row returns the evaluated Japanese row text.
    /// </summary>
    [Fact]
    public async Task TryResolveQuestDialogueAsync_MatchedRow_ReturnsJapanese()
    {
        var reader = new FakeReader();
        var provider = CreateProvider(
            reader,
            rows: [new QuestDialogueRowMatch(SheetName, "LucKbb111_03263_NPC_000_000", 3)]);

        var result = await provider.TryResolveQuestDialogueAsync("Welcome, adventurer.");

        Assert.Equal("ようこそ、冒険者さん。", result);
        Assert.Equal([(SheetName, 3, "LucKbb111_03263_NPC_000_000")], reader.Evaluated);
    }

    /// <summary>
    ///     Rows that disagree on the Japanese text are ambiguous.
    /// </summary>
    [Fact]
    public async Task TryResolveQuestDialogueAsync_AmbiguousRows_ReturnsNull()
    {
        var provider = CreateProvider(
            new FakeReader(),
            rows:
            [
                new QuestDialogueRowMatch(SheetName, "LucKbb111_03263_NPC_000_000", 3),
                new QuestDialogueRowMatch(SheetName, "LucKbb111_03263_NPC_000_010", 4),
            ]);

        Assert.Null(await provider.TryResolveQuestDialogueAsync("Yes."));
    }

    /// <summary>
    ///     No matching row keeps the caller on the client text.
    /// </summary>
    [Fact]
    public async Task TryResolveQuestDialogueAsync_NoRow_ReturnsNull()
    {
        var provider = CreateProvider(new FakeReader(), rows: []);

        Assert.Null(await provider.TryResolveQuestDialogueAsync("Unknown line."));
    }

    /// <summary>
    ///     A journal summary resolves through its row key.
    /// </summary>
    [Fact]
    public async Task TryResolveQuestTextAsync_SummaryKey_ReturnsJapaneseRow()
    {
        var provider = CreateProvider(new FakeReader(), rows: []);

        var result = await provider.TryResolveQuestTextAsync("Speak with Oriel.", 68799);

        Assert.Equal("オリエルと話す。", result);
    }

    /// <summary>
    ///     A visible quest title resolves to the Japanese title.
    /// </summary>
    [Fact]
    public async Task TryResolveQuestTextAsync_QuestTitle_ReturnsJapaneseTitle()
    {
        var provider = CreateProvider(new FakeReader(), rows: []);

        var result = await provider.TryResolveQuestTextAsync("For Better or Worse", null);

        Assert.Equal("良くも悪くも", result);
    }

    /// <summary>
    ///     A visible title of a quest that is not accepted resolves through
    ///     the title index.
    /// </summary>
    [Fact]
    public async Task TryResolveQuestTextAsync_TitleOfUnacceptedQuest_UsesTitleIndex()
    {
        var provider = new JapaneseQuestSourceTextProvider(
            new FakeReader(),
            (_, _) => Task.FromResult<IReadOnlyList<QuestDialogueRowMatch>>([]),
            _ => Task.FromResult<IReadOnlyList<(uint QuestId, byte QuestSequence)>>([]),
            (_, _, _) => null,
            title => title == "For Better or Worse" ? 68799u : null);

        var result = await provider.TryResolveQuestTextAsync("For Better or Worse", null);

        Assert.Equal("良くも悪くも", result);
    }

    /// <summary>
    ///     A text that no accepted quest owns returns null.
    /// </summary>
    [Fact]
    public async Task TryResolveQuestTextAsync_UnknownText_ReturnsNull()
    {
        var provider = CreateProvider(new FakeReader(), rows: []);

        Assert.Null(await provider.TryResolveQuestTextAsync("Defeat 0/4 enemies.", null));
    }

    private static JapaneseQuestSourceTextProvider CreateProvider(
        FakeReader reader,
        IReadOnlyList<QuestDialogueRowMatch> rows)
    {
        var snapshot = new QuestProgressSnapshot(
            QuestId: 68799,
            QuestSequence: 0,
            QuestName: "For Better or Worse",
            QuestSheetName: SheetName,
            QuestSteps: [Entry("LucKbb111_03263_TODO_00", "Speak with Oriel.")],
            QuestSeqTexts: [Entry("LucKbb111_03263_SEQ_00", "Oriel needs help.")],
            QuestSystemTexts: [],
            ContentHash: "hash");

        return new JapaneseQuestSourceTextProvider(
            reader,
            (_, _) => Task.FromResult(rows),
            _ => Task.FromResult<IReadOnlyList<(uint QuestId, byte QuestSequence)>>([(68799u, (byte)0)]),
            (questId, _, _) => questId == 68799 ? snapshot : null);
    }

    private static QuestProgressEntry Entry(string key, string text)
    {
        return new QuestProgressEntry(default(ReadOnlySeString), default(ReadOnlySeString), key, text);
    }

    private sealed class FakeReader : IJapaneseQuestDataReader
    {
        public List<(string Sheet, int Index, string Key)> Evaluated { get; } = [];

        public IReadOnlyList<QuestTextRow> ReadQuestTextRows(
            string questSheetName,
            CancellationToken cancellationToken)
        {
            return
            [
                new QuestTextRow(0, "LucKbb111_03263_SEQ_00", "オリエルが助けを求めている。"),
                new QuestTextRow(1, "LucKbb111_03263_TODO_00", "オリエルと話す。"),
            ];
        }

        public Task<string?> EvaluateQuestTextRowAsync(
            string questSheetName,
            int rowIndex,
            string expectedRowKey,
            CancellationToken cancellationToken)
        {
            this.Evaluated.Add((questSheetName, rowIndex, expectedRowKey));
            return Task.FromResult<string?>(rowIndex == 3 ? "ようこそ、冒険者さん。" : "はい。");
        }

        public string? ReadQuestName(uint questId)
        {
            return questId == 68799 ? "良くも悪くも" : null;
        }
    }
}
