// Copyright (c) xiSage
// xiSage licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Collections.Generic;
using System.Globalization;

namespace GodotEngineForCommandPalette;

/// <summary>
/// Builds the details the host shows next to the selected project list item.
/// </summary>
/// <remarks>
/// Only what the project actually declares is rendered: a key the project omits contributes no element at all,
/// and an icon that could not be resolved is left out rather than replaced by a fallback.
/// </remarks>
internal static class GodotProjectDetails
{
    public static Details ForProject(GodotProject project)
    {
        var metadata = new List<IDetailsElement>
        {
            PathElement(project),
        };

        if (project.MainScene is not null)
        {
            metadata.Add(Label("DetailsMainScene", new DetailsLink { Text = project.MainScene }));
        }

        if (project.GodotVersion is not null)
        {
            metadata.Add(Label("DetailsGodotVersion", new DetailsTags { Tags = [new Tag(project.GodotVersion)] }));
        }

        Tag[] features = FeatureTags(project);
        if (features.Length > 0)
        {
            metadata.Add(Label("DetailsFeatures", new DetailsTags { Tags = features }));
        }

        if (project.ConfigVersion is not null)
        {
            metadata.Add(Label("DetailsConfigVersion", new DetailsTags
            {
                Tags = [new Tag(project.ConfigVersion.Value.ToString(CultureInfo.InvariantCulture))],
            }));
        }

        var details = new Details
        {
            Title = project.Title,
            Metadata = [.. metadata],
        };

        if (project.IconPath is not null)
        {
            details.HeroImage = new IconInfo(project.IconPath);
        }

        return details;
    }

    /// <summary>Describes a project that could not be read, so it can still be located from the pane.</summary>
    public static Details ForFailedProject(GodotProject project, string error) =>
        new()
        {
            Title = project.Title,
            Metadata =
            [
                PathElement(project),
                Label("DetailsError", new DetailsLink { Text = error }),
            ],
        };

    private static DetailsElement PathElement(GodotProject project) =>
        Label("DetailsPath", new DetailsLink { Text = project.Path });

    private static DetailsElement Label(string labelKey, IDetailsData data) =>
        new() { Key = LocaleLoader.GetString(labelKey), Data = data };

    /// <summary>The first feature entry is the Godot version, so the tags start after it.</summary>
    private static Tag[] FeatureTags(GodotProject project)
    {
        if (project.Features is null || project.Features.Count < 2)
        {
            return [];
        }

        var tags = new List<Tag>(project.Features.Count - 1);
        for (var index = 1; index < project.Features.Count; index++)
        {
            tags.Add(new Tag(project.Features[index]));
        }

        return [.. tags];
    }
}
