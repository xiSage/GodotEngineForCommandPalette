// Copyright (c) xiSage
// xiSage licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace GodotEngineForCommandPalette.Tests;

public class GodotLauncherTests
{
    private const string GodotPath = @"C:\Godot\Godot.exe";
    private const string ProjectPath = @"C:\Projects\MyGame";

    private sealed class RecordingProcessStarter : IProcessStarter
    {
        public List<(string FileName, string[] Arguments)> Calls { get; } = [];

        public void Start(string fileName, string[] arguments)
        {
            Calls.Add((fileName, arguments));
        }
    }

    private static GodotLauncher CreateLauncher(RecordingProcessStarter starter)
    {
        return new(starter);
    }

    [Fact]
    public void EditProject_StartsGodotWithEditorArgs()
    {
        var starter = new RecordingProcessStarter();
        var launcher = CreateLauncher(starter);

        launcher.EditProject(GodotPath, ProjectPath);

        var (FileName, Arguments) = Assert.Single(starter.Calls);
        Assert.Equal(GodotPath, FileName);
        Assert.Equal(new[] { "-e", "--path", ProjectPath }, Arguments);
    }

    [Fact]
    public void RunProject_StartsGodotWithRunArgs()
    {
        var starter = new RecordingProcessStarter();
        var launcher = CreateLauncher(starter);

        launcher.RunProject(GodotPath, ProjectPath);

        var (FileName, Arguments) = Assert.Single(starter.Calls);
        Assert.Equal(GodotPath, FileName);
        Assert.Equal(new[] { "--path", ProjectPath }, Arguments);
    }

    [Fact]
    public void EditProject_WhenGodotPathEmpty_DoesNotStart()
    {
        var starter = new RecordingProcessStarter();
        var launcher = CreateLauncher(starter);

        launcher.EditProject("", ProjectPath);

        Assert.Empty(starter.Calls);
    }

    [Fact]
    public void RunProject_WhenGodotPathEmpty_DoesNotStart()
    {
        var starter = new RecordingProcessStarter();
        var launcher = CreateLauncher(starter);

        launcher.RunProject("", ProjectPath);

        Assert.Empty(starter.Calls);
    }
}
