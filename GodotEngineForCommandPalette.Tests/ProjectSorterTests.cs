// Copyright (c) xiSage
// xiSage licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using GodotEngineForCommandPalette;
using Xunit;

namespace GodotEngineForCommandPalette.Tests;

public class ProjectSorterTests
{
    private static GodotProject Project(string title, string path, bool isFavorite = false) =>
        new(title, path, null, null, isFavorite);

    private static GodotProject Error(string path) =>
        new("Broken", path, null, "boom");

    private static List<GodotProject> ThreeProjects() =>
    [
        Project("Zulu", @"C:\z"),
        Project("Alpha", @"C:\a"),
        Project("Mike", @"C:\m"),
    ];

    [Fact]
    public void Sort_WhenModeNull_KeepsInputOrder()
    {
        var sorted = ProjectSorter.Sort(ThreeProjects(), null, favoriteOnTop: false);

        Assert.Equal(["Zulu", "Alpha", "Mike"], sorted.ConvertAll(p => p.Title));
    }

    [Fact]
    public void Sort_ByNameAscending_SortsByTitle()
    {
        var sorted = ProjectSorter.Sort(ThreeProjects(), ProjectSortMode.NameAsc, favoriteOnTop: false);

        Assert.Equal(["Alpha", "Mike", "Zulu"], sorted.ConvertAll(p => p.Title));
    }

    [Fact]
    public void Sort_ByNameDescending_ReversesTitleOrder()
    {
        var sorted = ProjectSorter.Sort(ThreeProjects(), ProjectSortMode.NameDesc, favoriteOnTop: false);

        Assert.Equal(["Zulu", "Mike", "Alpha"], sorted.ConvertAll(p => p.Title));
    }

    [Fact]
    public void Sort_ByPathAscending_SortsByPath()
    {
        var sorted = ProjectSorter.Sort(ThreeProjects(), ProjectSortMode.PathAsc, favoriteOnTop: false);

        Assert.Equal([@"C:\a", @"C:\m", @"C:\z"], sorted.ConvertAll(p => p.Path));
    }

    [Fact]
    public void Sort_ByPathDescending_ReversesPathOrder()
    {
        var sorted = ProjectSorter.Sort(ThreeProjects(), ProjectSortMode.PathDesc, favoriteOnTop: false);

        Assert.Equal([@"C:\z", @"C:\m", @"C:\a"], sorted.ConvertAll(p => p.Path));
    }

    [Fact]
    public void Sort_TieTitle_BreaksByPathOrdinalIgnoreCase()
    {
        var projects = new List<GodotProject>
        {
            Project("Same", @"C:\B"),
            Project("Same", @"C:\a"),
        };

        var sorted = ProjectSorter.Sort(projects, ProjectSortMode.NameAsc, favoriteOnTop: false);

        Assert.Equal([@"C:\a", @"C:\B"], sorted.ConvertAll(p => p.Path));
    }

    [Fact]
    public void Sort_WhenFavoriteOnTop_PutsFavoritesFirst()
    {
        var projects = new List<GodotProject>
        {
            Project("Normal", @"C:\n"),
            Project("Fav", @"C:\f", isFavorite: true),
        };

        var sorted = ProjectSorter.Sort(projects, null, favoriteOnTop: true);

        Assert.Equal(["Fav", "Normal"], sorted.ConvertAll(p => p.Title));
    }

    [Fact]
    public void Sort_WhenFavoriteOnTop_SortsWithinGroups()
    {
        var projects = new List<GodotProject>
        {
            Project("Zulu", @"C:\z"),
            Project("FavZ", @"C:\fz", isFavorite: true),
            Project("Alpha", @"C:\a"),
            Project("FavA", @"C:\fa", isFavorite: true),
        };

        var sorted = ProjectSorter.Sort(projects, ProjectSortMode.NameAsc, favoriteOnTop: true);

        Assert.Equal(["FavA", "FavZ", "Alpha", "Zulu"], sorted.ConvertAll(p => p.Title));
    }

    [Fact]
    public void Sort_WhenNotFavoriteOnTop_IgnoresFavorites()
    {
        var projects = new List<GodotProject>
        {
            Project("Zulu", @"C:\z"),
            Project("FavA", @"C:\fa", isFavorite: true),
            Project("Alpha", @"C:\a"),
        };

        var sorted = ProjectSorter.Sort(projects, ProjectSortMode.NameAsc, favoriteOnTop: false);

        Assert.Equal(["Alpha", "FavA", "Zulu"], sorted.ConvertAll(p => p.Title));
    }

    [Fact]
    public void Sort_PinsErrorProjectsAtBottom()
    {
        var projects = new List<GodotProject>
        {
            Error(@"C:\z"),
            Project("Alpha", @"C:\a"),
            Error(@"C:\a"),
        };

        var sorted = ProjectSorter.Sort(projects, ProjectSortMode.NameAsc, favoriteOnTop: false);

        Assert.Equal(2, sorted.Count(p => p.Error is not null));
        Assert.Equal("Alpha", sorted[0].Title);
        Assert.NotNull(sorted[1].Error);
        Assert.NotNull(sorted[2].Error);
    }

    [Fact]
    public void Sort_PinsErrorProjectsBelowFavorites()
    {
        var projects = new List<GodotProject>
        {
            Error(@"C:\z"),
            Project("Fav", @"C:\f", isFavorite: true),
            Project("Normal", @"C:\n"),
        };

        var sorted = ProjectSorter.Sort(projects, null, favoriteOnTop: true);

        Assert.Equal(["Fav", "Normal"], [sorted[0].Title, sorted[1].Title]);
        Assert.NotNull(sorted[2].Error);
    }

    [Fact]
    public void Sort_WhenEmpty_ReturnsEmpty()
    {
        var sorted = ProjectSorter.Sort([], ProjectSortMode.NameAsc, favoriteOnTop: true);

        Assert.Empty(sorted);
    }
}
