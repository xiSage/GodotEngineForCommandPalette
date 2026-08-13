// Copyright (c) xiSage
// xiSage licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using GodotEngineForCommandPalette;
using Xunit;

namespace GodotEngineForCommandPalette.Tests;

public class GodotProjectCatalogTests
{
    private const string DataPath = @"C:\Users\Test\AppData\Roaming\Godot";
    private const string ProjectPath = @"C:\Projects\MyGame";
    private const string OtherProjectPath = @"C:\Projects\OtherGame";

    private const string SingleSectionCfg = """
        [C:\Projects\MyGame]

        favorite=false
        """;

    private const string TwoSectionCfg = """
        [C:\Projects\MyGame]

        favorite=false

        [C:\Projects\OtherGame]

        favorite=false
        """;

    private const string GoodProjectGodot = """
        ; Engine configuration file.
        config_version=5

        [application]

        config/name="My Game"
        config/icon="res://icon.svg"
        """;

    private const string GoodOtherProjectGodot = """
        ; Engine configuration file.
        config_version=5

        [application]

        config/name="Other Game"
        config/icon="res://icon.svg"
        """;

    private static MemoryFileSystem CreateFileSystem(string projectsCfg)
    {
        var fs = new MemoryFileSystem();
        fs.AddFile(Path.Join(DataPath, "projects.cfg"), projectsCfg);
        return fs;
    }

    private static MemoryFileSystem CreateSingleProjectFileSystem()
    {
        var fs = CreateFileSystem(SingleSectionCfg);
        fs.AddFile(Path.Join(ProjectPath, "project.godot"), GoodProjectGodot);
        fs.AddFile(Path.Join(ProjectPath, "icon.svg"), "icon");
        return fs;
    }

    private static GodotProjectCatalog CreateCatalog(MemoryFileSystem fs) => new(fs);

    private static GodotProject SingleProject(MemoryFileSystem fs)
    {
        return Assert.Single(CreateCatalog(fs).FindProjects(DataPath));
    }

    private static MemoryFileSystem WithProjectGodot(MemoryFileSystem fs, string projectGodot)
    {
        fs.AddFile(Path.Join(ProjectPath, "project.godot"), projectGodot);
        return fs;
    }

    [Fact]
    public void FindProjects_WhenProjectsCfgMissing_ReturnsEmpty()
    {
        var fs = new MemoryFileSystem();
        var catalog = CreateCatalog(fs);

        Assert.Empty(catalog.FindProjects(DataPath));
    }

    [Fact]
    public void FindProjects_WhenProjectsCfgReadThrows_ReturnsEmpty()
    {
        var fs = CreateFileSystem(SingleSectionCfg);
        fs.ThrowOnRead(Path.Join(DataPath, "projects.cfg"));
        var catalog = CreateCatalog(fs);

        Assert.Empty(catalog.FindProjects(DataPath));
    }

    [Fact]
    public void FindProjects_ReturnsProjectsInCfgOrder()
    {
        var fs = CreateFileSystem(TwoSectionCfg);
        fs.AddFile(Path.Join(ProjectPath, "project.godot"), GoodProjectGodot);
        fs.AddFile(Path.Join(OtherProjectPath, "project.godot"), GoodOtherProjectGodot);

        var projects = CreateCatalog(fs).FindProjects(DataPath);

        Assert.Equal(2, projects.Count);
        Assert.Equal(ProjectPath, projects[0].Path);
        Assert.Equal(OtherProjectPath, projects[1].Path);
        Assert.Equal("My Game", projects[0].Title);
        Assert.Equal("Other Game", projects[1].Title);
    }

    [Fact]
    public void Title_ComesFromConfigName()
    {
        var project = SingleProject(CreateSingleProjectFileSystem());

        Assert.Equal("My Game", project.Title);
    }

    [Fact]
    public void Title_FallsBackToDirectoryName_WhenConfigNameMissing()
    {
        var fs = WithProjectGodot(CreateSingleProjectFileSystem(), """
            ; Engine configuration file.
            config_version=5

            [application]

            config/icon="res://icon.svg"
            """);

        var project = SingleProject(fs);

        Assert.Equal("MyGame", project.Title);
    }

    [Fact]
    public void Title_FallsBackToDirectoryName_WhenConfigNameEmpty()
    {
        var fs = WithProjectGodot(CreateSingleProjectFileSystem(), """
            ; Engine configuration file.
            config_version=5

            [application]

            config/name=""
            config/icon="res://icon.svg"
            """);

        var project = SingleProject(fs);

        Assert.Equal("MyGame", project.Title);
    }

    [Fact]
    public void IconPath_ResolvesResIcon_WhenFileExists()
    {
        var project = SingleProject(CreateSingleProjectFileSystem());

        Assert.Equal(Path.Join(ProjectPath, "icon.svg"), project.IconPath);
    }

    [Fact]
    public void IconPath_IsNull_WhenResIconFileMissing()
    {
        var fs = WithProjectGodot(CreateFileSystem(SingleSectionCfg), """
            ; Engine configuration file.
            config_version=5

            [application]

            config/name="My Game"
            config/icon="res://icon.svg"
            """);

        var project = SingleProject(fs);

        Assert.Null(project.IconPath);
    }

    [Fact]
    public void IconPath_IsNull_WhenIconIsShortString()
    {
        var fs = WithProjectGodot(CreateSingleProjectFileSystem(), """
            ; Engine configuration file.
            config_version=5

            [application]

            config/name="My Game"
            config/icon="r"
            """);

        var project = SingleProject(fs);

        Assert.Null(project.IconPath);
    }

    [Fact]
    public void IconPath_IsNull_WhenIconEmpty()
    {
        var fs = WithProjectGodot(CreateSingleProjectFileSystem(), """
            ; Engine configuration file.
            config_version=5

            [application]

            config/name="My Game"
            config/icon=""
            """);

        var project = SingleProject(fs);

        Assert.Null(project.IconPath);
    }

    [Fact]
    public void IconPath_IsNull_WhenIconHasUnrecognizedPrefix()
    {
        var fs = WithProjectGodot(CreateSingleProjectFileSystem(), """
            ; Engine configuration file.
            config_version=5

            [application]

            config/name="My Game"
            config/icon="foo/bar.png"
            """);

        var project = SingleProject(fs);

        Assert.Null(project.IconPath);
    }

    [Fact]
    public void IconPath_IsNull_WhenUidCacheMissing()
    {
        var fs = WithProjectGodot(CreateSingleProjectFileSystem(), """
            ; Engine configuration file.
            config_version=5

            [application]

            config/name="My Game"
            config/icon="uid://abc123"
            """);

        var project = SingleProject(fs);

        Assert.Null(project.IconPath);
    }

    [Fact]
    public void IconPath_IsNull_WhenUidCacheMisses()
    {
        var fs = WithProjectGodot(CreateSingleProjectFileSystem(), """
            ; Engine configuration file.
            config_version=5

            [application]

            config/name="My Game"
            config/icon="uid://abc123"
            """);
        fs.AddFile(Path.Join(ProjectPath, ".godot", "uid_cache.bin"), "");

        var project = SingleProject(fs);

        Assert.Null(project.IconPath);
    }

    [Fact]
    public void ProjectGodotMissing_ReturnsError()
    {
        var fs = CreateFileSystem(TwoSectionCfg);
        fs.AddFile(Path.Join(ProjectPath, "project.godot"), GoodProjectGodot);

        var projects = CreateCatalog(fs).FindProjects(DataPath);

        Assert.Equal(2, projects.Count);
        Assert.Null(projects[0].Error);
        Assert.NotNull(projects[1].Error);
        Assert.Equal("OtherGame", projects[1].Title);
    }

    [Fact]
    public void ReadThrows_ReturnsError()
    {
        var fs = CreateFileSystem(SingleSectionCfg);
        fs.AddFile(Path.Join(ProjectPath, "project.godot"), GoodProjectGodot);
        fs.ThrowOnRead(Path.Join(ProjectPath, "project.godot"));

        var project = SingleProject(fs);

        Assert.NotNull(project.Error);
    }

    [Fact]
    public void OneBadProject_DoesNotBreakOthers()
    {
        var fs = CreateFileSystem(TwoSectionCfg);
        fs.AddFile(Path.Join(ProjectPath, "project.godot"), GoodProjectGodot);
        fs.AddFile(Path.Join(OtherProjectPath, "project.godot"), GoodOtherProjectGodot);
        fs.ThrowOnRead(Path.Join(ProjectPath, "project.godot"));

        var projects = CreateCatalog(fs).FindProjects(DataPath);

        Assert.Equal(2, projects.Count);
        Assert.NotNull(projects[0].Error);
        Assert.Null(projects[1].Error);
        Assert.Equal("Other Game", projects[1].Title);
    }
}
