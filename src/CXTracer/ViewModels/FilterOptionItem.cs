using CommunityToolkit.Mvvm.ComponentModel;

namespace CXTracer.ViewModels;

public sealed partial class FilterOptionItem : ObservableObject
{
    public string Key { get; }

    [ObservableProperty]
    private string _displayName;

    public FilterOptionItem(string key, string displayName)
    {
        Key = key;
        _displayName = displayName;
    }

    public override string ToString() => DisplayName;
}
