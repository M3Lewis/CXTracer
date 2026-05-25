using CodexLens.Models;
using System;
using System.IO;

namespace CodexLens.Services;

public sealed class SessionWatcher : IDisposable
{
    private FileSystemWatcher? _watcher;

    public event EventHandler<SessionFileChangedEventArgs>? SessionFileChanged;

    public void Start(string rootPath)
    {
        Stop();
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath)) return;

        _watcher = new FileSystemWatcher(rootPath, "*.jsonl")
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime
        };

        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Renamed += OnRenamed;
    }

    public void Stop()
    {
        if (_watcher is null) return;
        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnChanged;
        _watcher.Created -= OnChanged;
        _watcher.Renamed -= OnRenamed;
        _watcher.Dispose();
        _watcher = null;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (!e.FullPath.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase)) return;
        SessionFileChanged?.Invoke(this, new SessionFileChangedEventArgs { Path = e.FullPath });
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        if (!e.FullPath.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase)) return;
        SessionFileChanged?.Invoke(this, new SessionFileChangedEventArgs { Path = e.FullPath });
    }

    public void Dispose() => Stop();
}
