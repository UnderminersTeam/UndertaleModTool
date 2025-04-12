using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using Underanalyzer.Decompiler;

namespace UndertaleModTool
{
    public class Settings
    {
        public static string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UndertaleModTool");
        public static string ProfilesFolder = Path.Combine(AppDataFolder, "Profiles");

        /// <summary>
        /// Whether file associations settings should be prompted for on startup.
        /// </summary>
        public static bool ShouldPromptForAssociations { get; set; } = false;

        public string Version { get; set; } = MainWindow.Version;
        public string GameMakerStudioPath { get; set; } = "%appdata%\\GameMaker-Studio";
        public string GameMakerStudio2RuntimesPath { get; set; } = "%ProgramData%\\GameMakerStudio2\\Cache\\runtimes";
        public bool AssetOrderSwappingEnabled { get; set; } = false;
        public bool ProfileModeEnabled { get; set; } = false;
        public bool ProfileMessageShown { get; set; } = false;
        public bool AutomaticFileAssociation { get; set; } = true;
        public bool TempRunMessageShow { get; set; } = true;

        // The disk space impact will likely be small for the average user, it should be turned off by default for now.
        // "DeleteOldProfileOnSave" as it currently functions is dangerous to be on by default.
        // Especially if a script makes sweeping changes across the code that are hard to revert.
        // The end user will be blindsided as it currently stands.
        // This can be turned back on by default later if some sort of alternative backup limit is implemented,
        // to provide a buffer, similar to how GMS 1.4 did.
        // Example: 0 (unlimited), 1-20 backups (normal range). If a limit of 20 is set, it will start clearing
        // old backups only after 20 is reached (in the family tree, other unrelated mod families don't count)
        // starting with the oldest, with which one to clear determined from a parenting ledger file
        // (whose implementation does not exist yet).
        //
        // This comment should be cleared in the event that the remedies described are implemented.

        public bool DeleteOldProfileOnSave { get; set; } = false;
        public bool WarnOnClose { get; set; } = true;

        private double _globalGridWidth = 20;
        private double _globalGridHeight = 20;
        public double GlobalGridWidth { get => _globalGridWidth; set { if (value >= 0) _globalGridWidth = value; } }
        public bool GridWidthEnabled { get; set; } = false;
        public double GlobalGridHeight { get => _globalGridHeight; set { if (value >= 0) _globalGridHeight = value; } }
        public bool GridHeightEnabled { get; set; } = false;

        public double GlobalGridThickness { get; set; } = 1;
        public bool GridThicknessEnabled { get; set; } = false;

        public string TransparencyGridColor1 { get; set; } = "#FF666666";
        public string TransparencyGridColor2 { get; set; } = "#FF999999";

        public bool EnableDarkMode { get; set; } = false;
        public bool ShowDebuggerOption { get; set; } = false;
        public DecompilerSettings DecompilerSettings { get; set; }
        public string InstanceIdPrefix { get; set; } = "inst_";

        public bool ShowNullEntriesInResourceTree { get; set; } = false;

        public static Settings Instance { get; private set; }

        public static JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public Settings()
        {
            Instance = this;
        }

        public static void Load()
        {
            DecompilerSettings existingDecompilerSettings = Instance?.DecompilerSettings;

            try
            {
                string path = Path.Combine(AppDataFolder, "settings.json");
                if (!File.Exists(path))
                {
                    // No settings JSON exists, so make a new one
                    _ = new Settings() { DecompilerSettings = existingDecompilerSettings ?? new() };
                    Save();

                    // This is theoretically a first bootup, so prompt for file associations
                    ShouldPromptForAssociations = true;
                    return;
                }

                // Read in data
                byte[] bytes = File.ReadAllBytes(path);
                JsonSerializer.Deserialize<Settings>(bytes, JsonOptions);

                // Handle upgrading settings here when needed
                bool changed = false;
                if (Instance.Version != MainWindow.Version)
                {
                    changed = true;
                    // TODO: When necessary, account for any version upgrades
                }

                // Use existing decompiler settings (from last settings instance)
                if (existingDecompilerSettings is not null)
                    Instance.DecompilerSettings = existingDecompilerSettings;

                // If no settings were supplied at all, generate a new one (can be caused from downgrading)
                Instance.DecompilerSettings ??= new();

                // Auto-remove "argument{0}" syntax (become "arg{0}" by default)
                if (Instance.DecompilerSettings.UnknownArgumentNamePattern == "argument{0}")
                {
                    Instance.DecompilerSettings.UnknownArgumentNamePattern = "arg{0}";
                }

                // Update the version to this version
                Instance.Version = MainWindow.Version;
                if (changed)
                {
                    // Update settings to new version on disk as well
                    Save();
                }
            } 
            catch (Exception e)
            {
                MessageBox.Show($"Failed to load settings.json! Using default values.\n{e.Message}");
                new Settings() { DecompilerSettings = existingDecompilerSettings ?? new() };
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(AppDataFolder);
                string path = Path.Combine(AppDataFolder, "settings.json");
                byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(Instance, JsonOptions);
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to save settings.json!\n{e.Message}");
            }
        }
    }

    /// <summary>
    /// GML decompiler settings instance used by the main UndertaleModTool tool.
    /// </summary>
    public class DecompilerSettings : IDecompileSettings
    {
        /// <summary>
        /// Types of indents provided for in-tool decompilation.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter<IndentStyleKind>))]
        public enum IndentStyleKind
        {
            FourSpaces,
            TwoSpaces,
            Tabs
        }

        // Inner settings used to store values that we don't have any business reimplementing
        [JsonIgnore]
        private DecompileSettings InnerSettings { get; } = new DecompileSettings()
        {
            UnknownArgumentNamePattern = "arg{0}",
            RemoveSingleLineBlockBraces = true,
            EmptyLineAroundBranchStatements = true,
            EmptyLineBeforeSwitchCases = true
        };

        /// <summary>
        /// Indentation style being used for decompilation.
        /// </summary>
        public IndentStyleKind IndentStyle { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public string IndentString
        {
            get => IndentStyle switch
            {
                IndentStyleKind.FourSpaces => "    ",
                IndentStyleKind.TwoSpaces => "  ",
                IndentStyleKind.Tabs => "\t",
                _ => throw new Exception("Unknown indent style")
            };
        }

        // Interface implementation (passes through to inner settings instance)
        public bool UseSemicolon { get => InnerSettings.UseSemicolon; set => InnerSettings.UseSemicolon = value; }
        public bool UseCSSColors { get => InnerSettings.UseCSSColors; set => InnerSettings.UseCSSColors = value; }
        public bool PrintWarnings { get => InnerSettings.PrintWarnings; set => InnerSettings.PrintWarnings = value; }
        public bool MacroDeclarationsAtTop { get => InnerSettings.MacroDeclarationsAtTop; set => InnerSettings.MacroDeclarationsAtTop = value; }
        public bool EmptyLineAfterBlockLocals { get => InnerSettings.EmptyLineAfterBlockLocals; set => InnerSettings.EmptyLineAfterBlockLocals = value; }
        public bool EmptyLineAroundEnums { get => InnerSettings.EmptyLineAroundEnums; set => InnerSettings.EmptyLineAroundEnums = value; }
        public bool EmptyLineAroundBranchStatements { get => InnerSettings.EmptyLineAroundBranchStatements; set => InnerSettings.EmptyLineAroundBranchStatements = value; }
        public bool EmptyLineBeforeSwitchCases { get => InnerSettings.EmptyLineBeforeSwitchCases; set => InnerSettings.EmptyLineBeforeSwitchCases = value; }
        public bool EmptyLineAfterSwitchCases { get => InnerSettings.EmptyLineAfterSwitchCases; set => InnerSettings.EmptyLineAfterSwitchCases = value; }
        public bool EmptyLineAroundFunctionDeclarations { get => InnerSettings.EmptyLineAroundFunctionDeclarations; set => InnerSettings.EmptyLineAroundFunctionDeclarations = value; }
        public bool EmptyLineAroundStaticInitialization { get => InnerSettings.EmptyLineAroundStaticInitialization; set => InnerSettings.EmptyLineAroundStaticInitialization = value; }
        public bool OpenBlockBraceOnSameLine { get => InnerSettings.OpenBlockBraceOnSameLine; set => InnerSettings.OpenBlockBraceOnSameLine = value; }
        public bool RemoveSingleLineBlockBraces { get => InnerSettings.RemoveSingleLineBlockBraces; set => InnerSettings.RemoveSingleLineBlockBraces = value; }
        public bool CleanupTry { get => InnerSettings.CleanupTry; set => InnerSettings.CleanupTry = value; }
        public bool CleanupElseToContinue { get => InnerSettings.CleanupElseToContinue; set => InnerSettings.CleanupElseToContinue = value; }
        public bool CleanupDefaultArgumentValues { get => InnerSettings.CleanupDefaultArgumentValues; set => InnerSettings.CleanupDefaultArgumentValues = value; }
        public bool CleanupBuiltinArrayVariables { get => InnerSettings.CleanupBuiltinArrayVariables; set => InnerSettings.CleanupBuiltinArrayVariables = value; }
        public bool CleanupLocalVarDeclarations { get => InnerSettings.CleanupLocalVarDeclarations; set => InnerSettings.CleanupLocalVarDeclarations = value; }
        public bool CreateEnumDeclarations { get => InnerSettings.CreateEnumDeclarations; set => InnerSettings.CreateEnumDeclarations = value; }
        public string UnknownEnumName { get => InnerSettings.UnknownEnumName; set => InnerSettings.UnknownEnumName = value; }
        public string UnknownEnumValuePattern { get => InnerSettings.UnknownEnumValuePattern; set => InnerSettings.UnknownEnumValuePattern = value; }
        public string UnknownArgumentNamePattern { get => InnerSettings.UnknownArgumentNamePattern; set => InnerSettings.UnknownArgumentNamePattern = value; }
        public bool AllowLeftoverDataOnStack { get => InnerSettings.AllowLeftoverDataOnStack; set => InnerSettings.AllowLeftoverDataOnStack = value; }

        /// <inheritdoc/>
        public bool TryGetPredefinedDouble(double value, [MaybeNullWhen(false)] out string result, out bool isResultMultiPart)
        {
            // Pass through to inner settings instance, which has some predefined values already
            return InnerSettings.TryGetPredefinedDouble(value, out result, out isResultMultiPart);
        }
    }
}
