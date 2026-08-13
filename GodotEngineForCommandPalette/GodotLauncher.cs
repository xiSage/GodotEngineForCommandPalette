// Copyright (c) xiSage
// xiSage licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace GodotEngineForCommandPalette;

public interface IProcessStarter
{
    void Start(string fileName, string[] arguments);
}

public sealed class ProcessStarter : IProcessStarter
{
    public void Start(string fileName, string[] arguments)
    {
        _ = Process.Start(fileName, arguments);
    }
}

public sealed class GodotLauncher(IProcessStarter starter)
{
    public void EditProject(string godotPath, string projectPath)
    {
        if (string.IsNullOrEmpty(godotPath))
        {
            return;
        }
        else
        {
            starter.Start(godotPath, ["-e", "--path", projectPath]);
        }
    }

    public void RunProject(string godotPath, string projectPath)
    {
        if (string.IsNullOrEmpty(godotPath))
        {
            return;
        }
        else
        {
            starter.Start(godotPath, ["--path", projectPath]);
        }
    }
}
