// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace GodotEngineForCommandPalette;

[Guid("d19268bb-e2ed-4e05-b1f9-126dd3330081")]
public sealed partial class GodotEngineForCommandPalette(ManualResetEvent extensionDisposedEvent) : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent = extensionDisposedEvent;

    private readonly GodotEngineForCommandPaletteCommandsProvider _provider = new();

    public object? GetProvider(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.Commands => _provider,
            _ => null,
        };
    }

    public void Dispose() => this._extensionDisposedEvent.Set();
}
