// <copyright file="JapaneseSourceTextWiring.cs" company="lokinmodar">
// Copyright (c) lokinmodar. All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International Public License license.
// </copyright>

namespace Echoglossian;

/// <summary>
///     Plugin-owned wiring that hands the Japanese source-text option to the
///     addon handlers and prefetch runtimes.
/// </summary>
public partial class Echoglossian
{
  private ISourceTextLanguagePolicy? sourceTextLanguagePolicy;
  private IJapaneseQuestSourceTextProvider? japaneseQuestSourceText;

  /// <summary>
  ///     Gets the policy that decides when the Japanese original is preferred.
  /// </summary>
  internal ISourceTextLanguagePolicy SourceTextLanguagePolicy =>
      this.sourceTextLanguagePolicy ??= new JapaneseSourceTextPolicy(
          () => this.configuration.UseJapaneseSourceText);

  /// <summary>
  ///     Gets the provider that reads Japanese quest texts from game data.
  /// </summary>
  internal IJapaneseQuestSourceTextProvider JapaneseQuestSourceText =>
      this.japaneseQuestSourceText ??= new JapaneseQuestSourceTextProvider(
          new DialogueInterlocutorMetadataResolver(this));

  /// <summary>
  ///     Resolves the Japanese original of one visible quest dialogue line
  ///     when the option applies to the operation.
  /// </summary>
  /// <param name="originalText">The visible client-language dialogue line.</param>
  /// <param name="clientSource">The captured client source language.</param>
  /// <param name="targetLanguageCode">The operation target language.</param>
  /// <param name="cancellationToken">The handler-owned cancellation token.</param>
  /// <returns>The Japanese source text, or <see langword="null" />.</returns>
  private async Task<AlternateSourceText?> ResolveJapaneseDialogueSourceTextAsync(
      string originalText,
      SourceClientLanguage clientSource,
      string targetLanguageCode,
      CancellationToken cancellationToken)
  {
    if (!this.SourceTextLanguagePolicy.TryGetAlternateSource(
            clientSource,
            targetLanguageCode,
            out var japaneseSource))
    {
      return null;
    }

    var japaneseText = await this.JapaneseQuestSourceText
        .TryResolveQuestDialogueAsync(originalText, cancellationToken)
        .ConfigureAwait(false);
    return string.IsNullOrWhiteSpace(japaneseText)
        ? null
        : new AlternateSourceText(japaneseText, japaneseSource);
  }
}
