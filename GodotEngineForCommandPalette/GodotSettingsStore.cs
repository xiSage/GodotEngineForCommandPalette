// Copyright (c) xiSage
// xiSage licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GodotEngineForCommandPalette;

[JsonSerializable(typeof(GodotSettings))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
internal sealed partial class GodotSettingsContext : JsonSerializerContext { }

public sealed record GodotSettings
{
    [JsonPropertyName("GodotPath")]
    public string GodotPath { get; init; } = string.Empty;

    [JsonPropertyName("GodotDataPath")]
    public string GodotDataPath { get; init; } = string.Empty;
}

public sealed class GodotSettingsStore
{
    public event EventHandler? Changed;

    public static string DefaultFilePath { get; } = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microsoft", "PowerToys", "CommandPalette", "Extensions", "GodotEngineForCommandPalette", "settings.json");

    private readonly string _filePath;

    public GodotSettingsStore(string? filePath = null)
    {
        _filePath = filePath ?? DefaultFilePath;
    }

    public GodotSettings Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize(json, GodotSettingsContext.Default.GodotSettings) ?? new GodotSettings();
            }
        }
        catch
        {
        }

        return new GodotSettings();
    }

    public void Save(GodotSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (directory != null && !Directory.Exists(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, GodotSettingsContext.Default.GodotSettings);
            File.WriteAllText(_filePath, json);

            // Raise the event after successfully saving to file
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
        }
    }
}
