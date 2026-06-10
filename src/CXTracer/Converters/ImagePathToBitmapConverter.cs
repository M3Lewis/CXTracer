using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace CXTracer.Converters;

public sealed class ImagePathToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            if (path.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                var commaIndex = path.IndexOf(',');
                if (commaIndex >= 0)
                {
                    var base64Data = path[(commaIndex + 1)..];
                    var bytes = System.Convert.FromBase64String(base64Data);
                    using var stream = new MemoryStream(bytes);
                    return new Bitmap(stream);
                }
            }

            if (File.Exists(path))
            {
                using var stream = File.OpenRead(path);
                return new Bitmap(stream);
            }
        }
        catch
        {
            // Fail silently to prevent application crashes when images are corrupt or locked
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
