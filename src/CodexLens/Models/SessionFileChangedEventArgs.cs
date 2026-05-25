using System;

namespace CodexLens.Models;

public sealed class SessionFileChangedEventArgs : EventArgs
{
    public required string Path { get; init; }
}
