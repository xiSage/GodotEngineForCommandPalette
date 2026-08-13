// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GodotEngineForCommandPalette;

internal sealed partial class GodotEngineForCommandPalettePage : ListPage
{
    public readonly List<ListItem> ProjectItems = [];
    private readonly ListItem _refreshButton;
    private readonly GodotProjectCatalog _catalog = new(new FileSystem());

    private GodotSettings _settings;

    public GodotEngineForCommandPalettePage()
    {
        Icon = IconHelpers.FromRelativePath(@"Assets\Logo.png");
        Title = LocaleLoader.GetString("PageTitle");
        Name = LocaleLoader.GetString("PageName");

        _settings = GodotSettings.Load();

        // Subscribe to settings changes
        GodotSettings.SettingsChanged += OnSettingsChanged;

        _refreshButton = new(new AnonymousCommand(RefreshProjects) { Result = CommandResult.KeepOpen() })
        {
            Title = LocaleLoader.GetString("RefreshButton"),
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
        ProjectItems.Clear();

        // Check if Godot data path is set
        if (string.IsNullOrEmpty(_settings.GodotDataPath))
        {
            ProjectItems.Add(new ListItem(new NoOpCommand())
            {
                Title = LocaleLoader.GetString("ConfigurePathMessage"),
                Subtitle = LocaleLoader.GetString("ConfigurePathSubtitle")
            });
        }
        else
        {
            foreach (var project in _catalog.FindProjects(_settings.GodotDataPath))
            {
                if (project.Error is not null)
                {
                    ProjectItems.Add(new ListItem(new NoOpCommand())
                    {
                        Title = LocaleLoader.GetString("ErrorLoadingProject"),
                        Subtitle = project.Error
                    });
                }
                else
                {
                    ProjectItems.Add(new GodotProjectListItem(project, _settings.GodotPath));
                }
            }
        }
        RaiseItemsChanged();
        IsLoading = false;
    }

    public override IListItem[] GetItems()
    {
        return [.. ProjectItems, _refreshButton];
    }
}

internal sealed partial class GodotProjectListItem : ListItem
{
    public GodotProjectListItem(GodotProject project, string godotPath) : base(new NoOpCommand())
    {
        Title = project.Title;
        Subtitle = project.Path;
        Command = new AnonymousCommand(() => OpenProject(project.Path, godotPath)) { Name = LocaleLoader.GetString("EditCommand"), Id = project.Path };
        var runCommand = new AnonymousCommand(() => RunProject(project.Path, godotPath)) { Name = LocaleLoader.GetString("RunCommand") };
        MoreCommands = [new CommandContextItem(runCommand)];
        if (project.IconPath is not null)
        {
            Icon = new IconInfo(project.IconPath);
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