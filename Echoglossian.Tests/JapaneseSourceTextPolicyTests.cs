// <copyright file="JapaneseSourceTextPolicyTests.cs" company="lokinmodar">
// Copyright (c) lokinmodar. All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International Public License license.
// </copyright>

using Echoglossian.Translators;

using Xunit;

namespace Echoglossian.Tests;

/// <summary>
///     Covers when the Japanese source-text option applies.
/// </summary>
public class JapaneseSourceTextPolicyTests
{
    private static readonly SourceClientLanguage English = new("en", "en");

    /// <summary>
    ///     The option is off by default in a fresh configuration.
    /// </summary>
    [Fact]
    public void Policy_DefaultConfig_DoesNotApply()
    {
        var policy = new JapaneseSourceTextPolicy(new Config());

        Assert.False(policy.TryGetAlternateSource(English, "pt-BR", out _));
    }

    /// <summary>
    ///     An English client with the option on prefers the Japanese source.
    /// </summary>
    [Fact]
    public void Policy_EnabledWithEnglishClient_ReturnsJapanese()
    {
        var policy = new JapaneseSourceTextPolicy(new Config { UseJapaneseSourceText = true });

        Assert.True(policy.TryGetAlternateSource(English, "pt-BR", out var alternate));
        Assert.Equal(new SourceClientLanguage("ja", "ja"), alternate);
    }

    /// <summary>
    ///     A Japanese client already captures Japanese text.
    /// </summary>
    [Fact]
    public void Policy_JapaneseClient_DoesNotApply()
    {
        var policy = new JapaneseSourceTextPolicy(() => true);

        Assert.False(policy.TryGetAlternateSource(new SourceClientLanguage("ja", "ja"), "pt-BR", out _));
    }

    /// <summary>
    ///     A Japanese target would translate Japanese into Japanese.
    /// </summary>
    [Fact]
    public void Policy_JapaneseTarget_DoesNotApply()
    {
        var policy = new JapaneseSourceTextPolicy(() => true);

        Assert.False(policy.TryGetAlternateSource(English, "ja", out _));
    }

    /// <summary>
    ///     The resolved Japanese source passes the translation service source
    ///     check on a non-Japanese client.
    /// </summary>
    [Fact]
    public void Policy_AlternateSource_IsKnownCapturedSourceLanguage()
    {
        var policy = new JapaneseSourceTextPolicy(() => true);
        Assert.True(policy.TryGetAlternateSource(English, "pt-BR", out var alternate));

        Assert.True(TranslationService.IsKnownCapturedSourceLanguage(alternate));
    }
}
