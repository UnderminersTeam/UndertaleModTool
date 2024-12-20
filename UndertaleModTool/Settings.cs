using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace UndertaleModTool
{
    public class Settings
    {
        public static string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UndertaleModTool");
        public static string ProfilesFolder = Path.Combine(AppDataFolder, "Profiles");

        public string Version { get; set; } = MainWindow.Version;
        public string GameMakerStudioPath { get; set; } = "%appdata%\\GameMaker-Studio";
        public string GameMakerStudio2RuntimesPath { get; set; } = "%ProgramData%\\GameMakerStudio2\\Cache\\runtimes";
        public bool AssetOrderSwappingEnabled { get; set; } = false;
        public bool ProfileModeEnabled { get; set; } = false;
        public bool UseGMLCache { get; set; } = false;
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

        public string TransparencyGridColor1 { get; set; } = "#ff666666";
        public string TransparencyGridColor2 { get; set; } = "#ff999999";

        public bool EnableDarkMode { get; set; } = false;
        public bool ShowDebuggerOption { get; set; } = false;

        public static Settings Instance;

        public static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
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
            try
            {
                string path = Path.Combine(AppDataFolder, "settings.json");
                if (!File.Exists(path))
                {
                    new Settings();
                    Save();
                    return;
                }
                byte[] bytes = File.ReadAllBytes(path);
                JsonSerializer.Deserialize<Settings>(bytes, JsonOptions);

                // Handle upgrading settings here when needed
                bool changed = false;
                if (Instance.Version != MainWindow.Version)
                {
                    changed = true;
                    // TODO when it becomes necessary
                }

                // Update the version to this version
                Instance.Version = MainWindow.Version;
                if (changed)
                    Save();
            } catch (Exception e)
            {
                MessageBox.Show($"Failed to load settings.json! Using default values.\n{e.Message}");
                new Settings();
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
}
