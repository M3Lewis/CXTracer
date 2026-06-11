using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using CXTracer.Services;

namespace CXTracer.Models;

public sealed partial class DisplayEvent
{
    private static string? ExtractImagePath(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var mdMatch = MarkdownImageRegex.Match(text);
        if (mdMatch.Success)
        {
            return mdMatch.Groups[1].Value.Trim();
        }

        var htmlMatch = HtmlImageRegex.Match(text);
        if (htmlMatch.Success)
        {
            return htmlMatch.Groups[1].Value.Trim();
        }

        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                trimmed.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                trimmed.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                trimmed.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
                trimmed.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            {
                if (!trimmed.Contains(' ') && (trimmed.Contains('/') || trimmed.Contains('\\') || trimmed.Contains('.')))
                {
                    return trimmed;
                }
            }
        }

        return null;
    }

    private static string? ExtractImagePathFromJson(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson)) return null;

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("payload", out var payload))
            {
                if (payload.TryGetProperty("local_images", out var localImages) && localImages.ValueKind == JsonValueKind.Array)
                {
                    foreach (var img in localImages.EnumerateArray())
                    {
                        if (img.ValueKind == JsonValueKind.String)
                        {
                            var val = img.GetString();
                            if (!string.IsNullOrWhiteSpace(val)) return val;
                        }
                    }
                }

                if (payload.TryGetProperty("images", out var images) && images.ValueKind == JsonValueKind.Array)
                {
                    foreach (var img in images.EnumerateArray())
                    {
                        if (img.ValueKind == JsonValueKind.String)
                        {
                            var val = img.GetString();
                            if (!string.IsNullOrWhiteSpace(val)) return val;
                        }
                    }
                }

                if (payload.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
                {
                    var imgUrl = FindImageInContentArray(content);
                    if (imgUrl != null) return imgUrl;
                }
            }

            return FindImageRecursively(root);
        }
        catch
        {
            return null;
        }
    }

    private static string? FindImageInContentArray(JsonElement contentArray)
    {
        foreach (var item in contentArray.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object)
            {
                if (item.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String)
                {
                    var typeVal = typeProp.GetString();
                    if (typeVal == "input_image" || typeVal == "image")
                    {
                        if (item.TryGetProperty("image_url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
                        {
                            var url = urlProp.GetString();
                            if (!string.IsNullOrWhiteSpace(url)) return url;
                        }
                    }
                }
            }
        }
        return null;
    }

    private static string? FindImageRecursively(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.Name.Equals("image_url", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var val = prop.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(val)) return val;
                    }
                    else if ((prop.Name.Equals("local_images", StringComparison.OrdinalIgnoreCase) || prop.Name.Equals("images", StringComparison.OrdinalIgnoreCase)) && prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var img in prop.Value.EnumerateArray())
                        {
                            if (img.ValueKind == JsonValueKind.String)
                            {
                                var val = img.GetString();
                                if (!string.IsNullOrWhiteSpace(val)) return val;
                            }
                        }
                    }
                    else
                    {
                        var result = FindImageRecursively(prop.Value);
                        if (result != null) return result;
                    }
                }
                break;
            case JsonValueKind.Array:
                foreach (var child in element.EnumerateArray())
                {
                    var result = FindImageRecursively(child);
                    if (result != null) return result;
                }
                break;
        }
        return null;
    }

    private static string ResolvePath(string rawPath, string? sessionFilePath)
    {
        if (string.IsNullOrWhiteSpace(rawPath)) return rawPath;

        if (rawPath.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) ||
            rawPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            rawPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return rawPath;
        }

        if (rawPath.StartsWith("~"))
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            rawPath = Path.Combine(userProfile, rawPath[1..].TrimStart('/', '\\'));
        }

        rawPath = rawPath.Replace('/', '\\');

        if (rawPath.StartsWith("\\mnt\\", StringComparison.OrdinalIgnoreCase))
        {
            var components = rawPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            if (components.Length >= 3 && components[0].Equals("mnt", StringComparison.OrdinalIgnoreCase))
            {
                var driveLetter = components[1].ToUpperInvariant();
                if (driveLetter.Length == 1 && driveLetter[0] >= 'A' && driveLetter[0] <= 'Z')
                {
                    var subPath = string.Join('\\', components.Skip(2));
                    rawPath = $"{driveLetter}:\\{subPath}";
                }
            }
        }

        if (rawPath.StartsWith('\\') && !rawPath.StartsWith("\\\\"))
        {
            if (!string.IsNullOrEmpty(sessionFilePath))
            {
                if (sessionFilePath.StartsWith("\\\\wsl$\\") || sessionFilePath.StartsWith("\\\\wsl.localhost\\"))
                {
                    var parts = sessionFilePath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var wslPrefix = $"\\\\{parts[0]}\\{parts[1]}";
                        rawPath = wslPrefix + rawPath;
                    }
                }
            }
        }

        if (!Path.IsPathRooted(rawPath) && !string.IsNullOrEmpty(sessionFilePath))
        {
            var sessionDir = Path.GetDirectoryName(sessionFilePath);
            if (!string.IsNullOrEmpty(sessionDir))
            {
                rawPath = Path.GetFullPath(Path.Combine(sessionDir, rawPath));
            }
        }

        // If the path is rooted but the file does not exist, try to locate it locally in the session directory or by searching upwards from the app's base directory
        if (Path.IsPathRooted(rawPath) && !File.Exists(rawPath))
        {
            var fileName = Path.GetFileName(rawPath);
            if (!string.IsNullOrEmpty(fileName))
            {
                if (!string.IsNullOrEmpty(sessionFilePath))
                {
                    var sessionDir = Path.GetDirectoryName(sessionFilePath);
                    if (!string.IsNullOrEmpty(sessionDir))
                    {
                        var fallbackPath = Path.Combine(sessionDir, fileName);
                        if (File.Exists(fallbackPath)) return fallbackPath;
                    }
                }

                var currentDir = AppDomain.CurrentDomain.BaseDirectory;
                while (!string.IsNullOrEmpty(currentDir))
                {
                    var candidate = Path.Combine(currentDir, fileName);
                    if (File.Exists(candidate)) return candidate;

                    var parent = Path.GetDirectoryName(currentDir);
                    if (parent == currentDir) break;
                    currentDir = parent;
                }
            }
        }

        return rawPath;
    }

    public static void ResolvePlaceholderImages(IEnumerable<DisplayEvent> events)
    {
        var placeholderMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var localToPlaceholderMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // First pass: gather mappings
        foreach (var ev in events)
        {
            if (string.IsNullOrWhiteSpace(ev.RawJson)) continue;

            try
            {
                using var doc = JsonDocument.Parse(ev.RawJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("payload", out var payload))
                {
                    if (payload.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
                    {
                        string? currentPlaceholder = null;
                        foreach (var item in content.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.Object)
                            {
                                if (item.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String)
                                {
                                    var typeVal = typeProp.GetString();
                                    if (typeVal == "input_text")
                                    {
                                        if (item.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
                                        {
                                            var textVal = textProp.GetString();
                                            if (textVal != null)
                                            {
                                                var match = Regex.Match(textVal, @"<image\s+name=([^>]+)>");
                                                if (match.Success)
                                                {
                                                    currentPlaceholder = match.Groups[1].Value.Trim();
                                                }
                                            }
                                        }
                                    }
                                    else if ((typeVal == "input_image" || typeVal == "image") && currentPlaceholder != null)
                                    {
                                        if (item.TryGetProperty("image_url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
                                        {
                                            var urlVal = urlProp.GetString();
                                            if (!string.IsNullOrWhiteSpace(urlVal))
                                            {
                                                placeholderMap[currentPlaceholder] = urlVal;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (payload.TryGetProperty("local_images", out var localImages) && localImages.ValueKind == JsonValueKind.Array &&
                        payload.TryGetProperty("text_elements", out var textElements) && textElements.ValueKind == JsonValueKind.Array)
                    {
                        var localList = new List<string>();
                        foreach (var img in localImages.EnumerateArray())
                        {
                            if (img.ValueKind == JsonValueKind.String)
                            {
                                var val = img.GetString();
                                if (!string.IsNullOrWhiteSpace(val)) localList.Add(val);
                            }
                        }

                        var placeholderList = new List<string>();
                        foreach (var elem in textElements.EnumerateArray())
                        {
                            if (elem.ValueKind == JsonValueKind.Object)
                            {
                                if (elem.TryGetProperty("placeholder", out var placeholderProp) && placeholderProp.ValueKind == JsonValueKind.String)
                                {
                                    var val = placeholderProp.GetString();
                                    if (!string.IsNullOrWhiteSpace(val)) placeholderList.Add(val);
                                }
                            }
                        }

                        for (int i = 0; i < Math.Min(localList.Count, placeholderList.Count); i++)
                        {
                            localToPlaceholderMap[localList[i]] = placeholderList[i];
                        }
                    }
                }
            }
            catch
            {
                // Ignore parsing errors for individual lines
            }
        }

        // Second pass: resolve missing or placeholder images
        foreach (var ev in events)
        {
            if (!string.IsNullOrEmpty(ev.ImagePath) && 
                !ev.ImagePath.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) &&
                !ev.ImagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !ev.ImagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (!File.Exists(ev.ImagePath))
                {
                    if (localToPlaceholderMap.TryGetValue(ev.ImagePath, out var placeholder))
                    {
                        if (placeholderMap.TryGetValue(placeholder, out var base64Data))
                        {
                            ev.ImagePath = base64Data;
                            continue;
                        }
                    }
                    
                    var rawImgName = Path.GetFileName(ev.ImagePath);
                    foreach (var kvp in localToPlaceholderMap)
                    {
                        if (Path.GetFileName(kvp.Key).Equals(rawImgName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (placeholderMap.TryGetValue(kvp.Value, out var base64Data))
                            {
                                ev.ImagePath = base64Data;
                                break;
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(ev.ImagePath) && !string.IsNullOrEmpty(ev.Text))
            {
                foreach (var kvp in placeholderMap)
                {
                    if (ev.Text.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        ev.ImagePath = kvp.Value;
                        break;
                    }
                }
            }
        }
    }
}
