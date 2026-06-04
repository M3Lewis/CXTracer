using CodexLens.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodexLens.Services;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(ShortcutGesture))]
[JsonSerializable(typeof(JsonElement))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
