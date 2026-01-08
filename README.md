<a href="https://apps.microsoft.com/detail/9p401r7mt3s1?referrer=appbadge&mode=full">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

# Godot Engine for Command Palette

A Windows Command Palette extension that allows you to quickly open and run your Godot Engine projects directly from the Windows Command Palette.

## Features

- **List Godot Projects**: Automatically detects and lists all your Godot projects from the configured data path
- **Open Projects**: Open any Godot project in the Godot editor with a single click
- **Run Projects**: Run Godot projects directly without opening the editor
- **Project Icons**: Displays project icons as defined in your Godot project settings
- **Refresh Projects**: Manually refresh the project list whenever needed
- **Settings Integration**: Easy configuration of Godot paths through Command Palette settings

## Configuration

Before using the extension, you need to configure the following settings:

1. Open the Windows Command Palette
2. Click on the gear icon in the bottom-left corner to open settings
3. Navigate to **Extensions** > **Godot Engine for Command Palette**
4. Configure the following settings:

   - **Godot Editor Path**: Path to your Godot editor executable (e.g., `C:\Program Files\Godot\Godot.exe`)
   - **Godot Data Path**: Path to Godot's application data folder (typically `C:\Users\YourName\AppData\Roaming\Godot`)

5. Click **Save** to apply the settings

## Usage

1. Open the Windows Command Palette
2. Type "Godot" to see the extension
3. Select "Godot Engine for Command Palette" to view your projects
4. Choose a project to:
   - **Open**: Double click on the project or press enter to open it in the Godot editor
   - **Run**: Press ctrl + enter on the project to run the project directly
5. Click "Refresh" at the bottom to update the project list

## How It Works

The extension works by:

1. Reading your Godot projects from the `projects.cfg` file in your Godot data directory
2. Parsing each project's `project.godot` file to get project name and icon
3. Resolving project icons using Godot's UID cache system
4. Providing commands to open and run projects using the configured Godot executable

## Project Structure

```
GodotEngineForCommandPalette/
├── GodotEngineForCommandPalette/
│   ├── Assets/              # Extension icons and images
│   ├── Pages/               # UI pages for the extension
│   │   ├── GodotEngineForCommandPalettePage.cs    # Main project list page
│   │   └── GodotEngineForCommandPaletteSettingsPage.cs  # Settings page
│   ├── Properties/          # Project properties
│   ├── GodotEngineForCommandPalette.cs            # Main extension class
│   ├── GodotEngineForCommandPaletteCommandsProvider.cs  # Command provider
│   └── GodotEngineForCommandPalette.csproj        # Project file
└── GodotEngineForCommandPalette.sln               # Solution file
```

## Requirements

- PowerToys with Command Palette enabled
- .NET 10.0 Runtime