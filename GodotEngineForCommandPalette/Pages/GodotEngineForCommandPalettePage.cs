// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using GodotResourceUID;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace GodotEngineForCommandPalette;

internal sealed partial class GodotEngineForCommandPalettePage : ListPage
{
    private readonly List<ListItem> _projectItems = [];
    private readonly ListItem _refreshButton;
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private GodotSettings _settings;

    public GodotEngineForCommandPalettePage()
    {
        Icon = IconHelpers.FromRelativePath(@"Assets\Logo.png");
        Title = "Godot Engine for Command Palette";
        Name = "Open";

        _settings = GodotSettings.Load();

        // Subscribe to settings changes
        GodotSettings.SettingsChanged += OnSettingsChanged;

        _refreshButton = new(new AnonymousCommand(RefreshProjects) { Result = CommandResult.KeepOpen() })
        {
            Title = "Refresh",
            Icon = IconHelpers.FromRelativePaths(@"Assets\ArrowClockwiseLight.png", @"Assets\ArrowClockwiseDark.png"),
        };
        RefreshProjects();
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        // Reload settings and refresh projects
        _settings = GodotSettings.Load();
        RefreshProjects();
    }

    private void RefreshProjects()
    {
        IsLoading = true;
        _projectItems.Clear();

        // Check if Godot data path is set
        if (string.IsNullOrEmpty(_settings.GodotDataPath))
        {
            _projectItems.Add(new ListItem(new NoOpCommand())
            {
                Title = "Please configure Godot data path in settings",
                Subtitle = "Go to Command Palette settings > Godot Engine for Command Palette"
            });
        }
        else
        {

            var projectsCfg = new GodotConfigFile.ConfigFile();
            projectsCfg.Load(Path.Join(_settings.GodotDataPath, "projects.cfg"));
            foreach (var section in projectsCfg.GetSections())
            {
                try
                {
                    var projectCfg = new GodotConfigFile.ConfigFile();
                    projectCfg.Load(Path.Join(section, "project.godot"));
                    var icon = projectCfg.GetValue("application", "config/icon", "");
                    var name = projectCfg.GetValue("application", "config/name", "Unknown");
                    //var features = projectsCfg.GetValue<string[]>("application", "config/features", []);

                    if (icon.StartsWith("uid", StringComparison.Ordinal))
                    {
                        var uidCachePath = Path.Join(section, ".godot/uid_cache.bin");
                        if (File.Exists(uidCachePath))
                        {
                            var conterted_icon = ResourceUID.GetPathFromCache(uidCachePath, icon);
                            if (conterted_icon != "")
                            {
                                icon = conterted_icon;
                            }
                        }
                    }
                    _projectItems.Add(new GodotProjectListItem(name, section, icon, _settings.GodotPath));
                }
                catch (Exception ex)
                {
                    _projectItems.Add(new ListItem(new NoOpCommand())
                    {
                        Title = "Error loading project",
                        Subtitle = ex.Message
                    });
                }
            }

        }
        RaiseItemsChanged();
        IsLoading = false;
    }

    public override IListItem[] GetItems()
    {
        return [.. _projectItems, _refreshButton];
    }
}

internal sealed partial class GodotProjectListItem : ListItem
{
    public GodotProjectListItem(string title, string path, string icon, string godotPath) : base(new NoOpCommand())
    {
        Title = title;
        Subtitle = path;
        Command = new AnonymousCommand(() => OpenProject(path, godotPath)) { Name = "Edit" };
        var runCommand = new AnonymousCommand(() => RunProject(path, godotPath)) { Name = "Run" };
        MoreCommands = [new CommandContextItem(runCommand)];
        var iconPath = Path.Join(path, icon[6..]);
        if (File.Exists(iconPath))
        {
            Icon = new IconInfo(iconPath);
        }
    }

    private static void OpenProject(string path, string godotPath)
    {
        if (string.IsNullOrEmpty(godotPath))
        {
            return;
        }
        else
        {
            _ = Process.Start(godotPath, ["-e", "--path", path]);
        }
    }

    private static void RunProject(string path, string godotPath)
    {
        if (string.IsNullOrEmpty(godotPath))
        {
            return;
        }
        else
        {
            _ = Process.Start(godotPath, ["--path", path]);
        }
    }
}