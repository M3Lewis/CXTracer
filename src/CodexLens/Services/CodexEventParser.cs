using CodexLens.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CodexLens.Services;

public sealed class CodexEventParser
{
    private static readonly string[] TypeKeys = ["type", "event", "kind", "name"];
    private static readonly string[] RoleKeys = ["role", "author"];
    private static readonly string[] ChatTextKeys = ["text", "content", "message", "summary", "body", "answer", "markdown"];
    private static readonly string[] CommandKeys = ["command", "cmd", "shell", "argv"];
    private static readonly string[] OutputKeys = ["stdout", "stderr", "output", "exit_code", "exitcode", "status", "duration_ms"];
    private static readonly string[] DiffKeys = ["diff", "patch", "apply_patch"];
    private static readonly string[] PathKeys = ["cwd", "workdir", "working_directory", "project", "repo", "root", "path"];

    public DisplayEvent ParseLine(string line, int lineNumber)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var flat = new List<FlatValue>();
            Flatten(doc.RootElement, "", flat);

            var kind = Classify(flat, line);
            var pane = ToPane(kind);
            var title = BuildTitle(kind, flat);
            var text = BuildText(kind, flat, line);
            var timestamp = FindTimestamp(flat);

            return new DisplayEvent
            {
                Id = $"{lineNumber}:{HashCode.Combine(lineNumber, line.Length)}",
                LineNumber = lineNumber,
                Pane = pane,
                Kind = kind,
                Title = title,
                Text = text,
                RawJson = line,
                Timestamp = timestamp
            };
        }
        catch (Exception ex)
        {
            return new DisplayEvent
            {
                Id = $"{lineNumber}:parse-error",
                LineNumber = lineNumber,
                Pane = EventPane.Raw,
                Kind = EventKind.Raw,
                Title = "Parse error",
                Text = ex.Message,
                RawJson = line,
                Timestamp = null
            };
        }
    }

    public string ExtractFirstPrompt(IEnumerable<string> lines, int maxLines = 200)
    {
        var lineNo = 0;
        foreach (var line in lines.Take(maxLines))
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            var evt = ParseLine(line, lineNo);
            if (evt.Kind == EventKind.User && !string.IsNullOrWhiteSpace(evt.Text))
                return OneLine(evt.Text, 120);
        }

        return string.Empty;
    }

    public string ExtractProjectHint(IEnumerable<string> lines, string filePath, int maxLines = 200)
    {
        var candidates = new List<string>();

        foreach (var line in lines.Take(maxLines))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var flat = new List<FlatValue>();
                Flatten(doc.RootElement, "", flat);

                foreach (var value in flat.Where(v => IsAnyKey(v.Key, PathKeys)).Select(v => v.Value))
                {
                    if (LooksLikePath(value))
                    {
                        candidates.Add(value);
                    }
                }
            }
            catch
            {
                // Ignore malformed partial JSONL lines in active sessions.
            }
        }

        var best = candidates
            .Select(NormalizePathHint)
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        if (!string.IsNullOrWhiteSpace(best)) return best;

        var parent = System.IO.Path.GetDirectoryName(filePath);
        return string.IsNullOrWhiteSpace(parent) ? string.Empty : System.IO.Path.GetFileName(parent);
    }

    private static EventKind Classify(List<FlatValue> flat, string raw)
    {
        var roles = ValuesByKeys(flat, RoleKeys).Select(v => v.ToLowerInvariant()).ToArray();
        var types = ValuesByKeys(flat, TypeKeys).Select(v => v.ToLowerInvariant()).ToArray();
        var keys = string.Join(' ', flat.Select(v => v.Key)).ToLowerInvariant();
        var values = string.Join(' ', flat.Select(v => v.Value.Length > 300 ? v.Value[..300] : v.Value)).ToLowerInvariant();
        var hay = keys + " " + values;

        if (ContainsAny(hay, "panic", "traceback", "exception", "failed", "failure") && ContainsAny(keys, "stderr", "exit_code", "error"))
            return EventKind.Error;

        if (ContainsAny(hay, "apply_patch", "file_change", "patch", "diff --git") || flat.Any(v => IsAnyKey(v.Key, DiffKeys)))
            return EventKind.Diff;

        if (ContainsAny(hay, "exec_command", "command_execution", "shell_command", "terminal", "stdout", "stderr", "exit_code") || flat.Any(v => IsAnyKey(v.Key, CommandKeys)))
            return flat.Any(v => IsAnyKey(v.Key, OutputKeys)) ? EventKind.CommandOutput : EventKind.Command;

        if (ContainsAny(hay, "tool_call", "tool_result", "function_call", "mcp", "web_search", "read_file", "write_file"))
            return EventKind.Tool;

        // Internal reasoning/thinking events can be very long. Detect them before the
        // generic assistant/message checks so they do not leak into the Conversation pane.
        if (types.Any(t => t.Contains("reasoning") || t.Contains("thinking") || t.Contains("thought"))
            || ContainsAny(keys, "reasoning", "thinking", "thought"))
            return EventKind.Reasoning;

        // Plan/update_plan is useful metadata, but it is not a user/assistant chat message.
        // Keep it out of the left pane by classifying it separately. Do not inspect free-text
        // values here, otherwise normal assistant replies mentioning a "plan" would be hidden.
        if (types.Any(t => t.Contains("update_plan") || t.Contains("plan_update") || t == "plan")
            || ContainsAny(keys, "update_plan", "plan_update"))
            return EventKind.Plan;

        if (ContainsAny(hay, "final_answer", "final_response") || types.Any(t => t.Contains("final")))
            return EventKind.Final;

        if (roles.Any(r => r == "user" || r == "human") || types.Any(t => t.Contains("user_message") || t == "user"))
            return EventKind.User;

        if (roles.Any(r => r == "assistant" || r == "agent") || types.Any(t => t.Contains("agent_message") || t.Contains("assistant_message") || t == "assistant"))
            return EventKind.Assistant;

        return EventKind.Raw;
    }

    private static EventPane ToPane(EventKind kind) => kind switch
    {
        // The left pane is intentionally chat-only: user prompts and Codex's human-readable answer.
        // Reasoning and plan events stay hidden in Raw unless the user enables Raw events.
        EventKind.User or EventKind.Assistant or EventKind.Final => EventPane.Conversation,
        EventKind.Command or EventKind.CommandOutput or EventKind.Diff or EventKind.Tool or EventKind.Error => EventPane.Execution,
        _ => EventPane.Raw
    };

    private static string BuildTitle(EventKind kind, List<FlatValue> flat)
    {
        return kind switch
        {
            EventKind.User => "User",
            EventKind.Assistant => "Codex",
            EventKind.Plan => "Plan",
            EventKind.Final => "Codex",
            EventKind.Reasoning => "Reasoning",
            EventKind.Command => OneLine(FirstValueByKeys(flat, CommandKeys) ?? "Command", 96),
            EventKind.CommandOutput => "Command output",
            EventKind.Diff => "File changes / diff",
            EventKind.Tool => OneLine(FirstValueByKeys(flat, ["tool", "name", "server", "function"]) ?? "Tool call", 96),
            EventKind.Error => "Error",
            _ => "Raw event"
        };
    }

    private static string BuildText(EventKind kind, List<FlatValue> flat, string raw)
    {
        if (kind == EventKind.Raw)
            return PrettyCompact(raw);

        if (kind == EventKind.Command)
        {
            var command = FirstValueByKeys(flat, CommandKeys) ?? FirstValueContaining(flat, "exec_command") ?? string.Empty;
            var cwd = FirstValueByKeys(flat, ["cwd", "workdir", "working_directory"]);
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(cwd)) sb.AppendLine($"cwd: {cwd}");
            sb.AppendLine(string.IsNullOrWhiteSpace(command) ? PrettyCompact(raw) : command);
            return sb.ToString().Trim();
        }

        if (kind == EventKind.CommandOutput || kind == EventKind.Error)
        {
            var sb = new StringBuilder();
            AppendIfFound(sb, "stdout", FirstValueByKeys(flat, ["stdout"]));
            AppendIfFound(sb, "stderr", FirstValueByKeys(flat, ["stderr", "error"]));
            AppendIfFound(sb, "exit", FirstValueByKeys(flat, ["exit_code", "exitcode", "status"]));
            AppendIfFound(sb, "output", FirstValueByKeys(flat, ["output"]));
            if (sb.Length == 0) sb.Append(PrettyCompact(raw));
            return sb.ToString().Trim();
        }

        if (kind == EventKind.Diff)
        {
            var diff = FirstValueByKeys(flat, DiffKeys) ?? FirstValueContaining(flat, "diff --git");
            return string.IsNullOrWhiteSpace(diff) ? PrettyCompact(raw) : diff.Trim();
        }

        if (kind == EventKind.Tool)
        {
            var name = FirstValueByKeys(flat, ["tool", "name", "server", "function"]);
            var input = FirstValueByKeys(flat, ["input", "arguments", "args"]);
            var output = FirstValueByKeys(flat, ["result", "output", "content"]);
            var sb = new StringBuilder();
            AppendIfFound(sb, "tool", name);
            AppendIfFound(sb, "input", input);
            AppendIfFound(sb, "result", output);
            if (sb.Length == 0) sb.Append(PrettyCompact(raw));
            return sb.ToString().Trim();
        }

        var candidates = ValuesByKeys(flat, ChatTextKeys)
            .Where(v => !LooksLikeTypeValue(v))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct()
            .ToList();

        if (candidates.Count == 0)
        {
            var fallback = ValuesByKeys(flat, ["value", "data"])
                .FirstOrDefault(v => v.Length > 2 && !LooksLikeTypeValue(v));
            if (!string.IsNullOrWhiteSpace(fallback)) candidates.Add(fallback);
        }

        if (candidates.Count == 0) return PrettyCompact(raw);

        var joined = string.Join(Environment.NewLine + Environment.NewLine, candidates);
        return joined.Trim();
    }

    private static void AppendIfFound(StringBuilder sb, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (sb.Length > 0) sb.AppendLine();
        sb.AppendLine($"[{label}]");
        sb.AppendLine(value.Trim());
    }

    private static string? FirstValueByKeys(List<FlatValue> flat, IEnumerable<string> keys)
    {
        return ValuesByKeys(flat, keys).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    }

    private static string? FirstValueContaining(List<FlatValue> flat, string needle)
    {
        return flat.Select(v => v.Value).FirstOrDefault(v => v.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> ValuesByKeys(List<FlatValue> flat, IEnumerable<string> keys)
    {
        foreach (var value in flat)
        {
            if (IsAnyKey(value.Key, keys)) yield return value.Value;
        }
    }

    private static DateTimeOffset? FindTimestamp(List<FlatValue> flat)
    {
        foreach (var value in flat.Where(v => IsAnyKey(v.Key, ["timestamp", "time", "created_at", "createdAt", "ts"])).Select(v => v.Value))
        {
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dto))
                return dto;
            if (long.TryParse(value, out var unix))
            {
                try
                {
                    return unix > 9_999_999_999 ? DateTimeOffset.FromUnixTimeMilliseconds(unix) : DateTimeOffset.FromUnixTimeSeconds(unix);
                }
                catch { }
            }
        }

        return null;
    }

    private static void Flatten(JsonElement element, string path, List<FlatValue> values)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                    Flatten(prop.Value, string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}", values);
                break;
            case JsonValueKind.Array:
                var i = 0;
                foreach (var child in element.EnumerateArray())
                    Flatten(child, $"{path}[{i++}]", values);
                break;
            case JsonValueKind.String:
                values.Add(new FlatValue(LastKey(path), element.GetString() ?? string.Empty, path));
                break;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                values.Add(new FlatValue(LastKey(path), element.ToString(), path));
                break;
        }
    }

    private static string LastKey(string path)
    {
        var clean = Regex.Replace(path, "\\[\\d+\\]", string.Empty);
        var idx = clean.LastIndexOf('.');
        return idx >= 0 ? clean[(idx + 1)..] : clean;
    }

    private static bool IsAnyKey(string key, IEnumerable<string> candidates)
    {
        foreach (var c in candidates)
        {
            if (key.Equals(c, StringComparison.OrdinalIgnoreCase)) return true;
            if (key.Contains(c, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static bool ContainsAny(string haystack, params string[] needles)
    {
        return needles.Any(n => haystack.Contains(n, StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksLikeTypeValue(string value)
    {
        if (value.Length > 80) return false;
        var v = value.Trim().ToLowerInvariant();
        return v is "message" or "response_item" or "event_msg" or "assistant" or "user" or "system" or "input_text" or "output_text" or "text";
    }

    private static bool LooksLikePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 3) return false;
        return value.Contains(@"\") || value.Contains('/') || Regex.IsMatch(value, "^[A-Za-z]:");
    }

    private static string NormalizePathHint(string path)
    {
        path = path.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        var parts = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return string.Empty;
        return parts.Last();
    }

    private static string OneLine(string value, int max)
    {
        var one = Regex.Replace(value.Trim(), "\\s+", " ");
        return one.Length <= max ? one : one[..max] + "…";
    }

    private static string PrettyCompact(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return raw;
        }
    }

    private readonly record struct FlatValue(string Key, string Value, string Path);
}
