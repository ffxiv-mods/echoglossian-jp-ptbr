// <copyright file="SourceTextTranslationTests.cs" company="lokinmodar">
// Copyright (c) lokinmodar. All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International Public License license.
// </copyright>

using System.Threading.Tasks;

using Echoglossian.Translators;

using Xunit;

namespace Echoglossian.Tests;

/// <summary>
///     Covers alternate-source translation with client-language fallback.
/// </summary>
public class SourceTextTranslationTests
{
    private static readonly SourceClientLanguage English = new("en", "en");

    private static readonly SourceClientLanguage Japanese = new("ja", "ja");

    /// <summary>
    ///     A usable alternate translation is returned without a client call.
    /// </summary>
    [Fact]
    public void TranslatePreferringAlternate_UsableAlternate_SendsJapanese()
    {
        var calls = new List<(string Text, string Source)>();

        var result = SourceTextTranslation.TranslatePreferringAlternate(
            "Hello.",
            English,
            "こんにちは。",
            Japanese,
            "pt-BR",
            (text, source) =>
            {
                calls.Add((text, source.ProviderCode));
                return "Olá.";
            });

        Assert.Equal("Olá.", result);
        Assert.Equal([("こんにちは。", "ja")], calls);
    }

    /// <summary>
    ///     An echoed alternate result falls back to the client text.
    /// </summary>
    [Fact]
    public void TranslatePreferringAlternate_EchoedAlternate_FallsBackToClient()
    {
        var calls = new List<string>();

        var result = SourceTextTranslation.TranslatePreferringAlternate(
            "Hello.",
            English,
            "こんにちは。",
            Japanese,
            "pt-BR",
            (text, source) =>
            {
                calls.Add(source.ProviderCode);
                return source.ProviderCode == "ja" ? text : "Olá.";
            });

        Assert.Equal("Olá.", result);
        Assert.Equal(["ja", "en"], calls);
    }

    /// <summary>
    ///     Without an alternate text only the client text is translated.
    /// </summary>
    [Fact]
    public async Task TranslatePreferringAlternateAsync_NoAlternate_UsesClient()
    {
        var calls = new List<string>();

        var result = await SourceTextTranslation.TranslatePreferringAlternateAsync(
            "Hello.",
            English,
            null,
            Japanese,
            "pt-BR",
            (text, source) =>
            {
                calls.Add(text);
                return Task.FromResult("Olá.");
            });

        Assert.Equal("Olá.", result);
        Assert.Equal(["Hello."], calls);
    }

    /// <summary>
    ///     An empty alternate result falls back to the client text.
    /// </summary>
    [Fact]
    public async Task TranslatePreferringAlternateAsync_EmptyAlternate_FallsBackToClient()
    {
        var result = await SourceTextTranslation.TranslatePreferringAlternateAsync(
            "Hello.",
            English,
            "こんにちは。",
            Japanese,
            "pt-BR",
            (_, source) => Task.FromResult(source.ProviderCode == "ja" ? string.Empty : "Olá."));

        Assert.Equal("Olá.", result);
    }

    /// <summary>
    ///     Field batches keep client order and fall back per field.
    /// </summary>
    [Fact]
    public async Task TranslateFieldsPreferringAlternateAsync_FallsBackPerField()
    {
        var clientFields = new[]
        {
            new TranslationField("name", "Potion"),
            new TranslationField("description", "Restores HP."),
            new TranslationField("extra", "Extra"),
        };
        var alternateFields = new[]
        {
            new TranslationField("name", "ポーション"),
            new TranslationField("description", "HPを回復する。"),
        };
        var requests = new List<(string Source, string[] Names)>();

        var result = await SourceTextTranslation.TranslateFieldsPreferringAlternateAsync(
            clientFields,
            English,
            alternateFields,
            Japanese,
            "pt-BR",
            (fields, source) =>
            {
                requests.Add((source.ProviderCode, fields.Select(field => field.Name).ToArray()));
                var translated = fields.Select(field => new TranslationField(
                    field.Name,
                    source.ProviderCode == "ja" && field.Name == "description"
                        ? field.Text
                        : $"pt:{field.Text}"));
                return Task.FromResult(new TranslationFieldBatchResult(translated, false));
            });

        Assert.Equal(["name", "description", "extra"], result.Translations.Keys);
        Assert.Equal("pt:ポーション", result.Translations["name"]);
        Assert.Equal("pt:Restores HP.", result.Translations["description"]);
        Assert.Equal("pt:Extra", result.Translations["extra"]);
        Assert.Equal(2, requests.Count);
        Assert.Equal("ja", requests[0].Source);
        Assert.Equal(["name", "description"], requests[0].Names);
        Assert.Equal("en", requests[1].Source);
        Assert.Equal(["description", "extra"], requests[1].Names);
    }

    /// <summary>
    ///     Without alternate fields only one client batch is requested.
    /// </summary>
    [Fact]
    public async Task TranslateFieldsPreferringAlternateAsync_NoAlternate_UsesClientBatch()
    {
        var clientFields = new[] { new TranslationField("name", "Potion") };
        var sources = new List<string>();

        var result = await SourceTextTranslation.TranslateFieldsPreferringAlternateAsync(
            clientFields,
            English,
            null,
            null,
            "pt-BR",
            (fields, source) =>
            {
                sources.Add(source.ProviderCode);
                return Task.FromResult(new TranslationFieldBatchResult(
                    fields.Select(field => new TranslationField(field.Name, "Poção")),
                    false));
            });

        Assert.Equal("Poção", result.Translations["name"]);
        Assert.Equal(["en"], sources);
    }
}
