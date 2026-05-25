using CodexLens.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodexLens.Services;

public sealed class SessionReader
{
    private readonly CodexEventParser _parser;
    private readonly Dictionary<string, TailState> _tailStates = new(StringComparer.OrdinalIgnoreCase);

    public SessionReader(CodexEventParser parser)
    {
        _parser = parser;
    }

    public async Task<IReadOnlyList<DisplayEvent>> ReadAllAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var events = new List<DisplayEvent>();
        var lineNo = 0;

        await using var stream = OpenReadShared(filePath);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 64 * 1024, leaveOpen: true);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            lineNo++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            events.Add(_parser.ParseLine(line, lineNo));
        }

        var length = new FileInfo(filePath).Length;
        _tailStates[filePath] = new TailState(length, string.Empty, lineNo);
        return events;
    }

    public async Task<IReadOnlyList<DisplayEvent>> ReadAppendedAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var file = new FileInfo(filePath);
        if (!file.Exists) return [];

        if (!_tailStates.TryGetValue(filePath, out var state))
        {
            _tailStates[filePath] = new TailState(0, string.Empty, 0);
            state = _tailStates[filePath];
        }

        if (file.Length < state.Offset)
        {
            // File was rotated or truncated. Re-read from the beginning.
            state = new TailState(0, string.Empty, 0);
        }

        if (file.Length == state.Offset) return [];

        var bytesToRead = file.Length - state.Offset;
        var buffer = new byte[bytesToRead];

        await using (var stream = OpenReadShared(filePath))
        {
            stream.Seek(state.Offset, SeekOrigin.Begin);
            var read = 0;
            while (read < buffer.Length)
            {
                var n = await stream.ReadAsync(buffer.AsMemory(read, buffer.Length - read), cancellationToken).ConfigureAwait(false);
                if (n == 0) break;
                read += n;
            }
        }

        var chunk = Encoding.UTF8.GetString(buffer);
        var text = state.Pending + chunk;
        var lines = SplitCompleteLines(text, out var pending);
        var events = new List<DisplayEvent>();
        var lineNo = state.LineNumber;

        foreach (var line in lines)
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            events.Add(_parser.ParseLine(line, lineNo));
        }

        _tailStates[filePath] = new TailState(file.Length, pending, lineNo);
        return events;
    }

    public void Forget(string filePath) => _tailStates.Remove(filePath);

    private static FileStream OpenReadShared(string filePath)
    {
        return new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 64 * 1024,
            options: FileOptions.SequentialScan);
    }

    private static IReadOnlyList<string> SplitCompleteLines(string text, out string pending)
    {
        var lines = new List<string>();
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n') continue;
            var line = text[start..i].TrimEnd('\r');
            lines.Add(line);
            start = i + 1;
        }

        pending = start < text.Length ? text[start..] : string.Empty;
        return lines;
    }

    private sealed record TailState(long Offset, string Pending, int LineNumber);
}
