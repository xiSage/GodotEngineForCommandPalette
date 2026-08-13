// Copyright (c) xiSage
// xiSage licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace GodotEngineForCommandPalette;

public enum ProjectSortMode
{
    NameAsc,
    NameDesc,
    PathAsc,
    PathDesc,
}

public static class ProjectSorter
{
    public static List<GodotProject> Sort(
        IEnumerable<GodotProject> projects,
        ProjectSortMode? sortMode,
        bool favoriteOnTop)
    {
        var healthy = projects.Where(p => p.Error is null).ToList();
        var failed = projects.Where(p => p.Error is not null).ToList();

        var orderedHealthy = OrderByMode(healthy, sortMode);

        if (favoriteOnTop)
        {
            var favorites = orderedHealthy.Where(p => p.IsFavorite).ToList();
            var others = orderedHealthy.Where(p => !p.IsFavorite).ToList();
            return [.. favorites, .. others, .. failed];
        }

        return [.. orderedHealthy, .. failed];
    }

    private static List<GodotProject> OrderByMode(List<GodotProject> projects, ProjectSortMode? sortMode)
    {
        var byTitle = StringComparer.CurrentCulture;
        var byPath = StringComparer.OrdinalIgnoreCase;

        return sortMode switch
        {
            ProjectSortMode.NameAsc => projects
                .OrderBy(p => p.Title, byTitle)
                .ThenBy(p => p.Path, byPath)
                .ToList(),
            ProjectSortMode.NameDesc => projects
                .OrderByDescending(p => p.Title, byTitle)
                .ThenBy(p => p.Path, byPath)
                .ToList(),
            ProjectSortMode.PathAsc => projects
                .OrderBy(p => p.Path, byPath)
                .ThenBy(p => p.Title, byTitle)
                .ToList(),
            ProjectSortMode.PathDesc => projects
                .OrderByDescending(p => p.Path, byPath)
                .ThenBy(p => p.Title, byTitle)
                .ToList(),
            _ => projects,
        };
    }
}
