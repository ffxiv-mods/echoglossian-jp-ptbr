// <copyright file="AlternateSourceText.cs" company="lokinmodar">
// Copyright (c) lokinmodar. All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International Public License license.
// </copyright>

namespace Echoglossian;

/// <summary>
///     Represents a source text read from game data in another language than
///     the client, to be sent to the translation engine instead of the
///     client-language text.
/// </summary>
/// <param name="Text">The alternate source text.</param>
/// <param name="Source">The alternate source language.</param>
public readonly record struct AlternateSourceText(
    string Text,
    SourceClientLanguage Source);
