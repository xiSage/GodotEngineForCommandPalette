// Copyright (c) xiSage
// xiSage licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace GodotEngineForCommandPalette;

/// <summary>
/// Handles a launch that did not come from Command Palette.
/// <para>
/// This executable is a COM server for the Command Palette extension: on its own it has no window to
/// show, so a direct launch used to exit without any feedback. Instead, explain what this is and
/// offer to open Command Palette.
/// </para>
/// </summary>
internal static partial class StandaloneLaunch
{
    private const string OpenArgument = "--open";
    private const string SilentArgument = "--silent";

    private const uint MB_OKCANCEL = 0x00000001;
    private const uint MB_ICONINFORMATION = 0x00000040;
    private const uint MB_ICONERROR = 0x00000010;
    private const uint MB_SETFOREGROUND = 0x00010000;
    private const int IDOK = 1;

    // Command Palette registers this URI scheme, and activating it shows the palette.
    private const string CommandPaletteUri = "x-cmdpal:";

    private const string FallbackDisplayName = "Godot Projects";
    private const string FallbackTitle = "Godot Engine for Command Palette";
    private const string FallbackMessage = "Godot Engine for Command Palette is an extension for PowerToys Command Palette, so it has no window of its own.";
    private const string FallbackHint = "Select \"OK\" to open Command Palette, then search for \"{0}\". You can also press Win+Alt+Space at any time.";
    private const string FallbackOpenFailed = "Command Palette could not be opened. Install or start PowerToys Command Palette and try again, or press Win+Alt+Space to open it manually.";

    /// <summary>
    /// Runs the standalone path for the given command line arguments.
    /// </summary>
    /// <param name="args">The raw command line arguments.</param>
    public static void Run(string[] args)
    {
        if (HasArgument(args, SilentArgument))
        {
            // Automation friendly: neither a dialog nor an activation.
            return;
        }

        if (HasArgument(args, OpenArgument))
        {
            _ = OpenCommandPalette();
            return;
        }

        string title = Text("PageTitle", FallbackTitle);
        string hint = Text("StandaloneDialogHint", FallbackHint).Replace("{0}", Text("DisplayName", FallbackDisplayName), StringComparison.Ordinal);
        string message = Text("StandaloneDialogMessage", FallbackMessage) + Environment.NewLine + Environment.NewLine + hint;

        if (MessageBox(message, title, MB_OKCANCEL | MB_ICONINFORMATION | MB_SETFOREGROUND) != IDOK)
        {
            return;
        }

        if (!OpenCommandPalette())
        {
            _ = MessageBox(Text("StandaloneOpenFailedMessage", FallbackOpenFailed), title, MB_ICONERROR | MB_SETFOREGROUND);
        }
    }

    private static bool HasArgument(string[] args, string argument)
    {
        return Array.Exists(args, value => string.Equals(value, argument, StringComparison.OrdinalIgnoreCase));
    }

    private static bool OpenCommandPalette()
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo(CommandPaletteUri) { UseShellExecute = true });
            return true;
        }
        catch (Win32Exception)
        {
            // No application is registered for the URI scheme: Command Palette is not installed.
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static string Text(string resourceName, string fallback)
    {
        try
        {
            string value = LocaleLoader.GetString(resourceName);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
        catch (Exception exception) when (exception is COMException or TypeInitializationException or FileNotFoundException)
        {
            // Resource lookup needs package identity or resources.pri next to the executable, which a
            // launch from an output folder may not have. Fall back to English instead of failing.
            return fallback;
        }
    }

    private static int MessageBox(string text, string caption, uint type)
    {
        IntPtr textPointer = AllocateUnicode(text);
        IntPtr captionPointer = AllocateUnicode(caption);

        try
        {
            return MessageBoxW(IntPtr.Zero, textPointer, captionPointer, type);
        }
        finally
        {
            Marshal.FreeCoTaskMem(textPointer);
            Marshal.FreeCoTaskMem(captionPointer);
        }
    }

    // Allocate a null terminated UTF-16 string. Passing only blittable arguments to LibraryImport
    // keeps this trim and AOT safe, which matters because the project treats those warnings as errors.
    private static IntPtr AllocateUnicode(string value)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(value + '\0');
        IntPtr pointer = Marshal.AllocCoTaskMem(bytes.Length);
        Marshal.Copy(bytes, 0, pointer, bytes.Length);
        return pointer;
    }

    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW")]
    private static partial int MessageBoxW(IntPtr hWnd, IntPtr lpText, IntPtr lpCaption, uint uType);
}
