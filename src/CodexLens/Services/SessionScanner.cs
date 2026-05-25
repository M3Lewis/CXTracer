using CodexLens.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CodexLens.Services;

public sealed class SessionScanner
{
    private readonly CodexEventParser _parser;

    public SessionScanner(CodexEventParser parser)
    {
        _parser = parser;
    }

    public IReadOnlyList<SessionInfo> ScanLight(string rootPath, int maxResults = 300)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return [];
        }

        return Directory.EnumerateFiles(rootPath, "*.jsonl", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .Where(file => file.Exists)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Take(maxResults)
            .Select(SessionInfo.FromFile)
            .ToList();
    }

    public IReadOnlyList<SessionInfo> Scan(string rootPath, int maxResults = 300)
    {
        var sessions = ScanLight(rootPath, maxResults).ToList();

        foreach (var session in sessions)
        {
            Enrich(session);
        }

        return sessions;
    }

    public SessionInfo? TryGetSession(string filePath)
    {
        var file = new FileInfo(filePath);
        return IsSessionFile(file) ? SessionInfo.FromFile(file) : null;
    }

    public SessionInfo? TryGetSessionSummary(string filePath)
    {
        var session = TryGetSession(filePath);
        if (session is null)
        {
            return null;
        }

        Enrich(session);
        return session;
    }

    public void Enrich(SessionInfo session)
    {
        var file = new FileInfo(session.FilePath);

        if (!IsSessionFile(file))
        {
            return;
        }

        session.LastWriteTime = file.LastWriteTime;
        session.Length = file.Length;

        var sample = ReadHeadLines(file.FullName, 200).ToArray();
        session.FirstPrompt = _parser.ExtractFirstPrompt(sample);
        session.ProjectHint = _parser.ExtractProjectHint(sample, file.FullName);
        session.EventCount = EstimateLineCount(file.FullName, maxLines: 10000);
    }

    public static string DefaultRootPath()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(profile, ".codex", "sessions");
    }

    private static bool IsSessionFile(FileInfo file)
    {
        return file.Exists && file.Extension.Equals(".jsonl", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> ReadHeadLines(string filePath, int maxLines)
    {
        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            64 * 1024,
            FileOptions.SequentialScan);

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var count = 0;

        while (!reader.EndOfStream && count < maxLines)
        {
            var line = reader.ReadLine();
            count++;

            if (!string.IsNullOrWhiteSpace(line))
            {
                yield return line;
            }
        }
    }

    private static int EstimateLineCount(string filePath, int maxLines)
    {
        try
        {
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                64 * 1024,
                FileOptions.SequentialScan);

            var buffer = new byte[64 * 1024];
            var count = 0;
            int read;

            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (var i = 0; i < read; i++)
                {
                    if (buffer[i] == (byte)'\n')
                    {
                        count++;
                    }

                    if (count >= maxLines)
                    {
                        return count;
                    }
                }
            }

            return count;
        }
        catch
        {
            return 0;
        }
    }
}
