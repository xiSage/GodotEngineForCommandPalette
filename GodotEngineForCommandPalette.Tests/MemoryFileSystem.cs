// Copyright (c) xiSage
// xiSage licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using GodotEngineForCommandPalette;

namespace GodotEngineForCommandPalette.Tests;

internal sealed class MemoryFileSystem : IFileSystem
{
    private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _throwOnRead = new(StringComparer.OrdinalIgnoreCase);

    public void AddFile(string path, string content) => _files[path] = content;

    public void ThrowOnRead(string path) => _throwOnRead.Add(path);

    public bool FileExists(string path) => _files.ContainsKey(path);

    public string ReadAllText(string path)
    {
        if (_throwOnRead.Contains(path))
        {
            throw new IOException($"Simulated read failure: {path}");
        }
        return _files[path];
    }
}
