using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UndertaleModLib.Decompiler;

/// <summary>
/// Class that helps load game-specific data, which tailor some features (such as the decompiler) to specific games.
/// </summary>
public class GameSpecificResolver
{
    /// <summary>
    /// Base app directory used for locating the "GameSpecificData" directory, which should be immediately inside.
    /// </summary>
    public static string BaseDirectory { get; set; } = AppContext.BaseDirectory;

    private enum ConditionResult
    {
        Ignore,
        Accept,
        Reject
    }

    private static readonly Dictionary<string, Func<UndertaleData, string, ConditionResult>> _conditionEvaluators = new()
    {
        ["Always"] = (UndertaleData _, string _) => 
        {
            return ConditionResult.Accept;
        },

        ["DisplayName.Regex"] = (UndertaleData data, string value) =>
        {
            string displayName = data?.GeneralInfo?.DisplayName?.Content;
            if (displayName is null)
            {
                return ConditionResult.Ignore;
            }

            Match m = Regex.Match(displayName, value, RegexOptions.CultureInvariant);
            if (m.Success)
            {
                return ConditionResult.Accept;
            }

            return ConditionResult.Ignore;
        },
    };
    private static readonly object _lock = new();
    private static readonly List<GameSpecificDefinition> _definitions = new();
    private static bool _loadedDefinitions = false;

    public class GameSpecificCondition
    {
        /// <summary>
        /// Represents the kind of condition to be evaluated.
        /// </summary>
        public string ConditionKind { get; set; }

        /// <summary>
        /// Value to be used during evaluation of condition, if applicable.
        /// </summary>
        public string Value { get; set; }
    }

    public class GameSpecificDefinition
    {
        /// <summary>
        /// Integer representing the order this definition should be evaluated/loaded in.
        /// The lower this number is, the earlier this definition will be evaluated and loaded.
        /// </summary>
        /// <remarks>
        /// Built-in definitions currently use values 0 (for GameMaker builtins) and 1 (for games).
        /// </remarks>
        public int LoadOrder { get; set; } = 100;

        /// <summary>
        /// List of conditions that will be evaluated sequentially, to match this game-specific definition.
        /// </summary>
        public List<GameSpecificCondition> Conditions { get; set; }

        /// <summary>
        /// Filename to be loaded as an Underanalyzer game-specific config, when this definition successfully matches.
        /// If empty, null, or the file is otherwise nonexistent, this will be ignored.
        /// </summary>
        public string UnderanalyzerFilename { get; set; }

        /// <summary>
        /// Evaluates this game-specific definition against the given game data, returning whether this definition should be loaded.
        /// </summary>
        /// <returns>True if this definition should be loaded; false otherwise.</returns>
        public bool Evaluate(UndertaleData data)
        {
            foreach (var condition in Conditions)
            {
                switch (_conditionEvaluators[condition.ConditionKind](data, condition.Value))
                {
                    case ConditionResult.Accept:
                        return true;
                    case ConditionResult.Reject:
                        return false;
                    case ConditionResult.Ignore:
                        // Pass-through to next condition
                        break;
                }
            }

            // Default to reject
            return false;
        }

        /// <summary>
        /// Loads this game-specific definition.
        /// </summary>
        public void Load(UndertaleData data)
        {
            if (!string.IsNullOrEmpty(UnderanalyzerFilename))
            {
                string underanalyzerPath = Path.Combine(BaseDirectory, "GameSpecificData", "Underanalyzer", UnderanalyzerFilename);
                if (File.Exists(underanalyzerPath))
                {
                    data.GameSpecificRegistry.DeserializeFromJson(File.ReadAllText(underanalyzerPath));
                }
            }
        }
    }

    /// <summary>
    /// Forces a full reload of all game-specific definition files.
    /// </summary>
    public static void ReloadDefinitions()
    {
        lock (_lock)
        {
            // Mark definitions as loaded, and reset existing definitions
            _loadedDefinitions = true;
            _definitions.Clear();

            // Scan directory for files, if it exists
            string dir = Path.Combine(BaseDirectory, "GameSpecificData", "Definitions");
            if (Directory.Exists(dir))
            {
                foreach (string file in Directory.EnumerateFiles(dir, "*.json", SearchOption.TopDirectoryOnly))
                {
                    _definitions.Add(JsonSerializer.Deserialize<GameSpecificDefinition>(File.ReadAllText(file)));
                }
            }

            // Sort all definitions by their load order
            _definitions.Sort((a, b) => a.LoadOrder - b.LoadOrder);
        }
    }

    /// <summary>
    /// Loads game-specific definitions, if not loaded already. Call <see cref="ReloadDefinitions"/> to force a full reload.
    /// </summary>
    public static void LoadDefinitions()
    {
        bool loadedDefinitions;
        lock (_lock)
        {
            loadedDefinitions = _loadedDefinitions;
        }
        if (!loadedDefinitions)
        {
            ReloadDefinitions();
        }
    }

    /// <summary>
    /// Initializes the registry of game-specific data for the given game.
    /// </summary>
    public static void Initialize(UndertaleData data)
    {
        // Ensure all definitions are loaded
        LoadDefinitions();

        // Initialize empty game-specific registry for decompiler
        data.GameSpecificRegistry = new();

        // Evaluate all definitions, and load all successful ones
        lock (_lock)
        {
            foreach (var definition in _definitions)
            {
                if (definition.Evaluate(data))
                {
                    definition.Load(data);
                }
            }
        }
    }
}
