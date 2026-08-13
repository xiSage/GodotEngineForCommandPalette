// Copyright (c) xiSage
// xiSage licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using GodotResourceUID;
using System;
using System.Collections.Generic;
using System.IO;

namespace GodotEngineForCommandPalette;

public interface IFileSystem
{
    bool FileExists(string path);
    string ReadAllText(string path);
}

internal sealed class FileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public string ReadAllText(string path) => File.ReadAllText(path);
}

public sealed record GodotProject(string Title, string Path, string? IconPath, string? Error);

public sealed class GodotProjectCatalog
{
    private const string ResPrefix = "res://";
    private const string UidPrefix = "uid://";
    private const string ProjectsCfgFileName = "projects.cfg";
    private const string ProjectGodotFileName = "project.godot";
    private const string UidCacheRelativePath = ".godot/uid_cache.bin";

    private readonly IFileSystem _fileSystem;

    public GodotProjectCatalog(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public List<GodotProject> FindProjects(string godotDataPath)
    {
        var projects = new List<GodotProject>();
        var projectsCfgPath = Path.Join(godotDataPath, ProjectsCfgFileName);
        if (!_fileSystem.FileExists(projectsCfgPath))
        {
            return projects;
        }

        var projectsCfg = new GodotConfigFile.ConfigFile();
        try
        {
            projectsCfg.Parse(_fileSystem.ReadAllText(projectsCfgPath));
        }
        catch
        {
            return projects;
        }

        foreach (var section in projectsCfg.GetSections())
        {
            projects.Add(ReadProject(section));
        }
        return projects;
    }

    private GodotProject ReadProject(string projectPath)
    {
        try
        {
            var projectGodotPath = Path.Join(projectPath, ProjectGodotFileName);
            if (!_fileSystem.FileExists(projectGodotPath))
            {
                return Failed(projectPath, "project.godot not found");
            }

            var projectCfg = new GodotConfigFile.ConfigFile();
            projectCfg.Parse(_fileSystem.ReadAllText(projectGodotPath));

            var name = projectCfg.GetValue("application", "config/name", "");
            var icon = projectCfg.GetValue("application", "config/icon", "");
            var title = string.IsNullOrEmpty(name) ? DirectoryNameOf(projectPath) : name;

            return new GodotProject(title, projectPath, ResolveIconPath(projectPath, icon), null);
        }
        catch (Exception ex)
        {
            return Failed(projectPath, ex.Message);
        }
    }

    private static GodotProject Failed(string projectPath, string message) =>
        new(DirectoryNameOf(projectPath), projectPath, null, message);

    private static string DirectoryNameOf(string projectPath)
    {
        var trimmed = projectPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Path.GetFileName(trimmed);
    }

    private string? ResolveIconPath(string projectPath, string icon)
    {
        if (icon.StartsWith(ResPrefix, StringComparison.Ordinal))
        {
            return ExistingPath(Path.Join(projectPath, icon[ResPrefix.Length..]));
        }

        if (icon.StartsWith(UidPrefix, StringComparison.Ordinal))
        {
            var uidCachePath = Path.Join(projectPath, UidCacheRelativePath);
            if (_fileSystem.FileExists(uidCachePath))
            {
                var converted = ResourceUID.GetPathFromCache(uidCachePath, icon);
                if (converted.StartsWith(ResPrefix, StringComparison.Ordinal))
                {
                    return ExistingPath(Path.Join(projectPath, converted[ResPrefix.Length..]));
                }
            }
        }

        return null;
    }

    private string? ExistingPath(string path) => _fileSystem.FileExists(path) ? path : null;
}
