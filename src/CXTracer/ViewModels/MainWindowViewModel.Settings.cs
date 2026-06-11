using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using CXTracer.Models;
using CXTracer.Services;

namespace CXTracer.ViewModels;

public sealed partial class MainWindowViewModel
{
    private void LoadSettings()
    {
        try
        {
            var settings = _settingsService.Load();
            _isLoadingSettings = true;
            IsSynchronizedNavigationEnabled = settings.IsSynchronizedNavigationEnabled;
            _syncNavigationShortcut = settings.SyncNavigationShortcut?.IsValid == true
                ? settings.SyncNavigationShortcut
                : null;
            MinimizeToTray = settings.MinimizeToTray;
            CloseToTray = settings.CloseToTray;
            ExpandAllEventsByDefault = settings.ExpandAllEventsByDefault;

            var lang = settings.Language;
            if (string.IsNullOrWhiteSpace(lang))
            {
                lang = CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                    ? "zh"
                    : "en";
            }
            _currentLanguage = lang;
            _isLoadingSettings = false;

            OnPropertyChanged(nameof(SyncNavigationShortcutText));
            OnPropertyChanged(nameof(SyncShortcutEditorText));
            OnPropertyChanged(nameof(CurrentLanguage));
            OnPropertyChanged(nameof(ExpandAllEventsByDefault));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            _isLoadingSettings = false;
            StatusMessage = LF("StatusSettingsLoadFailed", "Settings load failed: {0}", ex.Message);
        }
    }

    private void SaveSettings()
    {
        try
        {
            _settingsService.Save(new AppSettings
            {
                IsSynchronizedNavigationEnabled = IsSynchronizedNavigationEnabled,
                SyncNavigationShortcut = _syncNavigationShortcut,
                Language = CurrentLanguage,
                MinimizeToTray = MinimizeToTray,
                CloseToTray = CloseToTray,
                ExpandAllEventsByDefault = ExpandAllEventsByDefault
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            StatusMessage = LF("StatusSettingsSaveFailed", "Settings save failed: {0}", ex.Message);
        }
    }

    // ── Localization helpers ──

    public void ApplyLanguage(string lang)
    {
        var app = Application.Current;
        if (app is null) return;

        var merged = app.Resources.MergedDictionaries;

        // Cache the pre-instantiated resource dictionaries from App.axaml on first run
        if (_enUsDictionary is null || _zhCnDictionary is null)
        {
            foreach (var provider in merged)
            {
                if (provider is IResourceDictionary dict)
                {
                    if (dict.TryGetResource("LocalizationLanguage", null, out var langVal) && langVal is string langStr)
                    {
                        if (langStr == "en")
                        {
                            _enUsDictionary = dict;
                        }
                        else if (langStr == "zh")
                        {
                            _zhCnDictionary = dict;
                        }
                    }
                }
            }
        }

        // Remove both from MergedDictionaries to ensure a clean state
        if (_enUsDictionary is not null) merged.Remove(_enUsDictionary);
        if (_zhCnDictionary is not null) merged.Remove(_zhCnDictionary);

        // Add the correct target dictionary
        var target = lang == "zh" ? _zhCnDictionary : _enUsDictionary;
        if (target is not null)
        {
            merged.Add(target);
        }

        // Refresh filter option display names
        foreach (var opt in FilterOptions)
        {
            if (FilterKeyToResourceKey.TryGetValue(opt.Key, out var resKey))
            {
                opt.DisplayName = L(resKey, opt.Key);
            }
        }

        // Refresh computed text properties
        OnPropertyChanged(nameof(SessionCountText));
        OnPropertyChanged(nameof(EventCountText));
        OnPropertyChanged(nameof(SelectedSessionPath));
        OnPropertyChanged(nameof(SyncNavigationShortcutText));
        OnPropertyChanged(nameof(SyncShortcutEditorText));
        OnPropertyChanged(nameof(ViewerToggleText));
    }

    /// <summary>Look up a localized string resource by key, with a fallback default.</summary>
    internal string L(string key, string fallback)
    {
        if (Application.Current?.Resources.TryGetResource(key, null, out var val) == true && val is string s)
        {
            return s;
        }
        return fallback;
    }

    /// <summary>Look up a localized format-string resource by key, then apply string.Format.</summary>
    internal string LF(string key, string fallbackFormat, params object[] args)
    {
        var fmt = L(key, fallbackFormat);
        return string.Format(fmt, args);
    }
}
