// Copyright (c) xiSage
// xiSage licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.ApplicationModel.Resources;

namespace GodotEngineForCommandPalette;

internal static class LocaleLoader
{
    private static readonly ResourceLoader _resourceLoader = new();

    public static string GetString(string resourceName)
    {
        return _resourceLoader.GetString(resourceName);
    }
}
