using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Project;

namespace UndertaleModToolAvalonia;

public partial class MainViewModel
{
    // Set this when testing.
    public IView? View;

    // Services
    public readonly IServiceProvider ServiceProvider;

    /// <summary>Error messages to be displayed after the view has been loaded.</summary>
    public List<string> LazyErrorMessages = [];

    // Settings
    public SettingsFile? Settings { get; set; }

    // Scripting
    public Scripting Scripting = null!;

    // Window
    public string Title => $"UndertaleModToolAvalonia - v" +
        (Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?.?.?.?") +
        $"{(Data?.GeneralInfo is not null ? " - " + Data.GeneralInfo.ToString() : "")}" +
        $"{(DataPath is not null ? " [" + DataPath + "]" : "")}";

    [Notify]
    private WindowState _WindowState = WindowState.Maximized;

    [Notify]
    private bool _IsEnabled = true;

    // Data
    [Notify]
    private UndertaleData? _Data;
    [Notify]
    private string? _DataPath;
    [Notify]
    private (uint Major, uint Minor, uint Release, uint Build) _DataVersion;

    // Project
    public ProjectContext? Project = null;

    // Tree data grid
    public partial class TreeDataGridItem
    {
        [Notify]
        private string _Text = "<unset text!>";
        public object? Value { get; set; }
        public object? Tag { get; set; }
        [Notify]
        private IList<TreeDataGridItem>? _Children;
    }

    [Notify]
    private ObservableCollection<TreeDataGridItem> _TreeDataGridData = [];

    public event Action<string>? FilterTextChanged;

    // Tabs
    public ObservableCollection<TabItemViewModel> Tabs { get; set; }

    [Notify]
    private TabItemViewModel? _TabSelected;
    [Notify]
    private int _TabSelectedIndex;
    [Notify]
    private string _TabSelectedResourceIdString = "None";

    // Command text box
    [Notify]
    private string _CommandTextBoxText = "";

    // Image cache
    public ImageCache ImageCache = new();

    // Internal clipboard
    public object? InternalClipboard = null;

    public MainViewModel(IServiceProvider? serviceProvider = null)
    {
        ServiceProvider = serviceProvider ?? App.Services;

        AudioPlayer.Init(f => Dispatcher.UIThread.Post(f));

        Tabs = [
            new TabItemViewModel(new DescriptionViewModel(
                "Welcome to UndertaleModTool!",
                "Open a data.win file to get started, then double click on the items on the left to view them."),
                isSelected: true),
        ];
    }

    public void Initialize()
    {
        Settings = SettingsFile.Load(ServiceProvider);
        Scripting = new(ServiceProvider);

        WindowState = Settings.StartMaximized ? WindowState.Maximized : WindowState.Normal;
    }

    public async void OnLoaded()
    {
        foreach (string message in LazyErrorMessages)
        {
            await View!.MessageDialog(message);
        }
        LazyErrorMessages.Clear();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.Args?.Length >= 1)
            {
                try
                {
                    using FileStream stream = File.OpenRead(desktop.Args[0]);
                    if (await LoadData(stream))
                    {
                        DataPath = stream.Name;
                    }
                }
                catch (SystemException e)
                {
                    await View!.MessageDialog($"Error opening data file from argument: {e.Message}");
                }
            }
        }
    }

    public async void OpenDroppedFiles(IEnumerable<IStorageItem>? files)
    {
        if (files is null)
            return;

        var list = files.ToList();
        if (list.Count != 1)
            return;

        if (list[0] is not IStorageFile file)
            return;

        if (!await AskFileSave("Save data file before opening a new one?"))
            return;

        CloseData();

        using Stream stream = await file.OpenReadAsync();

        if (await LoadData(stream))
        {
            DataPath = file.TryGetLocalPath();
        }
    }

    // Called by [Notify]
    public void OnDataChanged()
    {
        if (Data is not null)
        {
            if (Data.GeneralInfo is not null)
                Data.GeneralInfo.PropertyChanged += DataGeneralInfoChangedHandler;

            Data.ToolInfo.InstanceIdPrefix = () => Settings?.InstanceIdPrefix;
            Data.ToolInfo.DecompilerSettings = Settings?.DecompileSettings;
        }

        UpdateVersion();

        TreeDataGridData.Clear();

        if (FilterTextChanged is not null)
            foreach (Delegate item in FilterTextChanged.GetInvocationList())
            {
                FilterTextChanged -= (Action<string>)item;
            }

        if (Data is not null)
        {
            IList<TreeDataGridItem>? MakeChildren<T>(IList<T>? list) where T : notnull
            {
                if (list is not null)
                {
                    ObservableCollectionView<T, TreeDataGridItem> view = new(list,
                        transform: x => new TreeDataGridItem() { Text = "", Value = x });

                    FilterTextChanged += filterText =>
                    {
                        view.SetFilter(item => AssetNameContainsText(item, filterText));
                    };

                    return view.Output;
                }
                return null;
            }

            var dataItem = new TreeDataGridItem()
            {
                Value = Data,
                Text = "Data",
                Children = [],
            };

            if (Data.GeneralInfo is not null)
                dataItem.Children.Add(new() { Value = "GeneralInfo", Text = "General info" });
            if (Data.GlobalInitScripts is not null)
                dataItem.Children.Add(new() { Value = "GlobalInitScripts", Text = "Global init scripts" });
            if (Data.GameEndScripts is not null)
                dataItem.Children.Add(new() { Value = "GameEndScripts", Text = "Game End scripts" });

            if (Data.AudioGroups is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "AudioGroups", Text = "Audio groups",
                Children = MakeChildren(Data.AudioGroups)});
            if (Data.Sounds is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Sounds", Text = "Sounds",
                Children = MakeChildren(Data.Sounds)});
            if (Data.Sprites is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Sprites", Text = "Sprites",
                Children = MakeChildren(Data.Sprites)});
            if (Data.Backgrounds is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Backgrounds", Text = "Backgrounds & Tile sets",
                Children = MakeChildren(Data.Backgrounds)});
            if (Data.Paths is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Paths", Text = "Paths",
                Children = MakeChildren(Data.Paths)});
            if (Data.Scripts is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Scripts", Text = "Scripts",
                Children = MakeChildren(Data.Scripts)});
            if (Data.Shaders is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Shaders", Text = "Shaders",
                Children = MakeChildren(Data.Shaders)});
            if (Data.Fonts is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Fonts", Text = "Fonts",
                Children = MakeChildren(Data.Fonts)});
            if (Data.Timelines is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Timelines", Text = "Timelines",
                Children = MakeChildren(Data.Timelines)});
            if (Data.GameObjects is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "GameObjects", Text = "Game objects",
                Children = MakeChildren(Data.GameObjects)});
            if (Data.Rooms is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Rooms", Text = "Rooms",
                Children = MakeChildren(Data.Rooms)});
            if (Data.Extensions is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Extensions", Text = "Extensions",
                Children = MakeChildren(Data.Extensions)});
            if (Data.TexturePageItems is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "TexturePageItems", Text = "Texture page items",
                Children = MakeChildren(Data.TexturePageItems)});
            if (Data.Code is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Code", Text = "Code",
                Children = MakeChildren(Data.Code)});
            if (Data.Variables is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Variables", Text = "Variables",
                Children = MakeChildren(Data.Variables)});
            if (Data.Functions is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Functions", Text = "Functions",
                Children = MakeChildren(Data.Functions)});
            if (Data.CodeLocals is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "CodeLocals", Text = "Code locals",
                Children = MakeChildren(Data.CodeLocals)});
            if (Data.Strings is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Strings", Text = "Strings",
                Children = MakeChildren(Data.Strings)});
            if (Data.EmbeddedTextures is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "EmbeddedTextures", Text = "Embedded textures",
                Children = MakeChildren(Data.EmbeddedTextures)});
            if (Data.EmbeddedAudio is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "EmbeddedAudio", Text = "Embedded audio",
                Children = MakeChildren(Data.EmbeddedAudio)});
            if (Data.TextureGroupInfo is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "TextureGroupInformation", Text = "Texture group information",
                Children = MakeChildren(Data.TextureGroupInfo)});
            if (Data.EmbeddedImages is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "EmbeddedImages", Text = "Embedded images",
                Children = MakeChildren(Data.EmbeddedImages)});
            if (Data.AnimationCurves is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "AnimationCurves", Text = "Animation curves",
                Children = MakeChildren(Data.AnimationCurves)});
            if (Data.ParticleSystems is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "ParticleSystems", Text = "Particle systems",
                Children = MakeChildren(Data.ParticleSystems)});
            if (Data.ParticleSystemEmitters is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "ParticleSystemEmitters", Text = "Particle system emitters",
                Children = MakeChildren(Data.ParticleSystemEmitters)});

            TreeDataGridData.Add(dataItem);

            // HACK: Dirty! But I don't wanna make a whole interface for that
            if (View is MainView mainView)
                mainView.ExpandItemOnTree(dataItem);
        }
    }

    private bool AssetNameContainsText<T>(T asset, string text)
    {
        string? name = asset switch
        {
            UndertaleNamedResource namedResource => namedResource.Name.Content,
            UndertaleString _string => _string.Content,
            _ => null,
        };

        if (name is null)
            return true;

        return name.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Ask if user wants to save the current file before continuing.
    /// Returns true if either it saved successfully, or if the user didn't want to save, or if there is no file loaded.</summary>
    public async Task<bool> AskFileSave(string message)
    {
        if (Data is null)
            return true;

        var result = await View!.MessageDialog(message, buttons: MessageWindow.Buttons.YesNoCancel);
        if (result == MessageWindow.Result.Yes)
        {
            if (await FileSave())
            {
                return true;
            }
        }
        else if (result == MessageWindow.Result.No)
        {
            return true;
        }

        return false;
    }

    /// <summary>Ask if user wants to save the current project before continuing.
    /// Returns true if either it saved successfully, or if the user didn't want to save, or if there is no project loaded, or if the project has no unexported assets.</summary>
    public async Task<bool> AskProjectSave(string message)
    {
        if (Project is null || !Project.HasUnexportedAssets)
            return true;

        var result = await View!.MessageDialog(message, buttons: MessageWindow.Buttons.YesNoCancel);
        if (result == MessageWindow.Result.Yes)
        {
            if (await ProjectSave())
            {
                return true;
            }
        }
        else if (result == MessageWindow.Result.No)
        {
            return true;
        }

        return false;
    }

    public Task<bool> NewData()
    {
        CloseData();

        Data = UndertaleData.CreateNew();
        DataPath = null;

        return Task.FromResult(true);
    }

    public async Task<bool> LoadData(Stream stream)
    {
        IsEnabled = false;

        ILoaderWindow w = View!.LoaderOpen();
        w.SetText("Opening data file...");

        try
        {
            List<string> warnings = [];
            bool hadImportantWarnings = false;

            UndertaleData data = await Task.Run(() => UndertaleIO.Read(stream,
                (string warning, bool isImportant) =>
                {
                    warnings.Add(warning);
                    if (isImportant)
                    {
                        hadImportantWarnings = true;
                    }
                },
                (string message) =>
                {
                    Dispatcher.UIThread.Post(() => w.SetText($"Opening data file... {message}"));
                })
            );

            if (warnings.Count > 0)
            {
                w.EnsureShown();
                await View!.MessageDialog($"Warnings occurred when loading the data file:\n\n" +
                    $"{(hadImportantWarnings ? "Data loss will likely occur when trying to save.\n" : "")}" +
                    $"{String.Join("\n", warnings)}");
            }

            // TODO: Add other checks for possible stuff.

            Data = data;

            return true;
        }
        catch (Exception e)
        {
            w.EnsureShown();
            await View!.MessageDialog($"Error opening data file: {e.Message}");

            return false;
        }
        finally
        {
            IsEnabled = true;
            w.Close();
        }
    }

    public async Task<bool> SaveData(Stream stream)
    {
        IsEnabled = false;

        ILoaderWindow w = View!.LoaderOpen();
        w.SetText("Saving data file...");

        try
        {
            // TODO: RecompileAllCodeSourcesOnProjectSave setting
            if (Project is not null)
            {
                Project.RecompileAllCodeSources();
            }

            await Task.Run(() => UndertaleIO.Write(stream, Data, message =>
            {
                Dispatcher.UIThread.Post(() => w.SetText($"Saving data file... {message}"));
            }));

            return true;
        }
        catch (ProjectException e)
        {
            w.EnsureShown();
            await View!.MessageDialog($"Recompile error:\n{e.Message}");
        }
        catch (Exception e)
        {
            w.EnsureShown();
            await View!.MessageDialog($"Error saving data file:\n{e.Message}");
        }
        finally
        {
            IsEnabled = true;
            w.Close();
        }

        return false;
    }

    public void CloseData()
    {
        Data = null;
        DataPath = null;

        foreach (TabItemViewModel tab in Tabs)
        {
            tab.Content.OnDetached();
        }

        Tabs.Clear();

        ClearProject();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows.ToList())
            {
                if (window is SearchInCodeWindow)
                {
                    window.Close();
                }
            }
        }
    }

    public void UpdateVersion()
    {
        DataVersion = Data is not null && Data.GeneralInfo is not null ? (Data.GeneralInfo.Major, Data.GeneralInfo.Minor, Data.GeneralInfo.Release, Data.GeneralInfo.Build) : default;
    }

    private void DataGeneralInfoChangedHandler(object? sender, PropertyChangedEventArgs e)
    {
        if (Data is not null && e.PropertyName is
            nameof(UndertaleGeneralInfo.Major) or nameof(UndertaleGeneralInfo.Minor) or
            nameof(UndertaleGeneralInfo.Release) or nameof(UndertaleGeneralInfo.Build))
        {
            UpdateVersion();
        }
    }

    // Menus
    public async void FileNew()
    {
        if (await AskProjectSave("There are assets marked to be exported in the current project. Save project before closing it?")
            && await AskFileSave("Save data file before creating a new one?"))
        {
            await NewData();
        }
    }

    public async void FileOpen()
    {
        if (!await AskProjectSave("There are assets marked to be exported in the current project. Save project before closing it?"))
            return;
        if (!await AskFileSave("Save data file before opening a new one?"))
            return;

        var files = await View!.OpenFileDialog(new FilePickerOpenOptions()
        {
            Title = "Open data file",
            FileTypeFilter = FilePickerFileTypes.Data,
        });

        if (files.Count != 1)
            return;

        CloseData();

        using Stream stream = await files[0].OpenReadAsync();

        if (await LoadData(stream))
        {
            DataPath = files[0].TryGetLocalPath();
        }
    }

    public async Task<bool> FileSave()
    {
        if (Data is null)
            return false;

        if (Project is not null)
        {
            var result = await View!.MessageDialog("Save to the project's designated data file for saving?", buttons: MessageWindow.Buttons.YesNoCancel);
            if (result == MessageWindow.Result.Yes)
            {
                using FileStream fileStream = File.Open(Project.SaveDataPath, FileMode.Truncate);
                if (await SaveData(fileStream))
                {
                    return true;
                }
                return false;
            }
            else if (result != MessageWindow.Result.No)
            {
                return false;
            }
            // If pressed No, continue saving as if there's no project.
        }

        IStorageFile? file = await View!.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Save data file",
            FileTypeChoices = FilePickerFileTypes.Data,
            DefaultExtension = ".win",
        });

        if (file is null)
            return false;

        using Stream stream = await file.OpenWriteAsync();

        if (await SaveData(stream))
        {
            DataPath = file.TryGetLocalPath();
            return true;
        }

        return false;
    }

    public async void FileClose()
    {
        if (!await AskProjectSave("There are assets marked to be exported in the current project. Save project before closing it?"))
            return;
        if (!await AskFileSave("Save data file before closing?"))
            return;

        CloseData();
    }

    public async void FileRun()
    {
        // NOTE: The project system would make this a lot simpler!
        if (Data is null)
            return;

        string question = $"Save data file before running? {(DataPath is null
            ? " It must be saved before running."
            : $"If it's not saved, the data file at the last location will be used (\"{DataPath}\").")}";

        if (!await AskFileSave(question))
            return;

        if (DataPath is null)
            return;

        var files = await View!.OpenFileDialog(new FilePickerOpenOptions()
        {
            Title = "Open runner",
            FileTypeFilter = FilePickerFileTypes.All,
        });

        if (files.Count != 1)
            return;

        string runnerPath = files[0].TryGetLocalPath() ?? string.Empty;
        if (runnerPath == string.Empty)
            return;

        if (!File.Exists(DataPath))
            return;

        // "launcher" allows game_change data files to still access files above the data path.
        Process.Start(new ProcessStartInfo(runnerPath, $"-game \"{DataPath}\" launcher") { WorkingDirectory = Path.GetDirectoryName(DataPath) });
    }

    public async void FileSettings()
    {
        await View!.SettingsDialog();
    }

    public void FileExit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    public void ToolsSearchInCode()
    {
        View!.SearchInCodeOpen();
    }

    public async void ScriptsRunOtherScript()
    {
        var files = await View!.OpenFileDialog(new FilePickerOpenOptions()
        {
            Title = "Run script",
            FileTypeFilter = FilePickerFileTypes.CS,
        });

        if (files.Count != 1)
            return;

        string text;
        using (Stream stream = await files[0].OpenReadAsync())
        {
            using StreamReader streamReader = new(stream);
            text = streamReader.ReadToEnd();
        }

        string? filePath = files[0].TryGetLocalPath();
        await Scripting.RunScript(text, filePath);

        CommandTextBoxText = $"{Path.GetFileName(filePath) ?? "Script"} finished!";
    }

    void ClearProject()
    {
        Project = null;
    }

    void SetProject(ProjectContext projectContext)
    {
        Project = projectContext;
        Project.UnexportedAssetsChanged += (s, e) =>
        {
            // TODO: Change bottom bar.
        };
    }

    async Task<string?> AskProjectDestinationDataFile()
    {
        // Destination data file
        // TODO: Check if same as source and if empty directory
        IStorageFile? destinationDataFile = await View!.SaveFileDialog(new()
        {
            Title = "Select destination data file location",
            FileTypeChoices = FilePickerFileTypes.Data,
        });
        string? destinationDataPath = destinationDataFile?.TryGetLocalPath();

        return destinationDataPath;
    }

    public async void ProjectNew()
    {
        // TODO: Ask for source data file if nothing is opened
        if (Data is null || DataPath is null)
            return;

        if (!await AskProjectSave("There are assets marked to be exported in the current project. Save project before creating a new one?"))
            return;

        ClearProject();

        // Project name
        string? projectName = await View!.TextBoxDialog("Project name:", $"{Data.GeneralInfo?.DisplayName?.Content ?? "New"} Mod");
        if (projectName is null)
            return;

        // Project folder
        IReadOnlyList<IStorageFolder> projectFolderList = await View!.OpenFolderDialog(new() { Title = "Select project folder" });
        string? projectFolderPath = projectFolderList.ElementAtOrDefault(0)?.TryGetLocalPath();

        if (projectFolderPath is null)
            return;

        string projectFilePath = Path.Join(projectFolderPath, "project.json");

        // Destination data file
        string? destinationDataPath = await AskProjectDestinationDataFile();
        if (destinationDataPath is null)
            return;

        ProjectContext projectContext;
        try
        {
            projectContext = new(Data, DataPath, destinationDataPath, projectFilePath, projectName.Trim(), Dispatcher.UIThread.Invoke);
        }
        catch (ProjectException e)
        {
            await View!.MessageDialog($"Failed to create new project:\n{e.Message}");
            return;
        }
        catch (Exception e)
        {
            await View!.MessageDialog($"Error occurred when creating new project:\n{e}");
            return;
        }

        DataPath = destinationDataPath;
        SetProject(projectContext);
    }

    public async void ProjectOpen()
    {
        // TODO: Ask for source data file if nothing is opened
        if (Data is null || DataPath is null)
            return;

        if (!await AskProjectSave("There are assets marked to be exported in the current project. Save project before opening a new one?"))
            return;

        ClearProject();

        // Project file
        IReadOnlyList<IStorageFile> projectFileList = await View!.OpenFileDialog(new()
        {
            Title = "Select project.json file",
            FileTypeFilter = FilePickerFileTypes.JSON,
        });
        string? projectFilePath = projectFileList.ElementAtOrDefault(0)?.TryGetLocalPath();

        // Destination data file
        string? destinationDataPath = await AskProjectDestinationDataFile();
        if (destinationDataPath is null)
            return;

        ProjectContext projectContext;
        try
        {
            projectContext = ProjectContext.CreateWithDataFilePaths(DataPath, destinationDataPath, projectFilePath);
            projectContext.Import(Data, null, Dispatcher.UIThread.Invoke);
        }
        catch (ProjectException e)
        {
            await View!.MessageDialog($"Failed to load project:\n{e.Message}");
            return;
        }
        catch (Exception e)
        {
            await View!.MessageDialog($"Error occurred when loading project:\n{e}");
            return;
        }

        DataPath = destinationDataPath;
        SetProject(projectContext);
    }

    public async Task<bool> ProjectSave()
    {
        if (Project is null || Data is null || DataPath is null)
            return false;

        try
        {
            Project.Export(true);
            return true;
        }
        catch (ProjectException e)
        {
            await View!.MessageDialog($"Failed to save project:\n{e.Message}");
        }
        catch (Exception e)
        {
            await View!.MessageDialog($"Error occurred when saving project:\n{e}");
        }

        return false;
    }

    public async void ProjectViewUnexportedAssets()
    {
        if (Project is null || Data is null || DataPath is null)
            return;

        // TODO: Window
    }

    public async void ProjectClose()
    {
        if (!await AskProjectSave("There are assets marked to be exported in the current project. Save project before closing?"))
            return;

        ClearProject();
    }

    public async void HelpGitHub()
    {
        await View!.LaunchUriAsync(new Uri("https://github.com/UnderminersTeam/UndertaleModTool"));
    }

    public async void HelpAbout()
    {
        await View!.MessageDialog($"UndertaleModTool v{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?.?.?.?"} " +
            $"by the Underminers team\nLicensed under the GNU General Public License Version 3.", title: "About");
    }

    public void SetFilterText(string text)
    {
        FilterTextChanged?.Invoke(text);
    }

    public async void DataItemAdd(IList list)
    {
        if (Data is null || list is null)
            return;

        UndertaleResource res = UndertaleData.CreateResource(list);

        string? name = UndertaleData.GetDefaultResourceName(list);
        if (name is not null)
        {
            name = await View!.TextBoxDialog("Name of new asset:", name);
            if (name is null)
                return;

            static bool IsValidAssetIdentifier(string name)
            {
                if (string.IsNullOrEmpty(name))
                    return false;

                char firstChar = name[0];
                if (!char.IsAsciiLetter(firstChar) && firstChar != '_')
                    return false;

                foreach (char c in name.Skip(1))
                    if (!char.IsAsciiLetterOrDigit(c) && c != '_')
                        return false;

                return true;
            }

            if (!IsValidAssetIdentifier(name))
            {
                await View!.MessageDialog($"Asset name \"{name}\" is not a valid identifier. Only letters, digits and underscore allowed, and it must not start with a digit.");
                return;
            }
        }

        Data.InitializeResource(res, list, name);

        if (res is UndertaleRoom room)
        {
            if (await View!.MessageDialog("Add the new room to the end of the room order list?", buttons: MessageWindow.Buttons.YesNo) == MessageWindow.Result.Yes)
                Data.GeneralInfo?.RoomOrder.Add(new(room));
        }

        list.Add(res);

        if (Settings!.OpenNewResourceAfterCreatingIt)
        {
            TabOpen(res, inNewTab: true);
        }
    }

    public TabItemViewModel? TabOpen(object? item, bool inNewTab = false)
    {
        if (Data is null)
            return null;

        ITabContent? content = item switch
        {
            DescriptionViewModel vm => vm,
            "GeneralInfo" => new GeneralInfoViewModel(Data),
            "GlobalInitScripts" => new GlobalInitScriptsViewModel((Data.GlobalInitScripts as ObservableCollection<UndertaleGlobalInit>)!),
            "GameEndScripts" => new GameEndScriptsViewModel((Data.GameEndScripts as ObservableCollection<UndertaleGlobalInit>)!),
            UndertaleAudioGroup r => new UndertaleAudioGroupViewModel(r),
            UndertaleSound r => new UndertaleSoundViewModel(r),
            UndertaleSprite r => new UndertaleSpriteViewModel(r),
            UndertaleBackground r => new UndertaleBackgroundViewModel(r),
            UndertalePath r => new UndertalePathViewModel(r),
            UndertaleScript r => new UndertaleScriptViewModel(r),
            UndertaleShader r => new UndertaleShaderViewModel(r),
            UndertaleFont r => new UndertaleFontViewModel(r),
            UndertaleTimeline r => new UndertaleTimelineViewModel(r),
            UndertaleGameObject r => new UndertaleGameObjectViewModel(r),
            UndertaleRoom r => new UndertaleRoomViewModel(r),
            UndertaleExtension r => new UndertaleExtensionViewModel(r),
            UndertaleTexturePageItem r => new UndertaleTexturePageItemViewModel(r),
            UndertaleCode r => new UndertaleCodeViewModel(r),
            UndertaleVariable r => new UndertaleVariableViewModel(r),
            UndertaleFunction r => new UndertaleFunctionViewModel(r),
            UndertaleCodeLocals r => new UndertaleCodeLocalsViewModel(r),
            UndertaleString r => new UndertaleStringViewModel(r),
            UndertaleEmbeddedTexture r => new UndertaleEmbeddedTextureViewModel(r),
            UndertaleEmbeddedAudio r => new UndertaleEmbeddedAudioViewModel(r),
            UndertaleTextureGroupInfo r => new UndertaleTextureGroupInfoViewModel(r),
            UndertaleEmbeddedImage r => new UndertaleEmbeddedImageViewModel(r),
            UndertaleAnimationCurve r => new UndertaleAnimationCurveViewModel(r),
            UndertaleParticleSystem r => new UndertaleParticleSystemViewModel(r),
            UndertaleParticleSystemEmitter r => new UndertaleParticleSystemEmitterViewModel(r),
            _ => null,
        };

        if (content is not null)
        {
            if (!inNewTab && TabSelected is not null)
            {
                TabSelected.GoTo(content);
                return TabSelected;
            }
            else
            {
                TabItemViewModel tab = new(content);
                Tabs.Add(tab);
                TabSelected = tab;
                return tab;
            }
        }

        return null;
    }

    public void TabClose(TabItemViewModel tab)
    {
        var selected = TabSelected;
        var index = TabSelectedIndex;

        tab.Content.OnDetached();

        Tabs.Remove(tab);

        if (TabSelected != selected)
        {
            if (index >= Tabs.Count)
                index = Tabs.Count - 1;

            TabSelectedIndex = index;
        }
    }

    public void TabCloseSelected()
    {
        if (TabSelected is not null)
            TabClose(TabSelected);
    }

    public void TabCloseAll()
    {
        foreach (TabItemViewModel tab in Tabs.ToList())
        {
            TabClose(tab);
        }
    }

    public void TabSetToPrevious()
    {
        if (TabSelectedIndex > 0)
            TabSelectedIndex--;
        else
            TabSelectedIndex = Tabs.Count - 1;
    }

    public void TabSetToNext()
    {
        if (TabSelectedIndex < Tabs.Count - 1)
            TabSelectedIndex++;
        else
            TabSelectedIndex = 0;
    }

    public void TabGoBack()
    {
        TabSelected?.GoBack();
    }

    public void TabGoForward()
    {
        TabSelected?.GoForward();
    }

    private void OnTabSelectedChanged()
    {
        if (Data is not null && TabSelected?.Content is IUndertaleResourceViewModel vm)
        {
            TabSelectedResourceIdString = Data.IndexOf(vm.Resource).ToString();
        }
        else
        {
            TabSelectedResourceIdString = "None";
        }
    }
}