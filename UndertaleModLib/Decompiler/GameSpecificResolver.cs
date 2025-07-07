using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UndertaleModLib.Scripting;

namespace UndertaleModLib.Decompiler;

public class GameSpecificResolver
{
    // Reads a built-in game-specific data file from the assembly
    private static ReadOnlySpan<char> ReadGameSpecificDataFile(string filename)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string resourceName = $"UndertaleModLib.BuiltinGameSpecificData.{filename}";

        using Stream stream = assembly.GetManifestResourceStream(resourceName);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Initializes the registry of game-specific data for the given game.
    /// </summary>
    public static void Initialize(UndertaleData data)
    {
        data.GameSpecificRegistry = new();

        // TODO: make proper file/manifest for all games to use, not just UT/DR, and also not these specific names

        var foundGame = false;

        // Read registry data files
        string lowerName = data?.GeneralInfo?.DisplayName?.Content.ToLower(CultureInfo.InvariantCulture) ?? "";
        data.GameSpecificRegistry.DeserializeFromJson(ReadGameSpecificDataFile("gamemaker.json"));
        if (lowerName.StartsWith("undertale", StringComparison.InvariantCulture) || lowerName.StartsWith("under", StringComparison.InvariantCulture))
        {
            data.GameSpecificRegistry.DeserializeFromJson(ReadGameSpecificDataFile("undertale.json"));
            foundGame = true;
        }
        if (lowerName == "survey_program" || lowerName.StartsWith("deltarune", StringComparison.InvariantCulture) || lowerName.StartsWith("delta", StringComparison.InvariantCulture))
        {
            data.GameSpecificRegistry.DeserializeFromJson(ReadGameSpecificDataFile("deltarune.json"));
            foundGame = true;
        }
        if (!foundGame && File.Exists(lowerName + ".json"))
        {
            data.GameSpecificRegistry.DeserializeFromJson(ReadGameSpecificDataFile(lowerName + ".json"));
            foundGame = true;
        }
        if (!foundGame && data.ToolInfo.ProfileMode && File.Exists(data.ToolInfo.CurrentMD5 + ".json"))
        {
            data.GameSpecificRegistry.DeserializeFromJson(ReadGameSpecificDataFile(data.ToolInfo.CurrentMD5 + ".json"));
            foundGame = true;
        }

        /*if (!foundGame) ///////////////////////// not gonna happen until i somehow realize how to bring functions
        {
            if (data.ToolInfo.ProfileMode)
            {
                var ok = new IScriptInterface();
                ok.SimpleTextInput("uptext.", "lowertext", lowerName, true, true).ToLower();
            }
        }*/
    }
}
