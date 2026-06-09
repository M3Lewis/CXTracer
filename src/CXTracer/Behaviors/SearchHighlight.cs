using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace CXTracer.Behaviors;

public static class SearchHighlight
{
    public static readonly AttachedProperty<string?> TextProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, string?>("Text", typeof(SearchHighlight));

    public static readonly AttachedProperty<string?> QueryProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, string?>("Query", typeof(SearchHighlight));

    public static readonly AttachedProperty<IBrush?> HighlightBrushProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, IBrush?>("HighlightBrush", typeof(SearchHighlight));

    static SearchHighlight()
    {
        TextProperty.Changed.AddClassHandler<TextBlock>(OnPropertyChanged);
        QueryProperty.Changed.AddClassHandler<TextBlock>(OnPropertyChanged);
        HighlightBrushProperty.Changed.AddClassHandler<TextBlock>(OnPropertyChanged);
    }

    public static string? GetText(TextBlock element) => element.GetValue(TextProperty);
    public static void SetText(TextBlock element, string? value) => element.SetValue(TextProperty, value);

    public static string? GetQuery(TextBlock element) => element.GetValue(QueryProperty);
    public static void SetQuery(TextBlock element, string? value) => element.SetValue(QueryProperty, value);

    public static IBrush? GetHighlightBrush(TextBlock element) => element.GetValue(HighlightBrushProperty);
    public static void SetHighlightBrush(TextBlock element, IBrush? value) => element.SetValue(HighlightBrushProperty, value);

    private static void OnPropertyChanged(TextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        UpdateHighlight(textBlock);
    }

    private static void UpdateHighlight(TextBlock textBlock)
    {
        var text = GetText(textBlock);
        var query = GetQuery(textBlock);

        if (string.IsNullOrEmpty(text))
        {
            textBlock.Inlines?.Clear();
            textBlock.Text = string.Empty;
            return;
        }

        if (string.IsNullOrEmpty(query))
        {
            // Fast Path: No query, reset to plain text.
            textBlock.Inlines?.Clear();
            textBlock.Text = text;
            return;
        }

        int index = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            // Fast Path: Query not matched, reset to plain text.
            textBlock.Inlines?.Clear();
            textBlock.Text = text;
            return;
        }

        // Highlight Path: Match found
        var inlines = textBlock.Inlines;
        if (inlines == null)
        {
            inlines = new InlineCollection();
            textBlock.Inlines = inlines;
        }
        else
        {
            inlines.Clear();
        }

        // Get the highlight brush (try DynamicResource/Resource or fallback to Yellow)
        var highlightBrush = GetHighlightBrush(textBlock);
        if (highlightBrush == null)
        {
            if (textBlock.TryFindResource("SystemAccentColorLight2", out var resource) && resource is IBrush brush)
            {
                highlightBrush = brush;
            }
            else
            {
                highlightBrush = Brushes.Yellow;
            }
        }

        int queryLength = query.Length;
        int lastIndex = 0;

        while (index >= 0)
        {
            // Non-matching prefix
            if (index > lastIndex)
            {
                inlines.Add(new Run
                {
                    Text = text.Substring(lastIndex, index - lastIndex)
                });
            }

            // Matching segment
            inlines.Add(new Run
            {
                Text = text.Substring(index, queryLength),
                Background = highlightBrush
            });

            lastIndex = index + queryLength;
            index = text.IndexOf(query, lastIndex, StringComparison.OrdinalIgnoreCase);
        }

        // Remaining suffix
        if (lastIndex < text.Length)
        {
            inlines.Add(new Run
            {
                Text = text.Substring(lastIndex)
            });
        }
    }
}
