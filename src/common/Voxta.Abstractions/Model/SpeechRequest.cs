﻿namespace Voxta.Abstractions.Model;

public class SpeechRequest
{
    public required string Service { get; init; }
    public required string Text { get; init; }
    public required string Voice { get; init; }
    public required string ContentType { get; init; }
    public bool Reusable { get; init; }
    public string Culture { get; init; }
}