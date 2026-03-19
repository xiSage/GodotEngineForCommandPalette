// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Linq;

namespace GodotEngineForCommandPalette;

public partial class GodotEngineForCommandPaletteCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public GodotEngineForCommandPaletteCommandsProvider()
    {
        DisplayName = "Godot Engine for Command Palette";
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
        var allCommands = (_commands[0].Command as GodotEngineForCommandPalettePage)?.ProjectItems;
        if (allCommands is not null)
        {
            return allCommands.FirstOrDefault(c => c.Command?.Id == id);
        }
        return null;
    }

}
