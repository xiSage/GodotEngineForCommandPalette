// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GodotEngineForCommandPalette;

public partial class GodotEngineForCommandPaletteCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly GodotProjectCatalog _catalog = new(new FileSystem());

    public GodotEngineForCommandPaletteCommandsProvider()
    {
        DisplayName = LocaleLoader.GetString("DisplayName");
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");

        // Set up the settings provider
        Settings = new GodotSettingsProvider();

        _commands = [
            new CommandItem(new GodotEngineForCommandPalettePage()) { Title = DisplayName },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

    public override ICommandItem? GetCommandItem(string id)
    {
        var settings = GodotSettings.Load();
        if (string.IsNullOrEmpty(settings.GodotDataPath))
        {
            return null;
        }

        foreach (var project in _catalog.FindProjects(settings.GodotDataPath))
        {
            if (project.Error is null && project.Path == id)
            {
                return new GodotProjectListItem(project, settings.GodotPath);
            }
        }
        return null;
    }

}
