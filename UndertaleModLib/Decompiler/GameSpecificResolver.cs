using System;
using System.Globalization;
using System.IO;
using System.Reflection;

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
    public static void InitializeTypes(UndertaleData data)
    {
        data.GameSpecificRegistry = new();

        // TODO: make proper file/manifest for all games to use, not just UT/DR, and also not these specific names

        // Read registry data files
        string lowerName = data?.GeneralInfo?.DisplayName?.Content.ToLower(CultureInfo.InvariantCulture) ?? "";
        data.GameSpecificRegistry.DeserializeFromJson(ReadGameSpecificDataFile("gamemaker.json"));
        if (lowerName.StartsWith("undertale", StringComparison.InvariantCulture))
        {
            data.GameSpecificRegistry.DeserializeFromJson(ReadGameSpecificDataFile("undertale.json"));
        }
        if (lowerName == "survey_program" || lowerName.StartsWith("deltarune", StringComparison.InvariantCulture))
        {
            data.GameSpecificRegistry.DeserializeFromJson(ReadGameSpecificDataFile("deltarune.json"));
        }
    }
}
