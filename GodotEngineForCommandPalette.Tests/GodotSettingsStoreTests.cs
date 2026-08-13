// Copyright (c) xiSage
// xiSage licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using GodotEngineForCommandPalette;
using Xunit;

namespace GodotEngineForCommandPalette.Tests;

public class GodotSettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "GodotEngineForCommandPalette.Tests", Guid.NewGuid().ToString());

    private string StorePath => Path.Combine(_dir, "settings.json");

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch
        {
        }

        GC.SuppressFinalize(this);
    }

    private GodotSettingsStore CreateStore() => new(StorePath);

    [Fact]
    public void Load_WhenFileMissing_ReturnsDefaults()
    {
        var settings = CreateStore().Load();

        Assert.Equal("", settings.GodotPath);
        Assert.Equal("", settings.GodotDataPath);
    }

    [Fact]
    public void Load_WhenFileCorrupt_ReturnsDefaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(StorePath, "{ not json !!");

        var settings = CreateStore().Load();

        Assert.Equal("", settings.GodotPath);
        Assert.Equal("", settings.GodotDataPath);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsValues()
    {
        CreateStore().Save(new GodotSettings
        {
            GodotPath = @"C:\Godot\Godot.exe",
            GodotDataPath = @"C:\Data\Godot",
        });

        var loaded = CreateStore().Load();

        Assert.Equal(@"C:\Godot\Godot.exe", loaded.GodotPath);
        Assert.Equal(@"C:\Data\Godot", loaded.GodotDataPath);
    }

    [Fact]
    public void Save_CreatesMissingDirectory()
    {
        CreateStore().Save(new GodotSettings());

        Assert.True(File.Exists(StorePath));
    }

    [Fact]
    public void Save_RaisesChanged()
    {
        var store = CreateStore();
        var raised = false;
        store.Changed += (_, _) => raised = true;

        store.Save(new GodotSettings());

        Assert.True(raised);
    }
}
