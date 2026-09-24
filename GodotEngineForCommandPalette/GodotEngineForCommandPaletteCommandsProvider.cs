// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;

namespace GodotEngineForCommandPalette;

public partial class GodotEngineForCommandPaletteCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly GodotProjectCatalog _catalog = new(new FileSystem());
    private readonly GodotSettingsStore _store;

    public GodotEngineForCommandPaletteCommandsProvider()
    {
        DisplayName = LocaleLoader.GetString("DisplayName");
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");

        _store = new GodotSettingsStore();

        // Set up the settings provider
        Settings = new GodotSettingsProvider(_store);

        _commands = [
            new CommandItem(new GodotEngineForCommandPalettePage(_store)) { Title = DisplayName },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

    public override ICommandItem? GetCommandItem(string id)
    {
        var settings = _store.Load();
        if (string.IsNullOrEmpty(settings.GodotDataPath))
        {
            return null;
        }

        List<GodotProject> projects;
        try
        {
            projects = _catalog.FindProjects(settings.GodotDataPath);
        }
        catch (Exception)
        {
            // A discovery failure is the page's story to tell - it renders a row for it. Here there is simply
            // nothing to resolve, and no project can match an id that was never listed.
            return null;
        }

        foreach (var project in projects)
        {
            if (project.Error is null && project.Path == id)
            {
                return new GodotProjectListItem(project, settings.GodotPath);
            }
        }
        return null;
    }

}
