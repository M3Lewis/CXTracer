using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CXTracer.Models;

namespace CXTracer.Services;

public sealed class SessionReader
{
    private readonly CodexEventParser _parser;
    private readonly Dictionary<string, TailState> _tailStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SemaphoreSlim> _tailLocks = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    public SessionReader(CodexEventParser parser)
    {
        _parser = parser;
    }

    public async Task<IReadOnlyList<DisplayEvent>> ReadAllAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var gate = GetGate(filePath);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var events = new List<DisplayEvent>();
            var lineNo = 0;

            await using var stream = SessionFileAccess.OpenReadShared(filePath);
            using var reader = new StreamReader(
                stream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: SessionFileAccess.BufferSize,
                leaveOpen: true);

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                lineNo++;

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                events.Add(_parser.ParseLine(line, lineNo, filePath));
            }

            // 关键修复：
            // 这里不要用 new FileInfo(filePath).Length。
            // 因为文件可能在 ReadAll 结束后又增长。
            // offset 应该记录我们这个 stream 实际读到的位置。
            var actualReadOffset = stream.Position;

            lock (_sync)
            {
                _tailStates[filePath] = new TailState(actualReadOffset, string.Empty, lineNo);
            }

            return events;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<DisplayEvent>> ReadAppendedAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var gate = GetGate(filePath);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await ReadAppendedCoreAsync(filePath, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<IReadOnlyList<DisplayEvent>> ReadAppendedCoreAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var file = new FileInfo(filePath);

        if (!file.Exists)
        {
            return [];
        }

        TailState state;

        lock (_sync)
        {
            if (!_tailStates.TryGetValue(filePath, out state!))
            {
                state = new TailState(0, string.Empty, 0);
                _tailStates[filePath] = state;
            }
        }

        var snapshotLength = file.Length;

        if (snapshotLength < state.Offset)
        {
            // 文件被截断或轮换，重新从头读。
            state = new TailState(0, string.Empty, 0);
        }

        if (snapshotLength == state.Offset)
        {
            return [];
        }

        var bytesToReadLong = snapshotLength - state.Offset;

        if (bytesToReadLong <= 0)
        {
            return [];
        }

        if (bytesToReadLong > int.MaxValue)
        {
            // 极端情况：session 文件突然暴涨。
            // 为了避免一次性分配超大数组，重置为从头读取。
            state = new TailState(0, string.Empty, 0);
            bytesToReadLong = snapshotLength;
        }

        var bytesToRead = (int)bytesToReadLong;
        var buffer = new byte[bytesToRead];
        var totalRead = 0;

        await using (var stream = SessionFileAccess.OpenReadShared(filePath))
        {
            stream.Seek(state.Offset, SeekOrigin.Begin);

            while (totalRead < buffer.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var n = await stream.ReadAsync(
                    buffer.AsMemory(totalRead, buffer.Length - totalRead),
                    cancellationToken).ConfigureAwait(false);

                if (n == 0)
                {
                    break;
                }

                totalRead += n;
            }
        }

        if (totalRead <= 0)
        {
            return [];
        }

        var actualNewOffset = state.Offset + totalRead;
        var chunk = Encoding.UTF8.GetString(buffer, 0, totalRead);
        var text = state.Pending + chunk;
        var lines = SplitCompleteLines(text, out var pending);

        var events = new List<DisplayEvent>();
        var lineNo = state.LineNumber;

        foreach (var line in lines)
        {
            lineNo++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            events.Add(_parser.ParseLine(line, lineNo, filePath));
        }

        lock (_sync)
        {
            _tailStates[filePath] = new TailState(actualNewOffset, pending, lineNo);
        }

        return events;
    }

    public void Forget(string filePath)
    {
        lock (_sync)
        {
            _tailStates.Remove(filePath);
        }
    }

    private SemaphoreSlim GetGate(string filePath)
    {
        lock (_sync)
        {
            if (!_tailLocks.TryGetValue(filePath, out var gate))
            {
                gate = new SemaphoreSlim(1, 1);
                _tailLocks[filePath] = gate;
            }

            return gate;
        }
    }

    private static IReadOnlyList<string> SplitCompleteLines(string text, out string pending)
    {
        var lines = new List<string>();
        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n')
            {
                continue;
            }

            var line = text[start..i].TrimEnd('\r');
            lines.Add(line);
            start = i + 1;
        }

        pending = start < text.Length ? text[start..] : string.Empty;
        return lines;
    }

    private sealed record TailState(long Offset, string Pending, int LineNumber);
}
