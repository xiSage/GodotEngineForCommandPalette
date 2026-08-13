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
        new("ConfigOrder", LocaleLoader.GetString("SortModeConfigOrder")),
        new("NameAsc", LocaleLoader.GetString("SortModeNameAsc")),
        new("NameDesc", LocaleLoader.GetString("SortModeNameDesc")),
        new("PathAsc", LocaleLoader.GetString("SortModePathAsc")),
        new("PathDesc", LocaleLoader.GetString("SortModePathDesc")),
    ];

    public IContentPage SettingsPage => _settings.SettingsPage;

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
