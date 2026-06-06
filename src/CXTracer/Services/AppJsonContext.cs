using System.Text.Json;
using System.Text.Json.Serialization;
using CXTracer.Models;

namespace CXTracer.Services;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(ShortcutGesture))]
[JsonSerializable(typeof(JsonElement))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
