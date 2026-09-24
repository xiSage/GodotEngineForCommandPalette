// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;

namespace GodotEngineForCommandPalette;

internal sealed partial class GodotEngineForCommandPalettePage : ListPage
{
    private readonly List<ListItem> ProjectItems = [];
    private readonly ListItem _refreshButton;
    private readonly GodotProjectCatalog _catalog = new(new FileSystem());
    private readonly GodotSettingsStore _store;

    private GodotSettings _settings;

    public GodotEngineForCommandPalettePage(GodotSettingsStore store)
    {
        _store = store;

        Icon = IconHelpers.FromRelativePath(@"Assets\Logo.png");
        Title = LocaleLoader.GetString("PageTitle");
        Name = LocaleLoader.GetString("PageName");

        // Open the details pane together with the selection, the way the built-in Search apps page does.
        ShowDetails = true;

        _settings = _store.Load();

        // Subscribe to settings changes
        _store.Changed += OnSettingsChanged;

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
        _settings = _store.Load();
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
            List<GodotProject> projects;
            try
            {
                projects = ProjectSorter.Sort(_catalog.FindProjects(_settings.GodotDataPath), _settings.SortMode, _settings.FavoriteOnTop);
            }
            catch (Exception ex)
            {
                // The catalog already swallows an unreadable projects.cfg, so this only catches what it could
                // not anticipate. Report that as a single row instead of an empty list that never finishes
                // loading.
                projects =
                [
                    new GodotProject(
                        LocaleLoader.GetString("ErrorLoadingProject"),
                        _settings.GodotDataPath,
                        null,
                        ex.Message),
                ];
            }

            foreach (var project in projects)
            {
                if (project.Error is null)
                {
                    ProjectItems.Add(new GodotProjectListItem(project, _settings.GodotPath));
                }
                else
                {
                    ProjectItems.Add(FailedProjectListItem(project, project.Error));
                }
            }
        }
        RaiseItemsChanged();
        IsLoading = false;
    }

    private static ListItem FailedProjectListItem(GodotProject project, string error) =>
        new(new NoOpCommand())
        {
            Title = LocaleLoader.GetString("ErrorLoadingProject"),
            Subtitle = error,
            Details = GodotProjectDetails.ForFailedProject(project, error),
        };

    public override IListItem[] GetItems()
    {
        return [.. ProjectItems, _refreshButton];
    }
}

internal sealed partial class GodotProjectListItem : ListItem
{
    private readonly GodotLauncher _launcher = new(new ProcessStarter());

    public GodotProjectListItem(GodotProject project, string godotPath) : base(new NoOpCommand())
    {
        Title = project.Title;
        Subtitle = project.Path;
        Command = new AnonymousCommand(() => _launcher.EditProject(godotPath, project.Path)) { Name = LocaleLoader.GetString("EditCommand"), Id = project.Path };
        var runCommand = new AnonymousCommand(() => _launcher.RunProject(godotPath, project.Path)) { Name = LocaleLoader.GetString("RunCommand") };
        MoreCommands = [new CommandContextItem(runCommand)];
        if (project.IconPath is not null)
        {
            Icon = new IconInfo(project.IconPath);
        }

        Details = GodotProjectDetails.ForProject(project);
    }
}