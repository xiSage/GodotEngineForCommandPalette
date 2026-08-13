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

public sealed record GodotProject(string Title, string Path, string? IconPath, string? Error, bool IsFavorite = false);

public sealed class GodotProjectCatalog(IFileSystem fileSystem)
{
    private const string ResPrefix = "res://";
    private const string UidPrefix = "uid://";
    private const string ProjectsCfgFileName = "projects.cfg";
    private const string ProjectGodotFileName = "project.godot";
    private const string UidCacheRelativePath = ".godot/uid_cache.bin";

    public List<GodotProject> FindProjects(string godotDataPath)
    {
        var projects = new List<GodotProject>();
        var projectsCfgPath = Path.Join(godotDataPath, ProjectsCfgFileName);
        if (!fileSystem.FileExists(projectsCfgPath))
        {
            return projects;
        }

        var projectsCfg = new GodotConfigFile.ConfigFile();
        try
        {
            projectsCfg.Parse(fileSystem.ReadAllText(projectsCfgPath));
        }
        catch
        {
            return projects;
        }

        foreach (var section in projectsCfg.GetSections())
        {
            projects.Add(ReadProject(section, IsFavorite(section, projectsCfg)));
        }
        return projects;
    }

    private static bool IsFavorite(string section, GodotConfigFile.ConfigFile projectsCfg)
    {
        try
        {
            return projectsCfg.GetValue(section, "favorite", false);
        }
        catch
        {
            return false;
        }
    }

    private GodotProject ReadProject(string projectPath, bool isFavorite)
    {
        try
        {
            var projectGodotPath = Path.Join(projectPath, ProjectGodotFileName);
            if (!fileSystem.FileExists(projectGodotPath))
            {
                return Failed(projectPath, "project.godot not found", isFavorite);
            }

            var projectCfg = new GodotConfigFile.ConfigFile();
            projectCfg.Parse(fileSystem.ReadAllText(projectGodotPath));

            var name = projectCfg.GetValue("application", "config/name", "");
            var icon = projectCfg.GetValue("application", "config/icon", "");
            var title = string.IsNullOrEmpty(name) ? DirectoryNameOf(projectPath) : name;

            return new GodotProject(title, projectPath, ResolveIconPath(projectPath, icon), null, isFavorite);
        }
        catch (Exception ex)
        {
            return Failed(projectPath, ex.Message, isFavorite);
        }
    }

    private static GodotProject Failed(string projectPath, string message, bool isFavorite) =>
        new(DirectoryNameOf(projectPath), projectPath, null, message, isFavorite);

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
            if (fileSystem.FileExists(uidCachePath))
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

    private string? ExistingPath(string path) => fileSystem.FileExists(path) ? path : null;
}
