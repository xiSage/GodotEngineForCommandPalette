// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;

namespace GodotEngineForCommandPalette;

internal sealed partial class GodotSettingsProvider : ICommandSettings
{
    private readonly Settings _settings;
    private readonly GodotSettingsStore _store;
    private GodotSettings _godotSettings;
    private GodotSettingsPage? _settingsPage;

    public GodotSettingsProvider(GodotSettingsStore store)
    {
        _store = store;
        _godotSettings = _store.Load();
        _settings = new Settings();

        // Add Godot editor path setting
        _settings.Add(new TextSetting("godotPath", LocaleLoader.GetString("GodotEditorPathLabel"), LocaleLoader.GetString("GodotEditorPathDescription"), _godotSettings.GodotPath)
        {
            Placeholder = LocaleLoader.GetString("GodotEditorPathPlaceholder"),
            IsRequired = true,
            ErrorMessage = LocaleLoader.GetString("GodotEditorPathError")
        });

        // Add Godot data path setting
        _settings.Add(new TextSetting("godotDataPath", LocaleLoader.GetString("GodotDataPathLabel"), LocaleLoader.GetString("GodotDataPathDescription"), _godotSettings.GodotDataPath)
        {
            Placeholder = LocaleLoader.GetString("GodotDataPathPlaceholder")
        });

        // Add project sort mode setting
        _settings.Add(new ChoiceSetSetting("sortMode", LocaleLoader.GetString("SortModeLabel"), LocaleLoader.GetString("SortModeDescription"), SortModeChoices())
        {
            Value = _godotSettings.SortMode?.ToString() ?? "ConfigOrder",
        });

        // Add favorite-on-top setting
        _settings.Add(new ToggleSetting("favoriteOnTop", LocaleLoader.GetString("FavoriteOnTopLabel"), LocaleLoader.GetString("FavoriteOnTopDescription"), _godotSettings.FavoriteOnTop));

        // Subscribe to settings changes
        _settings.SettingsChanged += OnSettingsChanged;
    }

    private static List<ChoiceSetSetting.Choice> SortModeChoices() =>
    [
        new(LocaleLoader.GetString("SortModeConfigOrder"), "ConfigOrder"),
        new(LocaleLoader.GetString("SortModeNameAsc"), "NameAsc"),
        new(LocaleLoader.GetString("SortModeNameDesc"), "NameDesc"),
        new(LocaleLoader.GetString("SortModePathAsc"), "PathAsc"),
        new(LocaleLoader.GetString("SortModePathDesc"), "PathDesc"),
    ];

    // The toolkit can only put input controls into a Settings object, so the settings page is a page of our
    // own: it shows the same form plus one markdown link to the repository, which the shell opens in the
    // default browser. The shell reads this property more than once, so keep one page instead of building a
    // new one per read.
    public IContentPage SettingsPage => _settingsPage ??= new GodotSettingsPage(_settings);

    public string ToJson()
    {
        return _settings.ToJson();
    }

    public void Update(string data)
    {
        _settings.Update(data);
    }

    private void OnSettingsChanged(object sender, Settings e)
    {
        // Save the settings when they change
        var sortMode = _settings.GetSetting<string>("sortMode");
        _godotSettings = _godotSettings with
        {
            GodotPath = _settings.GetSetting<string>("godotPath") ?? _godotSettings.GodotPath,
            GodotDataPath = _settings.GetSetting<string>("godotDataPath") ?? _godotSettings.GodotDataPath,
            SortMode = ParseSortMode(sortMode),
            FavoriteOnTop = _settings.GetSetting<bool>("favoriteOnTop"),
        };
        _store.Save(_godotSettings);
    }

    private static ProjectSortMode? ParseSortMode(string? value) =>
        Enum.TryParse(value, ignoreCase: true, out ProjectSortMode mode) ? mode : null;
}

// Settings hands out its own page and only accepts input controls, so a link has to live on a page of our
// own. Everything above it is the toolkit's form, unchanged; the link is the last thing on the page.
internal sealed partial class GodotSettingsPage : ContentPage
{
    private const string RepositoryUrl = "https://github.com/xiSage/GodotEngineForCommandPalette";

    private readonly Settings _settings;

    public GodotSettingsPage(Settings settings)
    {
        _settings = settings;
        Name = LocaleLoader.GetString("DisplayName");
        Icon = new IconInfo("\uE713"); // Settings icon

        // The form is rebuilt whenever a value changes, so the page has to tell the shell to fetch it again.
        _settings.SettingsChanged += (sender, e) => RaiseItemsChanged();
    }

    public override IContent[] GetContent() =>
    [
        .. _settings.ToContent(),
        new MarkdownContent($"[{LocaleLoader.GetString("RepositoryLink")}]({RepositoryUrl})"),
    ];
}
