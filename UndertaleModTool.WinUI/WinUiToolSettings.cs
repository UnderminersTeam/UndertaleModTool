using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Underanalyzer.Decompiler;

namespace UndertaleModTool_WinUI;

public sealed class WinUiToolSettings
{
    public static readonly string AppDataFolder = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "UndertaleModTool");

    private const string SettingsFileName = "settings.json";
    private const string DefaultGameMakerStudioPath = "%appdata%\\GameMaker-Studio";
    private const string DefaultGameMakerStudio2RuntimesPath = "%ProgramData%\\GameMakerStudio2\\Cache\\runtimes";
    private const string DefaultInstanceIdPrefix = "inst_";

    private static bool _isLoaded;

    public static WinUiToolSettings Instance { get; private set; } = new();

    public static string? LastLoadError { get; private set; }

    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string Version { get; set; } = "WinUI";
    public string GameMakerStudioPath { get; set; } = DefaultGameMakerStudioPath;
    public string GameMakerStudio2RuntimesPath { get; set; } = DefaultGameMakerStudio2RuntimesPath;
    public bool AssetOrderSwappingEnabled { get; set; }
    public bool AutomaticFileAssociation { get; set; } = true;
    public bool TempRunMessageShow { get; set; } = true;
    public bool WarnOnClose { get; set; } = true;
    public double GlobalGridWidth { get; set; } = 20;
    public bool GridWidthEnabled { get; set; }
    public double GlobalGridHeight { get; set; } = 20;
    public bool GridHeightEnabled { get; set; }
    public double GlobalGridThickness { get; set; } = 1;
    public bool GridThicknessEnabled { get; set; }
    public string TransparencyGridColor1 { get; set; } = "#FF666666";
    public string TransparencyGridColor2 { get; set; } = "#FF999999";
    public bool EnableDarkMode { get; set; }
    public bool ShowDebuggerOption { get; set; }
    public bool AutoRenderPreviews { get; set; } = true;
    public WinUiDecompilerSettings DecompilerSettings { get; set; } = new();
    public string InstanceIdPrefix { get; set; } = DefaultInstanceIdPrefix;
    public bool ShowNullEntriesInResourceTree { get; set; }
    public bool RememberWindowPlacements { get; set; }
    public bool RecompileAllCodeSourcesOnProjectSave { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraProperties { get; set; }

    public static void EnsureLoaded()
    {
        if (_isLoaded)
            return;

        Load();
    }

    public static void Load()
    {
        LastLoadError = null;
        try
        {
            string path = Path.Join(AppDataFolder, SettingsFileName);
            if (!File.Exists(path))
            {
                Instance = new WinUiToolSettings();
                _isLoaded = true;
                if (!TrySave(out string? saveError))
                    LastLoadError = saveError;
                return;
            }

            byte[] bytes = File.ReadAllBytes(path);
            Instance = JsonSerializer.Deserialize<WinUiToolSettings>(bytes, JsonOptions) ?? new WinUiToolSettings();
            Instance.DecompilerSettings ??= new WinUiDecompilerSettings();
            if (Instance.DecompilerSettings.UnknownArgumentNamePattern == "argument{0}")
                Instance.DecompilerSettings.UnknownArgumentNamePattern = "arg{0}";
            _isLoaded = true;
        }
        catch (Exception ex)
        {
            Instance = new WinUiToolSettings();
            LastLoadError = ex.Message;
            _isLoaded = true;
        }
    }

    public static bool TrySave([NotNullWhen(false)] out string? error)
    {
        EnsureLoaded();
        try
        {
            Directory.CreateDirectory(AppDataFolder);
            string path = Path.Join(AppDataFolder, SettingsFileName);
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(Instance, JsonOptions);
            File.WriteAllBytes(path, bytes);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}

public sealed class WinUiDecompilerSettings : IDecompileSettings
{
    [JsonConverter(typeof(JsonStringEnumConverter<IndentStyleKind>))]
    public enum IndentStyleKind
    {
        FourSpaces,
        TwoSpaces,
        Tabs
    }

    [JsonIgnore]
    private DecompileSettings _innerSettings = new();

    public IndentStyleKind IndentStyle { get; set; }

    [JsonIgnore]
    public string IndentString => IndentStyle switch
    {
        IndentStyleKind.FourSpaces => "    ",
        IndentStyleKind.TwoSpaces => "  ",
        IndentStyleKind.Tabs => "\t",
        _ => "    "
    };

    public bool UseSemicolon { get => _innerSettings.UseSemicolon; set => _innerSettings.UseSemicolon = value; }
    public bool UseCSSColors { get => _innerSettings.UseCSSColors; set => _innerSettings.UseCSSColors = value; }
    public bool PrintWarnings { get => _innerSettings.PrintWarnings; set => _innerSettings.PrintWarnings = value; }
    public bool MacroDeclarationsAtTop { get => _innerSettings.MacroDeclarationsAtTop; set => _innerSettings.MacroDeclarationsAtTop = value; }
    public bool EmptyLineAfterBlockLocals { get => _innerSettings.EmptyLineAfterBlockLocals; set => _innerSettings.EmptyLineAfterBlockLocals = value; }
    public bool EmptyLineAroundEnums { get => _innerSettings.EmptyLineAroundEnums; set => _innerSettings.EmptyLineAroundEnums = value; }
    public bool EmptyLineAroundBranchStatements { get => _innerSettings.EmptyLineAroundBranchStatements; set => _innerSettings.EmptyLineAroundBranchStatements = value; }
    public bool EmptyLineBeforeSwitchCases { get => _innerSettings.EmptyLineBeforeSwitchCases; set => _innerSettings.EmptyLineBeforeSwitchCases = value; }
    public bool EmptyLineAfterSwitchCases { get => _innerSettings.EmptyLineAfterSwitchCases; set => _innerSettings.EmptyLineAfterSwitchCases = value; }
    public bool EmptyLineAroundFunctionDeclarations { get => _innerSettings.EmptyLineAroundFunctionDeclarations; set => _innerSettings.EmptyLineAroundFunctionDeclarations = value; }
    public bool EmptyLineAroundStaticInitialization { get => _innerSettings.EmptyLineAroundStaticInitialization; set => _innerSettings.EmptyLineAroundStaticInitialization = value; }
    public bool OpenBlockBraceOnSameLine { get => _innerSettings.OpenBlockBraceOnSameLine; set => _innerSettings.OpenBlockBraceOnSameLine = value; }
    public bool RemoveSingleLineBlockBraces { get => _innerSettings.RemoveSingleLineBlockBraces; set => _innerSettings.RemoveSingleLineBlockBraces = value; }
    public bool CleanupTry { get => _innerSettings.CleanupTry; set => _innerSettings.CleanupTry = value; }
    public bool CleanupElseToContinue { get => _innerSettings.CleanupElseToContinue; set => _innerSettings.CleanupElseToContinue = value; }
    public bool CleanupDefaultArgumentValues { get => _innerSettings.CleanupDefaultArgumentValues; set => _innerSettings.CleanupDefaultArgumentValues = value; }
    public bool CleanupBuiltinArrayVariables { get => _innerSettings.CleanupBuiltinArrayVariables; set => _innerSettings.CleanupBuiltinArrayVariables = value; }
    public bool CleanupLocalVarDeclarations { get => _innerSettings.CleanupLocalVarDeclarations; set => _innerSettings.CleanupLocalVarDeclarations = value; }
    public bool CreateEnumDeclarations { get => _innerSettings.CreateEnumDeclarations; set => _innerSettings.CreateEnumDeclarations = value; }
    public string UnknownEnumName { get => _innerSettings.UnknownEnumName; set => _innerSettings.UnknownEnumName = value; }
    public string UnknownEnumValuePattern { get => _innerSettings.UnknownEnumValuePattern; set => _innerSettings.UnknownEnumValuePattern = value; }
    public string UnknownArgumentNamePattern { get => _innerSettings.UnknownArgumentNamePattern; set => _innerSettings.UnknownArgumentNamePattern = value; }
    public bool AllowLeftoverDataOnStack { get => _innerSettings.AllowLeftoverDataOnStack; set => _innerSettings.AllowLeftoverDataOnStack = value; }

    public WinUiDecompilerSettings()
    {
        RestoreDefaults();
    }

    public void RestoreDefaults()
    {
        _innerSettings = new DecompileSettings
        {
            UnknownArgumentNamePattern = "arg{0}",
            RemoveSingleLineBlockBraces = true,
            EmptyLineAroundBranchStatements = true,
            EmptyLineBeforeSwitchCases = true
        };
        IndentStyle = IndentStyleKind.FourSpaces;
    }

    public bool TryGetPredefinedDouble(double value, [MaybeNullWhen(false)] out string result, out bool isResultMultiPart)
    {
        return _innerSettings.TryGetPredefinedDouble(value, out result, out isResultMultiPart);
    }
}
