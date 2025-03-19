using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UndertaleModTool;

internal class FileAssociations
{
    /// <summary>
    /// All file extensions used by GameMaker WADs.
    /// </summary>
    public static readonly IReadOnlyList<string> Extensions = new string[] { ".win", ".unx", ".ios", ".droid", ".3ds", ".symbian" };

    // Windows imports
    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(long wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    private const long SHCNE_ASSOCCHANGED = 0x08000000;

    // Class name used for file associations
    private const string SoftwareClassName = "UndertaleModTool";

    /// <summary>
    /// Sets a Windows registry subkey value.
    /// </summary>
    /// <param name="parent">Parent registry key.</param>
    /// <param name="name">Child/subkey name.</param>
    /// <param name="value">Value to set the subkey to.</param>
    /// <returns><see langword="true"/> if anything was changed; <see langword="false"/> otherwise.</returns>
    private static bool SetSubkeyValue(RegistryKey parent, string name, string value)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        RegistryKey subkey = parent.CreateSubKey(name);
        if (subkey.GetValue("") is string existingValue && existingValue == value)
        {
            // Existing value is the same; nothing to change
            return false;
        }

        // Change actual value
        subkey.SetValue("", value, RegistryValueKind.String);
        return true;
    }

    /// <summary>
    /// Creates file associations.
    /// </summary>
    public static void CreateAssociations()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Get filename to this executable (for later launching)
        string procFileName = Environment.ProcessPath;

        // Create software class for the tool
        RegistryKey HKCU_Classes = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true);
        RegistryKey UndertaleModTool_app = HKCU_Classes.CreateSubKey(SoftwareClassName);
        UndertaleModTool_app.SetValue("", SoftwareClassName);

        // Create launch commands
        bool anythingChanged = false;
        anythingChanged |= SetSubkeyValue(UndertaleModTool_app, @"shell\open\command", $"\"{procFileName}\" \"%1\"");
        anythingChanged |= SetSubkeyValue(UndertaleModTool_app, @"shell\launch\command", $"\"{procFileName}\" \"%1\" launch");
        anythingChanged |= SetSubkeyValue(UndertaleModTool_app, @"shell\launch", "Run game normally");
        anythingChanged |= SetSubkeyValue(UndertaleModTool_app, @"shell\launch\command", $"\"{procFileName}\" \"%1\" special_launch");
        anythingChanged |= SetSubkeyValue(UndertaleModTool_app, @"shell\special_launch", "Run extended options");

        // Associate with file extensions
        foreach (string extStr in Extensions)
        {
            anythingChanged |= SetSubkeyValue(HKCU_Classes, extStr, SoftwareClassName);
        }

        // If anything changed, create notification
        if (anythingChanged)
        {
            SHChangeNotify(SHCNE_ASSOCCHANGED, 0, IntPtr.Zero, IntPtr.Zero);
        }
    }

    /// <summary>
    /// Removes file associations.
    /// </summary>
    public static void RemoveAssociations()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Remove software class for the tool
        RegistryKey HKCU_Classes = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true);
        HKCU_Classes.DeleteSubKeyTree(SoftwareClassName);

        // Unassociate with file extensions, if associated with UndertaleModTool specifically
        bool anythingRemoved = false;
        foreach (string extStr in Extensions)
        {
            RegistryKey key = HKCU_Classes.OpenSubKey(extStr);
            if (key is not null && key.GetValue("") is string value && value == SoftwareClassName)
            {
                HKCU_Classes.DeleteSubKeyTree(extStr);
                anythingRemoved = true;
            }
        }

        // Create notification if any extensions were removed
        if (anythingRemoved)
        {
            SHChangeNotify(SHCNE_ASSOCCHANGED, 0, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
