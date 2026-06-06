using System;

namespace CXTracer.Models;

public sealed class SessionFileChangedEventArgs : EventArgs
{
    public required string Path { get; init; }
}
