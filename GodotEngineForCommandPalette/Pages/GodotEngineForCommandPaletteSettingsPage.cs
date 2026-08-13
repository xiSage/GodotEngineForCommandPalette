// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

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

        // Subscribe to settings changes
        _settings.SettingsChanged += OnSettingsChanged;
    }

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
        _godotSettings = _godotSettings with
        {
            GodotPath = _settings.GetSetting<string>("godotPath") ?? _godotSettings.GodotPath,
            GodotDataPath = _settings.GetSetting<string>("godotDataPath") ?? _godotSettings.GodotDataPath,
        };
        _store.Save(_godotSettings);
    }
}
