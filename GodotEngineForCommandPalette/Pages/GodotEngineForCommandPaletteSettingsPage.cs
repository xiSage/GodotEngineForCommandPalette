// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GodotEngineForCommandPalette;

[JsonSerializable(typeof(GodotSettings))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
internal sealed partial class GodotSettingsContext : JsonSerializerContext { }

internal sealed class GodotSettings
{
    // Event to notify when settings change
    public static event EventHandler? SettingsChanged;

    [JsonPropertyName("GodotPath")]
    public string GodotPath { get; set; } = string.Empty;
    [JsonPropertyName("GodotDataPath")]
    public string GodotDataPath { get; set; } = string.Empty;

    private static readonly string _settingsFilePath = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microsoft", "PowerToys", "CommandPalette", "Extensions", "GodotEngineForCommandPalette", "settings.json"
    );

    public static GodotSettings Load()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize(json, GodotSettingsContext.Default.GodotSettings) ?? new GodotSettings();
            }
            catch { }
        }

        return new GodotSettings();
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (directory != null && !Directory.Exists(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(this, GodotSettingsContext.Default.GodotSettings);
            File.WriteAllText(_settingsFilePath, json);

            // Raise the event after successfully saving to file
            OnSettingsChanged();
        }
        catch { }
    }

    public void ResetToDefaults()
    {
        GodotPath = string.Empty;
        GodotDataPath = string.Empty;
    }

    // Method to raise the SettingsChanged event
    private void OnSettingsChanged()
    {
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    // Method to reload settings from file and raise the SettingsChanged event
    public void Reload()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                var json = File.ReadAllText(_settingsFilePath);
                var newSettings = JsonSerializer.Deserialize(json, GodotSettingsContext.Default.GodotSettings);
                if (newSettings != null)
                {
                    GodotPath = newSettings.GodotPath;
                    GodotDataPath = newSettings.GodotDataPath;
                    OnSettingsChanged();
                }
            }
            catch { }
        }
    }
}

internal sealed partial class GodotSettingsProvider : ICommandSettings
{
    private readonly Settings _settings;
    private readonly GodotSettings _godotSettings;

    public GodotSettingsProvider()
    {
        _godotSettings = GodotSettings.Load();
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
        _godotSettings.GodotPath = _settings.GetSetting<string>("godotPath") ?? _godotSettings.GodotPath;
        _godotSettings.GodotDataPath = _settings.GetSetting<string>("godotDataPath") ?? _godotSettings.GodotDataPath;
        _godotSettings.Save();
    }
}