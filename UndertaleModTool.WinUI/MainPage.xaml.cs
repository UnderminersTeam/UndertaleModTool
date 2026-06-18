using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using ImageMagick;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using UndertaleModTool.Windows;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Project;
using UndertaleModLib.Scripting;
using UndertaleModLib.Util;
using Underanalyzer.Decompiler;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.Foundation;
using Windows.System;
using WinRT.Interop;

namespace UndertaleModTool_WinUI;

public sealed partial class MainPage : Page, IScriptInterface
{
    private enum GlobalToolsMode
    {
        None,
        Code,
        Strings
    }

    private static readonly Regex NewLineRegex = new(@"\r\n?|\n", RegexOptions.Compiled);
    private static readonly Lazy<IReadOnlyList<ResourceCategory>> NoDataCategories = new(BuildNoDataCategoriesCore);
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> DetailPropertiesByType = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> EditablePropertiesByType = new();
    private const string LastOpenedFilePathSetting = "LastOpenedFilePath";
    private const string RecentFilePathsSetting = "RecentFilePaths";
    private const int MaxRecentFileCount = 8;
    private const uint RoomTileIndexMask = 0x7ffff;
    private const uint RoomTileFlagsMask = ~RoomTileIndexMask;
    private const uint RoomTileFlipHorizontalFlag = 0x10000000;
    private const uint RoomTileFlipVerticalFlag = 0x20000000;
    private const uint RoomTileRotateFlag = 0x40000000;
    private const long TexturePreviewAutoRenderPixelLimit = 8_000_000;
    private const int PreviewSmokeTexturePageItemLimit = 32;
    private const int PreviewSmokeEmbeddedTextureLimit = 8;
    private const int PreviewSmokeRoomTilePaletteLimit = 96;
    private const long PreviewPngCacheByteLimit = 64L * 1024 * 1024;
    private const int PreviewPngCacheEntryLimit = 512;
    private const int PreviewTextureWorkerPageLimit = 16;
    private const int TexturePageItemReferenceDisplayLimit = 300;
    private const int RoomPreviewLegacyInstanceLimit = 900;
    private const int RoomPreviewTileLimit = 900;
    private const int RoomPreviewAssetSpriteLimit = 600;
    private const int RoomPreviewSequenceLimit = 300;
    private const int RoomPreviewParticleLimit = 300;
    private const string DefaultGameMakerStudioPath = "%appdata%\\GameMaker-Studio";
    private const string DefaultGameMakerStudio2RuntimesPath = "%ProgramData%\\GameMakerStudio2\\Cache\\runtimes";
    private readonly ScriptOptions _scriptOptions = ScriptingUtil.CreateDefaultScriptOptions();

    private UndertaleData? _data;
    private string? _currentFilePath;
    private ProjectContext? _project;
    private string? _lastOpenedFilePath;
    private List<string> _recentFilePaths = [];
    private IReadOnlyList<ResourceCategory> _categories = [];
    private ResourceCategory? _selectedCategory;
    private ResourceItem? _selectedResource;
    private ResourceCategory? _draggedResourceCategory;
    private ResourceItem? _draggedResourceItem;
    private readonly ObservableCollection<ResourceTab> _openResourceTabs = [];
    private readonly List<ClosedResourceTab> _closedResourceTabsHistory = [];
    private readonly List<ResourceNavigationEntry> _resourceNavigationHistory = [];
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _resourceFilterTimer;
    private ResourceCategory? _lastFilteredCategory;
    private string? _lastAppliedResourceFilter;
    private IReadOnlyList<ResourceItem>? _lastFilteredResourceItems;
    private bool _isDirty;
    private bool _isUpdatingOpenResourceTabs;
    private bool _isNavigatingResourceHistory;
    private int _resourceNavigationHistoryPosition = -1;
    private bool _isUpdatingResourceFilter;
    private bool _isUpdatingCodeSearch;
    private bool _isUpdatingCodeEditor;
    private bool _isUpdatingGlobalTools;
    private bool _isUpdatingProjectExportToggle;
    private bool _keepDetailsExpanded;
    private bool _keepScalarEditorExpanded;
    private bool _keepGlobalToolsExpanded;
    private GlobalToolsMode _selectedGlobalToolsMode = GlobalToolsMode.None;
    private bool _isUpdatingStringEditor;
    private bool _isUpdatingNamedResourceEditor;
    private bool _isUpdatingScalarEditor;
    private bool _isUpdatingSpriteFrame;
    private bool _isUpdatingSpriteViewMode;
    private bool _isSpritePreviewRendered;
    private SelectorBarItem? _lastSpriteEditorMode;
    private UndertaleSprite? _activeSprite;
    private bool _isUpdatingSpriteProperties;
    private bool _isUpdatingObjectEditor;
    private bool _isUpdatingBackgroundEditor;
    private bool _isUpdatingPathPointEditor;
    private bool _isUpdatingAudioGroupEditor;
    private bool _isUpdatingResourceReferenceEditor;
    private bool _isUpdatingGeneralInfoEditor;
    private bool _isUpdatingCodeLocalsEditor;
    private bool _isUpdatingTextureGroupEditor;
    private bool _isUpdatingFontGlyphEditor;
    private bool _isUpdatingFontKerningEditor;
    private bool _isUpdatingShaderEditor;
    private bool _isUpdatingTimelineEditor;
    private bool _isUpdatingExtensionEditor;
    private bool _isUpdatingParticleEmitterEditor;
    private bool _isUpdatingRoomPreviewOptions;
    private bool _isRoomPreviewRendered;
    private bool _isUpdatingRoomPreviewZoom;
    private bool _isUpdatingRoomInstanceEditor;
    private bool _isUpdatingRoomBackgroundEditor;
    private bool _isUpdatingRoomTileEditor;
    private bool _isUpdatingRoomViewEditor;
    private bool _isUpdatingRoomLayerEditor;
    private bool _isUpdatingTexturePreviewZoom;
    private bool _isImagePreviewDialogOpen;
    private bool _suppressNextTexturePreviewTap;
    private Point? _roomTileSourceDragOrigin;
    private uint _roomTileSourcePointerId;
    private bool _roomTileSourceDragHadChanges;
    private int _roomTileSourcePreviewGeneration;
    private bool _isTempRunningGame;
    private bool _isGeneratingOffsetMap;
    private bool _isFindingReferences;
    private bool _isProjectOperation;
    private bool _wasWarnedAboutTempRun;
    private UndertaleEmbeddedTexture? _activeTexturePreviewAtlas;
    private UndertaleTexturePageItem? _selectedTexturePreviewPageItem;
    private UndertaleTexturePageItem? _pendingTexturePreviewPageItemSelection;
    private CancellationTokenSource? _texturePreviewCts;
    private CancellationTokenSource? _spritePreviewCts;
    private CancellationTokenSource? _roomPreviewCts;
    private int _texturePreviewGeneration;
    private int _spritePreviewGeneration;
    private int _roomPreviewGeneration;
    private readonly object _previewCacheGate = new();
    private TextureWorker? _previewTextureWorker;
    private long _previewPngCacheBytes;
    private readonly Dictionary<UndertaleTexturePageItem, byte[]> _texturePageItemPreviewCache = new();
    private readonly Dictionary<UndertaleEmbeddedTexture, byte[]> _embeddedTexturePreviewCache = new();
    private readonly Dictionary<RoomPreviewTileKey, byte[]> _roomTilePreviewCache = new();
    private readonly Dictionary<int, ExternalAudioGroupCacheEntry> _externalAudioGroupCache = new();
    private readonly HashSet<UndertaleEmbeddedTexture> _previewTextureWorkerPages = [];
    private int _codeSearchMatchIndex = -1;
    private IReadOnlyList<int> _codeSearchMatches = [];
    private RoomPreviewDragState? _roomPreviewDragState;
    private RoomPreviewAssetDragState? _roomPreviewAssetDragState;
    private RoomPreviewViewDragState? _roomPreviewViewDragState;
    private readonly Stack<RoomPreviewUndoState> _roomPreviewUndoStack = new();
    private UndertaleRoom? _roomPreviewUndoRoom;
    private object? _copiedRoomItem;
    private Point? _lastRoomPreviewPointerPosition;
    private double _roomPreviewScale = 1;
    private PathPreviewDragState? _pathPreviewDragState;
    private string? _currentScriptPath;
    private bool _isRunningScript;
    private bool _scriptExecutionSuccess = true;
    private string _scriptErrorMessage = string.Empty;
    private string _scriptErrorType = string.Empty;
    private bool _finishedMessageEnabled = true;
    private readonly object _scriptProgressGate = new();
    private int _scriptProgressValue;
    private double _scriptProgressMaximum = 1;
    private string _scriptProgressMessage = "Script progress";
    private string _scriptProgressStatus = string.Empty;

    public MainPage()
    {
        InitializeComponent();
        CommandBox.AddHandler(
            UIElement.KeyDownEvent,
            new KeyEventHandler(CommandBox_KeyDown),
            true);
        WinUiToolSettings.EnsureLoaded();
        ApplySettingsToUi();
        string? settingsLoadError = WinUiToolSettings.LastLoadError;
        _recentFilePaths = ReadRecentFilePaths();
        _lastOpenedFilePath = _recentFilePaths.FirstOrDefault();
        _resourceFilterTimer = DispatcherQueue.CreateTimer();
        _resourceFilterTimer.Interval = TimeSpan.FromMilliseconds(120);
        _resourceFilterTimer.Tick += (_, _) =>
        {
            _resourceFilterTimer.Stop();
            ApplyResourceFilter();
        };
        InitializeToolbarButtonContent();
        DetailsExpander.RegisterPropertyChangedCallback(Expander.IsExpandedProperty, (_, _) =>
        {
            if (DetailsExpander.Visibility == Visibility.Visible)
                _keepDetailsExpanded = DetailsExpander.IsExpanded;
        });
        ScalarEditorPanel.RegisterPropertyChangedCallback(Expander.IsExpandedProperty, (_, _) =>
        {
            if (ScalarEditorPanel.Visibility == Visibility.Visible)
                _keepScalarEditorExpanded = ScalarEditorPanel.IsExpanded;
        });
        GlobalToolsExpander.RegisterPropertyChangedCallback(Expander.IsExpandedProperty, (_, _) =>
        {
            if (_isUpdatingGlobalTools || GlobalToolsExpander.Visibility != Visibility.Visible)
                return;

            if (GlobalToolsExpander.IsExpanded && _selectedGlobalToolsMode == GlobalToolsMode.None)
            {
                _selectedGlobalToolsMode = _selectedCategory?.Label switch
                {
                    "Code" => GlobalToolsMode.Code,
                    "Strings" => GlobalToolsMode.Strings,
                    _ => GlobalToolsMode.None
                };
                _keepGlobalToolsExpanded = _selectedGlobalToolsMode != GlobalToolsMode.None;
                UpdateGlobalToolsVisibility();
                return;
            }

            _keepGlobalToolsExpanded = GlobalToolsExpander.IsExpanded;
        });
        RegisterKeyboardAccelerators();
        OpenResourceTabsList.TabItemsSource = _openResourceTabs;
        UpdateCommandStates();
        UpdateGlobalToolsVisibility();
        SetNoDataBrowserState(isNoData: true);
        UpdateRecentFileUi();
        RefreshScriptsMenu();
        UpdateWindowTitle();
        if (settingsLoadError is not null)
            StatusBox.Text = $"Failed to load settings.json. Using default settings.{Environment.NewLine}{settingsLoadError}";
        Unloaded += (_, _) =>
        {
            SoundAudioPlayer.Source = null;
            EmbeddedAudioPlayer.Source = null;
            ClearPreviewCaches();
            _data?.Dispose();
        };
    }

    private void InitializeToolbarButtonContent()
    {
        SetIconButtonContent(ResourceBackButton, "\uE72B", "Back");
        SetIconButtonContent(ResourceForwardButton, "\uE72A", "Forward");
        SetToolbarButtonContent(OpenButton, "\uE8E5", "Open data file");
        SetSaveButtonContent("Save");
        SetSaveAsButtonContent("Save as");
    }

    private void SetSaveButtonContent(string text)
    {
        SetToolbarButtonContent(SaveButton, "\uE74E", text);
    }

    private void SetSaveAsButtonContent(string text)
    {
        SetToolbarButtonContent(SaveAsButton, "\uE792", text);
    }

    private static void SetToolbarButtonContent(Button button, string glyph, string text)
    {
        StackPanel content = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        content.Children.Add(new FontIcon
        {
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 14,
            Glyph = glyph
        });
        content.Children.Add(new TextBlock { Text = text, Foreground = button.Foreground });
        button.Content = content;
    }

    private static void SetIconButtonContent(Button button, string glyph, string toolTip)
    {
        button.Content = new FontIcon
        {
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 14,
            Glyph = glyph
        };
        ToolTipService.SetToolTip(button, toolTip);
    }

    private async void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        await PickAndOpenDataFileAsync();
    }

    private async void NewDataFileMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await CreateNewDataFileAsync();
    }

    private async System.Threading.Tasks.Task PickAndOpenDataFileAsync()
    {
        FileOpenPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add(".win");
        picker.FileTypeFilter.Add(".ios");
        picker.FileTypeFilter.Add(".unx");
        picker.FileTypeFilter.Add(".droid");

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        if (!await ConfirmReplacingCurrentDataAsync())
            return;

        await OpenDataFileAsync(file.Path);
    }

    private async System.Threading.Tasks.Task<bool> CreateNewDataFileAsync()
    {
        if (!await ConfirmReplacingCurrentDataAsync())
            return false;

        UndertaleData data;
        try
        {
            data = await System.Threading.Tasks.Task.Run(UndertaleData.CreateNew);
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Could not create a new data file:{Environment.NewLine}{ex}";
            return false;
        }

        CreateNewDataFileCore(data);
        return true;
    }

    private void CreateNewDataFileCore(UndertaleData data)
    {
        ApplySettingsToData(data);
        UnloadProject();
        _data?.Dispose();
        ClearExternalAudioGroupCache();
        _data = data;
        ClearPreviewCaches();
        _currentFilePath = null;
        _isDirty = true;
        _selectedResource = null;
        _openResourceTabs.Clear();
        _closedResourceTabsHistory.Clear();
        ResetResourceNavigationHistory();
        OpenResourceTabsList.Visibility = Visibility.Collapsed;
        _lastFilteredCategory = null;
        _lastAppliedResourceFilter = null;
        _lastFilteredResourceItems = null;
        SetResourceFilterText(string.Empty);
        SetNoDataBrowserState(isNoData: false);

        string gameName = FormatTitle(data.GeneralInfo?.Name?.Content ?? "New game");
        GameTitleText.Text = gameName;
        FilePathText.Text = "New unsaved data file";
        _categories = BuildCategories(data);
        CategoryList.ItemsSource = _categories;
        _selectedCategory = _categories.FirstOrDefault();
        CategoryList.SelectedItem = _selectedCategory;
        DetailsList.ItemsSource = null;
        DetailsTitleText.Text = "Details";
        UpdateGlobalToolsVisibility();
        ApplyResourceFilter(force: true);
        OpenFirstFilteredResourceOrShowCategory(addTab: false);

        SetSaveButtonContent("Save*");
        SetSaveAsButtonContent("Save as");
        SaveButton.IsEnabled = !data.UnsupportedBytecodeVersion;
        SaveAsButton.IsEnabled = !data.UnsupportedBytecodeVersion;
        CommandBox.IsEnabled = true;
        UpdateWindowTitle();
        UpdateResourceCommandButtons();
        UpdateCommandStates();
        StatusBox.Text = "Created new data file.";
    }

    public async System.Threading.Tasks.Task<bool> TryCloseAsync()
    {
        if (!WinUiToolSettings.Instance.WarnOnClose)
            return await ConfirmDiscardProjectAssetsAsync(
                "Project currently open",
                "There are assets marked to be exported in the current project. Discard those project changes?");

        return await ConfirmReplacingCurrentDataAsync();
    }

    private async void OpenRecentButton_Click(object sender, RoutedEventArgs e)
    {
        await OpenRecentPathAsync(_lastOpenedFilePath);
    }

    private async void OpenRecentMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: string path })
            await OpenRecentPathAsync(path);
    }

    private async void RecentFileCard_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (!OpenButton.IsEnabled)
            return;

        if (sender is FrameworkElement { Tag: string path })
        {
            e.Handled = true;
            await OpenRecentPathAsync(path);
        }
    }

    private void ClearRecentFilesButton_Click(object sender, RoutedEventArgs e)
    {
        _recentFilePaths.Clear();
        _lastOpenedFilePath = null;
        try
        {
            ApplicationData.Current.LocalSettings.Values.Remove(LastOpenedFilePathSetting);
            ApplicationData.Current.LocalSettings.Values.Remove(RecentFilePathsSetting);
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Could not clear recent files:{Environment.NewLine}{ex.Message}";
            return;
        }

        UpdateRecentFileUi();
        UpdateCommandStates();
        StatusBox.Text = "Recent files cleared.";
    }

    private async System.Threading.Tasks.Task OpenRecentPathAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusBox.Text = "No recent data file is available.";
            return;
        }

        if (!File.Exists(path))
        {
            StatusBox.Text = $"Recent data file was not found:{Environment.NewLine}{path}";
            _recentFilePaths = _recentFilePaths.Where(candidate => !string.Equals(candidate, path, StringComparison.OrdinalIgnoreCase))
                                               .ToList();
            _lastOpenedFilePath = _recentFilePaths.FirstOrDefault();
            SaveRecentFilePaths();
            UpdateRecentFileUi();
            return;
        }

        if (!await ConfirmReplacingCurrentDataAsync())
            return;

        await OpenDataFileAsync(path);
    }

    private void NoDataDropZone_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Open data file";
        }
    }

    private async void NoDataDropZone_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
            return;

        IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
        StorageFile? file = items.OfType<StorageFile>()
                                 .FirstOrDefault(IsSupportedDataFile);
        if (file is null)
        {
            StatusBox.Text = "Dropped file is not a supported GameMaker data file.";
            return;
        }

        if (!await ConfirmReplacingCurrentDataAsync())
            return;

        await OpenDataFileAsync(file.Path);
    }

    internal async System.Threading.Tasks.Task OpenInitialDataFileAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!IsSupportedDataFilePath(path))
        {
            StatusBox.Text = $"Startup file is not a supported GameMaker data file:{Environment.NewLine}{path}";
            return;
        }

        if (!File.Exists(path))
        {
            StatusBox.Text = $"Startup data file was not found:{Environment.NewLine}{path}";
            return;
        }

        await OpenDataFileAsync(path);
    }

    private async System.Threading.Tasks.Task OpenDataFileAsync(string path)
    {
        OpenButton.IsEnabled = false;
        SaveButton.IsEnabled = false;
        SaveAsButton.IsEnabled = false;
        UpdateCommandStates();
        CommandBox.IsEnabled = false;
        AddResourceButton.IsEnabled = false;
        DuplicateStringButton.IsEnabled = false;
        FilePathText.Text = path;
        GameTitleText.Text = "Loading...";
        StatusBox.Text = "Loading...";
        LoadProgressRing.Visibility = Visibility.Visible;
        LoadProgressRing.IsActive = true;
        CategoryList.ItemsSource = null;
        ResourceList.ItemsSource = null;
        GlobalCodeSearchResultsList.ItemsSource = null;
        GlobalStringSearchResultsList.ItemsSource = null;
        _openResourceTabs.Clear();
        _closedResourceTabsHistory.Clear();
        ResetResourceNavigationHistory();
        OpenResourceTabsList.Visibility = Visibility.Collapsed;
        _categories = [];
        _selectedCategory = null;
        UpdateGlobalToolsVisibility();
        DetailsList.ItemsSource = null;
        DetailsTitleText.Text = "Details";
        SetResourceFilterText(string.Empty);
        SetNoDataBrowserState(isNoData: false);
        HideEditors();

        try
        {
            LoadedGame loadedGame = await System.Threading.Tasks.Task.Run(() => LoadGame(path));
            UnloadProject();
            _data?.Dispose();
            ClearExternalAudioGroupCache();
            _data = loadedGame.Data;
            ClearPreviewCaches();
            _currentFilePath = path;
            _isDirty = false;
            _selectedResource = null;
            UpdateWindowTitle();

            GameTitleText.Text = loadedGame.GameName;
            _categories = loadedGame.Categories;
            CategoryList.ItemsSource = _categories;
            _selectedCategory = _categories.FirstOrDefault();
            CategoryList.SelectedItem = _selectedCategory;
            UpdateGlobalToolsVisibility();
            ApplyResourceFilter(force: true);
            OpenFirstFilteredResourceOrShowCategory(addTab: false);
            SetSaveButtonContent("Save");
            SaveButton.IsEnabled = false;
            SaveAsButton.IsEnabled = !loadedGame.Data.UnsupportedBytecodeVersion;
            CommandBox.IsEnabled = true;
            SetNoDataBrowserState(isNoData: false);
            UpdateResourceCommandButtons();
            UpdateCommandStates();
            StatusBox.Text = loadedGame.Status;
            RememberOpenedFile(path);
        }
        catch (Exception ex)
        {
            GameTitleText.Text = "Load failed";
            StatusBox.Text = ex.ToString();
            ClearLoadedDataState();
            SetNoDataBrowserState(isNoData: true);
            UpdateWindowTitle();
        }
        finally
        {
            LoadProgressRing.IsActive = false;
            LoadProgressRing.Visibility = Visibility.Collapsed;
            OpenButton.IsEnabled = true;
            UpdateCommandStates();
        }
    }

    private void ClearLoadedDataState()
    {
        UnloadProject();
        _data?.Dispose();
        ClearExternalAudioGroupCache();
        _data = null;
        ClearPreviewCaches();
        _currentFilePath = null;
        _isDirty = false;
        _categories = [];
        _selectedCategory = null;
        _selectedResource = null;
        _lastFilteredCategory = null;
        _lastAppliedResourceFilter = null;
        _lastFilteredResourceItems = null;
        _openResourceTabs.Clear();
        _closedResourceTabsHistory.Clear();
        ResetResourceNavigationHistory();

        FilePathText.Text = "Open a GameMaker data file to browse resources.";
        SetSaveButtonContent("Save");
        SetSaveAsButtonContent("Save as");
        SaveButton.IsEnabled = false;
        SaveAsButton.IsEnabled = false;
        CommandBox.IsEnabled = false;
        AddResourceButton.IsEnabled = false;
        DuplicateStringButton.IsEnabled = false;
        OpenResourceTabsList.Visibility = Visibility.Collapsed;
        DetailsTitleText.Text = "Details";
        DetailsList.ItemsSource = null;
        HideEditors();
        UpdateGlobalToolsVisibility();
        UpdateResourceCommandButtons();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isDirty || _data is null || _data.UnsupportedBytecodeVersion)
            return;

        await SaveCurrentFileAsync();
    }

    private async System.Threading.Tasks.Task<bool> SaveCurrentFileAsync()
    {
        if (!_isDirty || _data is null || _data.UnsupportedBytecodeVersion)
            return true;

        string? savePath = _project?.SaveDataPath ?? _currentFilePath;
        if (savePath is null)
            return await SaveCurrentFileAsAsync();

        if (_project is not null &&
            !await ShowConfirmationAsync(
                "Project save target",
                $"Save to the project's designated data file?{Environment.NewLine}{savePath}",
                "Save",
                "Cancel"))
        {
            return false;
        }

        OpenButton.IsEnabled = false;
        SaveButton.IsEnabled = false;
        SaveAsButton.IsEnabled = false;
        UpdateCommandStates();
        SetSaveButtonContent("Saving...");
        StatusBox.Text = "Saving...";

        try
        {
            string status = await System.Threading.Tasks.Task.Run(() => SaveGameWithProjectOptions(savePath, _data));
            _currentFilePath = savePath;
            FilePathText.Text = savePath;
            _isDirty = false;
            UpdateWindowTitle();
            SetSaveButtonContent("Save");
            SaveButton.IsEnabled = false;
            SaveAsButton.IsEnabled = !_data.UnsupportedBytecodeVersion;
            UpdateCommandStates();
            StatusBox.Text = status;
            RefreshCategoriesPreservingSelection();
            RememberOpenedFile(savePath);
            return true;
        }
        catch (Exception ex)
        {
            SetSaveButtonContent("Save");
            SaveButton.IsEnabled = true;
            SaveAsButton.IsEnabled = true;
            UpdateCommandStates();
            StatusBox.Text = ex.ToString();
            return false;
        }
        finally
        {
            OpenButton.IsEnabled = true;
            UpdateCommandStates();
        }
    }

    private async System.Threading.Tasks.Task<bool> ConfirmReplacingCurrentDataAsync()
    {
        if (_project is { HasUnexportedAssets: true } &&
            !await ShowConfirmationAsync(
                "Project currently open",
                "There are assets marked to be exported in the current project. Discard those project changes?",
                "Discard",
                "Cancel"))
        {
            return false;
        }

        if (!_isDirty || _data is null)
            return true;

        ContentDialog dialog = new()
        {
            Title = "Unsaved changes",
            Content = "Save the current data file before continuing?",
            SecondaryButtonText = "Discard",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };
        if (!_data.UnsupportedBytecodeVersion)
        {
            dialog.PrimaryButtonText = "Save";
            dialog.DefaultButton = ContentDialogButton.Primary;
        }

        ContentDialogResult result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
            return await SaveCurrentFileAsync();

        return result == ContentDialogResult.Secondary;
    }

    private async void SaveAsButton_Click(object sender, RoutedEventArgs e)
    {
        await SaveCurrentFileAsAsync();
    }

    private async void TempRunGameMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await TempRunGameAsync();
    }

    private async void RunWithOtherRunnerMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await RunWithOtherRunnerAsync();
    }

    private async void RunGmsDebuggerMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await RunUnderGmsDebuggerAsync();
    }

    private async void GenerateOffsetMapMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await GenerateOffsetMapAsync();
    }

    private async void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await ShowSettingsAsync();
    }

    private void CloseWindowMenuItem_Click(object sender, RoutedEventArgs e)
    {
        App.MainWindow?.Close();
    }

    private async void NewProjectMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await CreateNewProjectAsync();
    }

    private async void OpenProjectMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await OpenProjectAsync();
    }

    private async void SaveProjectMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await SaveProjectAsync();
    }

    private async void ViewProjectAssetsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await ShowProjectAssetsAsync();
    }

    private async void CloseProjectMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await CloseProjectAsync();
    }

    private void GitHubMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ScriptOpenURL("https://github.com/UnderminersTeam/UndertaleModTool");
    }

    private async void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new()
        {
            Title = "About UndertaleModTool.WinUI",
            Content = "UndertaleModTool WinUI sidecar\n\nA modern WinUI shell for browsing and editing GameMaker data files.",
            PrimaryButtonText = "OK",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async System.Threading.Tasks.Task ShowSettingsAsync()
    {
        WinUiToolSettings.EnsureLoaded();
        WinUiToolSettings settings = WinUiToolSettings.Instance;
        WinUiDecompilerSettings decompiler = settings.DecompilerSettings;

        StackPanel panel = new()
        {
            Spacing = 14,
            MinWidth = 560
        };

        TextBlock AddSectionTitle(string text)
        {
            TextBlock title = new()
            {
                Text = text,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            panel.Children.Add(title);
            return title;
        }

        TextBox AddTextBox(string header, string text)
        {
            TextBox box = new()
            {
                Header = header,
                Text = text,
                MinWidth = 520
            };
            panel.Children.Add(box);
            return box;
        }

        CheckBox AddCheckBox(Panel target, string text, bool value)
        {
            CheckBox checkBox = new()
            {
                Content = text,
                IsChecked = value
            };
            target.Children.Add(checkBox);
            return checkBox;
        }

        AddSectionTitle("Runtime");
        TextBox gms1PathBox = AddTextBox("GameMaker Studio 1.4 path", settings.GameMakerStudioPath);
        TextBox gms2RuntimesPathBox = AddTextBox("GameMaker Studio 2 runtimes path", settings.GameMakerStudio2RuntimesPath);
        CheckBox showDebuggerOptionBox = AddCheckBox(panel, "Show \"Run game under GMS debugger\"", settings.ShowDebuggerOption);

        AddSectionTitle("Behavior");
        CheckBox warnOnCloseBox = AddCheckBox(panel, "Warn before closing modified data", settings.WarnOnClose);
        CheckBox tempRunWarningBox = AddCheckBox(panel, "Warn before first temp run", settings.TempRunMessageShow);
        CheckBox assetOrderSwappingBox = AddCheckBox(panel, "Enable resource order swapping by dragging items", settings.AssetOrderSwappingEnabled);
        CheckBox automaticFileAssociationBox = AddCheckBox(panel, "Automatically associate GameMaker data files", settings.AutomaticFileAssociation);
        CheckBox rememberWindowPlacementsBox = AddCheckBox(panel, "Remember window placements", settings.RememberWindowPlacements);
        CheckBox showNullEntriesBox = AddCheckBox(panel, "Show null entries in resource tree", settings.ShowNullEntriesInResourceTree);
        CheckBox recompileProjectSourcesBox = AddCheckBox(panel, "Recompile all source GML when saving a project data file", settings.RecompileAllCodeSourcesOnProjectSave);
        CheckBox autoRenderPreviewsBox = AddCheckBox(panel, "Automatically render sprite and small texture previews", settings.AutoRenderPreviews);

        AddSectionTitle("Visuals");
        CheckBox enableDarkModeBox = AddCheckBox(panel, "Enable dark mode", settings.EnableDarkMode);
        TextBox transparencyGridColor1Box = AddTextBox("Transparency grid color 1", settings.TransparencyGridColor1);
        TextBox transparencyGridColor2Box = AddTextBox("Transparency grid color 2", settings.TransparencyGridColor2);

        AddSectionTitle("Room grid");
        CheckBox gridWidthEnabledBox = AddCheckBox(panel, "Override room grid width", settings.GridWidthEnabled);
        TextBox globalGridWidthBox = AddTextBox("Room grid width", settings.GlobalGridWidth.ToString(CultureInfo.InvariantCulture));
        CheckBox gridHeightEnabledBox = AddCheckBox(panel, "Override room grid height", settings.GridHeightEnabled);
        TextBox globalGridHeightBox = AddTextBox("Room grid height", settings.GlobalGridHeight.ToString(CultureInfo.InvariantCulture));
        CheckBox gridThicknessEnabledBox = AddCheckBox(panel, "Override room grid thickness", settings.GridThicknessEnabled);
        TextBox globalGridThicknessBox = AddTextBox("Room grid thickness", settings.GlobalGridThickness.ToString(CultureInfo.InvariantCulture));

        AddSectionTitle("GML decompiler");
        TextBox instanceIdPrefixBox = AddTextBox("Instance ID prefix", settings.InstanceIdPrefix);
        ComboBox indentStyleBox = new()
        {
            Header = "Indentation",
            MinWidth = 240
        };
        indentStyleBox.Items.Add(WinUiDecompilerSettings.IndentStyleKind.FourSpaces);
        indentStyleBox.Items.Add(WinUiDecompilerSettings.IndentStyleKind.TwoSpaces);
        indentStyleBox.Items.Add(WinUiDecompilerSettings.IndentStyleKind.Tabs);
        panel.Children.Add(indentStyleBox);

        StackPanel decompilerPanel = new()
        {
            Spacing = 8
        };
        CheckBox useCssColorsBox = AddCheckBox(decompilerPanel, "Use CSS color notation", decompiler.UseCSSColors);
        CheckBox useSemicolonBox = AddCheckBox(decompilerPanel, "Use semicolons", decompiler.UseSemicolon);
        CheckBox openBraceSameLineBox = AddCheckBox(decompilerPanel, "Open block brace on same line", decompiler.OpenBlockBraceOnSameLine);
        CheckBox removeSingleLineBlockBracesBox = AddCheckBox(decompilerPanel, "Remove single-line block braces", decompiler.RemoveSingleLineBlockBraces);
        CheckBox createEnumDeclarationsBox = AddCheckBox(decompilerPanel, "Create enum declarations", decompiler.CreateEnumDeclarations);
        CheckBox macroDeclarationsAtTopBox = AddCheckBox(decompilerPanel, "Put macro declarations at top", decompiler.MacroDeclarationsAtTop);
        CheckBox printWarningsBox = AddCheckBox(decompilerPanel, "Print decompiler warnings", decompiler.PrintWarnings);
        CheckBox allowLeftoverDataBox = AddCheckBox(decompilerPanel, "Allow leftover data on stack", decompiler.AllowLeftoverDataOnStack);

        Expander formattingExpander = new()
        {
            Header = "Formatting",
            Content = decompilerPanel,
            IsExpanded = true
        };
        panel.Children.Add(formattingExpander);

        StackPanel emptyLinePanel = new()
        {
            Spacing = 8
        };
        CheckBox emptyLineAroundBranchBox = AddCheckBox(emptyLinePanel, "Empty line around branch statements", decompiler.EmptyLineAroundBranchStatements);
        CheckBox emptyLineAfterBlockLocalsBox = AddCheckBox(emptyLinePanel, "Empty line after block locals", decompiler.EmptyLineAfterBlockLocals);
        CheckBox emptyLineBeforeSwitchCasesBox = AddCheckBox(emptyLinePanel, "Empty line before switch cases", decompiler.EmptyLineBeforeSwitchCases);
        CheckBox emptyLineAfterSwitchCasesBox = AddCheckBox(emptyLinePanel, "Empty line after switch cases", decompiler.EmptyLineAfterSwitchCases);
        CheckBox emptyLineAroundFunctionDeclarationsBox = AddCheckBox(emptyLinePanel, "Empty line around function declarations", decompiler.EmptyLineAroundFunctionDeclarations);
        CheckBox emptyLineAroundStaticInitializationBox = AddCheckBox(emptyLinePanel, "Empty line around static initialization", decompiler.EmptyLineAroundStaticInitialization);

        panel.Children.Add(new Expander
        {
            Header = "Empty lines",
            Content = emptyLinePanel
        });

        StackPanel cleanupPanel = new()
        {
            Spacing = 8
        };
        CheckBox cleanupTryBox = AddCheckBox(cleanupPanel, "Cleanup try/catch/finally", decompiler.CleanupTry);
        CheckBox cleanupElseToContinueBox = AddCheckBox(cleanupPanel, "Cleanup else to continue", decompiler.CleanupElseToContinue);
        CheckBox cleanupDefaultArgumentsBox = AddCheckBox(cleanupPanel, "Cleanup default argument values", decompiler.CleanupDefaultArgumentValues);
        CheckBox cleanupBuiltinArrayVariablesBox = AddCheckBox(cleanupPanel, "Cleanup builtin array variables", decompiler.CleanupBuiltinArrayVariables);
        CheckBox cleanupLocalVarDeclarationsBox = AddCheckBox(cleanupPanel, "Cleanup local variable declarations", decompiler.CleanupLocalVarDeclarations);

        panel.Children.Add(new Expander
        {
            Header = "Cleanup",
            Content = cleanupPanel
        });

        Button restoreGmlDefaultsButton = new()
        {
            Content = "Restore GML defaults",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        panel.Children.Add(restoreGmlDefaultsButton);

        Button openAppDataButton = new()
        {
            Content = "Open app data folder",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        openAppDataButton.Click += (_, _) => OpenShellTarget(WinUiToolSettings.AppDataFolder);
        panel.Children.Add(openAppDataButton);

        void LoadDecompilerControls(WinUiDecompilerSettings source)
        {
            indentStyleBox.SelectedItem = source.IndentStyle;
            useCssColorsBox.IsChecked = source.UseCSSColors;
            useSemicolonBox.IsChecked = source.UseSemicolon;
            openBraceSameLineBox.IsChecked = source.OpenBlockBraceOnSameLine;
            removeSingleLineBlockBracesBox.IsChecked = source.RemoveSingleLineBlockBraces;
            createEnumDeclarationsBox.IsChecked = source.CreateEnumDeclarations;
            macroDeclarationsAtTopBox.IsChecked = source.MacroDeclarationsAtTop;
            printWarningsBox.IsChecked = source.PrintWarnings;
            allowLeftoverDataBox.IsChecked = source.AllowLeftoverDataOnStack;
            emptyLineAroundBranchBox.IsChecked = source.EmptyLineAroundBranchStatements;
            emptyLineAfterBlockLocalsBox.IsChecked = source.EmptyLineAfterBlockLocals;
            emptyLineBeforeSwitchCasesBox.IsChecked = source.EmptyLineBeforeSwitchCases;
            emptyLineAfterSwitchCasesBox.IsChecked = source.EmptyLineAfterSwitchCases;
            emptyLineAroundFunctionDeclarationsBox.IsChecked = source.EmptyLineAroundFunctionDeclarations;
            emptyLineAroundStaticInitializationBox.IsChecked = source.EmptyLineAroundStaticInitialization;
            cleanupTryBox.IsChecked = source.CleanupTry;
            cleanupElseToContinueBox.IsChecked = source.CleanupElseToContinue;
            cleanupDefaultArgumentsBox.IsChecked = source.CleanupDefaultArgumentValues;
            cleanupBuiltinArrayVariablesBox.IsChecked = source.CleanupBuiltinArrayVariables;
            cleanupLocalVarDeclarationsBox.IsChecked = source.CleanupLocalVarDeclarations;
        }

        LoadDecompilerControls(decompiler);
        restoreGmlDefaultsButton.Click += (_, _) =>
        {
            WinUiDecompilerSettings restored = new();
            LoadDecompilerControls(restored);
            instanceIdPrefixBox.Text = "inst_";
        };

        ScrollViewer viewer = new()
        {
            Content = panel,
            MaxHeight = 650,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        ContentDialog dialog = new()
        {
            Title = "Settings",
            Content = viewer,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        bool oldShowNullEntries = settings.ShowNullEntriesInResourceTree;
        bool oldAutoRenderPreviews = settings.AutoRenderPreviews;

        settings.GameMakerStudioPath = string.IsNullOrWhiteSpace(gms1PathBox.Text)
            ? DefaultGameMakerStudioPath
            : gms1PathBox.Text.Trim();
        settings.GameMakerStudio2RuntimesPath = string.IsNullOrWhiteSpace(gms2RuntimesPathBox.Text)
            ? DefaultGameMakerStudio2RuntimesPath
            : gms2RuntimesPathBox.Text.Trim();
        settings.ShowDebuggerOption = showDebuggerOptionBox.IsChecked == true;
        settings.WarnOnClose = warnOnCloseBox.IsChecked == true;
        settings.TempRunMessageShow = tempRunWarningBox.IsChecked == true;
        settings.AssetOrderSwappingEnabled = assetOrderSwappingBox.IsChecked == true;
        settings.AutomaticFileAssociation = automaticFileAssociationBox.IsChecked == true;
        settings.RememberWindowPlacements = rememberWindowPlacementsBox.IsChecked == true;
        settings.ShowNullEntriesInResourceTree = showNullEntriesBox.IsChecked == true;
        settings.RecompileAllCodeSourcesOnProjectSave = recompileProjectSourcesBox.IsChecked == true;
        settings.AutoRenderPreviews = autoRenderPreviewsBox.IsChecked == true;
        settings.EnableDarkMode = enableDarkModeBox.IsChecked == true;
        settings.TransparencyGridColor1 = string.IsNullOrWhiteSpace(transparencyGridColor1Box.Text)
            ? "#FF666666"
            : transparencyGridColor1Box.Text.Trim();
        settings.TransparencyGridColor2 = string.IsNullOrWhiteSpace(transparencyGridColor2Box.Text)
            ? "#FF999999"
            : transparencyGridColor2Box.Text.Trim();
        settings.GridWidthEnabled = gridWidthEnabledBox.IsChecked == true;
        settings.GlobalGridWidth = ParseNonNegativeSettingDouble(globalGridWidthBox.Text, settings.GlobalGridWidth);
        settings.GridHeightEnabled = gridHeightEnabledBox.IsChecked == true;
        settings.GlobalGridHeight = ParseNonNegativeSettingDouble(globalGridHeightBox.Text, settings.GlobalGridHeight);
        settings.GridThicknessEnabled = gridThicknessEnabledBox.IsChecked == true;
        settings.GlobalGridThickness = ParseNonNegativeSettingDouble(globalGridThicknessBox.Text, settings.GlobalGridThickness);
        settings.InstanceIdPrefix = string.IsNullOrWhiteSpace(instanceIdPrefixBox.Text)
            ? "inst_"
            : instanceIdPrefixBox.Text.Trim();

        decompiler.IndentStyle = indentStyleBox.SelectedItem is WinUiDecompilerSettings.IndentStyleKind indentStyle
            ? indentStyle
            : WinUiDecompilerSettings.IndentStyleKind.FourSpaces;
        decompiler.UseCSSColors = useCssColorsBox.IsChecked == true;
        decompiler.UseSemicolon = useSemicolonBox.IsChecked == true;
        decompiler.OpenBlockBraceOnSameLine = openBraceSameLineBox.IsChecked == true;
        decompiler.RemoveSingleLineBlockBraces = removeSingleLineBlockBracesBox.IsChecked == true;
        decompiler.CreateEnumDeclarations = createEnumDeclarationsBox.IsChecked == true;
        decompiler.MacroDeclarationsAtTop = macroDeclarationsAtTopBox.IsChecked == true;
        decompiler.PrintWarnings = printWarningsBox.IsChecked == true;
        decompiler.AllowLeftoverDataOnStack = allowLeftoverDataBox.IsChecked == true;
        decompiler.EmptyLineAroundBranchStatements = emptyLineAroundBranchBox.IsChecked == true;
        decompiler.EmptyLineAfterBlockLocals = emptyLineAfterBlockLocalsBox.IsChecked == true;
        decompiler.EmptyLineBeforeSwitchCases = emptyLineBeforeSwitchCasesBox.IsChecked == true;
        decompiler.EmptyLineAfterSwitchCases = emptyLineAfterSwitchCasesBox.IsChecked == true;
        decompiler.EmptyLineAroundFunctionDeclarations = emptyLineAroundFunctionDeclarationsBox.IsChecked == true;
        decompiler.EmptyLineAroundStaticInitialization = emptyLineAroundStaticInitializationBox.IsChecked == true;
        decompiler.CleanupTry = cleanupTryBox.IsChecked == true;
        decompiler.CleanupElseToContinue = cleanupElseToContinueBox.IsChecked == true;
        decompiler.CleanupDefaultArgumentValues = cleanupDefaultArgumentsBox.IsChecked == true;
        decompiler.CleanupBuiltinArrayVariables = cleanupBuiltinArrayVariablesBox.IsChecked == true;
        decompiler.CleanupLocalVarDeclarations = cleanupLocalVarDeclarationsBox.IsChecked == true;

        if (!WinUiToolSettings.TrySave(out string? saveError))
        {
            StatusBox.Text = $"Failed to save settings.json:{Environment.NewLine}{saveError}";
            return;
        }

        ApplySettingsToUi();
        if (_data is not null)
            ApplySettingsToData(_data);
        if (_data is not null && oldShowNullEntries != settings.ShowNullEntriesInResourceTree)
            RefreshCategoriesPreservingSelection();
        if (_data is not null && !oldAutoRenderPreviews && settings.AutoRenderPreviews)
            RenderCurrentPreviewIfAutoEnabled();
        StatusBox.Text = "Settings saved.";
    }

    private static double ParseNonNegativeSettingDouble(string text, double fallback)
    {
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ||
            double.IsNaN(value) ||
            double.IsInfinity(value))
        {
            return fallback;
        }

        return Math.Max(0, value);
    }

    private static bool ShouldAutoRenderPreviews()
    {
        WinUiToolSettings.EnsureLoaded();
        return WinUiToolSettings.Instance.AutoRenderPreviews;
    }

    private void RenderCurrentPreviewIfAutoEnabled()
    {
        if (!ShouldAutoRenderPreviews() || _selectedResource is null)
            return;

        if (_selectedResource.Value is UndertaleSprite sprite &&
            CustomSpriteEditorPanel.Visibility == Visibility.Visible)
        {
            RenderOrResetSpritePreview(sprite);
            return;
        }

        if (TryGetPreviewableTextureValue(_selectedResource.Value, out object? textureValue) && textureValue is not null)
            ShowTexturePreviewFor(_selectedResource);
    }

    private void RenderOrResetSpritePreview(UndertaleSprite sprite)
    {
        if (_isSpritePreviewRendered || ShouldAutoRenderPreviews())
            _ = RefreshSpritePreviewAsync();
        else
            ResetSpritePreviewSurface(sprite);
    }

    private void OpenScriptsFolderMenuItem_Click(object sender, RoutedEventArgs e)
    {
        string? scriptsRoot = GetScriptsRoot();
        if (scriptsRoot is null)
        {
            StatusBox.Text = "Scripts folder was not found.";
            return;
        }

        OpenShellTarget(scriptsRoot);
    }

    private async void OpenOtherScriptMenuItem_Click(object sender, RoutedEventArgs e)
    {
        FileOpenPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add(".csx");

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        await RunScriptAsync(file.Path);
    }

    private async void BuiltInScriptMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: string path })
            await RunScriptAsync(path);
    }

    private async void CommandBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
        {
            return;
        }

        e.Handled = true;
        if (IsShiftKeyDown())
        {
            if (sender is TextBox textBox)
                InsertCommandBoxNewLine(textBox);
            return;
        }

        await RunCommandBoxAsync();
    }

    private void CommandBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateCommandStates();
    }

    private async void CommandRunButton_Click(object sender, RoutedEventArgs e)
    {
        await RunCommandBoxAsync();
    }

    private static bool IsShiftKeyDown()
    {
        return IsVirtualKeyDown(VirtualKey.LeftShift) ||
               IsVirtualKeyDown(VirtualKey.RightShift);
    }

    private static void InsertCommandBoxNewLine(TextBox textBox)
    {
        int textLength = textBox.Text.Length;
        int selectionStart = Math.Clamp(textBox.SelectionStart, 0, textLength);
        int selectionLength = Math.Clamp(textBox.SelectionLength, 0, textLength - selectionStart);
        textBox.Text = textBox.Text.Remove(selectionStart, selectionLength).Insert(selectionStart, Environment.NewLine);
        textBox.SelectionStart = selectionStart + Environment.NewLine.Length;
        textBox.SelectionLength = 0;
    }

    private static string FormatCommandResult(object? result)
    {
        if (result is null)
            return "Command finished.";

        string value = result.ToString() ?? string.Empty;
        if (value.Length == 0)
            return "Command result: (empty string)";

        value = NewLineRegex.Replace(value, " ");
        if (value.Length > 512)
            value = value[..512] + "...";

        return $"Command result: {value}";
    }

    private async System.Threading.Tasks.Task RunCommandBoxAsync()
    {
        string commandText = CommandBox.Text;
        if (string.IsNullOrWhiteSpace(commandText))
            return;

        if (_isRunningScript)
        {
            StatusBox.Text = "Another script is already running.";
            return;
        }

        _isRunningScript = true;
        _scriptExecutionSuccess = true;
        _scriptErrorMessage = string.Empty;
        _scriptErrorType = string.Empty;
        string previousScriptPath = _currentScriptPath ?? string.Empty;
        _currentScriptPath = string.Empty;
        CommandBox.IsEnabled = false;
        StatusBox.Text = "Running command...";
        UpdateCommandStates();

        try
        {
            object? result = await CSharpScript.EvaluateAsync(
                commandText,
                _scriptOptions,
                this,
                typeof(IScriptInterface));

            if (_finishedMessageEnabled)
            {
                StatusBox.Text = FormatCommandResult(result);
            }
            else
            {
                _finishedMessageEnabled = true;
            }

        }
        catch (CompilationErrorException ex)
        {
            _scriptExecutionSuccess = false;
            _scriptErrorMessage = ex.Message;
            _scriptErrorType = nameof(CompilationErrorException);
            StatusBox.Text = $"Command compile error:{Environment.NewLine}{ex.Message}";
        }
        catch (Exception ex)
        {
            _scriptExecutionSuccess = false;
            _scriptErrorMessage = ex.Message;
            _scriptErrorType = ex.GetType().Name;
            string pretty = ScriptingUtil.PrettifyException(in ex);
            StatusBox.Text = $"Command error:{Environment.NewLine}{pretty}";
        }
        finally
        {
            _currentScriptPath = previousScriptPath;
            _isRunningScript = false;
            HideProgressBar();
            CommandBox.IsEnabled = _data is not null;
            UpdateCommandStates();
        }
    }

    private async System.Threading.Tasks.Task RunScriptAsync(string path)
    {
        if (!File.Exists(path))
        {
            StatusBox.Text = $"Script file was not found:{Environment.NewLine}{path}";
            RefreshScriptsMenu();
            return;
        }

        if (_isRunningScript)
        {
            StatusBox.Text = "Another script is already running.";
            return;
        }

        _isRunningScript = true;
        _scriptExecutionSuccess = true;
        _scriptErrorMessage = string.Empty;
        _scriptErrorType = string.Empty;
        _currentScriptPath = path;
        StatusBox.Text = $"Running {Path.GetFileName(path)}...";
        UpdateCommandStates();

        try
        {
            object? result = await EvaluateScriptFileAsync(path);

            if (_finishedMessageEnabled)
            {
                StatusBox.Text = result is null
                    ? $"{Path.GetFileName(path)} finished."
                    : result.ToString();
            }
            else
            {
                _finishedMessageEnabled = true;
            }

        }
        catch (CompilationErrorException ex)
        {
            _scriptExecutionSuccess = false;
            _scriptErrorMessage = ex.Message;
            _scriptErrorType = nameof(CompilationErrorException);
            StatusBox.Text = $"Script compile error:{Environment.NewLine}{ex.Message}";
        }
        catch (Exception ex)
        {
            _scriptExecutionSuccess = false;
            _scriptErrorMessage = ex.Message;
            _scriptErrorType = ex.GetType().Name;
            StatusBox.Text = $"Script error:{Environment.NewLine}{ScriptingUtil.PrettifyException(in ex)}";
        }
        finally
        {
            _isRunningScript = false;
            HideProgressBar();
            UpdateCommandStates();
        }
    }

    private async System.Threading.Tasks.Task<object?> EvaluateScriptFileAsync(string path)
    {
        string previousScriptPath = _currentScriptPath ?? string.Empty;
        _currentScriptPath = path;
        try
        {
            string scriptText = $"#line 1 \"{path}\"{Environment.NewLine}" + File.ReadAllText(path, Encoding.UTF8);
            return await CSharpScript.EvaluateAsync(
                scriptText,
                _scriptOptions.WithFilePath(path).WithFileEncoding(Encoding.UTF8),
                this,
                typeof(IScriptInterface));
        }
        finally
        {
            _currentScriptPath = previousScriptPath;
        }
    }

    private async System.Threading.Tasks.Task<bool> SaveCurrentFileAsAsync()
    {
        if (_data is null || _data.UnsupportedBytecodeVersion)
            return false;

        FileSavePicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = _currentFilePath is null
                ? "data.win"
                : Path.GetFileName(_currentFilePath)
        };
        picker.FileTypeChoices.Add("GameMaker data file", [".win"]);
        picker.FileTypeChoices.Add("iOS data file", [".ios"]);
        picker.FileTypeChoices.Add("Unix data file", [".unx"]);
        picker.FileTypeChoices.Add("Android data file", [".droid"]);

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFile? file = await picker.PickSaveFileAsync();
        if (file is null)
            return false;

        OpenButton.IsEnabled = false;
        SaveButton.IsEnabled = false;
        SaveAsButton.IsEnabled = false;
        UpdateCommandStates();
        SetSaveAsButtonContent("Saving...");
        StatusBox.Text = "Saving...";

        try
        {
            string status = await System.Threading.Tasks.Task.Run(() => SaveGameWithProjectOptions(file.Path, _data));
            _currentFilePath = file.Path;
            FilePathText.Text = file.Path;
            _isDirty = false;
            UpdateWindowTitle();
            SetSaveButtonContent("Save");
            SaveButton.IsEnabled = false;
            SetSaveAsButtonContent("Save as");
            SaveAsButton.IsEnabled = true;
            UpdateCommandStates();
            StatusBox.Text = status;
            RefreshCategoriesPreservingSelection();
            RememberOpenedFile(file.Path);
            return true;
        }
        catch (Exception ex)
        {
            SetSaveAsButtonContent("Save as");
            SaveButton.IsEnabled = _isDirty;
            SaveAsButton.IsEnabled = true;
            UpdateCommandStates();
            StatusBox.Text = ex.ToString();
            return false;
        }
        finally
        {
            OpenButton.IsEnabled = true;
            UpdateCommandStates();
        }
    }

    private async System.Threading.Tasks.Task TempRunGameAsync()
    {
        if (_isTempRunningGame)
            return;

        if (_data is null || _currentFilePath is null)
        {
            StatusBox.Text = "Nothing to run.";
            return;
        }

        if (_data.UnsupportedBytecodeVersion)
        {
            StatusBox.Text = "Cannot run because this data file uses an unsupported bytecode version.";
            return;
        }

        UndertaleGeneralInfo? generalInfo = _data.GeneralInfo;
        if (generalInfo is null)
        {
            StatusBox.Text = "General info is missing.";
            return;
        }

        string? gameExeName = generalInfo.FileName?.Content;
        if (string.IsNullOrWhiteSpace(gameExeName))
        {
            StatusBox.Text = "Null game executable name or location.";
            return;
        }

        if (_project is not null)
        {
            string projectSaveDataFilePath = _project.SaveDataPath;
            string? projectGameExePath = Paths.TryJoinVerifyWithinDirectory(
                Path.GetDirectoryName(projectSaveDataFilePath),
                $"{gameExeName}.exe");
            if (projectGameExePath is null)
            {
                StatusBox.Text = "Failed to find valid game executable path; escaped directory.";
                return;
            }
            if (!File.Exists(projectGameExePath))
            {
                StatusBox.Text = $"Cannot find game executable path, expected to find it at:{Environment.NewLine}{projectGameExePath}";
                return;
            }

            _isTempRunningGame = true;
            UpdateCommandStates();
            try
            {
                StatusBox.Text = "Writing project run data...";
                string saveStatus = await System.Threading.Tasks.Task.Run(() => SaveGameWithProjectOptions(projectSaveDataFilePath, _data));
                ProcessStartInfo startInfo = new(projectGameExePath);
                startInfo.ArgumentList.Add("-game");
                startInfo.ArgumentList.Add(projectSaveDataFilePath);
                Process.Start(startInfo);
                StatusBox.Text = $"Started {Path.GetFileName(projectGameExePath)} using {Path.GetFileName(projectSaveDataFilePath)}.{Environment.NewLine}{saveStatus}";
            }
            catch (Exception ex)
            {
                StatusBox.Text = $"Project run failed:{Environment.NewLine}{ex}";
            }
            finally
            {
                _isTempRunningGame = false;
                UpdateCommandStates();
            }

            return;
        }

        string? gameDirectory = Path.GetDirectoryName(_currentFilePath);
        string? gameExePath = Paths.TryJoinVerifyWithinDirectory(gameDirectory, $"{gameExeName}.exe");
        if (gameExePath is null)
        {
            StatusBox.Text = "Failed to find valid game executable path; escaped directory.";
            return;
        }

        if (!File.Exists(gameExePath))
        {
            StatusBox.Text = $"Cannot find game executable path, expected to find it at:{Environment.NewLine}{gameExePath}";
            return;
        }

        string? saveDataFilePath = Paths.TryJoinVerifyWithinDirectory(gameDirectory, "mod_temprun.temp");
        if (saveDataFilePath is null)
        {
            StatusBox.Text = "Failed to find valid temp-run data path; escaped directory.";
            return;
        }

        if (WinUiToolSettings.Instance.TempRunMessageShow && !_wasWarnedAboutTempRun)
        {
            StatusBox.Text =
                "Temp running does not permanently save changes. Use Save to keep changes in the data file.";
            _wasWarnedAboutTempRun = true;
        }

        bool oldDisableDebuggerState = generalInfo.IsDebuggerDisabled;
        int oldSteamValue = generalInfo.SteamAppID;
        _isTempRunningGame = true;
        TempRunGameMenuItem.IsEnabled = false;
        UpdateCommandStates();

        try
        {
            generalInfo.SteamAppID = 0;
            generalInfo.IsDebuggerDisabled = true;
            StatusBox.Text = "Writing temp-run data...";

            string saveStatus = await System.Threading.Tasks.Task.Run(() => SaveGame(saveDataFilePath, _data));
            if (!File.Exists(saveDataFilePath))
            {
                StatusBox.Text = $"Cannot find temp-run data path, expected to find it at:{Environment.NewLine}{saveDataFilePath}";
                return;
            }

            ProcessStartInfo startInfo = new(gameExePath);
            startInfo.ArgumentList.Add("-game");
            startInfo.ArgumentList.Add(saveDataFilePath);
            Process.Start(startInfo);
            StatusBox.Text = $"Started {Path.GetFileName(gameExePath)} using {Path.GetFileName(saveDataFilePath)}.{Environment.NewLine}{saveStatus}";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Temp run failed:{Environment.NewLine}{ex}";
        }
        finally
        {
            generalInfo.SteamAppID = oldSteamValue;
            generalInfo.IsDebuggerDisabled = oldDisableDebuggerState;
            _isTempRunningGame = false;
            UpdateCommandStates();
        }
    }

    private async System.Threading.Tasks.Task RunWithOtherRunnerAsync()
    {
        if (_data is null || _currentFilePath is null || _data.UnsupportedBytecodeVersion)
        {
            StatusBox.Text = "Nothing runnable is loaded.";
            return;
        }

        UndertaleGeneralInfo? generalInfo = _data.GeneralInfo;
        if (generalInfo is null)
        {
            StatusBox.Text = "General info is missing.";
            return;
        }

        string runDataFilePath = GetRunnableDataFilePath();
        bool saveOk = true;
        bool oldDisableDebuggerState = generalInfo.IsDebuggerDisabled;

        if (!generalInfo.IsDebuggerDisabled)
        {
            if (!await ShowConfirmationAsync(
                    "Debugger enabled",
                    "The game has the debugger enabled. Disable it and save so the game can run with a normal runner?",
                    "Disable and save",
                    "Cancel"))
            {
                StatusBox.Text = "Use the GMS debugger run option for this data file.";
                return;
            }

            generalInfo.IsDebuggerDisabled = true;
            MarkDirty(markProjectAsset: false);
            saveOk = await SaveCurrentFileAsync();
            if (!saveOk)
            {
                generalInfo.IsDebuggerDisabled = oldDisableDebuggerState;
                StatusBox.Text = "You must save your changes to run.";
                return;
            }
        }
        else if (_isDirty &&
                 await ShowConfirmationAsync("Save before run", "Save changes before running?", "Save", "Run without saving"))
        {
            saveOk = await SaveCurrentFileAsync();
        }

        if (!saveOk)
            return;

        runDataFilePath = GetRunnableDataFilePath();
        RuntimeCandidate? runtime = await PickRuntimeAsync(runDataFilePath, _data, requireDebugger: false);
        if (runtime is null)
            return;

        try
        {
            ProcessStartInfo startInfo = new(runtime.Path);
            startInfo.ArgumentList.Add("-game");
            startInfo.ArgumentList.Add(runDataFilePath);
            startInfo.ArgumentList.Add("-debugoutput");
            startInfo.ArgumentList.Add(Path.ChangeExtension(runDataFilePath, ".gamelog.txt"));
            Process.Start(startInfo);
            StatusBox.Text = $"Started {runtime.Version} using {Path.GetFileName(runDataFilePath)}.";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Run failed:{Environment.NewLine}{ex}";
        }
    }

    private async System.Threading.Tasks.Task RunUnderGmsDebuggerAsync()
    {
        if (_data is null || _currentFilePath is null || _data.UnsupportedBytecodeVersion)
        {
            StatusBox.Text = "Nothing runnable is loaded.";
            return;
        }

        UndertaleGeneralInfo? generalInfo = _data.GeneralInfo;
        if (generalInfo is null)
        {
            StatusBox.Text = "General info is missing.";
            return;
        }

        if (!await ShowConfirmationAsync(
                "Run under GMS debugger",
                "Run the game with the GameMaker Studio debugger? For in-game debug modes, scripts are usually the better option.",
                "Run",
                "Cancel"))
        {
            return;
        }

        bool oldDisableDebuggerState = generalInfo.IsDebuggerDisabled;
        generalInfo.IsDebuggerDisabled = false;
        MarkDirty(markProjectAsset: false);

        bool saveOk = await SaveCurrentFileAsync();
        if (!saveOk)
        {
            generalInfo.IsDebuggerDisabled = oldDisableDebuggerState;
            StatusBox.Text = "You must save your changes to run under the debugger.";
            return;
        }

        string runDataFilePath = GetRunnableDataFilePath();
        RuntimeCandidate? runtime = await PickRuntimeAsync(runDataFilePath, _data, requireDebugger: true);
        generalInfo.IsDebuggerDisabled = oldDisableDebuggerState;
        if (runtime is null)
            return;

        if (runtime.DebuggerPath is null)
        {
            StatusBox.Text = "The selected runtime does not support debugging.";
            return;
        }

        string tempProject = Path.ChangeExtension(Path.GetTempFileName(), ".gmx");
        try
        {
            File.WriteAllText(tempProject, """
<!-- Without this file the debugger crashes, but it doesn't actually need to contain anything. -->
<assets>
  <Configs name="configs">
    <Config>Configs\Default</Config>
  </Configs>
  <NewExtensions/>
  <sounds name="sound"/>
  <sprites name="sprites"/>
  <backgrounds name="background"/>
  <paths name="paths"/>
  <objects name="objects"/>
  <rooms name="rooms"/>
  <help/>
  <TutorialState>
    <IsTutorial>0</IsTutorial>
    <TutorialName></TutorialName>
    <TutorialPage>0</TutorialPage>
  </TutorialState>
</assets>
""");

            ProcessStartInfo runnerStartInfo = new(runtime.Path);
            runnerStartInfo.ArgumentList.Add("-game");
            runnerStartInfo.ArgumentList.Add(runDataFilePath);
            runnerStartInfo.ArgumentList.Add("-debugoutput");
            runnerStartInfo.ArgumentList.Add(Path.ChangeExtension(runDataFilePath, ".gamelog.txt"));
            Process.Start(runnerStartInfo);

            ProcessStartInfo debuggerStartInfo = new(runtime.DebuggerPath);
            debuggerStartInfo.ArgumentList.Add($"-d={Path.ChangeExtension(runDataFilePath, ".yydebug")}");
            debuggerStartInfo.ArgumentList.Add("-t=127.0.0.1");
            debuggerStartInfo.ArgumentList.Add($"-tp={generalInfo.DebuggerPort}");
            debuggerStartInfo.ArgumentList.Add($"-p={tempProject}");
            Process.Start(debuggerStartInfo);

            StatusBox.Text = $"Started {runtime.Version} with GMS debugger.";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Debugger run failed:{Environment.NewLine}{ex}";
        }
    }

    private string GetRunnableDataFilePath()
    {
        return _project?.SaveDataPath ?? _currentFilePath ?? string.Empty;
    }

    private async System.Threading.Tasks.Task<RuntimeCandidate?> PickRuntimeAsync(
        string dataFilePath,
        UndertaleData data,
        bool requireDebugger)
    {
        RuntimeCandidate[] runtimes = DiscoverRuntimes(dataFilePath, data).ToArray();
        if (requireDebugger)
            runtimes = runtimes.Where(runtime => runtime.DebuggerPath is not null).ToArray();

        if (runtimes.Length == 0)
        {
            StatusBox.Text = requireDebugger
                ? "Unable to find an installed Studio runtime with debugger support."
                : "Unable to find game EXE or any installed Studio runtime.";
            return null;
        }

        if (runtimes.Length == 1)
            return runtimes[0];

        ListView runtimesList = new()
        {
            ItemsSource = runtimes,
            DisplayMemberPath = nameof(RuntimeCandidate.DisplayText),
            SelectionMode = ListViewSelectionMode.Single,
            MinWidth = 520,
            MaxHeight = 320
        };
        runtimesList.SelectedIndex = 0;

        ContentDialog dialog = new()
        {
            Title = "Choose runtime",
            Content = runtimesList,
            PrimaryButtonText = "Run",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary
            ? runtimesList.SelectedItem as RuntimeCandidate
            : null;
    }

    private static IEnumerable<RuntimeCandidate> DiscoverRuntimes(string dataFilePath, UndertaleData data)
    {
        string? gameExeName = data.GeneralInfo?.FileName?.Content;
        if (!string.IsNullOrWhiteSpace(gameExeName))
        {
            string? gameExePath = Paths.TryJoinVerifyWithinDirectory(Path.GetDirectoryName(dataFilePath), $"{gameExeName}.exe");
            if (gameExePath is not null && File.Exists(gameExePath))
                yield return new RuntimeCandidate("Game EXE", gameExePath, null);
        }

        RuntimeSettings runtimeSettings = ReadRuntimeSettings();

        string studioRunner = Path.Join(Environment.ExpandEnvironmentVariables(runtimeSettings.GameMakerStudioPath), "Runner.exe");
        if (File.Exists(studioRunner))
        {
            string? studioDebugger = Path.Join(Environment.ExpandEnvironmentVariables(runtimeSettings.GameMakerStudioPath), @"GMDebug\GMDebug.exe");
            if (!File.Exists(studioDebugger))
                studioDebugger = null;

            yield return new RuntimeCandidate("1.4.xxx", studioRunner, studioDebugger);
        }

        string runtimesPath = Environment.ExpandEnvironmentVariables(runtimeSettings.GameMakerStudio2RuntimesPath);
        if (!Directory.Exists(runtimesPath))
            yield break;

        Regex runtimePattern = new(@"^runtime-(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        foreach (string runtimePath in Directory.EnumerateDirectories(runtimesPath))
        {
            Match match = runtimePattern.Match(Path.GetFileName(runtimePath));
            if (!match.Success)
                continue;

            string runtimeRunner = Path.Join(runtimePath, @"windows\Runner.exe");
            string runtimeRunnerX64 = Path.Join(runtimePath, @"windows\x64\Runner.exe");
            if (Environment.Is64BitOperatingSystem && File.Exists(runtimeRunnerX64))
                runtimeRunner = runtimeRunnerX64;
            if (!File.Exists(runtimeRunner))
                continue;

            yield return new RuntimeCandidate(match.Groups[1].Value, runtimeRunner, null);
        }
    }

    private static RuntimeSettings ReadRuntimeSettings()
    {
        WinUiToolSettings.EnsureLoaded();
        WinUiToolSettings settings = WinUiToolSettings.Instance;
        return new RuntimeSettings(settings.GameMakerStudioPath, settings.GameMakerStudio2RuntimesPath);
    }

    private async System.Threading.Tasks.Task GenerateOffsetMapAsync()
    {
        FileOpenPicker openPicker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        openPicker.FileTypeFilter.Add(".win");
        openPicker.FileTypeFilter.Add(".ios");
        openPicker.FileTypeFilter.Add(".unx");
        openPicker.FileTypeFilter.Add(".droid");
        InitializePickerWithMainWindow(openPicker);

        StorageFile? inputFile = await openPicker.PickSingleFileAsync();
        if (inputFile is null)
            return;

        FileSavePicker savePicker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = Path.GetFileName(inputFile.Path) + ".offsetmap"
        };
        savePicker.FileTypeChoices.Add("Text files", [".txt"]);
        InitializePickerWithMainWindow(savePicker);

        StorageFile? outputFile = await savePicker.PickSaveFileAsync();
        if (outputFile is null)
            return;

        _isGeneratingOffsetMap = true;
        GenerateOffsetMapMenuItem.IsEnabled = false;
        StatusBox.Text = "Generating offset map...";
        UpdateCommandStates();

        try
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                using FileStream stream = new(inputFile.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
                Dictionary<uint, UndertaleObject> offsets = UndertaleIO.GenerateOffsetMap(stream);
                using StreamWriter writer = File.CreateText(outputFile.Path);
                foreach (KeyValuePair<uint, UndertaleObject> offset in offsets.OrderBy(pair => pair.Key))
                    writer.WriteLine(offset.Key.ToString("X8", CultureInfo.InvariantCulture) + " " + (offset.Value?.ToString() ?? string.Empty).Replace("\n", "\\\n"));
            });

            StatusBox.Text = $"Generated offset map:{Environment.NewLine}{outputFile.Path}";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Offset map generation failed:{Environment.NewLine}{ex}";
        }
        finally
        {
            _isGeneratingOffsetMap = false;
            GenerateOffsetMapMenuItem.IsEnabled = true;
            UpdateCommandStates();
        }
    }

    private async System.Threading.Tasks.Task CreateNewProjectAsync()
    {
        if (_isProjectOperation)
            return;

        if (!await ConfirmDiscardProjectAssetsAsync("Project already open", "Create a new project and discard all unexported project changes?"))
            return;

        if (_data is null || _currentFilePath is null)
        {
            string? sourcePath = await PickDataFilePathAsync("Choose source data file");
            if (sourcePath is null)
                return;

            await OpenDataFileAsync(sourcePath);
            if (_data is null || _currentFilePath is null)
                return;
        }

        UndertaleData data = _data;
        string loadFilePath = _currentFilePath;
        string defaultName = $"{FormatTitle(data.GeneralInfo?.DisplayName?.Content ?? data.GeneralInfo?.Name?.Content ?? "New")} Mod";
        string? projectName = await ShowTextInputDialogAsync("Choose project name", "Choose a name for the new project.", defaultName);
        if (projectName is null)
        {
            StatusBox.Text = "Cancelled new project creation.";
            return;
        }

        projectName = projectName.Trim();
        if (projectName.Length == 0)
        {
            StatusBox.Text = "Project creation cancelled: project name is empty.";
            return;
        }

        string? directory = await PickProjectDirectoryAsync();
        if (directory is null)
        {
            StatusBox.Text = "Cancelled new project creation.";
            return;
        }

        string? saveFilePath = await ChooseProjectSaveFileAsync(loadFilePath);
        if (saveFilePath is null)
            return;

        _isProjectOperation = true;
        StatusBox.Text = "Creating project...";
        UpdateCommandStates();

        try
        {
            ProjectContext project = await System.Threading.Tasks.Task.Run(() =>
                new ProjectContext(
                    data,
                    loadFilePath,
                    saveFilePath,
                    Path.Join(directory, "project.json"),
                    projectName,
                    RunOnUiThreadBlocking));

            AssignProject(project);
            StatusBox.Text = $"Project \"{projectName}\" created successfully.";
        }
        catch (ProjectException ex)
        {
            StatusBox.Text = $"Project creation failed:{Environment.NewLine}{ex.Message}";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Project creation failed:{Environment.NewLine}{ex}";
        }
        finally
        {
            _isProjectOperation = false;
            UpdateCommandStates();
        }
    }

    private async System.Threading.Tasks.Task OpenProjectAsync()
    {
        if (_isProjectOperation)
            return;

        if (!await ConfirmDiscardProjectAssetsAsync("Project already open", "Open another project and discard all unexported project changes?"))
            return;

        StorageFile? projectFile = await PickProjectFileAsync();
        if (projectFile is null)
            return;

        string? dataFilePathToLoad = null;
        if (_data is null || _currentFilePath is null)
        {
            dataFilePathToLoad = await PickDataFilePathAsync("Choose source data file");
            if (dataFilePathToLoad is null)
                return;
        }

        string sourceDataPath = dataFilePathToLoad ?? _currentFilePath!;
        string? saveFilePath = await ChooseProjectSaveFileAsync(sourceDataPath);
        if (saveFilePath is null)
            return;

        if (dataFilePathToLoad is not null)
        {
            await OpenDataFileAsync(dataFilePathToLoad);
            if (_data is null || _currentFilePath is null)
                return;
        }

        UndertaleData data = _data!;
        string loadFilePath = _currentFilePath!;
        string previousFilePath = _currentFilePath!;
        _currentFilePath = saveFilePath;
        FilePathText.Text = saveFilePath;
        UpdateWindowTitle();

        _isProjectOperation = true;
        StatusBox.Text = "Opening project...";
        UpdateCommandStates();

        try
        {
            ProjectContext project = await System.Threading.Tasks.Task.Run(() =>
            {
                ProjectContext loadedProject = ProjectContext.CreateWithDataFilePaths(loadFilePath, saveFilePath, projectFile.Path);
                loadedProject.Import(data, null, RunOnUiThreadBlocking);
                return loadedProject;
            });

            AssignProject(project);
            MarkDirty(markProjectAsset: false);
            RefreshCategoriesPreservingSelection();
            StatusBox.Text = $"Opened project \"{project.Name}\".";
        }
        catch (ProjectException ex)
        {
            _currentFilePath = previousFilePath;
            FilePathText.Text = previousFilePath;
            UpdateWindowTitle();
            StatusBox.Text = $"Project failed to open:{Environment.NewLine}{ex.Message}";
        }
        catch (Exception ex)
        {
            _currentFilePath = previousFilePath;
            FilePathText.Text = previousFilePath;
            UpdateWindowTitle();
            StatusBox.Text = $"Project failed to open:{Environment.NewLine}{ex}";
        }
        finally
        {
            _isProjectOperation = false;
            UpdateCommandStates();
        }
    }

    private async System.Threading.Tasks.Task SaveProjectAsync()
    {
        if (_data is null || _project is null || _isProjectOperation)
            return;

        _isProjectOperation = true;
        StatusBox.Text = "Saving project...";
        UpdateCommandStates();

        try
        {
            await System.Threading.Tasks.Task.Run(() => _project.Export(clearMarkedAssets: true));
            StatusBox.Text = "Saved project successfully.";
            UpdateCommandStates();
        }
        catch (ProjectException ex)
        {
            StatusBox.Text = $"Project failed to save:{Environment.NewLine}{ex.Message}";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Project failed to save:{Environment.NewLine}{ex}";
        }
        finally
        {
            _isProjectOperation = false;
            UpdateCommandStates();
        }
    }

    private async System.Threading.Tasks.Task ShowProjectAssetsAsync()
    {
        if (_project is null)
            return;

        ProjectAssetSummary[] assets = BuildProjectAssetSummaries(_project);
        if (assets.Length == 0)
        {
            ContentDialog emptyDialog = new()
            {
                Title = "Unexported project assets",
                Content = "No assets are currently marked for project export.",
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };
            await emptyDialog.ShowAsync();
            return;
        }

        ListView assetsList = new()
        {
            ItemsSource = assets,
            DisplayMemberPath = nameof(ProjectAssetSummary.DisplayText),
            SelectionMode = ListViewSelectionMode.Single,
            MinWidth = 420,
            MaxHeight = 360
        };

        ContentDialog dialog = new()
        {
            Title = "Unexported project assets",
            Content = assetsList,
            PrimaryButtonText = "Open selected",
            SecondaryButtonText = "Unmark selected",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();
        if (assetsList.SelectedItem is not ProjectAssetSummary selected)
        {
            if (result is ContentDialogResult.Primary or ContentDialogResult.Secondary)
                StatusBox.Text = "No project asset was selected.";
            return;
        }

        if (result == ContentDialogResult.Primary)
        {
            ChangeSelection(selected.Asset);
            StatusBox.Text = $"Opened {selected.DisplayText}.";
        }
        else if (result == ContentDialogResult.Secondary && _project.UnmarkAssetForExport(selected.Asset))
        {
            StatusBox.Text = $"Unmarked {selected.DisplayText} for project export.";
        }
    }

    private async System.Threading.Tasks.Task CloseProjectAsync()
    {
        if (_project is null)
            return;

        if (!await ConfirmDiscardProjectAssetsAsync("Close project", "Close the current project and discard all unexported project changes?"))
            return;

        string projectName = _project.Name;
        UnloadProject();
        StatusBox.Text = $"Project \"{projectName}\" closed.";
    }

    private void AssignProject(ProjectContext project)
    {
        UnloadProject();
        _project = project;
        _project.UnexportedAssetsChanged += Project_UnexportedAssetsChanged;
        UpdateProjectExportToggle();
        UpdateCommandStates();
        UpdateWindowTitle();
    }

    private void UnloadProject()
    {
        if (_project is not null)
            _project.UnexportedAssetsChanged -= Project_UnexportedAssetsChanged;

        _project = null;
        UpdateProjectExportToggle();
        UpdateCommandStates();
        UpdateWindowTitle();
    }

    private void Project_UnexportedAssetsChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateProjectExportToggle();
            UpdateCommandStates();
            UpdateResourceCommandButtons();
            UpdateWindowTitle();
        });
    }

    private async System.Threading.Tasks.Task<bool> ConfirmDiscardProjectAssetsAsync(string title, string message)
    {
        return _project is not { HasUnexportedAssets: true } ||
               await ShowConfirmationAsync(title, message, "Discard", "Cancel");
    }

    private static ProjectAssetSummary[] BuildProjectAssetSummaries(ProjectContext project)
    {
        return project.EnumerateUnexportedAssets()
                      .Select(asset => new ProjectAssetSummary(
                          asset.ProjectName,
                          asset.ProjectAssetType.ToInterfaceName(),
                          asset))
                      .OrderBy(summary => summary.AssetType, StringComparer.OrdinalIgnoreCase)
                      .ThenBy(summary => summary.Name, StringComparer.OrdinalIgnoreCase)
                      .ToArray();
    }

    private static ReferenceTypeOption[] BuildReferenceTypeOptions(UndertaleData data, bool includeStrings)
    {
        if (data.GeneralInfo is null)
            return [];

        return UndertaleResourceReferenceMap
            .GetReferenceableTypes((data.GeneralInfo.Major, data.GeneralInfo.Minor, data.GeneralInfo.Release))
            .Where(pair => includeStrings || pair.Key != typeof(UndertaleString))
            .Where(pair => data.Code is not null || !UndertaleResourceReferenceMap.CodeTypes.Contains(pair.Key))
            .Select(pair => new ReferenceTypeOption(pair.Key, pair.Value))
            .OrderBy(option => option.Label, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Dictionary<Type, string> BuildReferenceTypeDictionary(UndertaleData data, bool includeStrings)
    {
        return BuildReferenceTypeOptions(data, includeStrings)
            .ToDictionary(option => option.Type, option => option.Label);
    }

    private async System.Threading.Tasks.Task FindUnreferencedAssetsAsync()
    {
        if (_data is null || _isFindingReferences)
            return;

        UndertaleData data = _data;
        if (data.GeneralInfo is null)
        {
            StatusBox.Text = "Cannot determine GameMaker version: General info is missing.";
            return;
        }

        ReferenceTypeOption[] options = BuildReferenceTypeOptions(data, includeStrings: true);

        if (options.Length == 0)
        {
            StatusBox.Text = "No referenceable asset types are available.";
            return;
        }

        CheckBox[] checkBoxes = options
            .Select(option => new CheckBox
            {
                Content = option.Label,
                DataContext = option,
                IsChecked = true,
                Margin = new Thickness(0, 0, 12, 4)
            })
            .ToArray();

        Button selectAllButton = new()
        {
            Content = "Select all"
        };
        selectAllButton.Click += (_, _) =>
        {
            foreach (CheckBox checkBox in checkBoxes)
                checkBox.IsChecked = true;
        };

        Button deselectAllButton = new()
        {
            Content = "Deselect all"
        };
        deselectAllButton.Click += (_, _) =>
        {
            foreach (CheckBox checkBox in checkBoxes)
                checkBox.IsChecked = false;
        };

        StackPanel toolbar = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        toolbar.Children.Add(selectAllButton);
        toolbar.Children.Add(deselectAllButton);

        ItemsControl typesList = new()
        {
            ItemsSource = checkBoxes
        };

        ScrollViewer scrollViewer = new()
        {
            Content = typesList,
            MaxHeight = 360,
            MinWidth = 420,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        StackPanel content = new()
        {
            Spacing = 10
        };
        content.Children.Add(new TextBlock
        {
            Text = "Select asset types to scan for unreferenced entries.",
            TextWrapping = TextWrapping.Wrap
        });
        content.Children.Add(toolbar);
        content.Children.Add(scrollViewer);

        ContentDialog pickerDialog = new()
        {
            Title = "Find unreferenced assets",
            Content = content,
            PrimaryButtonText = "Search",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await pickerDialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        Dictionary<Type, string> selectedTypes = checkBoxes
            .Where(checkBox => checkBox.IsChecked == true && checkBox.DataContext is ReferenceTypeOption)
            .Select(checkBox => (ReferenceTypeOption)checkBox.DataContext)
            .ToDictionary(option => option.Type, option => option.Label);

        if (selectedTypes.Count == 0)
        {
            StatusBox.Text = "At least one asset type must be selected.";
            return;
        }

        if (selectedTypes.Count > 1 &&
            selectedTypes.ContainsKey(typeof(UndertaleString)) &&
            data.Strings.Count > 5000 &&
            !await ShowConfirmationAsync(
                "Large string scan",
                "Strings are selected and this file has a lot of strings. The search can take noticeably longer. Proceed?",
                "Proceed",
                "Cancel"))
        {
            return;
        }

        _isFindingReferences = true;
        StatusBox.Text = "Searching for unreferenced assets...";
        UpdateCommandStates();

        try
        {
            Dictionary<string, List<object>>? results =
                await UndertaleResourceReferenceMethodsMap.GetUnreferencedObjects(data, selectedTypes);

            ReferenceSearchResult[] rows = BuildReferenceSearchRows(results);
            await ShowUnreferencedAssetResultsAsync(rows);
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Unreferenced asset search failed:{Environment.NewLine}{ex}";
        }
        finally
        {
            _isFindingReferences = false;
            UpdateCommandStates();
        }
    }

    private async System.Threading.Tasks.Task FindReferencesForResourceAsync(ResourceItem item)
    {
        if (_data is null || _isFindingReferences || item.Value is NullResourcePlaceholder)
            return;

        UndertaleData data = _data;
        if (data.GeneralInfo is null)
        {
            StatusBox.Text = "Cannot determine GameMaker version: General info is missing.";
            return;
        }

        HashSetTypesOverride selectedTypes = new(isYYC: data.Code is null);
        foreach (ReferenceTypeOption option in BuildReferenceTypeOptions(data, includeStrings: true))
            selectedTypes.Add(option.Type);

        if (selectedTypes.Count == 0)
        {
            StatusBox.Text = "No referenceable asset types are available.";
            return;
        }

        _isFindingReferences = true;
        StatusBox.Text = $"Searching references for {item.Title}...";
        UpdateCommandStates();

        try
        {
            Dictionary<string, List<object>>? results = await System.Threading.Tasks.Task.Run(() =>
                UndertaleResourceReferenceMethodsMap.GetReferencesOfObject(item.Value, data, selectedTypes));

            ReferenceSearchResult[] rows = BuildReferenceSearchRows(results);
            await ShowReferenceSearchResultsAsync(item, rows);
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Reference search failed:{Environment.NewLine}{ex}";
        }
        finally
        {
            _isFindingReferences = false;
            UpdateCommandStates();
        }
    }

    private ReferenceSearchResult[] BuildReferenceSearchRows(Dictionary<string, List<object>>? results)
    {
        if (results is null || results.Count == 0)
            return [];

        return results
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .SelectMany(pair => pair.Value
                .Where(value => value is not null)
                .Select(value => new ReferenceSearchResult(pair.Key, BuildReferenceResultTitle(value), value)))
            .OrderBy(row => row.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private string BuildReferenceResultTitle(object value)
    {
        if (FindResourceItem(value, out ResourceCategory? category) is { } item)
            return $"{item.Title} ({category?.Label ?? "Resources"})";

        return value switch
        {
            UndertaleNamedResource named => FormatTitle(named.Name?.Content),
            UndertaleString str => FormatTitle(str.Content),
            _ => FormatTitle(value.ToString())
        };
    }

    private async System.Threading.Tasks.Task ShowUnreferencedAssetResultsAsync(ReferenceSearchResult[] rows)
    {
        if (rows.Length == 0)
        {
            ContentDialog emptyDialog = new()
            {
                Title = "Unreferenced assets",
                Content = "No unreferenced assets found.",
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };
            await emptyDialog.ShowAsync();
            StatusBox.Text = "No unreferenced assets found.";
            return;
        }

        ListView resultsList = new()
        {
            ItemsSource = rows,
            DisplayMemberPath = nameof(ReferenceSearchResult.DisplayText),
            SelectionMode = ListViewSelectionMode.Single,
            MinWidth = 520,
            MaxHeight = 420
        };

        ContentDialog resultsDialog = new()
        {
            Title = "Unreferenced assets",
            Content = resultsList,
            PrimaryButtonText = "Open selected",
            SecondaryButtonText = "Export TXT",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        ContentDialogResult result = await resultsDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            if (resultsList.SelectedItem is ReferenceSearchResult selected)
            {
                ChangeSelection(selected.Value);
                StatusBox.Text = $"Opened {selected.DisplayText}.";
            }
            else
            {
                StatusBox.Text = "No unreferenced asset was selected.";
            }
        }
        else if (result == ContentDialogResult.Secondary)
        {
            await ExportReferenceSearchResultsAsync(rows, "unreferenced-assets", "Unreferenced game assets");
        }
        else
        {
            StatusBox.Text = $"Found {rows.Length} unreferenced asset(s).";
        }
    }

    private async System.Threading.Tasks.Task ShowReferenceSearchResultsAsync(ResourceItem source, ReferenceSearchResult[] rows)
    {
        if (rows.Length == 0)
        {
            ContentDialog emptyDialog = new()
            {
                Title = "References",
                Content = $"No references found for {source.Title}.",
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };
            await emptyDialog.ShowAsync();
            StatusBox.Text = $"No references found for {source.Title}.";
            return;
        }

        ListView resultsList = new()
        {
            ItemsSource = rows,
            DisplayMemberPath = nameof(ReferenceSearchResult.DisplayText),
            SelectionMode = ListViewSelectionMode.Single,
            MinWidth = 520,
            MaxHeight = 420
        };

        ContentDialog resultsDialog = new()
        {
            Title = $"References for {source.Title}",
            Content = resultsList,
            PrimaryButtonText = "Open selected",
            SecondaryButtonText = "Export TXT",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        ContentDialogResult result = await resultsDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            if (resultsList.SelectedItem is ReferenceSearchResult selected)
            {
                ChangeSelection(selected.Value);
                StatusBox.Text = $"Opened {selected.DisplayText}.";
            }
            else
            {
                StatusBox.Text = "No reference was selected.";
            }
        }
        else if (result == ContentDialogResult.Secondary)
        {
            await ExportReferenceSearchResultsAsync(
                rows,
                $"references-{SafeFileName(source.Title, "resource")}",
                $"References for {source.Title}");
        }
        else
        {
            StatusBox.Text = $"Found {rows.Length} reference(s) for {source.Title}.";
        }
    }

    private async System.Threading.Tasks.Task ExportReferenceSearchResultsAsync(
        IReadOnlyList<ReferenceSearchResult> rows,
        string suggestedFileName,
        string heading)
    {
        FileSavePicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedFileName
        };
        picker.FileTypeChoices.Add("Text file", [".txt"]);
        InitializePickerWithMainWindow(picker);

        StorageFile? file = await picker.PickSaveFileAsync();
        if (file is null)
            return;

        List<string> lines = [heading, ""];
        foreach (IGrouping<string, ReferenceSearchResult> group in rows.GroupBy(row => row.Group).OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            lines.Add(group.Key);
            foreach (ReferenceSearchResult row in group.OrderBy(row => row.Title, StringComparer.OrdinalIgnoreCase))
                lines.Add($"  {row.Title}");
            lines.Add(string.Empty);
        }

        await File.WriteAllLinesAsync(file.Path, lines, Encoding.UTF8);
        StatusBox.Text = $"Exported {rows.Count} reference result(s) to {file.Path}.";
    }

    private async System.Threading.Tasks.Task<StorageFile?> PickProjectFileAsync()
    {
        FileOpenPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add(".json");
        InitializePickerWithMainWindow(picker);
        return await picker.PickSingleFileAsync();
    }

    private async System.Threading.Tasks.Task<string?> PickDataFilePathAsync(string title)
    {
        FileOpenPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            CommitButtonText = title
        };
        picker.FileTypeFilter.Add(".win");
        picker.FileTypeFilter.Add(".ios");
        picker.FileTypeFilter.Add(".unx");
        picker.FileTypeFilter.Add(".droid");
        picker.FileTypeFilter.Add(".dat");
        InitializePickerWithMainWindow(picker);

        StorageFile? file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    private async System.Threading.Tasks.Task<string?> PickProjectDirectoryAsync()
    {
        FolderPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add("*");
        InitializePickerWithMainWindow(picker);

        StorageFolder? folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private async System.Threading.Tasks.Task<string?> ChooseProjectSaveFileAsync(string sourceFilePath)
    {
        FileSavePicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = Path.GetFileName(sourceFilePath)
        };
        picker.FileTypeChoices.Add("GameMaker data file", [".win"]);
        picker.FileTypeChoices.Add("iOS data file", [".ios"]);
        picker.FileTypeChoices.Add("Unix data file", [".unx"]);
        picker.FileTypeChoices.Add("Android data file", [".droid"]);
        InitializePickerWithMainWindow(picker);

        StorageFile? file = await picker.PickSaveFileAsync();
        if (file is null)
            return null;

        string saveFilePath = file.Path;
        string? sourceDirectory = Path.GetDirectoryName(sourceFilePath);
        string? saveDirectory = Path.GetDirectoryName(saveFilePath);
        if (!string.IsNullOrWhiteSpace(sourceDirectory) &&
            !string.IsNullOrWhiteSpace(saveDirectory) &&
            Path.GetFullPath(sourceDirectory).Equals(Path.GetFullPath(saveDirectory), StringComparison.OrdinalIgnoreCase) &&
            !await ShowConfirmationAsync(
                "Destination file in same directory as source file",
                "The destination data file is in the same directory as the source data file. This may overwrite external data files. Proceed?",
                "Proceed",
                "Cancel"))
        {
            return null;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(saveDirectory) &&
                Directory.Exists(saveDirectory) &&
                !Directory.EnumerateFileSystemEntries(saveDirectory).Any())
            {
                ContentDialog warningDialog = new()
                {
                    Title = "Empty destination directory",
                    Content = "The destination data file's directory is empty. Copy the other game files there if external assets should load correctly.",
                    PrimaryButtonText = "OK",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = XamlRoot
                };
                await warningDialog.ShowAsync();
            }
        }
        catch
        {
        }

        return saveFilePath;
    }

    private async System.Threading.Tasks.Task<string?> ShowTextInputDialogAsync(string title, string label, string defaultValue)
    {
        TextBox input = new()
        {
            Text = defaultValue,
            MinWidth = 360,
            TextWrapping = TextWrapping.NoWrap
        };
        StackPanel content = new()
        {
            Spacing = 8
        };
        content.Children.Add(new TextBlock
        {
            Text = label,
            TextWrapping = TextWrapping.Wrap
        });
        content.Children.Add(input);

        ContentDialog dialog = new()
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? input.Text : null;
    }

    private async System.Threading.Tasks.Task<bool> ShowConfirmationAsync(
        string title,
        string message,
        string primaryText,
        string closeText)
    {
        ContentDialog dialog = new()
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primaryText,
            CloseButtonText = closeText,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private void CategoryList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not ResourceCategory category)
            return;

        ResourceList.SelectedIndex = -1;
        _selectedCategory = category;
        _selectedResource = null;
        UpdateGlobalToolsVisibility();
        SetResourceFilterText(string.Empty);
        ApplyResourceFilter(force: true);
        UpdateResourceCommandButtons();
        ShowCategorySummaryWithoutResourceSelection();
    }

    private void ResourceList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not ResourceItem item)
            return;

        ResourceList.SelectedItem = item;
        OpenResourceItem(item, addTab: false, revealInList: false, syncTabSelection: false);
    }

    private void ResourceItemBorder_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ResourceItem item } element)
            return;

        ResourceList.SelectedItem = item;
        MenuFlyout flyout = new();
        MenuFlyoutItem openInNewTabItem = new()
        {
            Text = "Open in new tab",
            CommandParameter = item
        };
        openInNewTabItem.Click += OpenResourceContextNewTab_Click;
        flyout.Items.Add(openInNewTabItem);

        if (item.Value is not NullResourcePlaceholder)
        {
            MenuFlyoutItem findReferencesItem = new()
            {
                Text = "Find all references",
                CommandParameter = item
            };
            findReferencesItem.Click += FindReferencesContextItem_Click;
            flyout.Items.Add(findReferencesItem);

            MenuFlyoutItem copyNameItem = new()
            {
                Text = "Copy name to clipboard",
                CommandParameter = item
            };
            copyNameItem.Click += CopyResourceNameContextItem_Click;
            flyout.Items.Add(copyNameItem);
        }

        if (CanDeleteResourceItem(_selectedCategory, item))
        {
            MenuFlyoutItem deleteItem = new()
            {
                Text = "Delete",
                CommandParameter = item
            };
            deleteItem.Click += DeleteResourceContextItem_Click;
            flyout.Items.Add(new MenuFlyoutSeparator());
            flyout.Items.Add(deleteItem);
        }

        flyout.ShowAt(element, e.GetPosition(element));
        e.Handled = true;
    }

    private async void ResourceList_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Delete ||
            ResourceList.SelectedItem is not ResourceItem item ||
            !CanDeleteResourceItem(_selectedCategory, item))
        {
            return;
        }

        e.Handled = true;
        await DeleteResourceItemAsync(item);
    }

    private async void DeleteResourceContextItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { CommandParameter: ResourceItem item })
            await DeleteResourceItemAsync(item);
    }

    private async void FindReferencesContextItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { CommandParameter: ResourceItem item })
            await FindReferencesForResourceAsync(item);
    }

    private void CopyResourceNameContextItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { CommandParameter: ResourceItem item } ||
            item.Value is NullResourcePlaceholder)
        {
            return;
        }

        DataPackage package = new();
        package.SetText(GetResourceClipboardName(item));
        Clipboard.SetContent(package);
        StatusBox.Text = $"Copied {item.Title} name to clipboard.";
    }

    private void ResourceList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        _draggedResourceCategory = null;
        _draggedResourceItem = null;

        if (!CanSwapResourcesInCategory(_selectedCategory) ||
            e.Items.FirstOrDefault() is not ResourceItem item)
        {
            e.Cancel = true;
            return;
        }

        _draggedResourceCategory = _selectedCategory;
        _draggedResourceItem = item;
        e.Data.RequestedOperation = DataPackageOperation.Move;
        e.Data.SetText(item.Title);
    }

    private void ResourceList_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = CanDropDraggedResource(e, out _) ? DataPackageOperation.Move : DataPackageOperation.None;
        e.Handled = true;
    }

    private void ResourceList_Drop(object sender, DragEventArgs e)
    {
        try
        {
            if (!CanDropDraggedResource(e, out ResourceItem? targetItem) || targetItem is null)
                return;

            SwapDraggedResourceWith(targetItem);
        }
        finally
        {
            _draggedResourceCategory = null;
            _draggedResourceItem = null;
            e.Handled = true;
        }
    }

    private bool CanDropDraggedResource(DragEventArgs e, out ResourceItem? targetItem)
    {
        targetItem = FindResourceItemFromOriginalSource(e.OriginalSource);
        return _draggedResourceCategory is not null &&
               _selectedCategory is not null &&
               _draggedResourceCategory.Label == _selectedCategory.Label &&
               CanSwapResourcesInCategory(_draggedResourceCategory) &&
               _draggedResourceItem is not null &&
               targetItem is not null &&
               !ReferenceEquals(_draggedResourceItem, targetItem) &&
               _draggedResourceItem.Value.GetType() == targetItem.Value.GetType();
    }

    private void SwapDraggedResourceWith(ResourceItem targetItem)
    {
        if (_draggedResourceCategory?.SourceList is not IList list || _draggedResourceItem is null)
            return;

        int sourceIndex = list.IndexOf(_draggedResourceItem.Value);
        int targetIndex = list.IndexOf(targetItem.Value);
        if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex)
            return;

        object sourceValue = list[sourceIndex]!;
        object targetValue = list[targetIndex]!;
        list[sourceIndex] = targetValue;
        list[targetIndex] = sourceValue;

        string categoryLabel = _draggedResourceCategory.Label;
        RemapResourceIndicesAfterSwap(categoryLabel, sourceIndex, targetIndex);
        MarkDirty(markProjectAsset: false);
        RefreshCategoriesPreservingSelection();
        NavigateToResource(categoryLabel, targetIndex, addTab: false);
        StatusBox.Text = $"Swapped {categoryLabel} #{sourceIndex} and #{targetIndex}.";
    }

    private static ResourceItem? FindResourceItemFromOriginalSource(object originalSource)
    {
        DependencyObject? current = originalSource as DependencyObject;
        while (current is not null)
        {
            if (current is FrameworkElement { DataContext: ResourceItem item })
                return item;

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static T? FindDataContextFromOriginalSource<T>(object originalSource)
        where T : class
    {
        DependencyObject? current = originalSource as DependencyObject;
        while (current is not null)
        {
            if (current is FrameworkElement { DataContext: T item })
                return item;

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static bool IsMiddlePointerPressed(PointerRoutedEventArgs e, UIElement relativeTo)
    {
        return e.GetCurrentPoint(relativeTo).Properties.IsMiddleButtonPressed;
    }

    private void OpenResourceTabsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingOpenResourceTabs)
            return;

        if (OpenResourceTabsList.SelectedItem is not ResourceTab tab)
            return;

        NavigateToResource(tab.CategoryLabel, tab.ItemIndex, addTab: false);
    }

    private void OpenResourceTabsList_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is not ResourceTab tab)
            return;

        CloseResourceTab(tab);
    }

    private void ResourceBackButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateResourceHistory(-1);
    }

    private void ResourceForwardButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateResourceHistory(1);
    }

    private void BackNavigationMenuItem_Click(object sender, RoutedEventArgs e)
    {
        NavigateResourceHistory(-1);
    }

    private void ForwardNavigationMenuItem_Click(object sender, RoutedEventArgs e)
    {
        NavigateResourceHistory(1);
    }

    private void PreviousTabMenuItem_Click(object sender, RoutedEventArgs e)
    {
        NavigateRelativeResourceTab(-1);
    }

    private void NextTabMenuItem_Click(object sender, RoutedEventArgs e)
    {
        NavigateRelativeResourceTab(1);
    }

    private void CloseTabMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (OpenResourceTabsList.SelectedItem is ResourceTab tab)
            CloseResourceTab(tab);
    }

    private void CloseTabContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { CommandParameter: ResourceTab tab })
            CloseResourceTab(tab);
    }

    private void CloseOtherTabsContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { CommandParameter: ResourceTab tab })
            CloseOtherResourceTabs(tab);
    }

    private void CloseAllTabsContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        CloseAllResourceTabs();
    }

    private void CloseAllTabsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        CloseAllResourceTabs();
    }

    private void RestoreClosedTabMenuItem_Click(object sender, RoutedEventArgs e)
    {
        RestoreClosedResourceTab();
    }

    private void CloseResourceTab(ResourceTab tab, bool rememberClosed = true)
    {
        int tabIndex = _openResourceTabs.IndexOf(tab);
        if (tabIndex < 0)
            return;

        bool wasSelected = OpenResourceTabsList.SelectedItem == tab;
        if (rememberClosed)
            RememberClosedResourceTab(tab);

        _isUpdatingOpenResourceTabs = true;
        try
        {
            _openResourceTabs.Remove(tab);
            OpenResourceTabsList.Visibility = _openResourceTabs.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        finally
        {
            _isUpdatingOpenResourceTabs = false;
        }
        UpdateCommandStates();

        if (!wasSelected || _openResourceTabs.Count == 0)
            return;

        int nextIndex = Math.Clamp(tabIndex, 0, _openResourceTabs.Count - 1);
        ResourceTab nextTab = _openResourceTabs[nextIndex];
        NavigateToResource(nextTab.CategoryLabel, nextTab.ItemIndex, addTab: false);
    }

    private void CloseOtherResourceTabs(ResourceTab tab)
    {
        if (!_openResourceTabs.Contains(tab))
            return;

        foreach (ResourceTab closingTab in _openResourceTabs.Where(candidate => candidate != tab).Reverse().ToArray())
            RememberClosedResourceTab(closingTab);

        _isUpdatingOpenResourceTabs = true;
        try
        {
            _openResourceTabs.Clear();
            _openResourceTabs.Add(tab);
            OpenResourceTabsList.Visibility = Visibility.Visible;
            OpenResourceTabsList.SelectedItem = tab;
        }
        finally
        {
            _isUpdatingOpenResourceTabs = false;
        }

        NavigateToResource(tab.CategoryLabel, tab.ItemIndex, addTab: false);
        UpdateCommandStates();
    }

    private void CloseAllResourceTabs()
    {
        if (_openResourceTabs.Count == 0)
            return;

        _closedResourceTabsHistory.Clear();
        _isUpdatingOpenResourceTabs = true;
        try
        {
            OpenResourceTabsList.SelectedItem = null;
            _openResourceTabs.Clear();
            OpenResourceTabsList.Visibility = Visibility.Collapsed;
        }
        finally
        {
            _isUpdatingOpenResourceTabs = false;
        }

        ResourceList.SelectedIndex = -1;
        _selectedResource = null;
        HideEditors();
        ShowSelectedCategorySummary();
        UpdateResourceCommandButtons();
        UpdateCommandStates();
    }

    private async System.Threading.Tasks.Task DeleteResourceItemAsync(ResourceItem item)
    {
        if (_data is null || _selectedCategory is not ResourceCategory category)
            return;

        if (!CanDeleteResourceItem(category, item) || category.SourceList is not IList list)
            return;

        int removedIndex = list.IndexOf(item.Value);
        if (removedIndex < 0)
            return;

        bool isLast = removedIndex == list.Count - 1;
        string warning = isLast
            ? string.Empty
            : $"{Environment.NewLine}{Environment.NewLine}Note that code often references resources by ID. Deleting this will shift later IDs and can break references.";
        bool confirmed = await ShowConfirmationAsync(
            "Delete resource",
            $"Delete {item.Title} from {category.Label}?{warning}",
            "Delete",
            "Cancel");
        if (!confirmed)
            return;

        object deletedValue = item.Value;
        string categoryLabel = category.Label;
        list.RemoveAt(removedIndex);

        if (_project is not null && deletedValue is IProjectAsset projectAsset)
            _project.UnmarkAssetForExport(projectAsset);

        int nextIndex = list.Count == 0 ? -1 : Math.Clamp(removedIndex, 0, list.Count - 1);
        RemapResourceIndicesAfterRemove(categoryLabel, removedIndex);
        MarkDirty(markProjectAsset: false);

        _selectedResource = null;
        RefreshCategoriesPreservingSelection();
        RefreshResourceTabTitles(categoryLabel);

        if (nextIndex >= 0)
        {
            NavigateToResource(categoryLabel, nextIndex, addTab: false);
        }
        else
        {
            ResourceList.SelectedIndex = -1;
            HideEditors();
            ShowSelectedCategorySummary();
        }

        UpdateResourceCommandButtons();
        UpdateCommandStates();
        StatusBox.Text = $"Deleted {item.Title}.";
    }

    private void RemapResourceIndicesAfterRemove(string categoryLabel, int removedIndex)
    {
        _isUpdatingOpenResourceTabs = true;
        try
        {
            for (int i = _openResourceTabs.Count - 1; i >= 0; i--)
            {
                ResourceTab tab = _openResourceTabs[i];
                if (tab.CategoryLabel != categoryLabel)
                    continue;

                if (tab.ItemIndex == removedIndex)
                {
                    _openResourceTabs.RemoveAt(i);
                    continue;
                }

                if (tab.ItemIndex > removedIndex)
                    tab.ItemIndex--;
            }
        }
        finally
        {
            _isUpdatingOpenResourceTabs = false;
        }

        OpenResourceTabsList.Visibility = _openResourceTabs.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        RemapClosedResourceTabsAfterRemove(categoryLabel, removedIndex);
        RemapResourceNavigationHistoryAfterRemove(categoryLabel, removedIndex);
    }

    private void RemapClosedResourceTabsAfterRemove(string categoryLabel, int removedIndex)
    {
        for (int i = _closedResourceTabsHistory.Count - 1; i >= 0; i--)
        {
            ClosedResourceTab tab = _closedResourceTabsHistory[i];
            if (tab.CategoryLabel != categoryLabel)
                continue;

            if (tab.ItemIndex == removedIndex)
            {
                _closedResourceTabsHistory.RemoveAt(i);
                continue;
            }

            if (tab.ItemIndex > removedIndex)
                _closedResourceTabsHistory[i] = tab with { ItemIndex = tab.ItemIndex - 1 };
        }

        RemoveConsecutiveClosedResourceTabDuplicates();
    }

    private void RemapResourceNavigationHistoryAfterRemove(string categoryLabel, int removedIndex)
    {
        for (int i = _resourceNavigationHistory.Count - 1; i >= 0; i--)
        {
            ResourceNavigationEntry entry = _resourceNavigationHistory[i];
            if (entry.CategoryLabel != categoryLabel)
                continue;

            if (entry.ItemIndex == removedIndex)
            {
                _resourceNavigationHistory.RemoveAt(i);
                if (i <= _resourceNavigationHistoryPosition)
                    _resourceNavigationHistoryPosition--;
                continue;
            }

            if (entry.ItemIndex > removedIndex)
                _resourceNavigationHistory[i] = entry with { ItemIndex = entry.ItemIndex - 1 };
        }

        RemoveConsecutiveResourceNavigationDuplicates();

        if (_resourceNavigationHistory.Count == 0)
            _resourceNavigationHistoryPosition = -1;
        else
            _resourceNavigationHistoryPosition = Math.Clamp(
                _resourceNavigationHistoryPosition,
                0,
                _resourceNavigationHistory.Count - 1);
    }

    private void RemoveConsecutiveClosedResourceTabDuplicates()
    {
        for (int i = 0; i < _closedResourceTabsHistory.Count - 1; i++)
        {
            if (_closedResourceTabsHistory[i] != _closedResourceTabsHistory[i + 1])
                continue;

            _closedResourceTabsHistory.RemoveAt(i + 1);
            i--;
        }
    }

    private void RemoveConsecutiveResourceNavigationDuplicates()
    {
        for (int i = 0; i < _resourceNavigationHistory.Count - 1; i++)
        {
            if (_resourceNavigationHistory[i] != _resourceNavigationHistory[i + 1])
                continue;

            _resourceNavigationHistory.RemoveAt(i + 1);
            if (i + 1 <= _resourceNavigationHistoryPosition)
                _resourceNavigationHistoryPosition--;
            i--;
        }
    }

    private void RefreshResourceTabTitles(string categoryLabel)
    {
        ResourceCategory? category = _categories.FirstOrDefault(candidate => candidate.Label == categoryLabel);
        if (category is null)
            return;

        foreach (ResourceTab tab in _openResourceTabs.Where(candidate => candidate.CategoryLabel == categoryLabel))
        {
            if (tab.ItemIndex < 0 || tab.ItemIndex >= category.Items.Count)
                continue;

            ResourceItem item = category.Items[tab.ItemIndex];
            tab.Title = item.Title;
            tab.IconSymbol = item.IconSymbol;
        }

        for (int i = 0; i < _closedResourceTabsHistory.Count; i++)
        {
            ClosedResourceTab tab = _closedResourceTabsHistory[i];
            if (tab.CategoryLabel != categoryLabel || tab.ItemIndex < 0 || tab.ItemIndex >= category.Items.Count)
                continue;

            ResourceItem item = category.Items[tab.ItemIndex];
            _closedResourceTabsHistory[i] = tab with { Title = item.Title, IconSymbol = item.IconSymbol };
        }
    }

    private void RememberClosedResourceTab(ResourceTab tab)
    {
        _closedResourceTabsHistory.Add(new ClosedResourceTab(tab.CategoryLabel, tab.ItemIndex, tab.Title, tab.IconSymbol));
        const int maxClosedTabHistory = 32;
        if (_closedResourceTabsHistory.Count > maxClosedTabHistory)
            _closedResourceTabsHistory.RemoveRange(0, _closedResourceTabsHistory.Count - maxClosedTabHistory);
    }

    private bool RestoreClosedResourceTab()
    {
        while (_closedResourceTabsHistory.Count > 0)
        {
            ClosedResourceTab tab = _closedResourceTabsHistory[^1];
            _closedResourceTabsHistory.RemoveAt(_closedResourceTabsHistory.Count - 1);

            if (NavigateToResource(tab.CategoryLabel, tab.ItemIndex, addTab: true))
            {
                UpdateCommandStates();
                return true;
            }
        }

        UpdateCommandStates();
        StatusBox.Text = "No closed resource tab can be restored.";
        return false;
    }

    private void ResetResourceNavigationHistory()
    {
        _resourceNavigationHistory.Clear();
        _resourceNavigationHistoryPosition = -1;
    }

    private void RemapResourceIndicesAfterSwap(string categoryLabel, int firstIndex, int secondIndex)
    {
        int Remap(string label, int index)
        {
            if (label != categoryLabel)
                return index;

            if (index == firstIndex)
                return secondIndex;
            if (index == secondIndex)
                return firstIndex;
            return index;
        }

        foreach (ResourceTab tab in _openResourceTabs)
            tab.ItemIndex = Remap(tab.CategoryLabel, tab.ItemIndex);

        for (int i = 0; i < _closedResourceTabsHistory.Count; i++)
        {
            ClosedResourceTab tab = _closedResourceTabsHistory[i];
            int remapped = Remap(tab.CategoryLabel, tab.ItemIndex);
            if (remapped != tab.ItemIndex)
                _closedResourceTabsHistory[i] = tab with { ItemIndex = remapped };
        }

        for (int i = 0; i < _resourceNavigationHistory.Count; i++)
        {
            ResourceNavigationEntry entry = _resourceNavigationHistory[i];
            int remapped = Remap(entry.CategoryLabel, entry.ItemIndex);
            if (remapped != entry.ItemIndex)
                _resourceNavigationHistory[i] = entry with { ItemIndex = remapped };
        }
    }

    private void RecordResourceNavigation(string categoryLabel, int itemIndex)
    {
        if (_isNavigatingResourceHistory)
            return;

        ResourceNavigationEntry entry = new(categoryLabel, itemIndex);
        if (_resourceNavigationHistoryPosition >= 0 &&
            _resourceNavigationHistoryPosition < _resourceNavigationHistory.Count &&
            _resourceNavigationHistory[_resourceNavigationHistoryPosition] == entry)
        {
            return;
        }

        if (_resourceNavigationHistoryPosition < _resourceNavigationHistory.Count - 1)
            _resourceNavigationHistory.RemoveRange(
                _resourceNavigationHistoryPosition + 1,
                _resourceNavigationHistory.Count - _resourceNavigationHistoryPosition - 1);

        _resourceNavigationHistory.Add(entry);
        _resourceNavigationHistoryPosition = _resourceNavigationHistory.Count - 1;

        const int maxNavigationHistory = 256;
        if (_resourceNavigationHistory.Count > maxNavigationHistory)
        {
            int removeCount = _resourceNavigationHistory.Count - maxNavigationHistory;
            _resourceNavigationHistory.RemoveRange(0, removeCount);
            _resourceNavigationHistoryPosition -= removeCount;
        }
    }

    private bool NavigateResourceHistory(int direction)
    {
        int targetPosition = _resourceNavigationHistoryPosition + direction;
        while (targetPosition >= 0 && targetPosition < _resourceNavigationHistory.Count)
        {
            ResourceNavigationEntry entry = _resourceNavigationHistory[targetPosition];
            _isNavigatingResourceHistory = true;
            bool opened;
            try
            {
                opened = NavigateToResource(entry.CategoryLabel, entry.ItemIndex, addTab: false);
            }
            finally
            {
                _isNavigatingResourceHistory = false;
            }

            if (opened)
            {
                _resourceNavigationHistoryPosition = targetPosition;
                UpdateCommandStates();
                return true;
            }

            _resourceNavigationHistory.RemoveAt(targetPosition);
            if (targetPosition <= _resourceNavigationHistoryPosition)
                _resourceNavigationHistoryPosition--;
            targetPosition = _resourceNavigationHistoryPosition + direction;
        }

        UpdateCommandStates();
        return false;
    }

    private void NavigateRelativeResourceTab(int offset)
    {
        if (_openResourceTabs.Count == 0)
            return;

        int selectedIndex = OpenResourceTabsList.SelectedItem is ResourceTab selectedTab
            ? _openResourceTabs.IndexOf(selectedTab)
            : -1;
        if (selectedIndex < 0)
            selectedIndex = 0;

        int nextIndex = (selectedIndex + offset + _openResourceTabs.Count) % _openResourceTabs.Count;
        ResourceTab nextTab = _openResourceTabs[nextIndex];
        NavigateToResource(nextTab.CategoryLabel, nextTab.ItemIndex, addTab: false);
    }

    private void ResourceFilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingResourceFilter)
            return;

        ScheduleResourceFilter();
    }

    private void ResourceFilterBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_selectedCategory is null)
            return;

        if (e.Key == VirtualKey.Escape)
        {
            _resourceFilterTimer?.Stop();
            if (!string.IsNullOrEmpty(ResourceFilterBox.Text))
            {
                SetResourceFilterText(string.Empty);
                ApplyResourceFilter(force: true);
            }

            e.Handled = true;
            return;
        }

        if (e.Key != VirtualKey.Enter)
            return;

        _resourceFilterTimer?.Stop();
        ApplyResourceFilter(force: true);
        ResourceItem? item = _lastFilteredResourceItems?.FirstOrDefault();
        if (item is null)
        {
            StatusBox.Text = "No matching resource to open.";
            e.Handled = true;
            return;
        }

        ResourceList.SelectedItem = item;
        OpenResourceItem(item, addTab: false, revealInList: false, syncTabSelection: false);
        e.Handled = true;
    }

    private void FindResourceMenuItem_Click(object sender, RoutedEventArgs e)
    {
        FocusPrimaryFindTarget();
    }

    private void FindCodeMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (NavigateToCategory("Code"))
        {
            ShowGlobalTools(GlobalToolsMode.Code);
            GlobalCodeSearchBox.Focus(FocusState.Programmatic);
            GlobalCodeSearchBox.SelectAll();
        }
    }

    private void FindStringsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (NavigateToCategory("Strings"))
        {
            ShowGlobalTools(GlobalToolsMode.Strings);
            GlobalStringSearchBox.Focus(FocusState.Programmatic);
            GlobalStringSearchBox.SelectAll();
        }
    }

    private async void FindUnreferencedAssetsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await FindUnreferencedAssetsAsync();
    }

    private void FocusPrimaryFindTarget()
    {
        if (_selectedResource?.Value is UndertaleCode && CodeViewerPanel.Visibility == Visibility.Visible)
        {
            CodeSearchBox.Focus(FocusState.Programmatic);
            CodeSearchBox.SelectAll();
            return;
        }

        if (_selectedCategory?.Label == "Strings" && GlobalStringToolsPanel.Visibility == Visibility.Visible)
        {
            GlobalStringSearchBox.Focus(FocusState.Programmatic);
            GlobalStringSearchBox.SelectAll();
            return;
        }

        ResourceFilterBox.Focus(FocusState.Programmatic);
        ResourceFilterBox.SelectAll();
    }

    private async void GlobalCodeSearchButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null)
            return;

        string query = GlobalCodeSearchBox.Text.Trim();
        if (query.Length == 0)
        {
            GlobalCodeSearchResultsList.ItemsSource = null;
            StatusBox.Text = "Enter text to search across code.";
            return;
        }

        bool decompiled = GlobalCodeSearchModeBox.SelectedIndex == 1;
        GlobalCodeSearchButton.IsEnabled = false;
        GlobalCodeSearchResultsList.ItemsSource = null;
        StatusBox.Text = decompiled ? "Searching decompiled code..." : "Searching disassembly...";

        try
        {
            CodeSearchResult[] results = await System.Threading.Tasks.Task.Run(() => SearchAllCode(_data, query, decompiled));
            GlobalCodeSearchResultsList.ItemsSource = results;
            StatusBox.Text = $"Found {results.Length} code match(es) for \"{query}\".";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Code search failed:{Environment.NewLine}{ex}";
        }
        finally
        {
            GlobalCodeSearchButton.IsEnabled = true;
        }
    }

    private void GlobalCodeSearchResultsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not ICodeSearchResult result)
            return;

        if (!NavigateToResource("Code", result.CodeIndex))
            return;

        CodeViewModeBox.SelectedIndex = result.Decompiled ? 1 : 0;
        string searchText = result.SearchText.Length == 0 ? GlobalCodeSearchBox.Text : result.SearchText;
        SetCodeSearchText(searchText);
        RefreshCodeSearch(selectFirstMatch: true);
        StatusBox.Text = $"Opened {result.CodeName}, line {result.LineNumber}.";
    }

    private async void ExportAllCodeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null)
            return;

        FolderPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add("*");

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFolder? folder = await picker.PickSingleFolderAsync();
        if (folder is null)
            return;

        bool decompiled = GlobalCodeSearchModeBox.SelectedIndex == 1;
        ExportAllCodeButton.IsEnabled = false;
        StatusBox.Text = decompiled ? "Exporting all decompiled code..." : "Exporting all disassembly...";

        try
        {
            int exportedCount = await System.Threading.Tasks.Task.Run(() => ExportAllCode(_data, folder.Path, decompiled));
            StatusBox.Text = $"Exported {exportedCount} code file(s) to {folder.Path}.";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to export all code:{Environment.NewLine}{ex}";
        }
        finally
        {
            ExportAllCodeButton.IsEnabled = true;
        }
    }

    private void GlobalStringSearchButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null)
            return;

        string query = GlobalStringSearchBox.Text.Trim();
        if (query.Length == 0)
        {
            GlobalStringSearchResultsList.ItemsSource = null;
            StatusBox.Text = "Enter text to search across strings.";
            return;
        }

        StringSearchResult[] results = SearchAllStrings(_data, query);
        GlobalStringSearchResultsList.ItemsSource = results;
        StatusBox.Text = $"Found {results.Length} string match(es) for \"{query}\".";
    }

    private void GlobalStringSearchResultsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not StringSearchResult result)
            return;

        if (!NavigateToResource("Strings", result.StringIndex))
            return;

        StatusBox.Text = $"Opened string #{result.StringIndex}.";
    }

    private async void AddResourceButton_Click(object sender, RoutedEventArgs e)
    {
        await AddResourceToSelectedCategoryAsync();
    }

    private void DuplicateStringButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _data.UnsupportedBytecodeVersion || _selectedResource?.Value is not UndertaleString selectedString)
            return;

        int newIndex = _data.Strings.Count;
        _data.Strings.Add(new UndertaleString(selectedString.Content ?? string.Empty));
        MarkDirty();
        RefreshCategoriesPreservingSelection();
        NavigateToResource("Strings", newIndex);
        StatusBox.Text = $"Duplicated selected string to #{newIndex}.";
    }

    private async System.Threading.Tasks.Task AddResourceToSelectedCategoryAsync()
    {
        if (_data is null || _data.UnsupportedBytecodeVersion || _selectedCategory?.SourceList is not IList list)
            return;

        Type? itemType = GetListItemType(list);
        if (itemType is null)
        {
            StatusBox.Text = $"Cannot add to {_selectedCategory.Label}: resource list type is unknown.";
            return;
        }

        if (!typeof(UndertaleResource).IsAssignableFrom(itemType) && itemType != typeof(UndertaleString))
        {
            StatusBox.Text = $"Cannot add to {_selectedCategory.Label}: {itemType.Name} is not a resource type.";
            return;
        }

        object? created;
        try
        {
            created = Activator.CreateInstance(itemType);
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Cannot create {itemType.Name}:{Environment.NewLine}{ex.Message}";
            return;
        }

        if (created is null)
        {
            StatusBox.Text = $"Cannot create {itemType.Name}.";
            return;
        }

        string? createdName = null;
        if (created is UndertaleNamedResource namedResource)
        {
            bool makeDataString = created is not (UndertaleTexturePageItem or UndertaleEmbeddedAudio or UndertaleEmbeddedTexture);
            string? rawName = created switch
            {
                UndertaleTexturePageItem => $"PageItem {list.Count}",
                UndertaleEmbeddedAudio => $"EmbeddedSound {list.Count}",
                UndertaleEmbeddedTexture => $"Texture {list.Count}",
                _ => null
            };

            if (makeDataString)
            {
                string assetTypeName = itemType.Name.Replace("Undertale", string.Empty, StringComparison.Ordinal)
                                               .Replace("GameObject", "Object", StringComparison.Ordinal)
                                               .ToLowerInvariant();
                string defaultName = $"{assetTypeName}{list.Count}";
                string? requestedName = await ShowTextInputDialogAsync(
                    $"Choose new {assetTypeName} name",
                    "Name of new asset:",
                    defaultName);
                if (requestedName is null)
                    return;

                requestedName = requestedName.Trim();
                if (IsValidAssetIdentifier(requestedName))
                {
                    createdName = requestedName;
                }
                else if (await ShowConfirmationAsync(
                             "Invalid asset name",
                             $"Asset name \"{requestedName}\" is not a valid identifier. Add the asset using \"{defaultName}\" instead?",
                             "Use generated name",
                             "Cancel"))
                {
                    createdName = defaultName;
                }
                else
                {
                    return;
                }

                namedResource.Name = _data.Strings.MakeString(createdName);
                await InitializeNewNamedResourceAsync(_data, created, createdName);
            }
            else
            {
                namedResource.Name = new UndertaleString(rawName ?? $"{itemType.Name} {list.Count}");
                createdName = namedResource.Name.Content;
            }
        }
        else if (created is UndertaleString str)
        {
            str.Content = $"string{list.Count}";
            createdName = str.Content;
        }

        try
        {
            list.Add(created);
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Could not add {itemType.Name}:{Environment.NewLine}{ex.Message}";
            return;
        }

        if (_project is not null && created is IProjectAsset projectAsset)
            _project.MarkAssetForExport(projectAsset);

        MarkDirty(markProjectAsset: created is not IProjectAsset);
        int newIndex = list.Count - 1;
        string categoryLabel = _selectedCategory.Label;
        RefreshCategoriesPreservingSelection();
        NavigateToResource(categoryLabel, newIndex);
        StatusBox.Text = $"Added {FormatTitle(createdName ?? itemType.Name)} to {categoryLabel} #{newIndex}.";
    }

    private void OpenResourceContextNewTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { CommandParameter: ResourceItem item })
            return;

        ResourceCategory? category = _categories.FirstOrDefault(candidate => candidate.Items.Any(resource => ReferenceEquals(resource, item)));
        if (category is null)
            return;

        _selectedCategory = category;
        _selectedResource = item;
        CategoryList.SelectedItem = category;
        ResourceList.SelectedItem = item;
        OpenResourceItem(item, addTab: true, revealInList: false);
        StatusBox.Text = $"Opened {item.Title} in a resource tab.";
    }

    private async System.Threading.Tasks.Task InitializeNewNamedResourceAsync(UndertaleData data, object created, string name)
    {
        switch (created)
        {
            case UndertaleRoom room:
                if (data.IsGameMaker2())
                {
                    room.Caption = null;
                    room.Backgrounds.Clear();
                    if (data.IsVersionAtLeast(2024, 13))
                    {
                        room.Flags |= UndertaleRoom.RoomEntryFlags.IsGM2024_13;
                        room.InstanceCreationOrderIDs ??= new();
                    }
                    else
                    {
                        room.Flags |= UndertaleRoom.RoomEntryFlags.IsGMS2;
                        if (data.IsVersionAtLeast(2, 3))
                            room.Flags |= UndertaleRoom.RoomEntryFlags.IsGMS2_3;
                    }
                }
                else
                {
                    room.Caption = data.Strings.MakeString(string.Empty);
                }

                if (data.GeneralInfo is not null &&
                    await ShowConfirmationAsync(
                        "Add room order",
                        "Add the new room to the end of the room order list?",
                        "Add",
                        "Skip"))
                {
                    data.GeneralInfo.RoomOrder.Add(new(room));
                }
                break;

            case UndertaleScript script:
                if (data.IsVersionAtLeast(2, 3))
                {
                    script.Code = UndertaleCode.CreateEmptyEntry(data, $"gml_GlobalScript_{name}");
                    if (data.GlobalInitScripts is IList<UndertaleGlobalInit> globalInitScripts)
                    {
                        globalInitScripts.Add(new UndertaleGlobalInit
                        {
                            Code = script.Code
                        });
                    }
                }
                else
                {
                    script.Code = UndertaleCode.CreateEmptyEntry(data, $"gml_Script_{name}");
                }

                if (_project is not null)
                    _project.MarkAssetForExport(script.Code);
                break;

            case UndertaleCode code:
                if (data.CodeLocals is not null)
                {
                    code.LocalsCount = 1;
                    UndertaleCodeLocals.CreateEmptyEntry(data, code.Name);
                }
                else
                {
                    code.WeirdLocalFlag = true;
                }
                break;

            case UndertaleExtension when IsExtensionProductIdEligible(data):
                data.FORM.EXTN.productIdData.Add(
                [
                    0xBA, 0x5E, 0xBA, 0x11, 0xBA, 0xDD, 0x06, 0x60,
                    0xBE, 0xEF, 0xED, 0xBA, 0x0B, 0xAB, 0xBA, 0xBE
                ]);
                break;

            case UndertaleShader shader:
                shader.GLSL_ES_Vertex = data.Strings.MakeString(string.Empty, true);
                shader.GLSL_ES_Fragment = data.Strings.MakeString(string.Empty, true);
                shader.GLSL_Vertex = data.Strings.MakeString(string.Empty, true);
                shader.GLSL_Fragment = data.Strings.MakeString(string.Empty, true);
                shader.HLSL9_Vertex = data.Strings.MakeString(string.Empty, true);
                shader.HLSL9_Fragment = data.Strings.MakeString(string.Empty, true);
                break;
        }
    }

    private static bool IsExtensionProductIdEligible(UndertaleData data)
    {
        uint major = data.GeneralInfo?.Major ?? 0;
        uint build = data.GeneralInfo?.Build ?? 0;
        return major >= 2 || (major == 1 && (build >= 1773 || build == 1539));
    }

    private async void ExportStringsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null)
            return;

        FileSavePicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = "strings"
        };
        picker.FileTypeChoices.Add("JSON file", [".json"]);

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFile? file = await picker.PickSaveFileAsync();
        if (file is null)
            return;

        try
        {
            string json = ExportStringsJson(_data);
            await File.WriteAllTextAsync(file.Path, json);
            StatusBox.Text = $"Exported {_data.Strings.Count} strings to {file.Path}";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to export strings:{Environment.NewLine}{ex}";
        }
    }

    private async void ImportStringsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _data.UnsupportedBytecodeVersion)
            return;

        FileOpenPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add(".json");

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        try
        {
            string json = await File.ReadAllTextAsync(file.Path);
            int updatedCount = ImportStringsJson(_data, json);
            MarkDirty();
            RefreshCategoriesPreservingSelection();
            GlobalStringSearchResultsList.ItemsSource = null;
            StatusBox.Text = $"Imported {updatedCount} string update(s) from {file.Path}";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to import strings:{Environment.NewLine}{ex}";
        }
    }

    private static LoadedGame LoadGame(string path)
    {
        WinUiToolSettings.EnsureLoaded();
        StringBuilder status = new();
        FileStream stream = File.OpenRead(path);
        UndertaleData data = UndertaleIO.Read(stream, (warning, important) =>
        {
            status.Append(important ? "[important] " : "[warning] ");
            status.AppendLine(warning);
        });
        stream.Dispose();
        ApplySettingsToData(data);

        string gameName = FormatTitle(data.GeneralInfo?.Name?.Content ?? "Unknown game");
        status.Insert(0, $"Loaded {gameName}{Environment.NewLine}{path}{Environment.NewLine}{Environment.NewLine}");

        IReadOnlyList<ResourceCategory> categories = BuildCategories(data);

        return new LoadedGame(data, gameName, categories, status.ToString());
    }

    private static void ApplySettingsToData(UndertaleData data)
    {
        WinUiToolSettings.EnsureLoaded();
        data.ToolInfo.DecompilerSettings = WinUiToolSettings.Instance.DecompilerSettings;
        data.ToolInfo.InstanceIdPrefix = () => WinUiToolSettings.Instance.InstanceIdPrefix;
    }

    private string SaveGameWithProjectOptions(string path, UndertaleData data)
    {
        WinUiToolSettings.EnsureLoaded();
        if (_project is not null && WinUiToolSettings.Instance.RecompileAllCodeSourcesOnProjectSave)
            _project.RecompileAllCodeSources();

        return SaveGame(path, data);
    }

    private static string SaveGame(string path, UndertaleData data)
    {
        StringBuilder status = new();
        string tempPath = path + "temp";

        try
        {
            using (FileStream stream = new(tempPath, FileMode.Create, FileAccess.Write))
            {
                UndertaleIO.Write(stream, data, message => status.AppendLine(message));
            }

            File.Move(tempPath, path, true);
            status.Insert(0, $"Saved {path}{Environment.NewLine}{Environment.NewLine}");
            return status.ToString();
        }
        catch
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);

            throw;
        }
    }

    internal static int RunSmokeTest(string dataFilePath)
    {
        if (string.IsNullOrWhiteSpace(dataFilePath) || !File.Exists(dataFilePath))
            return 2;

        string tempPath = Path.Combine(
            Path.GetTempPath(),
            $"UndertaleModTool.WinUI.Smoke.{Guid.NewGuid():N}.win");
        string newDataTempPath = Path.Combine(
            Path.GetTempPath(),
            $"UndertaleModTool.WinUI.NewDataSmoke.{Guid.NewGuid():N}.win");

        try
        {
            LoadedGame loadedGame = LoadGame(dataFilePath);
            try
            {
                if (!ValidateSmokeCategories(loadedGame.Data, loadedGame.Categories))
                    return 4;

                SaveGame(tempPath, loadedGame.Data);
            }
            finally
            {
                loadedGame.Data.Dispose();
            }

            if (!File.Exists(tempPath) || new FileInfo(tempPath).Length == 0)
                return 3;

            using (UndertaleData newData = UndertaleData.CreateNew())
            {
                IReadOnlyList<ResourceCategory> newDataCategories = BuildCategories(newData);
                if (!ValidateSmokeCategories(newData, newDataCategories))
                    return 5;

                SaveGame(newDataTempPath, newData);
            }

            if (!File.Exists(newDataTempPath) || new FileInfo(newDataTempPath).Length == 0)
                return 6;

            return 0;
        }
        catch
        {
            return 1;
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                if (File.Exists(newDataTempPath))
                    File.Delete(newDataTempPath);
            }
            catch
            {
                // Best effort cleanup for a development smoke path.
            }
        }
    }

    internal static int RunReferenceSmokeTest(string dataFilePath, string? outputPath = null)
    {
        Dictionary<string, object?> metrics = new()
        {
            ["dataFilePath"] = dataFilePath,
            ["outputPath"] = outputPath,
            ["startedUtc"] = DateTimeOffset.UtcNow
        };

        if (string.IsNullOrWhiteSpace(dataFilePath) || !File.Exists(dataFilePath))
        {
            metrics["exitCode"] = 2;
            metrics["error"] = "Data file does not exist.";
            PrintPerfSmokeMetrics(metrics, outputPath, 2);
            return 2;
        }

        UndertaleData? data = null;
        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            using (FileStream stream = File.OpenRead(dataFilePath))
            {
                data = UndertaleIO.Read(stream, (_, _) => { });
            }
            stopwatch.Stop();
            metrics["readMs"] = ToMilliseconds(stopwatch);

            Dictionary<Type, string> selectedTypes = BuildReferenceTypeDictionary(data, includeStrings: false);
            metrics["selectedTypeCount"] = selectedTypes.Count;
            metrics["stringsIncluded"] = false;

            stopwatch.Restart();
            Dictionary<string, List<object>>? results =
                UndertaleResourceReferenceMethodsMap.GetUnreferencedObjects(data, selectedTypes)
                                                    .GetAwaiter()
                                                    .GetResult();
            stopwatch.Stop();

            metrics["referenceSearchMs"] = ToMilliseconds(stopwatch);
            metrics["resultGroupCount"] = results?.Count ?? 0;
            metrics["resultCount"] = results?.Values.Sum(list => list.Count) ?? 0;
            metrics["exitCode"] = selectedTypes.Count > 0 ? 0 : 4;
            return PrintPerfSmokeMetrics(metrics, outputPath, selectedTypes.Count > 0 ? 0 : 4);
        }
        catch (Exception ex)
        {
            metrics["exitCode"] = 1;
            metrics["error"] = ex.ToString();
            return PrintPerfSmokeMetrics(metrics, outputPath, 1);
        }
        finally
        {
            data?.Dispose();
        }
    }

    internal static int RunRuntimeSmokeTest(string dataFilePath, string? outputPath = null)
    {
        Dictionary<string, object?> metrics = new()
        {
            ["dataFilePath"] = dataFilePath,
            ["outputPath"] = outputPath,
            ["startedUtc"] = DateTimeOffset.UtcNow
        };

        if (string.IsNullOrWhiteSpace(dataFilePath) || !File.Exists(dataFilePath))
        {
            metrics["exitCode"] = 2;
            metrics["error"] = "Data file does not exist.";
            return PrintPerfSmokeMetrics(metrics, outputPath, 2);
        }

        UndertaleData? data = null;
        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            using (FileStream stream = File.OpenRead(dataFilePath))
            {
                data = UndertaleIO.Read(stream, (_, _) => { });
            }
            stopwatch.Stop();
            metrics["readMs"] = ToMilliseconds(stopwatch);

            stopwatch.Restart();
            RuntimeCandidate[] runtimes = DiscoverRuntimes(dataFilePath, data).ToArray();
            stopwatch.Stop();
            metrics["runtimeDiscoveryMs"] = ToMilliseconds(stopwatch);
            metrics["runtimeCount"] = runtimes.Length;
            metrics["debugRuntimeCount"] = runtimes.Count(runtime => runtime.DebuggerPath is not null);
            metrics["runtimes"] = runtimes.Select(runtime => runtime.DisplayText).ToArray();
            metrics["exitCode"] = 0;
            return PrintPerfSmokeMetrics(metrics, outputPath, 0);
        }
        catch (Exception ex)
        {
            metrics["exitCode"] = 1;
            metrics["error"] = ex.ToString();
            return PrintPerfSmokeMetrics(metrics, outputPath, 1);
        }
        finally
        {
            data?.Dispose();
        }
    }

    internal static int RunPerfSmokeTest(string dataFilePath, string? outputPath = null)
    {
        Dictionary<string, object?> metrics = new()
        {
            ["dataFilePath"] = dataFilePath,
            ["outputPath"] = outputPath,
            ["startedUtc"] = DateTimeOffset.UtcNow
        };

        if (string.IsNullOrWhiteSpace(dataFilePath) || !File.Exists(dataFilePath))
        {
            metrics["exitCode"] = 2;
            metrics["error"] = "Data file does not exist.";
            PrintPerfSmokeMetrics(metrics, outputPath);
            return 2;
        }

        string tempPath = Path.Combine(
            Path.GetTempPath(),
            $"UndertaleModTool.WinUI.PerfSmoke.{Guid.NewGuid():N}.win");

        UndertaleData? data = null;
        try
        {
            metrics["dataFileBytes"] = new FileInfo(dataFilePath).Length;
            metrics["gcMemoryBeforeBytes"] = GC.GetTotalMemory(forceFullCollection: true);

            StringBuilder warnings = new();
            int warningCount = 0;
            int importantWarningCount = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            using (FileStream stream = File.OpenRead(dataFilePath))
            {
                data = UndertaleIO.Read(stream, (warning, important) =>
                {
                    warningCount++;
                    if (important)
                        importantWarningCount++;
                    warnings.Append(important ? "[important] " : "[warning] ");
                    warnings.AppendLine(warning);
                });
            }
            stopwatch.Stop();
            metrics["readMs"] = ToMilliseconds(stopwatch);
            metrics["warningCount"] = warningCount;
            metrics["importantWarningCount"] = importantWarningCount;

            stopwatch.Restart();
            IReadOnlyList<ResourceCategory> categories = BuildCategories(data);
            stopwatch.Stop();
            metrics["buildCategoriesMs"] = ToMilliseconds(stopwatch);
            metrics["categoryCount"] = categories.Count;
            metrics["resourceCount"] = categories.Sum(category => category.Count);

            stopwatch.Restart();
            int detailRowCount = CountDetailRows(categories);
            stopwatch.Stop();
            metrics["buildAllDetailRowsMs"] = ToMilliseconds(stopwatch);
            metrics["detailRowCount"] = detailRowCount;

            stopwatch.Restart();
            int editablePropertyRowCount = CountEditablePropertyRows(categories);
            stopwatch.Stop();
            metrics["buildAllEditableRowsMs"] = ToMilliseconds(stopwatch);
            metrics["editablePropertyRowCount"] = editablePropertyRowCount;

            TexturePreviewPerfMetrics texturePreviewMetrics = MeasureTexturePreviewBuild(data);
            metrics["texturePreviewSampleCount"] = texturePreviewMetrics.SampleCount;
            metrics["texturePreviewTexturePageItemCount"] = texturePreviewMetrics.TexturePageItemCount;
            metrics["texturePreviewEmbeddedTextureCount"] = texturePreviewMetrics.EmbeddedTextureCount;
            metrics["texturePreviewFirstPassMs"] = texturePreviewMetrics.FirstPassMs;
            metrics["texturePreviewSecondPassMs"] = texturePreviewMetrics.SecondPassMs;
            metrics["texturePreviewBytes"] = texturePreviewMetrics.Bytes;

            RoomTilePalettePreviewPerfMetrics tilePalettePreviewMetrics = MeasureRoomTilePalettePreviewBuild(data);
            metrics["tilePalettePreviewSampleCount"] = tilePalettePreviewMetrics.SampleCount;
            metrics["tilePalettePreviewBackgroundCount"] = tilePalettePreviewMetrics.BackgroundCount;
            metrics["tilePalettePreviewFirstPassMs"] = tilePalettePreviewMetrics.FirstPassMs;
            metrics["tilePalettePreviewSecondPassMs"] = tilePalettePreviewMetrics.SecondPassMs;
            metrics["tilePalettePreviewBytes"] = tilePalettePreviewMetrics.Bytes;

            RoomTilePreviewPerfMetrics roomTilePreviewMetrics = MeasureRoomTilePreviewBuild(data);
            metrics["roomTilePreviewSampleCount"] = roomTilePreviewMetrics.SampleCount;
            metrics["roomTilePreviewRoomCount"] = roomTilePreviewMetrics.RoomCount;
            metrics["roomTilePreviewFirstPassMs"] = roomTilePreviewMetrics.FirstPassMs;
            metrics["roomTilePreviewSecondPassMs"] = roomTilePreviewMetrics.SecondPassMs;
            metrics["roomTilePreviewBytes"] = roomTilePreviewMetrics.Bytes;

            stopwatch.Restart();
            bool categoriesValid = ValidateSmokeCategories(data, categories);
            stopwatch.Stop();
            metrics["validateSmokeCategoriesMs"] = ToMilliseconds(stopwatch);
            metrics["categoriesValid"] = categoriesValid;
            if (!categoriesValid)
            {
                metrics["exitCode"] = 4;
                return PrintPerfSmokeMetrics(metrics, outputPath, 4);
            }

            stopwatch.Restart();
            SaveGame(tempPath, data);
            stopwatch.Stop();
            metrics["saveMs"] = ToMilliseconds(stopwatch);
            metrics["savedFileBytes"] = new FileInfo(tempPath).Length;
            metrics["gcMemoryAfterBytes"] = GC.GetTotalMemory(forceFullCollection: true);
            metrics["exitCode"] = 0;
            return PrintPerfSmokeMetrics(metrics, outputPath, 0);
        }
        catch (Exception ex)
        {
            metrics["exitCode"] = 1;
            metrics["error"] = ex.ToString();
            return PrintPerfSmokeMetrics(metrics, outputPath, 1);
        }
        finally
        {
            data?.Dispose();
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // Best effort cleanup for a development smoke path.
            }
        }
    }

    private static int PrintPerfSmokeMetrics(Dictionary<string, object?> metrics, string? outputPath, int exitCode = 0)
    {
        string json = JsonSerializer.Serialize(metrics, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        Console.WriteLine(json);
        if (!string.IsNullOrWhiteSpace(outputPath))
            File.WriteAllText(outputPath, json);
        return exitCode;
    }

    private static double ToMilliseconds(Stopwatch stopwatch) =>
        Math.Round(stopwatch.Elapsed.TotalMilliseconds, 3);

    private static int CountDetailRows(IReadOnlyList<ResourceCategory> categories)
    {
        int count = 0;
        foreach (ResourceCategory category in categories)
        {
            foreach (ResourceItem item in category.Items)
                count += BuildDetails(item).Count();
        }

        return count;
    }

    private static TexturePreviewPerfMetrics MeasureTexturePreviewBuild(UndertaleData data)
    {
        UndertaleTexturePageItem[] texturePageItems = data.TexturePageItems
            .Where(item => item is not null)
            .Take(PreviewSmokeTexturePageItemLimit)
            .ToArray();
        UndertaleEmbeddedTexture[] embeddedTextures = data.EmbeddedTextures
            .Where(texture => texture?.TextureData?.Image is not null)
            .Take(PreviewSmokeEmbeddedTextureLimit)
            .ToArray();
        object[] previewValues = texturePageItems.Cast<object>()
                                                 .Concat(embeddedTextures)
                                                 .ToArray();
        Dictionary<UndertaleTexturePageItem, byte[]> texturePageItemCache = [];
        Dictionary<UndertaleEmbeddedTexture, byte[]> embeddedTextureCache = [];
        using TextureWorker textureWorker = new();

        long bytes = 0;
        Stopwatch stopwatch = Stopwatch.StartNew();
        foreach (object value in previewValues)
            bytes += GetPerfPreviewPng(value, textureWorker, texturePageItemCache, embeddedTextureCache).Length;
        stopwatch.Stop();
        double firstPassMs = ToMilliseconds(stopwatch);

        stopwatch.Restart();
        foreach (object value in previewValues)
            bytes += GetPerfPreviewPng(value, textureWorker, texturePageItemCache, embeddedTextureCache).Length;
        stopwatch.Stop();

        return new TexturePreviewPerfMetrics(
            previewValues.Length,
            texturePageItems.Length,
            embeddedTextures.Length,
            firstPassMs,
            ToMilliseconds(stopwatch),
            bytes);
    }

    private static byte[] GetPerfPreviewPng(
        object value,
        TextureWorker textureWorker,
        Dictionary<UndertaleTexturePageItem, byte[]> texturePageItemCache,
        Dictionary<UndertaleEmbeddedTexture, byte[]> embeddedTextureCache)
    {
        if (value is UndertaleTexturePageItem item)
        {
            if (!texturePageItemCache.TryGetValue(item, out byte[]? bytes))
            {
                bytes = BuildTexturePageItemPreviewPng(item, textureWorker);
                texturePageItemCache[item] = bytes;
            }

            return bytes;
        }

        if (value is UndertaleEmbeddedTexture texture)
        {
            if (!embeddedTextureCache.TryGetValue(texture, out byte[]? bytes))
            {
                bytes = BuildEmbeddedTexturePreviewPng(texture);
                embeddedTextureCache[texture] = bytes;
            }

            return bytes;
        }

        throw new InvalidOperationException($"Unsupported texture preview type {value.GetType().Name}");
    }

    private static RoomTilePalettePreviewPerfMetrics MeasureRoomTilePalettePreviewBuild(UndertaleData data)
    {
        List<RoomPreviewTileKey> tileKeys = [];
        int backgroundCount = 0;
        foreach (UndertaleBackground background in data.Backgrounds)
        {
            if (tileKeys.Count >= PreviewSmokeRoomTilePaletteLimit)
                break;

            if (background?.Texture is null ||
                background.GMS2TileIds is null ||
                background.GMS2TileIds.Count == 0)
            {
                continue;
            }

            int beforeCount = tileKeys.Count;
            int tileWidth = (int)Math.Max(1, background.GMS2TileWidth);
            int tileHeight = (int)Math.Max(1, background.GMS2TileHeight);
            int step = Math.Max(1, (int)background.GMS2ItemsPerTileCount);
            for (int index = 0;
                 index < background.GMS2TileIds.Count && tileKeys.Count < PreviewSmokeRoomTilePaletteLimit;
                 index += step)
            {
                uint tileId = background.GMS2TileIds[index].ID & RoomTileIndexMask;
                if (tileId == 0)
                    continue;

                if (!TryGetGms2TileSource(background, tileId, out int sourceX, out int sourceY))
                    continue;

                tileKeys.Add(new RoomPreviewTileKey(background.Texture, sourceX, sourceY, tileWidth, tileHeight, 0));
            }

            if (tileKeys.Count > beforeCount)
                backgroundCount++;
        }

        Dictionary<RoomPreviewTileKey, byte[]> roomTileCache = [];
        using TextureWorker textureWorker = new();

        long bytes = 0;
        Stopwatch stopwatch = Stopwatch.StartNew();
        foreach (RoomPreviewTileKey key in tileKeys)
            bytes += GetPerfRoomTilePreviewPng(key, textureWorker, roomTileCache).Length;
        stopwatch.Stop();
        double firstPassMs = ToMilliseconds(stopwatch);

        stopwatch.Restart();
        foreach (RoomPreviewTileKey key in tileKeys)
            bytes += GetPerfRoomTilePreviewPng(key, textureWorker, roomTileCache).Length;
        stopwatch.Stop();

        return new RoomTilePalettePreviewPerfMetrics(
            tileKeys.Count,
            backgroundCount,
            firstPassMs,
            ToMilliseconds(stopwatch),
            bytes);
    }

    private static byte[] GetPerfRoomTilePreviewPng(
        RoomPreviewTileKey key,
        TextureWorker textureWorker,
        Dictionary<RoomPreviewTileKey, byte[]> roomTileCache)
    {
        if (!roomTileCache.TryGetValue(key, out byte[]? bytes))
        {
            bytes = BuildRoomTilePreviewPng(key, textureWorker);
            roomTileCache[key] = bytes;
        }

        return bytes;
    }

    private static RoomTilePreviewPerfMetrics MeasureRoomTilePreviewBuild(UndertaleData data)
    {
        List<RoomPreviewTileKey> tileKeys = [];
        int roomCount = 0;
        foreach (UndertaleRoom room in data.Rooms)
        {
            if (tileKeys.Count >= PreviewSmokeRoomTilePaletteLimit)
                break;

            int beforeCount = tileKeys.Count;
            foreach (RoomPreviewTileSummary summary in BuildRoomPreviewTileSummaries(room, PreviewSmokeRoomTilePaletteLimit - tileKeys.Count))
            {
                if (tileKeys.Count >= PreviewSmokeRoomTilePaletteLimit)
                    break;

                tileKeys.Add(summary.TileKey);
            }

            if (tileKeys.Count > beforeCount)
                roomCount++;
        }

        Dictionary<RoomPreviewTileKey, byte[]> roomTileCache = [];
        using TextureWorker textureWorker = new();

        long bytes = 0;
        Stopwatch stopwatch = Stopwatch.StartNew();
        foreach (RoomPreviewTileKey key in tileKeys)
            bytes += GetPerfRoomTilePreviewPng(key, textureWorker, roomTileCache).Length;
        stopwatch.Stop();
        double firstPassMs = ToMilliseconds(stopwatch);

        stopwatch.Restart();
        foreach (RoomPreviewTileKey key in tileKeys)
            bytes += GetPerfRoomTilePreviewPng(key, textureWorker, roomTileCache).Length;
        stopwatch.Stop();

        return new RoomTilePreviewPerfMetrics(
            tileKeys.Count,
            roomCount,
            firstPassMs,
            ToMilliseconds(stopwatch),
            bytes);
    }

    private static int CountEditablePropertyRows(IReadOnlyList<ResourceCategory> categories)
    {
        int count = 0;
        foreach (ResourceCategory category in categories)
        {
            foreach (ResourceItem item in category.Items)
                count += BuildEditableProperties(item.Value).Count();
        }

        return count;
    }

    private static bool ValidateSmokeCategories(UndertaleData data, IReadOnlyList<ResourceCategory> categories)
    {
        string[] requiredLabels =
        [
            "Sprites",
            "Sounds",
            "Rooms",
            "Objects",
            "Code",
            "General info",
            "Global init",
            "Game End scripts",
            "Audio groups",
            "Strings",
            "Texture page items",
            "Embedded textures",
            "Embedded audio",
            "Texture group information",
            "Embedded images",
            "Fonts",
            "Scripts",
            "Extensions",
            "Backgrounds",
            "Paths",
            "Shaders",
            "Timelines",
            "Variables",
            "Functions",
            "Code locals",
            "Sequences",
            "Animation curves",
            "Particle systems",
            "Particle system emitters"
        ];

        if (categories.Count != requiredLabels.Length)
            return false;

        foreach (string label in requiredLabels)
        {
            ResourceCategory? category = categories.FirstOrDefault(candidate => candidate.Label == label);
            if (category is null || category.Count != category.Items.Count)
                return false;

            ResourceItem? firstItem = category.Items.FirstOrDefault();
            if (firstItem is not null && !BuildDetails(firstItem).Any())
                return false;
        }

        if (data.Sprites.FirstOrDefault(sprite => sprite is not null) is UndertaleSprite sprite)
        {
            if (BuildSpriteFrameItems(sprite).Count != sprite.Textures.Count)
                return false;

            if (BuildSpriteTextureSummaries(sprite).Count == 0)
                return false;
        }

        return true;
    }

    private static ResourceCategory BuildCategory(string label, IEnumerable? source)
    {
        WinUiToolSettings.EnsureLoaded();
        bool showNullEntries = WinUiToolSettings.Instance.ShowNullEntriesInResourceTree;
        List<ResourceItem> items = [];

        if (source is IList list)
        {
            for (int index = 0; index < list.Count; index++)
            {
                object? value = list[index];
                if (value is null)
                {
                    if (showNullEntries)
                        items.Add(BuildNullResourceItem(index));
                    continue;
                }

                items.Add(BuildResourceItem(value, index));
            }
        }
        else if (source is not null)
        {
            int index = 0;
            foreach (object? value in source)
            {
                if (value is null)
                {
                    if (showNullEntries)
                        items.Add(BuildNullResourceItem(index));
                }
                else
                {
                    items.Add(BuildResourceItem(value, index));
                }

                index++;
            }
        }

        return new ResourceCategory(label, GetCategorySymbol(label), items.Count, items, source as IList);
    }

    private static ResourceCategory BuildSingleItemCategory(string label, object? item)
    {
        ResourceItem[] items = item is null ? [] : [BuildResourceItem(item, 0)];
        return new ResourceCategory(label, GetCategorySymbol(label), items.Length, items, null);
    }

    private static Symbol GetCategorySymbol(string label)
    {
        return label switch
        {
            "Sprites" => Symbol.Pictures,
            "Sounds" => Symbol.Audio,
            "General info" => Symbol.Setting,
            "Global init" => Symbol.Library,
            "Game End scripts" => Symbol.Stop,
            "Audio groups" => Symbol.Audio,
            "Rooms" => Symbol.Home,
            "Objects" => Symbol.World,
            "Code" => Symbol.Document,
            "Strings" => Symbol.Character,
            "Texture page items" => Symbol.Crop,
            "Embedded textures" => Symbol.Pictures,
            "Embedded audio" => Symbol.Audio,
            "Texture group information" => Symbol.Library,
            "Embedded images" => Symbol.Pictures,
            "Fonts" => Symbol.Font,
            "Scripts" => Symbol.Page,
            "Extensions" => Symbol.Setting,
            "Backgrounds" => Symbol.BrowsePhotos,
            "Paths" => Symbol.Map,
            "Shaders" => Symbol.Setting,
            "Timelines" => Symbol.Clock,
            "Variables" => Symbol.Tag,
            "Functions" => Symbol.Library,
            "Code locals" => Symbol.Bullets,
            "Sequences" => Symbol.Play,
            "Animation curves" => Symbol.Trim,
            "Particle systems" => Symbol.Setting,
            "Particle system emitters" => Symbol.Remote,
            _ => Symbol.Document
        };
    }

    private static ResourceItem BuildResourceItem(object item, int index)
    {
        if (item is NullResourcePlaceholder)
            return BuildNullResourceItem(index);

        string typeName = item.GetType().Name;
        string title = FormatResourceTitle(item, index);
        string subtitle = $"#{index} - {typeName}";
        return new ResourceItem(index, title, subtitle, GetResourceSymbol(item), item);
    }

    private static ResourceItem BuildNullResourceItem(int index)
    {
        return new ResourceItem(index, $"Null entry #{index}", $"#{index} - removed or empty resource slot", Symbol.Document, NullResourcePlaceholder.Instance);
    }

    private static Symbol GetResourceSymbol(object item)
    {
        return item switch
        {
            UndertaleSprite => Symbol.Pictures,
            UndertaleSound => Symbol.Audio,
            UndertaleGeneralInfo => Symbol.Setting,
            UndertaleGlobalInit => Symbol.Library,
            UndertaleAudioGroup => Symbol.Audio,
            UndertaleRoom => Symbol.Home,
            UndertaleGameObject => Symbol.World,
            UndertaleCode => Symbol.Document,
            UndertaleString => Symbol.Character,
            UndertaleTexturePageItem => Symbol.Crop,
            UndertaleEmbeddedTexture => Symbol.Pictures,
            UndertaleEmbeddedAudio => Symbol.Audio,
            UndertaleTextureGroupInfo => Symbol.Library,
            UndertaleEmbeddedImage => Symbol.Pictures,
            UndertaleFont => Symbol.Font,
            UndertaleScript => Symbol.Page,
            UndertaleExtension => Symbol.Setting,
            UndertaleBackground => Symbol.BrowsePhotos,
            UndertalePath => Symbol.Map,
            UndertaleShader => Symbol.Setting,
            UndertaleTimeline => Symbol.Clock,
            UndertaleVariable => Symbol.Tag,
            UndertaleFunction => Symbol.Library,
            UndertaleCodeLocals => Symbol.Bullets,
            UndertaleSequence => Symbol.Play,
            UndertaleAnimationCurve => Symbol.Trim,
            UndertaleParticleSystem => Symbol.Setting,
            UndertaleParticleSystemEmitter => Symbol.Remote,
            _ => Symbol.Document
        };
    }

    private static IEnumerable<DetailRow> BuildDetails(ResourceItem item)
    {
        if (item.Value is NullResourcePlaceholder)
        {
            yield return new DetailRow("Index", item.Index.ToString(CultureInfo.InvariantCulture));
            yield return new DetailRow("Type", "Null resource entry");
            yield break;
        }

        Type valueType = item.Value.GetType();
        yield return new DetailRow("Index", item.Index.ToString());
        yield return new DetailRow("Type", valueType.FullName ?? valueType.Name);

        if (item.Value is UndertaleNamedResource named)
            yield return new DetailRow("Name", FormatTitle(named.Name?.Content));

        if (item.Value is UndertaleString str)
            yield return new DetailRow("Content", FormatTitle(str.Content));

        if (item.Value is UndertaleGlobalInit globalInit)
            yield return new DetailRow("Code", FormatTitle(globalInit.Code?.Name?.Content));

        if (item.Value is UndertaleEmbeddedImage embeddedImage)
            yield return new DetailRow("Texture entry", FormatObjectTitle(embeddedImage.TextureEntry));

        if (item.Value is UndertaleTextureGroupInfo textureGroup)
        {
            yield return new DetailRow("Texture pages", (textureGroup.TexturePages?.Count ?? 0).ToString(CultureInfo.InvariantCulture));
            yield return new DetailRow("Sprites", (textureGroup.Sprites?.Count ?? 0).ToString(CultureInfo.InvariantCulture));
            yield return new DetailRow("Spine sprites", (textureGroup.SpineSprites?.Count ?? 0).ToString(CultureInfo.InvariantCulture));
            yield return new DetailRow("Fonts", (textureGroup.Fonts?.Count ?? 0).ToString(CultureInfo.InvariantCulture));
            yield return new DetailRow("Tilesets", (textureGroup.Tilesets?.Count ?? 0).ToString(CultureInfo.InvariantCulture));
        }

        foreach (PropertyInfo property in GetDetailProperties(valueType))
        {
            string? value = TryFormatPropertyValue(item.Value, property);
            if (value is null)
                continue;

            yield return new DetailRow(property.Name, value);
        }
    }

    private static PropertyInfo[] GetDetailProperties(Type type)
    {
        return DetailPropertiesByType.GetOrAdd(type, static currentType =>
            currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                       .Where(IsDetailProperty)
                       .ToArray());
    }

    private static bool IsDetailProperty(PropertyInfo property)
    {
        if (property.GetIndexParameters().Length != 0)
            return false;

        if (property.Name is "Name" or "Content")
            return false;

        Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        return IsSimpleDetailType(type) || typeof(UndertaleString).IsAssignableFrom(type);
    }

    private static string? TryFormatPropertyValue(object owner, PropertyInfo property)
    {
        try
        {
            object? value = property.GetValue(owner);
            return FormatDetailValue(value);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsSimpleDetailType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(Guid);
    }

    private static string FormatObjectTitle(object value)
    {
        return value switch
        {
            UndertaleNamedResource named => FormatTitle(named.Name?.Content),
            UndertaleString str => FormatTitle(str.Content),
            _ => FormatTitle(value.ToString())
        };
    }

    private static string FormatResourceTitle(object value, int index)
    {
        return value switch
        {
            UndertaleSprite sprite => FormatNamedResourceTitle(sprite.Name?.Content, "Sprite", index),
            UndertaleSound sound => FormatNamedResourceTitle(sound.Name?.Content, "Sound", index),
            UndertaleRoom room => FormatNamedResourceTitle(room.Name?.Content, "Room", index),
            UndertaleGameObject gameObject => FormatNamedResourceTitle(gameObject.Name?.Content, "Object", index),
            UndertaleCode code => FormatNamedResourceTitle(code.Name?.Content, "Code", index),
            UndertaleGeneralInfo => "General info",
            UndertaleGlobalInit globalInit => FormatNamedResourceTitle(globalInit.Code?.Name?.Content, "Code entry", index),
            UndertaleNamedResource named => FormatNamedResourceTitle(named.Name?.Content, value.GetType().Name, index),
            UndertaleString str => FormatTitle(str.Content),
            _ => FormatTitle(value.ToString())
        };
    }

    private static string GetResourceClipboardName(ResourceItem item)
    {
        return item.Value switch
        {
            UndertaleNamedResource named => named.Name?.Content ?? item.Title,
            UndertaleString str => str.Content ?? string.Empty,
            UndertaleGlobalInit globalInit => globalInit.Code?.Name?.Content ?? item.Title,
            _ => item.Title
        };
    }

    private static string FormatNamedResourceTitle(string? value, string fallbackType, int index)
    {
        if (string.IsNullOrWhiteSpace(value))
            return $"{fallbackType} #{index}";

        return FormatTitle(value);
    }

    private static string FormatDetailValue(object? value)
    {
        return value switch
        {
            null => "(null)",
            UndertaleString str => FormatTitle(str.Content),
            _ => FormatTitle(value.ToString())
        };
    }

    private static string FormatTitle(string? value)
    {
        if (value is null)
            return "(null)";

        if (value.Length == 0)
            return "(empty string)";

        if (value.Length > 256)
            value = value[..256] + "...";

        return NewLineRegex.Replace(value, " ");
    }

    private static SolidColorBrush CreateAccentBrush()
    {
        return Application.Current.Resources["WinUiAccentBrush"] is SolidColorBrush brush
            ? new SolidColorBrush(brush.Color)
            : new SolidColorBrush(Microsoft.UI.Colors.LightGray);
    }

    private void ShowEditorFor(ResourceItem item)
    {
        bool preserveDetailsExpanded = DetailsExpander.IsExpanded || _keepDetailsExpanded;
        DetailsExpander.Visibility = item.Value is UndertaleCode or UndertaleSprite
            ? Visibility.Collapsed
            : Visibility.Visible;
        if (DetailsExpander.Visibility == Visibility.Visible)
            DetailsExpander.IsExpanded = preserveDetailsExpanded;

        if (item.Value is UndertaleNamedResource named && item.Value is not UndertaleSprite)
        {
            _isUpdatingNamedResourceEditor = true;
            ResourceNameBox.Text = named.Name?.Content ?? string.Empty;
            _isUpdatingNamedResourceEditor = false;
            NamedResourceEditorPanel.Visibility = Visibility.Visible;
        }
        else
        {
            HideNamedResourceEditor();
        }

        if (item.Value is UndertaleString str)
        {
            _isUpdatingStringEditor = true;
            StringContentBox.Text = str.Content ?? string.Empty;
            _isUpdatingStringEditor = false;
            StringEditorPanel.Visibility = Visibility.Visible;
            HideNonStringEditors();
            return;
        }

        HideStringEditor();

        if (item.Value is UndertaleSprite)
        {
            HideScalarEditor();
            ShowCustomSpriteEditorFor(item);
        }
        else
        {
            HideCustomSpriteEditor();
            ShowScalarEditorFor(item);
            HideSpritePreview();
        }

        ShowCodeViewerFor(item);
        ShowCodeLocalsEditorFor(item);
        ShowAudioGroupEditorFor(item);
        ShowResourceReferenceEditorFor(item);
        ShowGeneralInfoEditorFor(item);
        ShowTextureGroupEditorFor(item);
        ShowObjectSummaryFor(item);
        ShowRoomSummaryFor(item);
        ShowBackgroundSummaryFor(item);
        ShowPathSummaryFor(item);
        ShowFontSummaryFor(item);
        ShowShaderEditorFor(item);
        ShowTimelineEditorFor(item);
        ShowExtensionEditorFor(item);
        ShowParticleSystemEditorFor(item);
        ShowParticleEmitterEditorFor(item);
        ShowTexturePageItemReferencesFor(item);
        ShowTexturePreviewFor(item);
        ShowSoundAudioEditorFor(item);
        ShowEmbeddedAudioEditorFor(item);
    }

    private void HideNonStringEditors()
    {
        HideScalarEditor();
        HideCodeViewer();
        HideCodeLocalsEditor();
        HideAudioGroupEditor();
        HideResourceReferenceEditor();
        HideGeneralInfoEditor();
        HideTextureGroupEditor();
        HideSpritePreview();
        HideCustomSpriteEditor();
        HideObjectSummary();
        HideRoomSummary();
        HideBackgroundSummary();
        HidePathSummary();
        HideFontSummary();
        HideShaderEditor();
        HideTimelineEditor();
        HideExtensionEditor();
        HideParticleSystemEditor();
        HideParticleEmitterEditor();
        HideTexturePageItemReferences();
        HideTexturePreview();
        HideSoundAudioEditor();
        HideEmbeddedAudioEditor();
    }

    private void HideEditors()
    {
        HideProjectExportToggle();
        DetailsExpander.Visibility = Visibility.Collapsed;
        HideNamedResourceEditor();
        HideStringEditor();
        HideScalarEditor();
        HideCodeViewer();
        HideCodeLocalsEditor();
        HideAudioGroupEditor();
        HideResourceReferenceEditor();
        HideGeneralInfoEditor();
        HideTextureGroupEditor();
        HideSpritePreview();
        HideCustomSpriteEditor();
        HideObjectSummary();
        HideRoomSummary();
        HideBackgroundSummary();
        HidePathSummary();
        HideFontSummary();
        HideShaderEditor();
        HideTimelineEditor();
        HideExtensionEditor();
        HideParticleSystemEditor();
        HideParticleEmitterEditor();
        HideTexturePageItemReferences();
        HideTexturePreview();
        HideSoundAudioEditor();
        HideEmbeddedAudioEditor();
    }

    private void SetNoDataBrowserState(bool isNoData)
    {
        NoDataEmptyStatePanel.Visibility = isNoData ? Visibility.Visible : Visibility.Collapsed;
        RecentFilesPanel.Visibility = isNoData ? Visibility.Visible : Visibility.Collapsed;
        ResourceItemsPane.Visibility = isNoData ? Visibility.Collapsed : Visibility.Visible;
        ResourceItemsPanel.Visibility = isNoData ? Visibility.Collapsed : Visibility.Visible;
        if (isNoData)
            DetailsExpander.Visibility = Visibility.Collapsed;
        CategoryList.IsEnabled = !isNoData;
        ResourceList.IsEnabled = !isNoData;
        ResourceFilterBox.IsEnabled = !isNoData;
        CategoryList.Opacity = isNoData ? 0.62 : 1;
        ResourceItemsColumn.Width = isNoData ? new GridLength(0) : new GridLength(280);
        RecentColumn.Width = isNoData ? new GridLength(360) : new GridLength(0);
        Grid.SetColumn(DetailsScrollViewer, isNoData ? 1 : 2);
        Grid.SetColumnSpan(DetailsScrollViewer, isNoData ? 2 : 1);
        if (isNoData)
        {
            CategoryColumn.Width = new GridLength(280);
            CategoryList.ItemsSource = NoDataCategories.Value;
            CategoryList.SelectedIndex = -1;
            ResourceList.ItemsSource = null;
        }
        else
        {
            CategoryColumn.Width = new GridLength(270);
            ResourceItemsColumn.Width = new GridLength(280);
        }

        UpdateRecentFileUi();
    }

    private void HideNamedResourceEditor()
    {
        _isUpdatingNamedResourceEditor = true;
        ResourceNameBox.Text = string.Empty;
        _isUpdatingNamedResourceEditor = false;
        NamedResourceEditorPanel.Visibility = Visibility.Collapsed;
    }

    private void HideStringEditor()
    {
        _isUpdatingStringEditor = true;
        StringContentBox.Text = string.Empty;
        _isUpdatingStringEditor = false;
        StringEditorPanel.Visibility = Visibility.Collapsed;
    }

    private void ShowScalarEditorFor(ResourceItem item)
    {
        EditablePropertyRow[] rows = BuildEditableProperties(item.Value).ToArray();
        _isUpdatingScalarEditor = true;
        ScalarPropertiesList.ItemsSource = rows;
        if (ScalarEditorPanel is Expander scalarEditorPanel)
            scalarEditorPanel.IsExpanded = _keepScalarEditorExpanded;
        ScalarEditorPanel.Visibility = rows.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        _isUpdatingScalarEditor = false;
    }

    private void HideScalarEditor()
    {
        _isUpdatingScalarEditor = true;
        ScalarPropertiesList.ItemsSource = null;
        ScalarEditorPanel.Visibility = Visibility.Collapsed;
        _isUpdatingScalarEditor = false;
    }

    private void ShowCodeViewerFor(ResourceItem item)
    {
        if (_data is null || item.Value is not UndertaleCode code)
        {
            HideCodeViewer();
            return;
        }

        UpdateCodeViewer(_data, code);
        CodeViewerPanel.Visibility = Visibility.Visible;
    }

    private void HideCodeViewer()
    {
        CodeTextBox.Text = string.Empty;
        CodeTextBox.IsReadOnly = true;
        SetCodeEditToggle(false);
        CodeEditToggle.IsEnabled = false;
        CodeApplyButton.IsEnabled = false;
        CodeImportButton.IsEnabled = false;
        CodeExportButton.IsEnabled = false;
        SetCodeSearchText(string.Empty);
        _codeSearchMatches = [];
        _codeSearchMatchIndex = -1;
        UpdateCodeSearchControls();
        CodeViewerPanel.Visibility = Visibility.Collapsed;
    }

    private void ShowCodeLocalsEditorFor(ResourceItem item)
    {
        if (_data is null || item.Value is not UndertaleCodeLocals codeLocals)
        {
            HideCodeLocalsEditor();
            return;
        }

        CodeLocalsEditorPanel.Visibility = Visibility.Visible;
        RefreshCodeLocalsEditor(codeLocals, selectedLocal: null);
    }

    private void HideCodeLocalsEditor()
    {
        _isUpdatingCodeLocalsEditor = true;
        CodeLocalsSummaryText.Text = string.Empty;
        CodeLocalsList.ItemsSource = null;
        CodeLocalsList.SelectedItem = null;
        CodeLocalIndexBox.Text = string.Empty;
        CodeLocalNameBox.Text = string.Empty;
        CodeLocalIndexBox.IsReadOnly = true;
        CodeLocalNameBox.IsReadOnly = true;
        CodeLocalEditorPanel.Visibility = Visibility.Collapsed;
        CodeLocalsAddButton.IsEnabled = false;
        CodeLocalsRemoveButton.IsEnabled = false;
        CodeLocalsEditorPanel.Visibility = Visibility.Collapsed;
        _isUpdatingCodeLocalsEditor = false;
    }

    private void RefreshCodeLocalsEditor(UndertaleCodeLocals codeLocals, UndertaleCodeLocals.LocalVar? selectedLocal)
    {
        CodeLocalsSummaryText.Text = BuildCodeLocalsSummary(codeLocals);
        CodeLocalSummary[] summaries = BuildCodeLocalSummaries(codeLocals).ToArray();
        CodeLocalsList.ItemsSource = summaries;

        CodeLocalSummary? selectedSummary = selectedLocal is null
            ? summaries.FirstOrDefault()
            : summaries.FirstOrDefault(summary => ReferenceEquals(summary.Local, selectedLocal));
        CodeLocalsList.SelectedItem = selectedSummary;

        bool canMutate = _data is not null && !_data.UnsupportedBytecodeVersion;
        CodeLocalsAddButton.IsEnabled = canMutate;
        CodeLocalsRemoveButton.IsEnabled = canMutate && selectedSummary is not null;
        CodeLocalIndexBox.IsReadOnly = !canMutate;
        CodeLocalNameBox.IsReadOnly = !canMutate;
        UpdateCodeLocalEditor(selectedSummary);
    }

    private void UpdateCodeLocalEditor(CodeLocalSummary? summary)
    {
        _isUpdatingCodeLocalsEditor = true;
        try
        {
            if (summary is null)
            {
                CodeLocalIndexBox.Text = string.Empty;
                CodeLocalNameBox.Text = string.Empty;
                CodeLocalEditorPanel.Visibility = Visibility.Collapsed;
                return;
            }

            CodeLocalIndexBox.Text = summary.Local.Index.ToString(CultureInfo.InvariantCulture);
            CodeLocalNameBox.Text = summary.Local.Name?.Content ?? string.Empty;
            CodeLocalEditorPanel.Visibility = Visibility.Visible;
        }
        finally
        {
            _isUpdatingCodeLocalsEditor = false;
        }
    }

    private void SelectCodeLocal(UndertaleCodeLocals codeLocals, CodeLocalSummary summary)
    {
        CodeLocalsList.SelectedItem = summary;
        CodeLocalsRemoveButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion;
        UpdateCodeLocalEditor(summary);
    }

    private void CodeLocalsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (_selectedResource?.Value is not UndertaleCodeLocals codeLocals || e.ClickedItem is not CodeLocalSummary summary)
            return;

        SelectCodeLocal(codeLocals, summary);
    }

    private void CodeLocalsAddButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleCodeLocals codeLocals)
        {
            return;
        }

        int index = codeLocals.Locals.Count;
        UndertaleCodeLocals.LocalVar local = new()
        {
            Index = (uint)index,
            Name = _data.Strings.MakeString($"local{index}")
        };
        codeLocals.Locals.Add(local);
        MarkDirty();
        RefreshCodeLocalsEditor(codeLocals, local);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Added code local.";
    }

    private void CodeLocalsRemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleCodeLocals codeLocals ||
            CodeLocalsList.SelectedItem is not CodeLocalSummary summary)
        {
            return;
        }

        int index = codeLocals.Locals.IndexOf(summary.Local);
        if (index < 0)
            return;

        codeLocals.Locals.RemoveAt(index);
        UndertaleCodeLocals.LocalVar? nextSelection = codeLocals.Locals.Count == 0
            ? null
            : codeLocals.Locals[Math.Clamp(index, 0, codeLocals.Locals.Count - 1)];
        MarkDirty();
        RefreshCodeLocalsEditor(codeLocals, nextSelection);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Removed code local.";
    }

    private void CodeLocalBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingCodeLocalsEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleCodeLocals codeLocals ||
            CodeLocalsList.SelectedItem is not CodeLocalSummary summary)
        {
            return;
        }

        if (!uint.TryParse(CodeLocalIndexBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint index))
        {
            UpdateCodeLocalEditor(summary);
            StatusBox.Text = "Could not update code local: expected an unsigned integer index.";
            return;
        }

        string name = CodeLocalNameBox.Text.Trim();
        if (name.Length == 0)
        {
            UpdateCodeLocalEditor(summary);
            StatusBox.Text = "Could not update code local: name cannot be empty.";
            return;
        }

        UndertaleCodeLocals.LocalVar local = summary.Local;
        if (local.Index == index && string.Equals(local.Name?.Content, name, StringComparison.Ordinal))
            return;

        local.Index = index;
        if (!string.Equals(local.Name?.Content, name, StringComparison.Ordinal))
            local.Name = _data.Strings.MakeString(name);

        MarkDirty();
        RefreshCodeLocalsEditor(codeLocals, local);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Updated code local.";
    }

    private void ShowAudioGroupEditorFor(ResourceItem item)
    {
        if (_data is null || item.Value is not UndertaleAudioGroup audioGroup)
        {
            HideAudioGroupEditor();
            return;
        }

        AudioGroupEditorPanel.Visibility = Visibility.Visible;
        UpdateAudioGroupEditor(audioGroup);
    }

    private void HideAudioGroupEditor()
    {
        _isUpdatingAudioGroupEditor = true;
        try
        {
            AudioGroupSummaryText.Text = string.Empty;
            AudioGroupPathBox.Text = string.Empty;
            AudioGroupPathBox.IsReadOnly = true;
            AudioGroupEditorPanel.Visibility = Visibility.Collapsed;
        }
        finally
        {
            _isUpdatingAudioGroupEditor = false;
        }
    }

    private void UpdateAudioGroupEditor(UndertaleAudioGroup audioGroup)
    {
        _isUpdatingAudioGroupEditor = true;
        try
        {
            AudioGroupSummaryText.Text = BuildAudioGroupSummary(audioGroup);
            AudioGroupPathBox.Text = audioGroup.Path?.Content ?? string.Empty;
            AudioGroupPathBox.IsReadOnly = _data is null || _data.UnsupportedBytecodeVersion;
        }
        finally
        {
            _isUpdatingAudioGroupEditor = false;
        }
    }

    private void AudioGroupPathBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingAudioGroupEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleAudioGroup audioGroup)
        {
            return;
        }

        string path = AudioGroupPathBox.Text.Trim();
        string current = audioGroup.Path?.Content ?? string.Empty;
        if (string.Equals(current, path, StringComparison.Ordinal))
            return;

        audioGroup.Path = path.Length == 0 ? null : _data.Strings.MakeString(path);
        MarkDirty();
        ClearExternalAudioGroupCache();
        UpdateAudioGroupEditor(audioGroup);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Updated audio group path.";
    }

    private void ShowResourceReferenceEditorFor(ResourceItem item)
    {
        if (_data is null ||
            !TryCreateResourceReferenceState(item, out ResourceReferenceState? state) ||
            state is null)
        {
            HideResourceReferenceEditor();
            return;
        }

        _isUpdatingResourceReferenceEditor = true;
        try
        {
            ResourceReferenceEditorPanel.Visibility = Visibility.Visible;
            ResourceReferenceTitleText.Text = state.Title;
            ResourceReferenceSummaryText.Text = state.Summary;
            ResourceReferencePickerBox.ItemsSource = state.Options;
            ResourceReferencePickerBox.SelectedItem = state.CurrentResource is null
                ? null
                : state.Options.FirstOrDefault(option => ReferenceEquals(option.Resource, state.CurrentResource));
            ResourceReferenceSetButton.IsEnabled = !_data.UnsupportedBytecodeVersion && ResourceReferencePickerBox.SelectedItem is ResourceReferenceOption;
            ResourceReferenceOpenButton.IsEnabled = state.CurrentIndex >= 0;
            ResourceReferenceClearButton.IsEnabled = !_data.UnsupportedBytecodeVersion && state.CanClear && state.CurrentResource is not null;
        }
        finally
        {
            _isUpdatingResourceReferenceEditor = false;
        }
    }

    private void HideResourceReferenceEditor()
    {
        _isUpdatingResourceReferenceEditor = true;
        ResourceReferenceTitleText.Text = string.Empty;
        ResourceReferenceSummaryText.Text = string.Empty;
        ResourceReferencePickerBox.ItemsSource = null;
        ResourceReferencePickerBox.SelectedItem = null;
        ResourceReferenceSetButton.IsEnabled = false;
        ResourceReferenceOpenButton.IsEnabled = false;
        ResourceReferenceClearButton.IsEnabled = false;
        ResourceReferenceEditorPanel.Visibility = Visibility.Collapsed;
        _isUpdatingResourceReferenceEditor = false;
    }

    private void ResourceReferencePickerBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingResourceReferenceEditor)
            return;

        ResourceReferenceSetButton.IsEnabled =
            _data is not null &&
            !_data.UnsupportedBytecodeVersion &&
            ResourceReferencePickerBox.SelectedItem is ResourceReferenceOption;
    }

    private void ResourceReferenceSetButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource is null ||
            ResourceReferencePickerBox.SelectedItem is not ResourceReferenceOption option ||
            !SetResourceReference(_selectedResource, option.Resource))
        {
            return;
        }

        MarkDirty();
        RefreshResourceReferenceEditorAfterChange();
        StatusBox.Text = $"Updated reference to {option.Title}.";
    }

    private void ResourceReferenceOpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedResource is null ||
            !TryCreateResourceReferenceState(_selectedResource, out ResourceReferenceState? state) ||
            state is null)
            return;

        if (state.CurrentIndex >= 0)
            NavigateToResource(state.CategoryLabel, state.CurrentIndex);
    }

    private void ResourceReferenceClearButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource is null ||
            !TryCreateResourceReferenceState(_selectedResource, out ResourceReferenceState? state) ||
            state is null ||
            !state.CanClear ||
            !ClearResourceReference(_selectedResource))
        {
            return;
        }

        MarkDirty();
        RefreshResourceReferenceEditorAfterChange();
        StatusBox.Text = "Cleared reference.";
    }

    private void RefreshResourceReferenceEditorAfterChange()
    {
        if (_selectedResource is null)
            return;

        ShowResourceReferenceEditorFor(_selectedResource);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        RefreshSelectedResourceTitle();
        RefreshSelectedResourceDependentPanels();
    }

    private void ShowGeneralInfoEditorFor(ResourceItem item)
    {
        if (_data is null || item.Value is not UndertaleGeneralInfo generalInfo)
        {
            HideGeneralInfoEditor();
            return;
        }

        GeneralInfoEditorPanel.Visibility = Visibility.Visible;
        RefreshGeneralInfoEditor(generalInfo);
    }

    private void HideGeneralInfoEditor()
    {
        _isUpdatingGeneralInfoEditor = true;
        GeneralInfoRoomOrderSummaryText.Text = string.Empty;
        GeneralInfoRoomPickerBox.ItemsSource = null;
        GeneralInfoRoomPickerBox.SelectedItem = null;
        GeneralInfoRoomOrderList.ItemsSource = null;
        GeneralInfoRoomOrderList.SelectedItem = null;
        GeneralInfoOptionsSummaryText.Text = string.Empty;
        GeneralInfoOptionsScalarList.ItemsSource = null;
        GeneralInfoOptionTextureSlotBox.ItemsSource = null;
        GeneralInfoOptionTextureSlotBox.SelectedItem = null;
        GeneralInfoOptionTextureItemBox.ItemsSource = null;
        GeneralInfoOptionTextureItemBox.SelectedItem = null;
        GeneralInfoConstantsList.ItemsSource = null;
        GeneralInfoConstantsList.SelectedItem = null;
        GeneralInfoConstantNameBox.Text = string.Empty;
        GeneralInfoConstantValueBox.Text = string.Empty;
        GeneralInfoLanguageSummaryText.Text = string.Empty;
        GeneralInfoLanguageScalarList.ItemsSource = null;
        GeneralInfoLanguagesList.ItemsSource = null;
        GeneralInfoOptionsPanel.Visibility = Visibility.Collapsed;
        GeneralInfoConstantEditorPanel.Visibility = Visibility.Collapsed;
        GeneralInfoLanguagePanel.Visibility = Visibility.Collapsed;
        GeneralInfoSyncRoomOrderButton.IsEnabled = false;
        GeneralInfoOpenRoomButton.IsEnabled = false;
        GeneralInfoSetRoomButton.IsEnabled = false;
        GeneralInfoRemoveRoomButton.IsEnabled = false;
        GeneralInfoSetTextureButton.IsEnabled = false;
        GeneralInfoOpenTextureButton.IsEnabled = false;
        GeneralInfoClearTextureButton.IsEnabled = false;
        GeneralInfoAddConstantButton.IsEnabled = false;
        GeneralInfoRemoveConstantButton.IsEnabled = false;
        GeneralInfoEditorPanel.Visibility = Visibility.Collapsed;
        _isUpdatingGeneralInfoEditor = false;
    }

    private void RefreshGeneralInfoEditor(UndertaleGeneralInfo generalInfo, int selectedOrderIndex = 0)
    {
        _isUpdatingGeneralInfoEditor = true;
        try
        {
            GeneralInfoRoomOrderSummaryText.Text = BuildGeneralInfoRoomOrderSummary(generalInfo, _data);
            RoomReferenceOption[] rooms = _data is null ? [] : BuildRoomReferenceOptions(_data).ToArray();
            GeneralInfoRoomPickerBox.ItemsSource = rooms;

            GeneralInfoRoomOrderSummary[] order = _data is null
                ? []
                : BuildGeneralInfoRoomOrderSummaries(generalInfo, _data).ToArray();
            GeneralInfoRoomOrderList.ItemsSource = order;

            GeneralInfoRoomOrderSummary? selected = order.Length == 0
                ? null
                : order[Math.Clamp(selectedOrderIndex, 0, order.Length - 1)];
            GeneralInfoRoomOrderList.SelectedItem = selected;
            GeneralInfoRoomPickerBox.SelectedItem = selected?.RoomIndex >= 0
                ? rooms.FirstOrDefault(room => room.Index == selected.RoomIndex) ?? rooms.FirstOrDefault()
                : rooms.FirstOrDefault();

            RefreshGeneralInfoOptionsEditor(_data?.Options);
            RefreshGeneralInfoLanguageEditor(_data?.Language);
        }
        finally
        {
            _isUpdatingGeneralInfoEditor = false;
        }

        UpdateGeneralInfoRoomOrderButtons();
    }

    private void GeneralInfoRoomOrderList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is GeneralInfoRoomOrderSummary summary)
        {
            GeneralInfoRoomOrderList.SelectedItem = summary;
            if (GeneralInfoRoomPickerBox.ItemsSource is IEnumerable<RoomReferenceOption> rooms && summary.RoomIndex >= 0)
                GeneralInfoRoomPickerBox.SelectedItem = rooms.FirstOrDefault(room => room.Index == summary.RoomIndex);
        }

        UpdateGeneralInfoRoomOrderButtons();
    }

    private void GeneralInfoRoomPickerBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingGeneralInfoEditor)
            return;

        UpdateGeneralInfoRoomOrderButtons();
    }

    private void UpdateGeneralInfoRoomOrderButtons()
    {
        bool canMutate = _data is not null && !_data.UnsupportedBytecodeVersion && _selectedResource?.Value is UndertaleGeneralInfo;
        bool hasRoomSelection = GeneralInfoRoomPickerBox.SelectedItem is RoomReferenceOption;
        bool hasOrderSelection = GeneralInfoRoomOrderList.SelectedItem is GeneralInfoRoomOrderSummary;
        GeneralInfoSyncRoomOrderButton.IsEnabled = canMutate && _data?.Rooms is { Count: > 0 };
        GeneralInfoSetRoomButton.IsEnabled = canMutate && hasRoomSelection;
        GeneralInfoRemoveRoomButton.IsEnabled = canMutate && hasOrderSelection;
        GeneralInfoOpenRoomButton.IsEnabled = GeneralInfoRoomOrderList.SelectedItem is GeneralInfoRoomOrderSummary { RoomIndex: >= 0 };
    }

    private void GeneralInfoOpenRoomButton_Click(object sender, RoutedEventArgs e)
    {
        if (GeneralInfoRoomOrderList.SelectedItem is GeneralInfoRoomOrderSummary { RoomIndex: >= 0 } summary)
            NavigateToResource("Rooms", summary.RoomIndex);
    }

    private void GeneralInfoSetRoomButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleGeneralInfo generalInfo ||
            GeneralInfoRoomPickerBox.SelectedItem is not RoomReferenceOption room)
        {
            return;
        }

        int selectedIndex = GeneralInfoRoomOrderList.SelectedItem is GeneralInfoRoomOrderSummary summary
            ? summary.OrderIndex
            : generalInfo.RoomOrder.Count;
        UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM> roomRef = new(room.Room);
        if (selectedIndex >= 0 && selectedIndex < generalInfo.RoomOrder.Count)
            generalInfo.RoomOrder[selectedIndex] = roomRef;
        else
            generalInfo.RoomOrder.Add(roomRef);

        MarkDirty();
        RefreshGeneralInfoEditor(generalInfo, selectedIndex);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = $"Updated room order entry to {room.Title}.";
    }

    private void GeneralInfoRemoveRoomButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleGeneralInfo generalInfo ||
            GeneralInfoRoomOrderList.SelectedItem is not GeneralInfoRoomOrderSummary summary ||
            summary.OrderIndex < 0 ||
            summary.OrderIndex >= generalInfo.RoomOrder.Count)
        {
            return;
        }

        generalInfo.RoomOrder.RemoveAt(summary.OrderIndex);
        MarkDirty();
        RefreshGeneralInfoEditor(generalInfo, Math.Min(summary.OrderIndex, generalInfo.RoomOrder.Count - 1));
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Removed room order entry.";
    }

    private void GeneralInfoSyncRoomOrderButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleGeneralInfo generalInfo)
        {
            return;
        }

        generalInfo.RoomOrder.Clear();
        foreach (UndertaleRoom room in _data.Rooms)
            generalInfo.RoomOrder.Add(new UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>(room));

        MarkDirty();
        RefreshGeneralInfoEditor(generalInfo);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Synced room order with the room list.";
    }

    private void RefreshGeneralInfoOptionsEditor(
        UndertaleOptions? options,
        int selectedConstantIndex = 0,
        GeneralInfoOptionTextureSlot selectedSlot = GeneralInfoOptionTextureSlot.BackImage)
    {
        bool wasUpdating = _isUpdatingGeneralInfoEditor;
        _isUpdatingGeneralInfoEditor = true;
        try
        {
            if (options is null)
            {
                GeneralInfoOptionsSummaryText.Text = "No options chunk is available.";
                GeneralInfoOptionsScalarList.ItemsSource = null;
                GeneralInfoOptionTextureSlotBox.ItemsSource = null;
                GeneralInfoOptionTextureSlotBox.SelectedItem = null;
                GeneralInfoOptionTextureItemBox.ItemsSource = null;
                GeneralInfoOptionTextureItemBox.SelectedItem = null;
                GeneralInfoConstantsList.ItemsSource = null;
                GeneralInfoConstantsList.SelectedItem = null;
                GeneralInfoConstantNameBox.Text = string.Empty;
                GeneralInfoConstantValueBox.Text = string.Empty;
                GeneralInfoConstantEditorPanel.Visibility = Visibility.Collapsed;
                GeneralInfoOptionsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            GeneralInfoOptionsPanel.Visibility = Visibility.Visible;
            GeneralInfoOptionsSummaryText.Text = BuildGeneralInfoOptionsSummary(options);
            GeneralInfoOptionsScalarList.ItemsSource = BuildEditableProperties(options).ToArray();

            GeneralInfoTextureSlotOption[] slots = BuildGeneralInfoTextureSlotOptions(options, _data).ToArray();
            GeneralInfoOptionTextureSlotBox.ItemsSource = slots;
            GeneralInfoOptionTextureSlotBox.SelectedItem =
                slots.FirstOrDefault(slot => slot.Slot == selectedSlot) ?? slots.FirstOrDefault();

            TexturePageItemOption[] textureOptions = _data is null ? [] : BuildTexturePageItemOptions(_data).ToArray();
            GeneralInfoOptionTextureItemBox.ItemsSource = textureOptions;
            UpdateGeneralInfoOptionTextureItemSelection();

            RefreshGeneralInfoConstants(options, selectedConstantIndex);
        }
        finally
        {
            _isUpdatingGeneralInfoEditor = wasUpdating;
        }

        UpdateGeneralInfoOptionsButtons();
    }

    private void RefreshGeneralInfoConstants(UndertaleOptions options, int selectedConstantIndex)
    {
        bool wasUpdating = _isUpdatingGeneralInfoEditor;
        _isUpdatingGeneralInfoEditor = true;
        try
        {
            GeneralInfoConstantSummary[] constants = BuildGeneralInfoConstantSummaries(options).ToArray();
            GeneralInfoConstantsList.ItemsSource = constants;

            GeneralInfoConstantSummary? selected = constants.Length == 0
                ? null
                : constants[Math.Clamp(selectedConstantIndex, 0, constants.Length - 1)];
            GeneralInfoConstantsList.SelectedItem = selected;
            UpdateGeneralInfoConstantEditor(selected);
            GeneralInfoOptionsSummaryText.Text = BuildGeneralInfoOptionsSummary(options);
        }
        finally
        {
            _isUpdatingGeneralInfoEditor = wasUpdating;
        }

        UpdateGeneralInfoOptionsButtons();
    }

    private void UpdateGeneralInfoOptionTextureItemSelection()
    {
        if (GeneralInfoOptionTextureSlotBox.SelectedItem is not GeneralInfoTextureSlotOption slot ||
            GeneralInfoOptionTextureItemBox.ItemsSource is not IEnumerable<TexturePageItemOption> textureOptions)
        {
            GeneralInfoOptionTextureItemBox.SelectedItem = null;
            return;
        }

        GeneralInfoOptionTextureItemBox.SelectedItem = slot.Texture is null
            ? null
            : textureOptions.FirstOrDefault(option => ReferenceEquals(option.Texture, slot.Texture));
    }

    private void UpdateGeneralInfoConstantEditor(GeneralInfoConstantSummary? summary)
    {
        GeneralInfoConstantEditorPanel.Visibility = summary is null ? Visibility.Collapsed : Visibility.Visible;
        GeneralInfoConstantNameBox.Text = summary?.Constant.Name?.Content ?? string.Empty;
        GeneralInfoConstantValueBox.Text = summary?.Constant.Value?.Content ?? string.Empty;
    }

    private void UpdateGeneralInfoOptionsButtons()
    {
        bool canMutate = _data is not null &&
                         !_data.UnsupportedBytecodeVersion &&
                         _selectedResource?.Value is UndertaleGeneralInfo &&
                         _data.Options is not null;
        bool hasConstantSelection = GeneralInfoConstantsList.SelectedItem is GeneralInfoConstantSummary;
        bool hasTextureSlot = GeneralInfoOptionTextureSlotBox.SelectedItem is GeneralInfoTextureSlotOption;
        bool hasTextureSelection = GeneralInfoOptionTextureItemBox.SelectedItem is TexturePageItemOption;
        bool slotHasTexture = GeneralInfoOptionTextureSlotBox.SelectedItem is GeneralInfoTextureSlotOption { Texture: not null };

        GeneralInfoAddConstantButton.IsEnabled = canMutate;
        GeneralInfoRemoveConstantButton.IsEnabled = canMutate && hasConstantSelection;
        GeneralInfoSetTextureButton.IsEnabled = canMutate && hasTextureSlot && hasTextureSelection;
        GeneralInfoOpenTextureButton.IsEnabled = slotHasTexture;
        GeneralInfoClearTextureButton.IsEnabled = canMutate && slotHasTexture;
    }

    private void GeneralInfoOptionTextureSlotBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingGeneralInfoEditor)
            return;

        bool wasUpdating = _isUpdatingGeneralInfoEditor;
        _isUpdatingGeneralInfoEditor = true;
        try
        {
            UpdateGeneralInfoOptionTextureItemSelection();
        }
        finally
        {
            _isUpdatingGeneralInfoEditor = wasUpdating;
        }

        UpdateGeneralInfoOptionsButtons();
    }

    private void GeneralInfoOptionTextureItemBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingGeneralInfoEditor)
            return;

        UpdateGeneralInfoOptionsButtons();
    }

    private void GeneralInfoSetTextureButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _data.Options is not UndertaleOptions options ||
            GeneralInfoOptionTextureSlotBox.SelectedItem is not GeneralInfoTextureSlotOption slot ||
            GeneralInfoOptionTextureItemBox.SelectedItem is not TexturePageItemOption textureOption)
        {
            return;
        }

        UndertaleSprite.TextureEntry entry = GetGeneralInfoTextureEntry(options, slot.Slot);
        if (ReferenceEquals(entry.Texture, textureOption.Texture))
            return;

        entry.Texture = textureOption.Texture;
        MarkDirty();
        RefreshGeneralInfoOptionsEditor(options, GetSelectedGeneralInfoConstantIndex(), slot.Slot);
        DetailsList.ItemsSource = _selectedResource is null ? null : BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = $"Updated {slot.Label} to {textureOption.Title}.";
    }

    private void GeneralInfoOpenTextureButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            GeneralInfoOptionTextureSlotBox.SelectedItem is not GeneralInfoTextureSlotOption { Texture: not null } slot)
        {
            return;
        }

        int textureIndex = _data.TexturePageItems.IndexOf(slot.Texture);
        if (textureIndex >= 0)
            NavigateToResource("Texture page items", textureIndex);
    }

    private void GeneralInfoClearTextureButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _data.Options is not UndertaleOptions options ||
            GeneralInfoOptionTextureSlotBox.SelectedItem is not GeneralInfoTextureSlotOption { Texture: not null } slot)
        {
            return;
        }

        UndertaleSprite.TextureEntry entry = GetGeneralInfoTextureEntry(options, slot.Slot);
        entry.Texture = null!;
        MarkDirty();
        RefreshGeneralInfoOptionsEditor(options, GetSelectedGeneralInfoConstantIndex(), slot.Slot);
        DetailsList.ItemsSource = _selectedResource is null ? null : BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = $"Cleared {slot.Label}.";
    }

    private void GeneralInfoConstantsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is GeneralInfoConstantSummary summary)
        {
            GeneralInfoConstantsList.SelectedItem = summary;
            UpdateGeneralInfoConstantEditor(summary);
        }

        UpdateGeneralInfoOptionsButtons();
    }

    private void GeneralInfoConstantsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingGeneralInfoEditor)
            return;

        UpdateGeneralInfoConstantEditor(GeneralInfoConstantsList.SelectedItem as GeneralInfoConstantSummary);
        UpdateGeneralInfoOptionsButtons();
    }

    private void GeneralInfoAddConstantButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _data.Options is not UndertaleOptions options)
        {
            return;
        }

        int index = options.Constants.Count;
        options.Constants.Add(new UndertaleOptions.Constant
        {
            Name = _data.Strings.MakeString($"constant{index}"),
            Value = _data.Strings.MakeString("0")
        });

        MarkDirty();
        RefreshGeneralInfoConstants(options, index);
        DetailsList.ItemsSource = _selectedResource is null ? null : BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = $"Added options constant #{index}.";
    }

    private void GeneralInfoRemoveConstantButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _data.Options is not UndertaleOptions options ||
            GeneralInfoConstantsList.SelectedItem is not GeneralInfoConstantSummary summary ||
            summary.Index < 0 ||
            summary.Index >= options.Constants.Count)
        {
            return;
        }

        options.Constants.RemoveAt(summary.Index);
        MarkDirty();
        RefreshGeneralInfoConstants(options, Math.Min(summary.Index, options.Constants.Count - 1));
        DetailsList.ItemsSource = _selectedResource is null ? null : BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Removed options constant.";
    }

    private void GeneralInfoConstantBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingGeneralInfoEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _data.Options is not UndertaleOptions options ||
            GeneralInfoConstantsList.SelectedItem is not GeneralInfoConstantSummary summary)
        {
            return;
        }

        string name = GeneralInfoConstantNameBox.Text;
        string value = GeneralInfoConstantValueBox.Text;
        bool changed = false;

        if (!string.Equals(summary.Constant.Name?.Content ?? string.Empty, name, StringComparison.Ordinal))
        {
            summary.Constant.Name = _data.Strings.MakeString(name);
            changed = true;
        }

        if (!string.Equals(summary.Constant.Value?.Content ?? string.Empty, value, StringComparison.Ordinal))
        {
            summary.Constant.Value = _data.Strings.MakeString(value);
            changed = true;
        }

        if (!changed)
            return;

        MarkDirty();
        RefreshGeneralInfoConstants(options, summary.Index);
        DetailsList.ItemsSource = _selectedResource is null ? null : BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = $"Updated options constant #{summary.Index}.";
    }

    private void RefreshGeneralInfoLanguageEditor(UndertaleLanguage? language)
    {
        bool wasUpdating = _isUpdatingGeneralInfoEditor;
        _isUpdatingGeneralInfoEditor = true;
        try
        {
            if (language is null)
            {
                GeneralInfoLanguageSummaryText.Text = string.Empty;
                GeneralInfoLanguageScalarList.ItemsSource = null;
                GeneralInfoLanguagesList.ItemsSource = null;
                GeneralInfoLanguagePanel.Visibility = Visibility.Collapsed;
                return;
            }

            GeneralInfoLanguagePanel.Visibility = Visibility.Visible;
            GeneralInfoLanguageSummaryText.Text = BuildGeneralInfoLanguageSummary(language);
            GeneralInfoLanguageScalarList.ItemsSource = BuildEditableProperties(language).ToArray();
            GeneralInfoLanguagesList.ItemsSource = BuildGeneralInfoLanguageSummaries(language).ToArray();
        }
        finally
        {
            _isUpdatingGeneralInfoEditor = wasUpdating;
        }
    }

    private void ShowTextureGroupEditorFor(ResourceItem item)
    {
        if (_data is null || item.Value is not UndertaleTextureGroupInfo textureGroup)
        {
            HideTextureGroupEditor();
            return;
        }

        TextureGroupEditorPanel.Visibility = Visibility.Visible;
        RefreshTextureGroupEditor(textureGroup);
    }

    private void HideTextureGroupEditor()
    {
        _isUpdatingTextureGroupEditor = true;
        TextureGroupSummaryText.Text = string.Empty;
        TextureGroupSectionComboBox.ItemsSource = null;
        TextureGroupSectionComboBox.SelectedItem = null;
        TextureGroupResourceComboBox.ItemsSource = null;
        TextureGroupResourceComboBox.SelectedItem = null;
        TextureGroupItemsList.ItemsSource = null;
        TextureGroupItemsList.SelectedItem = null;
        TextureGroupAddButton.IsEnabled = false;
        TextureGroupReplaceButton.IsEnabled = false;
        TextureGroupRemoveButton.IsEnabled = false;
        TextureGroupOpenSelectedButton.IsEnabled = false;
        TextureGroupEditorPanel.Visibility = Visibility.Collapsed;
        _isUpdatingTextureGroupEditor = false;
    }

    private void RefreshTextureGroupEditor(
        UndertaleTextureGroupInfo textureGroup,
        TextureGroupSectionKind? selectedKind = null,
        int selectedSectionIndex = -1)
    {
        _isUpdatingTextureGroupEditor = true;
        try
        {
            TextureGroupSummaryText.Text = BuildTextureGroupSummary(textureGroup);
            TextureGroupSectionOption[] sections = BuildTextureGroupSectionOptions();
            TextureGroupSectionComboBox.ItemsSource = sections;

            TextureGroupEntrySummary[] entries = _data is null
                ? []
                : BuildTextureGroupEntrySummaries(textureGroup, _data).ToArray();
            TextureGroupItemsList.ItemsSource = entries;

            TextureGroupEntrySummary? selectedEntry = selectedKind is null
                ? entries.FirstOrDefault()
                : entries.FirstOrDefault(entry => entry.SectionKind == selectedKind && entry.SectionIndex == selectedSectionIndex);
            TextureGroupItemsList.SelectedItem = selectedEntry;

            TextureGroupSectionKind sectionKind = selectedEntry?.SectionKind ?? selectedKind ?? TextureGroupSectionKind.TexturePages;
            TextureGroupSectionOption selectedSection = sections.First(section => section.Kind == sectionKind);
            TextureGroupSectionComboBox.SelectedItem = selectedSection;
            RefreshTextureGroupResourcePicker(sectionKind, selectedEntry?.ResourceIndex ?? -1);
        }
        finally
        {
            _isUpdatingTextureGroupEditor = false;
        }

        UpdateTextureGroupActionButtons();
    }

    private void TextureGroupItemsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is TextureGroupEntrySummary summary)
            SelectTextureGroupEntry(summary);
        else
            UpdateTextureGroupActionButtons();
    }

    private void SelectTextureGroupEntry(TextureGroupEntrySummary summary)
    {
        TextureGroupItemsList.SelectedItem = summary;
        TextureGroupSectionComboBox.SelectedItem = BuildTextureGroupSectionOptions().First(section => section.Kind == summary.SectionKind);
        RefreshTextureGroupResourcePicker(summary.SectionKind, summary.ResourceIndex);
        UpdateTextureGroupActionButtons();
    }

    private void TextureGroupSectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingTextureGroupEditor ||
            TextureGroupSectionComboBox.SelectedItem is not TextureGroupSectionOption section)
        {
            return;
        }

        RefreshTextureGroupResourcePicker(section.Kind);
        UpdateTextureGroupActionButtons();
    }

    private void TextureGroupResourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingTextureGroupEditor)
            return;

        UpdateTextureGroupActionButtons();
    }

    private void RefreshTextureGroupResourcePicker(TextureGroupSectionKind sectionKind, int selectedResourceIndex = -1)
    {
        TextureGroupResourceOption[] resources = _data is null
            ? []
            : BuildTextureGroupResourceOptions(sectionKind, _data).ToArray();
        TextureGroupResourceComboBox.ItemsSource = resources;
        TextureGroupResourceComboBox.SelectedItem = selectedResourceIndex >= 0
            ? resources.FirstOrDefault(resource => resource.Index == selectedResourceIndex) ?? resources.FirstOrDefault()
            : resources.FirstOrDefault();
    }

    private void UpdateTextureGroupActionButtons()
    {
        bool canMutate = _data is not null && !_data.UnsupportedBytecodeVersion;
        TextureGroupResourceOption? resourceSelection = TextureGroupResourceComboBox.SelectedItem as TextureGroupResourceOption;
        TextureGroupEntrySummary? entrySelection = TextureGroupItemsList.SelectedItem as TextureGroupEntrySummary;
        bool hasResourceSelection = resourceSelection is not null;
        bool hasEntrySelection = entrySelection is not null;
        TextureGroupAddButton.IsEnabled = canMutate && hasResourceSelection && TextureGroupSectionComboBox.SelectedItem is TextureGroupSectionOption;
        TextureGroupReplaceButton.IsEnabled = canMutate &&
                                              hasEntrySelection &&
                                              resourceSelection is not null &&
                                              entrySelection is not null &&
                                              resourceSelection.SectionKind == entrySelection.SectionKind;
        TextureGroupRemoveButton.IsEnabled = canMutate && hasEntrySelection;
        TextureGroupOpenSelectedButton.IsEnabled = TextureGroupItemsList.SelectedItem is TextureGroupEntrySummary { ResourceIndex: >= 0 };
    }

    private void TextureGroupOpenSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (TextureGroupItemsList.SelectedItem is not TextureGroupEntrySummary { ResourceIndex: >= 0 } summary)
            return;

        NavigateToResource(summary.CategoryLabel, summary.ResourceIndex);
    }

    private void TextureGroupAddButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleTextureGroupInfo textureGroup ||
            TextureGroupSectionComboBox.SelectedItem is not TextureGroupSectionOption section ||
            TextureGroupResourceComboBox.SelectedItem is not TextureGroupResourceOption resource)
        {
            return;
        }

        int newIndex = GetTextureGroupSectionCount(textureGroup, section.Kind);
        if (!AddTextureGroupResource(textureGroup, section.Kind, resource.Resource))
            return;

        MarkDirty();
        RefreshTextureGroupEditor(textureGroup, section.Kind, newIndex);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = $"Added {resource.Title} to {section.Label}.";
    }

    private void TextureGroupReplaceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleTextureGroupInfo textureGroup ||
            TextureGroupItemsList.SelectedItem is not TextureGroupEntrySummary entry ||
            TextureGroupResourceComboBox.SelectedItem is not TextureGroupResourceOption resource ||
            resource.SectionKind != entry.SectionKind)
        {
            return;
        }

        if (!ReplaceTextureGroupResource(textureGroup, entry.SectionKind, entry.SectionIndex, resource.Resource))
            return;

        MarkDirty();
        RefreshTextureGroupEditor(textureGroup, entry.SectionKind, entry.SectionIndex);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = $"Updated {entry.Section} #{entry.SectionIndex}.";
    }

    private void TextureGroupRemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleTextureGroupInfo textureGroup ||
            TextureGroupItemsList.SelectedItem is not TextureGroupEntrySummary entry)
        {
            return;
        }

        if (!RemoveTextureGroupResource(textureGroup, entry.SectionKind, entry.SectionIndex))
            return;

        int nextIndex = Math.Min(entry.SectionIndex, Math.Max(0, GetTextureGroupSectionCount(textureGroup, entry.SectionKind) - 1));
        MarkDirty();
        RefreshTextureGroupEditor(textureGroup, entry.SectionKind, nextIndex);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = $"Removed {entry.Section} #{entry.SectionIndex}.";
    }

    private void HideSpritePreview()
    {
        _spritePreviewCts?.Cancel();
        _spritePreviewGeneration++;
        _isSpritePreviewRendered = false;
        _isUpdatingSpriteFrame = true;
        SpriteFrameComboBox.ItemsSource = null;
        SpriteFrameComboBox.SelectedIndex = -1;
        _isUpdatingSpriteFrame = false;
        UpdateSpriteFrameNavigationState();
        SpritePreviewImage.Source = null;
        SpriteLargePreviewImage.Source = null;
        UpdateImagePreviewOpenStates();
        SpritePreviewInfoText.Text = string.Empty;
        SpriteLargePreviewInfoText.Text = string.Empty;
        SpriteTexturesList.ItemsSource = null;
        SpriteMasksList.ItemsSource = null;
        SpriteMasksList.SelectedIndex = -1;
        SpriteFramesSummaryText.Text = string.Empty;
        SpriteCollisionSummaryText.Text = string.Empty;
        SpriteExportSummaryText.Text = string.Empty;
        SpriteExportCurrentFrameText.Text = string.Empty;
        SpriteExportFrameButton.IsEnabled = false;
        SpriteExportAllButton.IsEnabled = false;
        SpriteOpenTextureButton.IsEnabled = false;
        SpriteSideOpenTextureButton.IsEnabled = false;
        SpriteRenderPreviewButton.IsEnabled = false;
        CustomSpriteEditorPanel.Visibility = Visibility.Collapsed;
    }

    private void SpriteFrameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingSpriteFrame)
            return;

        UpdateSpriteFrameNavigationState();
        if (_selectedResource?.Value is UndertaleSprite sprite)
            RenderOrResetSpritePreview(sprite);
    }

    private void UpdateSpriteFrameNavigationState()
    {
        bool canNavigate = SpriteFrameComboBox.Items.Count > 1;
        SpriteFramePrevButton.IsEnabled = canNavigate;
        SpriteFrameNextButton.IsEnabled = canNavigate;
        SpriteLargeFramePrevButton.IsEnabled = canNavigate;
        SpriteLargeFrameNextButton.IsEnabled = canNavigate;
    }

    private async System.Threading.Tasks.Task RefreshSpritePreviewAsync()
    {
        if (_selectedResource?.Value is not UndertaleSprite sprite)
            return;

        _spritePreviewCts?.Cancel();
        _isSpritePreviewRendered = true;
        int frameIndex = SpriteFrameComboBox.SelectedItem is SpriteFrameItem frame ? frame.Index : 0;
        int generation = ++_spritePreviewGeneration;
        SpritePreviewImage.Source = null;
        SpriteLargePreviewImage.Source = null;
        UpdateImagePreviewOpenStates();
        UpdateSpriteFrameActionState(sprite);
        SpriteRenderPreviewButton.IsEnabled = false;

        if (!TryGetSpriteFrameTexture(sprite, frameIndex, out UndertaleTexturePageItem? texture) || texture is null)
        {
            SpritePreviewInfoText.Text = BuildSpritePreviewInfo(sprite);
            SpriteLargePreviewInfoText.Text = SpritePreviewInfoText.Text;
            ClearSpritePreviewDetails();
            UpdateSpriteFrameActionState(sprite);
            return;
        }

        SpritePreviewInfoText.Text = BuildSpritePreviewInfo(sprite, frameIndex, texture);
        SpriteLargePreviewInfoText.Text = SpritePreviewInfoText.Text;
        UpdateSpritePreviewDetails(sprite, frameIndex, texture);

        CancellationTokenSource previewCts = new();
        _spritePreviewCts = previewCts;
        CancellationToken token = previewCts.Token;

        try
        {
            await System.Threading.Tasks.Task.Delay(75, token);
            byte[] previewBytes = await System.Threading.Tasks.Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                byte[] bytes = GetCachedTexturePageItemPreviewPng(texture);
                token.ThrowIfCancellationRequested();
                return bytes;
            }, token);
            if (token.IsCancellationRequested || generation != _spritePreviewGeneration)
                return;

            SpritePreviewImage.Source = LoadBitmapImage(previewBytes);
            SpriteLargePreviewImage.Source = LoadBitmapImage(previewBytes);
            UpdateImagePreviewOpenStates();
            UpdateSpriteFrameActionState(sprite);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (generation != _spritePreviewGeneration)
                return;

            SpritePreviewInfoText.Text = $"Could not render sprite frame: {ex.Message}";
            SpriteLargePreviewInfoText.Text = SpritePreviewInfoText.Text;
        }
        finally
        {
            if (ReferenceEquals(_spritePreviewCts, previewCts))
            {
                _spritePreviewCts = null;
                if (generation == _spritePreviewGeneration && ReferenceEquals(_selectedResource?.Value, sprite))
                    UpdateSpriteFrameActionState(sprite);
            }

            previewCts.Dispose();
        }
    }

    private void SpriteRenderPreviewButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RefreshSpritePreviewAsync();
    }

    private void ResetSpritePreviewSurface(UndertaleSprite sprite)
    {
        _spritePreviewCts?.Cancel();
        _isSpritePreviewRendered = false;
        _spritePreviewGeneration++;
        SpritePreviewImage.Source = null;
        SpriteLargePreviewImage.Source = null;
        UpdateImagePreviewOpenStates();
        SpritePreviewTitleText.Text = "Preview not rendered";
        SpritePreviewInfoText.Text = BuildSpritePreviewInfo(sprite);
        SpriteLargePreviewInfoText.Text = "Preview not rendered.";
        SpritePreviewSourceSizeText.Text = $"{sprite.Width} x {sprite.Height}";
        SpritePreviewBoundsText.Text = "-";
        SpritePreviewOriginText.Text = $"{sprite.OriginX}, {sprite.OriginY}";
        SpritePreviewTextureText.Text = "-";
        SpritePreviewTextureSizeText.Text = "-";
        SpritePreviewTextureIndexText.Text = "-";
        SpriteExportCurrentFrameText.Text = "No frame rendered.";
        UpdateSpriteFrameActionState(sprite);
    }

    private void UpdateSpriteFrameActionState(UndertaleSprite sprite)
    {
        int frameIndex = SpriteFrameComboBox.SelectedItem is SpriteFrameItem frame ? frame.Index : 0;
        bool hasFrameTexture = TryGetSpriteFrameTexture(sprite, frameIndex, out UndertaleTexturePageItem? texture) && texture is not null;
        SpriteExportFrameButton.IsEnabled = hasFrameTexture;
        SpriteExportAllButton.IsEnabled = sprite.Textures.Count > 0;
        SpriteOpenTextureButton.IsEnabled = hasFrameTexture;
        SpriteSideOpenTextureButton.IsEnabled = hasFrameTexture;
        SpriteRenderPreviewButton.IsEnabled = hasFrameTexture;
    }

    private async void SpriteExportFrameButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedResource?.Value is not UndertaleSprite sprite)
            return;

        ResourceItem selectedResource = _selectedResource;
        int frameIndex = SpriteFrameComboBox.SelectedItem is SpriteFrameItem frame ? frame.Index : 0;
        if (!TryGetSpriteFrameTexture(sprite, frameIndex, out UndertaleTexturePageItem? texture) || texture is null)
            return;

        FileSavePicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            SuggestedFileName = $"{SafeFileName(sprite.Name?.Content, "sprite")}_{frameIndex}"
        };
        picker.FileTypeChoices.Add("PNG image", [".png"]);

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFile? file = await picker.PickSaveFileAsync();
        if (file is null)
            return;

        SpriteExportFrameButton.IsEnabled = false;
        try
        {
            await System.Threading.Tasks.Task.Run(() => ExportSpriteFrame(texture!, file.Path));
            StatusBox.Text = $"Exported sprite frame {frameIndex} to {file.Path}";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to export sprite frame: {ex}";
        }
        finally
        {
            if (ReferenceEquals(_selectedResource, selectedResource))
                UpdateSpriteFrameActionState(sprite);
        }
    }

    private async void SpriteExportAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedResource?.Value is not UndertaleSprite sprite || sprite.Textures.Count == 0)
            return;

        ResourceItem selectedResource = _selectedResource;
        FolderPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary
        };
        picker.FileTypeFilter.Add("*");

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFolder? folder = await picker.PickSingleFolderAsync();
        if (folder is null)
            return;

        SpriteExportAllButton.IsEnabled = false;
        try
        {
            int exportedCount = await System.Threading.Tasks.Task.Run(() => ExportAllSpriteFrames(sprite, folder.Path));
            StatusBox.Text = $"Exported {exportedCount} sprite frame(s) to {folder.Path}.";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to export sprite frames: {ex}";
        }
        finally
        {
            if (ReferenceEquals(_selectedResource, selectedResource))
                UpdateSpriteFrameActionState(sprite);
        }
    }

    private void ShowObjectSummaryFor(ResourceItem item)
    {
        if (item.Value is not UndertaleGameObject gameObject)
        {
            HideObjectSummary();
            return;
        }

        _isUpdatingObjectEditor = true;
        try
        {
            ObjectSummaryPanel.Visibility = Visibility.Visible;
            ObjectSpriteText.Text = $"Sprite: {FormatTitle(gameObject.Sprite?.Name?.Content)}";
            ObjectParentText.Text = $"Parent: {FormatTitle(gameObject.ParentId?.Name?.Content)}";
            ObjectMaskText.Text = $"Texture mask: {FormatTitle(gameObject.TextureMaskId?.Name?.Content)}";
            ObjectOpenSpriteButton.IsEnabled = gameObject.Sprite is not null;
            ObjectOpenParentButton.IsEnabled = gameObject.ParentId is not null;
            ObjectOpenMaskButton.IsEnabled = gameObject.TextureMaskId is not null;
            ObjectPhysicsSummaryText.Text = BuildObjectPhysicsSummary(gameObject);
            ObjectPhysicsVertexSummary[] vertices = BuildObjectPhysicsVertexSummaries(gameObject).ToArray();
            ObjectPhysicsVerticesList.ItemsSource = vertices;
            ObjectPhysicsVerticesList.SelectedItem = vertices.FirstOrDefault();
            UpdateObjectPhysicsVertexEditor(vertices.FirstOrDefault());
            ObjectNewEventTypeComboBox.ItemsSource = Enum.GetValues<EventType>();
            ObjectNewEventTypeComboBox.SelectedItem = EventType.Create;
            ObjectNewEventSubtypeBox.Text = "0";
            ObjectAddEventButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion;
            ObjectEventSummary[] events = BuildObjectEventSummaries(gameObject).ToArray();
            ObjectEventsList.ItemsSource = events;
            ObjectEventsList.SelectedItem = events.FirstOrDefault();
            UpdateObjectEventEditor(events.FirstOrDefault(), selectedAction: null);
        }
        finally
        {
            _isUpdatingObjectEditor = false;
        }
    }

    private void HideObjectSummary()
    {
        _isUpdatingObjectEditor = true;
        ObjectSpriteText.Text = string.Empty;
        ObjectParentText.Text = string.Empty;
        ObjectMaskText.Text = string.Empty;
        ObjectPhysicsSummaryText.Text = string.Empty;
        ObjectOpenSpriteButton.IsEnabled = false;
        ObjectOpenParentButton.IsEnabled = false;
        ObjectOpenMaskButton.IsEnabled = false;
        ObjectAddPhysicsVertexButton.IsEnabled = false;
        ObjectRemovePhysicsVertexButton.IsEnabled = false;
        ObjectPhysicsVerticesList.ItemsSource = null;
        ObjectPhysicsVerticesList.SelectedItem = null;
        ObjectPhysicsVertexXBox.Text = string.Empty;
        ObjectPhysicsVertexYBox.Text = string.Empty;
        ObjectPhysicsVertexEditorPanel.Visibility = Visibility.Collapsed;
        ObjectNewEventTypeComboBox.ItemsSource = null;
        ObjectNewEventTypeComboBox.SelectedItem = null;
        ObjectNewEventSubtypeBox.Text = string.Empty;
        ObjectAddEventButton.IsEnabled = false;
        ObjectRemoveEventButton.IsEnabled = false;
        ObjectEventsList.ItemsSource = null;
        ObjectEventsList.SelectedItem = null;
        ObjectEventSelectionText.Text = string.Empty;
        ObjectEventSubtypeBox.Text = string.Empty;
        ObjectEventActionsList.ItemsSource = null;
        ObjectEventActionsList.SelectedItem = null;
        ObjectEventActionCodeComboBox.ItemsSource = null;
        ObjectEventActionCodeComboBox.SelectedItem = null;
        ObjectAddEventActionButton.IsEnabled = false;
        ObjectOpenEventActionCodeButton.IsEnabled = false;
        ObjectRemoveEventActionButton.IsEnabled = false;
        ObjectEventActionEditorPanel.Visibility = Visibility.Collapsed;
        ObjectEventEditorPanel.Visibility = Visibility.Collapsed;
        ObjectSummaryPanel.Visibility = Visibility.Collapsed;
        _isUpdatingObjectEditor = false;
    }

    private void ObjectOpenSpriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _selectedResource?.Value is not UndertaleGameObject gameObject || gameObject.Sprite is null)
            return;

        int spriteIndex = _data.Sprites.IndexOf(gameObject.Sprite);
        if (spriteIndex >= 0)
            NavigateToResource("Sprites", spriteIndex);
    }

    private void ObjectOpenParentButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _selectedResource?.Value is not UndertaleGameObject gameObject || gameObject.ParentId is null)
            return;

        int objectIndex = _data.GameObjects.IndexOf(gameObject.ParentId);
        if (objectIndex >= 0)
            NavigateToResource("Objects", objectIndex);
    }

    private void ObjectOpenMaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _selectedResource?.Value is not UndertaleGameObject gameObject || gameObject.TextureMaskId is null)
            return;

        int spriteIndex = _data.Sprites.IndexOf(gameObject.TextureMaskId);
        if (spriteIndex >= 0)
            NavigateToResource("Sprites", spriteIndex);
    }

    private void ObjectEventsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ObjectEventSummary summary)
        {
            ObjectEventsList.SelectedItem = summary;
            UpdateObjectEventEditor(summary, selectedAction: null);
        }
    }

    private void ObjectEventsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingObjectEditor)
            return;

        UpdateObjectEventEditor(ObjectEventsList.SelectedItem as ObjectEventSummary, selectedAction: null);
    }

    private void ObjectPhysicsVerticesList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ObjectPhysicsVertexSummary summary)
        {
            ObjectPhysicsVerticesList.SelectedItem = summary;
            UpdateObjectPhysicsVertexEditor(summary);
        }
    }

    private void ObjectPhysicsVerticesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingObjectEditor)
            return;

        UpdateObjectPhysicsVertexEditor(ObjectPhysicsVerticesList.SelectedItem as ObjectPhysicsVertexSummary);
    }

    private void UpdateObjectPhysicsVertexEditor(ObjectPhysicsVertexSummary? summary)
    {
        bool canEdit = _data is not null &&
                       !_data.UnsupportedBytecodeVersion &&
                       _selectedResource?.Value is UndertaleGameObject;
        ObjectAddPhysicsVertexButton.IsEnabled = canEdit;
        ObjectRemovePhysicsVertexButton.IsEnabled = canEdit && summary is not null;

        _isUpdatingObjectEditor = true;
        try
        {
            ObjectPhysicsVertexEditorPanel.Visibility = summary is null ? Visibility.Collapsed : Visibility.Visible;
            ObjectPhysicsVertexXBox.Text = summary?.Vertex.X.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ObjectPhysicsVertexYBox.Text = summary?.Vertex.Y.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        }
        finally
        {
            _isUpdatingObjectEditor = false;
        }
    }

    private void ObjectAddPhysicsVertexButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleGameObject gameObject)
        {
            return;
        }

        UndertaleGameObject.UndertalePhysicsVertex vertex = new();
        gameObject.PhysicsVertices.Add(vertex);
        MarkDirty();
        RefreshObjectPhysicsVertices(gameObject, vertex);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Added object physics vertex.";
    }

    private void ObjectRemovePhysicsVertexButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleGameObject gameObject ||
            ObjectPhysicsVerticesList.SelectedItem is not ObjectPhysicsVertexSummary summary)
        {
            return;
        }

        int index = gameObject.PhysicsVertices.IndexOf(summary.Vertex);
        if (index < 0)
            return;

        gameObject.PhysicsVertices.RemoveAt(index);
        UndertaleGameObject.UndertalePhysicsVertex? nextSelection = gameObject.PhysicsVertices.Count == 0
            ? null
            : gameObject.PhysicsVertices[Math.Clamp(index, 0, gameObject.PhysicsVertices.Count - 1)];
        MarkDirty();
        RefreshObjectPhysicsVertices(gameObject, nextSelection);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Removed object physics vertex.";
    }

    private void ObjectPhysicsVertexBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingObjectEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleGameObject gameObject ||
            ObjectPhysicsVerticesList.SelectedItem is not ObjectPhysicsVertexSummary summary)
        {
            return;
        }

        if (!float.TryParse(ObjectPhysicsVertexXBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
            !float.TryParse(ObjectPhysicsVertexYBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
        {
            UpdateObjectPhysicsVertexEditor(summary);
            StatusBox.Text = "Invalid physics vertex coordinate. Use numeric X and Y values.";
            return;
        }

        if (summary.Vertex.X.Equals(x) && summary.Vertex.Y.Equals(y))
            return;

        summary.Vertex.X = x;
        summary.Vertex.Y = y;
        MarkDirty();
        RefreshObjectPhysicsVertices(gameObject, summary.Vertex);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = $"Updated object physics vertex #{summary.Index}.";
    }

    private void RefreshObjectPhysicsVertices(
        UndertaleGameObject gameObject,
        UndertaleGameObject.UndertalePhysicsVertex? selectedVertex)
    {
        _isUpdatingObjectEditor = true;
        try
        {
            ObjectPhysicsSummaryText.Text = BuildObjectPhysicsSummary(gameObject);
            ObjectPhysicsVertexSummary[] vertices = BuildObjectPhysicsVertexSummaries(gameObject).ToArray();
            ObjectPhysicsVerticesList.ItemsSource = vertices;
            ObjectPhysicsVertexSummary? selected = selectedVertex is null
                ? vertices.FirstOrDefault()
                : vertices.FirstOrDefault(summary => ReferenceEquals(summary.Vertex, selectedVertex));
            ObjectPhysicsVerticesList.SelectedItem = selected;
            UpdateObjectPhysicsVertexEditor(selected);
        }
        finally
        {
            _isUpdatingObjectEditor = false;
        }
    }

    private void UpdateObjectEventEditor(
        ObjectEventSummary? summary,
        UndertaleGameObject.EventAction? selectedAction)
    {
        bool canEdit = _data is not null &&
                       !_data.UnsupportedBytecodeVersion &&
                       _selectedResource?.Value is UndertaleGameObject;
        _isUpdatingObjectEditor = true;
        try
        {
            ObjectRemoveEventButton.IsEnabled = canEdit && summary is not null;
            ObjectAddEventActionButton.IsEnabled = canEdit && summary is not null;

            if (summary is null)
            {
                ObjectEventSelectionText.Text = string.Empty;
                ObjectEventSubtypeBox.Text = string.Empty;
                ObjectEventActionsList.ItemsSource = null;
                ObjectEventActionsList.SelectedItem = null;
                ObjectEventActionCodeComboBox.ItemsSource = null;
                ObjectEventActionCodeComboBox.SelectedItem = null;
                ObjectOpenEventActionCodeButton.IsEnabled = false;
                ObjectRemoveEventActionButton.IsEnabled = false;
                ObjectEventActionEditorPanel.Visibility = Visibility.Collapsed;
                ObjectEventEditorPanel.Visibility = Visibility.Collapsed;
                return;
            }

            ObjectEventSelectionText.Text = $"{summary.Title} - {summary.Subtitle}";
            ObjectEventSubtypeBox.Text = summary.Event.EventSubtype.ToString(CultureInfo.InvariantCulture);
            ObjectEventEditorPanel.Visibility = Visibility.Visible;

            ObjectEventActionSummary[] actions = BuildObjectEventActionSummaries(summary.Event).ToArray();
            ObjectEventActionsList.ItemsSource = actions;
            ObjectEventActionSummary? selected = selectedAction is null
                ? actions.FirstOrDefault()
                : actions.FirstOrDefault(action => ReferenceEquals(action.Action, selectedAction));
            ObjectEventActionsList.SelectedItem = selected;
            UpdateObjectEventActionEditor(selected);
        }
        finally
        {
            _isUpdatingObjectEditor = false;
        }
    }

    private void UpdateObjectEventActionEditor(ObjectEventActionSummary? summary)
    {
        bool canEdit = _data is not null &&
                       !_data.UnsupportedBytecodeVersion &&
                       _selectedResource?.Value is UndertaleGameObject;
        _isUpdatingObjectEditor = true;
        try
        {
            ObjectOpenEventActionCodeButton.IsEnabled = summary?.Action.CodeId is not null;
            ObjectRemoveEventActionButton.IsEnabled = canEdit && summary is not null;

            if (_data is null || summary is null)
            {
                ObjectEventActionCodeComboBox.ItemsSource = null;
                ObjectEventActionCodeComboBox.SelectedItem = null;
                ObjectEventActionEditorPanel.Visibility = Visibility.Collapsed;
                return;
            }

            CodeReferenceItem[] codeItems = BuildCodeReferenceItems(_data).ToArray();
            ObjectEventActionCodeComboBox.ItemsSource = codeItems;
            ObjectEventActionCodeComboBox.SelectedItem =
                codeItems.FirstOrDefault(item => ReferenceEquals(item.Code, summary.Action.CodeId)) ??
                codeItems.FirstOrDefault();
            ObjectEventActionEditorPanel.Visibility = Visibility.Visible;
        }
        finally
        {
            _isUpdatingObjectEditor = false;
        }
    }

    private void RefreshObjectEvents(
        UndertaleGameObject gameObject,
        UndertaleGameObject.Event? selectedEvent,
        UndertaleGameObject.EventAction? selectedAction)
    {
        _isUpdatingObjectEditor = true;
        try
        {
            ObjectEventSummary[] events = BuildObjectEventSummaries(gameObject).ToArray();
            ObjectEventsList.ItemsSource = events;
            ObjectEventSummary? selected = selectedEvent is null
                ? events.FirstOrDefault()
                : events.FirstOrDefault(summary => ReferenceEquals(summary.Event, selectedEvent));
            ObjectEventsList.SelectedItem = selected;
            UpdateObjectEventEditor(selected, selectedAction);
        }
        finally
        {
            _isUpdatingObjectEditor = false;
        }
    }

    private void ObjectAddEventButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleGameObject gameObject ||
            ObjectNewEventTypeComboBox.SelectedItem is not EventType eventType)
        {
            return;
        }

        if (!uint.TryParse(ObjectNewEventSubtypeBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint subtype))
        {
            StatusBox.Text = "Invalid event subtype. Use a non-negative integer.";
            return;
        }

        UndertalePointerList<UndertaleGameObject.Event> events = EnsureObjectEventBucket(gameObject, eventType);
        UndertaleGameObject.Event? ev = events.FirstOrDefault(current => current.EventSubtype == subtype);
        UndertaleGameObject.EventAction? selectedAction = null;
        if (ev is null)
        {
            ev = new UndertaleGameObject.Event
            {
                EventSubtype = subtype
            };
            selectedAction = new UndertaleGameObject.EventAction
            {
                ActionName = _data.Strings.MakeString(string.Empty)
            };
            ev.Actions.Add(selectedAction);
            events.Add(ev);
            MarkDirty();
            StatusBox.Text = $"Added {FormatEventType(eventType)} / {FormatEventSubtype(eventType, subtype)} event.";
        }
        else
        {
            StatusBox.Text = "Selected existing event with that type/subtype.";
            selectedAction = ev.Actions?.FirstOrDefault();
        }

        RefreshObjectEvents(gameObject, ev, selectedAction);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
    }

    private void ObjectRemoveEventButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleGameObject gameObject ||
            ObjectEventsList.SelectedItem is not ObjectEventSummary summary ||
            summary.EventTypeIndex < 0 ||
            summary.EventTypeIndex >= gameObject.Events.Count ||
            gameObject.Events[summary.EventTypeIndex] is not UndertalePointerList<UndertaleGameObject.Event> bucket)
        {
            return;
        }

        int index = bucket.IndexOf(summary.Event);
        if (index < 0)
            return;

        bucket.RemoveAt(index);
        UndertaleGameObject.Event? nextSelection = bucket.Count == 0
            ? null
            : bucket[Math.Clamp(index, 0, bucket.Count - 1)];
        MarkDirty();
        RefreshObjectEvents(gameObject, nextSelection, selectedAction: null);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Removed object event.";
    }

    private void ObjectEventSubtypeBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingObjectEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleGameObject gameObject ||
            ObjectEventsList.SelectedItem is not ObjectEventSummary summary)
        {
            return;
        }

        if (!uint.TryParse(ObjectEventSubtypeBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint subtype))
        {
            UpdateObjectEventEditor(summary, selectedAction: null);
            StatusBox.Text = "Invalid event subtype. Use a non-negative integer.";
            return;
        }

        if (summary.Event.EventSubtype == subtype)
            return;

        summary.Event.EventSubtype = subtype;
        MarkDirty();
        RefreshObjectEvents(gameObject, summary.Event, selectedAction: null);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Updated object event subtype.";
    }

    private void ObjectEventActionsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ObjectEventActionSummary summary)
        {
            ObjectEventActionsList.SelectedItem = summary;
            UpdateObjectEventActionEditor(summary);
        }
    }

    private void ObjectEventActionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingObjectEditor)
            return;

        UpdateObjectEventActionEditor(ObjectEventActionsList.SelectedItem as ObjectEventActionSummary);
    }

    private void ObjectAddEventActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleGameObject gameObject ||
            ObjectEventsList.SelectedItem is not ObjectEventSummary eventSummary)
        {
            return;
        }

        UndertaleGameObject.EventAction action = new()
        {
            ActionName = _data.Strings.MakeString(string.Empty)
        };
        eventSummary.Event.Actions.Add(action);
        MarkDirty();
        RefreshObjectEvents(gameObject, eventSummary.Event, action);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Added object event action.";
    }

    private void ObjectRemoveEventActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleGameObject gameObject ||
            ObjectEventsList.SelectedItem is not ObjectEventSummary eventSummary ||
            ObjectEventActionsList.SelectedItem is not ObjectEventActionSummary actionSummary)
        {
            return;
        }

        int index = eventSummary.Event.Actions.IndexOf(actionSummary.Action);
        if (index < 0)
            return;

        eventSummary.Event.Actions.RemoveAt(index);
        UndertaleGameObject.EventAction? nextSelection = eventSummary.Event.Actions.Count == 0
            ? null
            : eventSummary.Event.Actions[Math.Clamp(index, 0, eventSummary.Event.Actions.Count - 1)];
        MarkDirty();
        RefreshObjectEvents(gameObject, eventSummary.Event, nextSelection);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Removed object event action.";
    }

    private void ObjectOpenEventActionCodeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            ObjectEventActionsList.SelectedItem is not ObjectEventActionSummary summary ||
            summary.Action.CodeId is null)
        {
            return;
        }

        int codeIndex = _data.Code.IndexOf(summary.Action.CodeId);
        if (codeIndex >= 0)
            NavigateToResource("Code", codeIndex);
    }

    private void ObjectEventActionCodeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingObjectEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleGameObject gameObject ||
            ObjectEventsList.SelectedItem is not ObjectEventSummary eventSummary ||
            ObjectEventActionsList.SelectedItem is not ObjectEventActionSummary actionSummary ||
            ObjectEventActionCodeComboBox.SelectedItem is not CodeReferenceItem codeItem)
        {
            return;
        }

        if (ReferenceEquals(actionSummary.Action.CodeId, codeItem.Code))
            return;

        actionSummary.Action.CodeId = codeItem.Code;
        MarkDirty();
        RefreshObjectEvents(gameObject, eventSummary.Event, actionSummary.Action);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Updated object event action code.";
    }

    private void ShowRoomSummaryFor(ResourceItem item)
    {
        if (item.Value is not UndertaleRoom room)
        {
            HideRoomSummary();
            return;
        }

        RoomSummaryPanel.Visibility = Visibility.Visible;
        if (!ReferenceEquals(_roomPreviewUndoRoom, room))
        {
            _roomPreviewUndoRoom = room;
            _roomPreviewUndoStack.Clear();
        }
        RoomOverviewText.Text = BuildRoomOverview(room);
        RoomOpenCreationCodeButton.IsEnabled = room.CreationCodeId is not null;
        RoomInstanceSummary[] instanceSummaries = BuildRoomInstanceSummaries(room).ToArray();
        RoomInstancesList.ItemsSource = instanceSummaries;
        RoomInstancesList.SelectedItem = instanceSummaries.FirstOrDefault();
        UpdateRoomInstanceButtons(instanceSummaries.FirstOrDefault());
        bool usesLegacyBackgrounds = UsesLegacyRoomBackgroundSlots(room);
        RoomBackgroundSlotsPanel.Visibility = usesLegacyBackgrounds ? Visibility.Visible : Visibility.Collapsed;
        if (usesLegacyBackgrounds)
            RefreshRoomBackgrounds(room, selectedBackground: room.Backgrounds?.FirstOrDefault(background => background.Enabled) ?? room.Backgrounds?.FirstOrDefault());
        else
            HideRoomBackgroundEditor();
        RefreshRoomViews(room, selectedView: room.Views?.FirstOrDefault(view => view.Enabled) ?? room.Views?.FirstOrDefault());
        RoomTileSummary[] tileSummaries = BuildRoomTileSummaries(room).ToArray();
        bool showRoomTiles = usesLegacyBackgrounds || tileSummaries.Length > 0;
        RoomTilesPanel.Visibility = showRoomTiles ? Visibility.Visible : Visibility.Collapsed;
        if (showRoomTiles)
            RefreshRoomTiles(room, selectedTile: tileSummaries.FirstOrDefault()?.Tile);
        else
            HideRoomTileEditor();
        RefreshRoomLayers(room, selectedLayer: room.Layers?.FirstOrDefault());
        RefreshRoomInstanceEditor(instanceSummaries.FirstOrDefault());
        _isUpdatingRoomPreviewOptions = true;
        RoomPreviewBackgroundsCheckBox.IsChecked = true;
        RoomPreviewInstancesCheckBox.IsChecked = true;
        RoomPreviewTilesCheckBox.IsChecked = true;
        RoomPreviewViewsCheckBox.IsChecked = true;
        RoomPreviewInstanceLabelsCheckBox.IsChecked = true;
        _isUpdatingRoomPreviewOptions = false;
        _isRoomPreviewRendered = false;
        RoomRenderPreviewButton.IsEnabled = true;
        RoomExportPreviewButton.IsEnabled = false;
        SetRoomPreviewZoomControlsEnabled(false);
        ResetRoomPreviewViewport();
        ClearRoomPreviewSurface(room, instanceSummaries);
    }

    private void HideRoomSummary()
    {
        _roomPreviewCts?.Cancel();
        _roomPreviewGeneration++;
        _isRoomPreviewRendered = false;
        RoomOverviewText.Text = string.Empty;
        RoomPreviewInfoText.Text = string.Empty;
        RoomPreviewCanvas.Children.Clear();
        RoomPreviewCanvas.Width = double.NaN;
        RoomPreviewCanvas.Height = double.NaN;
        _lastRoomPreviewPointerPosition = null;
        _roomPreviewScale = 1;
        _isUpdatingRoomPreviewOptions = true;
        RoomPreviewBackgroundsCheckBox.IsChecked = true;
        RoomPreviewInstancesCheckBox.IsChecked = true;
        RoomPreviewTilesCheckBox.IsChecked = true;
        RoomPreviewViewsCheckBox.IsChecked = true;
        RoomPreviewInstanceLabelsCheckBox.IsChecked = true;
        _isUpdatingRoomPreviewOptions = false;
        RoomRenderPreviewButton.IsEnabled = false;
        RoomExportPreviewButton.IsEnabled = false;
        SetRoomPreviewZoomControlsEnabled(false);
        ResetRoomPreviewViewport();
        RoomOpenCreationCodeButton.IsEnabled = false;
        RoomAddInstanceButton.IsEnabled = false;
        RoomDuplicateInstanceButton.IsEnabled = false;
        RoomRemoveInstanceButton.IsEnabled = false;
        RoomMoveInstanceUpButton.IsEnabled = false;
        RoomMoveInstanceDownButton.IsEnabled = false;
        RoomInstancesList.ItemsSource = null;
        RoomInstancesList.SelectedItem = null;
        RoomAddBackgroundButton.IsEnabled = false;
        RoomDuplicateBackgroundButton.IsEnabled = false;
        RoomRemoveBackgroundButton.IsEnabled = false;
        RoomBackgroundsList.ItemsSource = null;
        RoomBackgroundsList.SelectedItem = null;
        HideRoomBackgroundEditor();
        RoomBackgroundSlotsPanel.Visibility = Visibility.Collapsed;
        RoomDisableViewButton.IsEnabled = false;
        RoomViewsList.ItemsSource = null;
        RoomViewsList.SelectedItem = null;
        HideRoomViewEditor();
        RoomAddTileButton.IsEnabled = false;
        RoomDuplicateTileButton.IsEnabled = false;
        RoomRemoveTileButton.IsEnabled = false;
        RoomTilesList.ItemsSource = null;
        RoomTilesList.SelectedItem = null;
        HideRoomTileEditor();
        RoomTilesPanel.Visibility = Visibility.Collapsed;
        UpdateRoomLayerButtons(selectedSummary: null);
        RoomLayersList.ItemsSource = null;
        RoomLayersList.SelectedItem = null;
        HideRoomLayerEditor();
        HideRoomInstanceEditor();
        _roomPreviewUndoRoom = null;
        _roomPreviewUndoStack.Clear();
        RoomSummaryPanel.Visibility = Visibility.Collapsed;
    }

    private void ClearRoomPreviewSurface(UndertaleRoom room, IReadOnlyList<RoomInstanceSummary> instanceSummaries)
    {
        _roomPreviewCts?.Cancel();
        _roomPreviewGeneration++;
        RoomPreviewCanvas.Children.Clear();
        RoomExportPreviewButton.IsEnabled = false;
        SetRoomPreviewZoomControlsEnabled(false);
        ResetRoomPreviewViewport();
        _lastRoomPreviewPointerPosition = null;
        _roomPreviewScale = 1;
        RoomPreviewCanvas.Width = Math.Max(240, Math.Min(room.Width, 960));
        RoomPreviewCanvas.Height = Math.Max(160, Math.Min(room.Height, 540));
        RoomPreviewInfoText.Text = $"Preview not rendered. Room has {instanceSummaries.Count} legacy instance(s), {room.Backgrounds.Count} background slot(s), and {room.Tiles.Count} legacy tile(s).";
    }

    private async void RoomRenderPreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedResource?.Value is not UndertaleRoom room)
            return;

        await RefreshRoomPreviewAsync(room, BuildRoomInstanceSummaries(room).ToArray());
    }

    private void ResetRoomPreviewViewport()
    {
        _isUpdatingRoomPreviewZoom = true;
        RoomPreviewZoomSlider.Value = 1;
        _isUpdatingRoomPreviewZoom = false;
        RoomPreviewScrollViewer.ChangeView(0, 0, 1);
    }

    private void RoomPreviewZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isUpdatingRoomPreviewZoom || RoomPreviewScrollViewer is null)
            return;

        RoomPreviewScrollViewer.ChangeView(null, null, (float)e.NewValue);
    }

    private void RoomPreviewScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (!RoomPreviewZoomSlider.IsEnabled)
            return;

        int wheelDelta = e.GetCurrentPoint(RoomPreviewScrollViewer).Properties.MouseWheelDelta;
        if (wheelDelta == 0)
            return;

        double factor = wheelDelta > 0 ? 1.15 : 1 / 1.15;
        SetRoomPreviewZoom(RoomPreviewZoomSlider.Value * factor, resetOffset: false);
        e.Handled = true;
    }

    private void RoomPreviewActualSizeButton_Click(object sender, RoutedEventArgs e)
    {
        SetRoomPreviewZoom(1);
    }

    private void RoomPreviewFitButton_Click(object sender, RoutedEventArgs e)
    {
        if (!double.IsFinite(RoomPreviewCanvas.Width) ||
            !double.IsFinite(RoomPreviewCanvas.Height) ||
            RoomPreviewCanvas.Width <= 0 ||
            RoomPreviewCanvas.Height <= 0 ||
            RoomPreviewScrollViewer.ViewportWidth <= 0 ||
            RoomPreviewScrollViewer.ViewportHeight <= 0)
        {
            SetRoomPreviewZoom(1);
            return;
        }

        double availableWidth = Math.Max(1, RoomPreviewScrollViewer.ViewportWidth - 32);
        double availableHeight = Math.Max(1, RoomPreviewScrollViewer.ViewportHeight - 32);
        double scale = Math.Min(availableWidth / RoomPreviewCanvas.Width, availableHeight / RoomPreviewCanvas.Height);
        SetRoomPreviewZoom(scale);
    }

    private void SetRoomPreviewZoom(double zoom, bool resetOffset = true)
    {
        zoom = Math.Clamp(zoom, RoomPreviewZoomSlider.Minimum, RoomPreviewZoomSlider.Maximum);
        _isUpdatingRoomPreviewZoom = true;
        RoomPreviewZoomSlider.Value = zoom;
        _isUpdatingRoomPreviewZoom = false;
        RoomPreviewScrollViewer.ChangeView(resetOffset ? 0 : null, resetOffset ? 0 : null, (float)zoom);
    }

    private void SetRoomPreviewZoomControlsEnabled(bool enabled)
    {
        RoomPreviewZoomSlider.IsEnabled = enabled;
        RoomPreviewActualSizeButton.IsEnabled = enabled;
        RoomPreviewFitButton.IsEnabled = enabled;
    }

    private void RoomPreviewCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        _lastRoomPreviewPointerPosition = e.GetCurrentPoint(RoomPreviewCanvas).Position;
    }

    private void RoomPreviewCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _lastRoomPreviewPointerPosition = e.GetCurrentPoint(RoomPreviewCanvas).Position;
        RoomPreviewCanvas.Focus(FocusState.Pointer);
    }

    private async void RoomPreviewCanvas_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!IsVirtualKeyDown(VirtualKey.Control) ||
            !IsRoomPasteKey(e.Key) ||
            _selectedResource?.Value is not UndertaleRoom room)
        {
            return;
        }

        Point previewPosition = _lastRoomPreviewPointerPosition ?? new Point();
        bool snapToGrid = !IsVirtualKeyDown(VirtualKey.Shift);
        if (await PasteCopiedRoomItemAtPreviewPositionAsync(room, previewPosition, snapToGrid))
            e.Handled = true;
    }

    private async System.Threading.Tasks.Task<bool> PasteCopiedRoomItemAtPreviewPositionAsync(
        UndertaleRoom room,
        Point previewPosition,
        bool snapToGrid)
    {
        if (_data is null || _data.UnsupportedBytecodeVersion)
            return false;

        (int x, int y) = GetRoomPositionFromPreview(room, previewPosition, snapToGrid);
        switch (_copiedRoomItem)
        {
            case UndertaleRoom.GameObject source:
                await PasteRoomInstanceAtAsync(room, source, x, y);
                return true;
            case UndertaleRoom.SpriteInstance sprite:
                return await PasteRoomLayerAssetAtAsync(room, sprite, x, y);
            case UndertaleRoom.SequenceInstance sequence when _data.IsVersionAtLeast(2, 3):
                return await PasteRoomLayerAssetAtAsync(room, sequence, x, y);
            case UndertaleRoom.ParticleSystemInstance particle when _data.IsNonLTSVersionAtLeast(2023, 2):
                return await PasteRoomLayerAssetAtAsync(room, particle, x, y);
            default:
                StatusBox.Text = "No compatible room item copied.";
                return false;
        }
    }

    private (int X, int Y) GetRoomPositionFromPreview(
        UndertaleRoom room,
        Point previewPosition,
        bool snapToGrid)
    {
        double scale = Math.Max(0.01, _roomPreviewScale);
        int x = (int)Math.Round(previewPosition.X / scale);
        int y = (int)Math.Round(previewPosition.Y / scale);

        if (!snapToGrid)
            return (x, y);

        int gridWidth = GetRoomGridWidth(room);
        int gridHeight = GetRoomGridHeight(room);
        x = (int)Math.Floor((double)x / gridWidth) * gridWidth;
        y = (int)Math.Floor((double)y / gridHeight) * gridHeight;
        return (x, y);
    }

    private async System.Threading.Tasks.Task PasteRoomInstanceAtAsync(
        UndertaleRoom room,
        UndertaleRoom.GameObject source,
        int x,
        int y)
    {
        if (_data is null)
            return;

        RoomInstanceSummary? selectedSummary = RoomInstancesList.SelectedItem as RoomInstanceSummary;
        UndertaleRoom.GameObject? insertAfter = selectedSummary?.Instance ?? source;
        UndertaleRoom.GameObject duplicate = new()
        {
            X = x,
            Y = y,
            ObjectDefinition = source.ObjectDefinition,
            InstanceID = _data.GeneralInfo.LastObj++,
            CreationCode = source.CreationCode,
            ScaleX = source.ScaleX,
            ScaleY = source.ScaleY,
            Color = source.Color,
            Rotation = source.Rotation,
            PreCreateCode = source.PreCreateCode,
            ImageSpeed = source.ImageSpeed,
            ImageIndex = source.ImageIndex
        };

        int insertIndex = insertAfter is null ? -1 : room.GameObjects.IndexOf(insertAfter);
        if (insertIndex < 0)
            insertIndex = room.GameObjects.IndexOf(source);
        if (insertIndex < 0)
            room.GameObjects.Add(duplicate);
        else
            room.GameObjects.Insert(Math.Min(insertIndex + 1, room.GameObjects.Count), duplicate);

        AddRoomInstanceCreationOrder(room, duplicate, insertAfter?.InstanceID ?? source.InstanceID);
        AddRoomInstanceToDefaultLayer(room, duplicate, source, GetSelectedRoomInstanceLayer());
        await RefreshRoomInstanceAfterEditAsync(room, duplicate, $"Pasted room instance at {x},{y}.");
    }

    private async System.Threading.Tasks.Task<bool> PasteRoomLayerAssetAtAsync(
        UndertaleRoom room,
        object source,
        int x,
        int y)
    {
        if (_data is null ||
            GetSelectedOrDefaultRoomAssetsLayer(room) is not { } layer ||
            layer.AssetsData is not { } assetsData)
        {
            StatusBox.Text = "Select an asset layer before pasting asset instances.";
            return false;
        }

        EnsureRoomLayerAssetLists(layer, _data);
        switch (source)
        {
            case UndertaleRoom.SpriteInstance sprite:
            {
                assetsData.Sprites ??= new UndertalePointerList<UndertaleRoom.SpriteInstance>();
                UndertaleRoom.SpriteInstance duplicate = CloneRoomLayerSpriteInstance(sprite, _data, offset: 0);
                duplicate.X = x;
                duplicate.Y = y;
                UndertaleRoom.SpriteInstance? after = (RoomLayerAssetSpritesList.SelectedItem as RoomLayerAssetSpriteInstanceSummary)?.Instance ?? sprite;
                InsertAfter(assetsData.Sprites, duplicate, after);
                await RefreshRoomLayerAfterEditAsync(room, layer, $"Pasted asset sprite at {x},{y}.");
                SelectRoomLayerAssetChild(duplicate);
                return true;
            }
            case UndertaleRoom.SequenceInstance sequence when _data.IsVersionAtLeast(2, 3):
            {
                assetsData.Sequences ??= new UndertalePointerList<UndertaleRoom.SequenceInstance>();
                UndertaleRoom.SequenceInstance duplicate = CloneRoomLayerSequenceInstance(sequence, _data, offset: 0);
                duplicate.X = x;
                duplicate.Y = y;
                UndertaleRoom.SequenceInstance? after = (RoomLayerAssetSequencesList.SelectedItem as RoomLayerAssetSequenceInstanceSummary)?.Instance ?? sequence;
                InsertAfter(assetsData.Sequences, duplicate, after);
                await RefreshRoomLayerAfterEditAsync(room, layer, $"Pasted asset sequence at {x},{y}.");
                SelectRoomLayerAssetChild(duplicate);
                return true;
            }
            case UndertaleRoom.ParticleSystemInstance particle when _data.IsNonLTSVersionAtLeast(2023, 2):
            {
                assetsData.ParticleSystems ??= new UndertalePointerList<UndertaleRoom.ParticleSystemInstance>();
                UndertaleRoom.ParticleSystemInstance duplicate = CloneRoomLayerParticleInstance(particle, _data, offset: 0);
                duplicate.X = x;
                duplicate.Y = y;
                UndertaleRoom.ParticleSystemInstance? after = (RoomLayerAssetParticlesList.SelectedItem as RoomLayerAssetParticleInstanceSummary)?.Instance ?? particle;
                InsertAfter(assetsData.ParticleSystems, duplicate, after);
                await RefreshRoomLayerAfterEditAsync(room, layer, $"Pasted asset particle at {x},{y}.");
                SelectRoomLayerAssetChild(duplicate);
                return true;
            }
            default:
                StatusBox.Text = "No compatible asset instance copied.";
                return false;
        }
    }

    private UndertaleRoom.Layer? GetSelectedOrDefaultRoomAssetsLayer(UndertaleRoom room)
    {
        if (RoomLayersList.SelectedItem is RoomLayerSummary { Layer.AssetsData: not null } selected)
            return selected.Layer;

        return room.Layers?.FirstOrDefault(layer => layer.AssetsData is not null);
    }

    private async void RoomExportPreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isRoomPreviewRendered ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomPreviewCanvas.Children.Count == 0)
        {
            StatusBox.Text = "Render the room preview before exporting.";
            RoomExportPreviewButton.IsEnabled = false;
            return;
        }

        FileSavePicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            SuggestedFileName = SafeFileName(room.Name?.Content, "room")
        };
        picker.FileTypeChoices.Add("PNG image", [".png"]);
        InitializePickerWithMainWindow(picker);

        StorageFile? file = await picker.PickSaveFileAsync();
        if (file is null)
            return;

        RoomExportPreviewButton.IsEnabled = false;
        try
        {
            await SaveRoomPreviewPngAsync(file);
            StatusBox.Text = $"Exported room preview to {file.Path}.";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to export room preview: {ex.Message}";
        }
        finally
        {
            RoomExportPreviewButton.IsEnabled = _isRoomPreviewRendered &&
                                                ReferenceEquals(_selectedResource?.Value, room) &&
                                                RoomPreviewCanvas.Children.Count > 0;
        }
    }

    private async System.Threading.Tasks.Task SaveRoomPreviewPngAsync(StorageFile file)
    {
        RoomPreviewCanvas.UpdateLayout();

        RenderTargetBitmap bitmap = new();
        await bitmap.RenderAsync(RoomPreviewCanvas);
        if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
            throw new InvalidOperationException("Room preview is empty.");

        IBuffer pixelBuffer = await bitmap.GetPixelsAsync();
        byte[] pixels = new byte[checked((int)pixelBuffer.Length)];
        using (DataReader reader = DataReader.FromBuffer(pixelBuffer))
        {
            reader.ReadBytes(pixels);
        }

        using IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
        stream.Size = 0;
        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)bitmap.PixelWidth,
            (uint)bitmap.PixelHeight,
            96,
            96,
            pixels);
        await encoder.FlushAsync();
    }

    private void RoomOpenCreationCodeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _selectedResource?.Value is not UndertaleRoom room || room.CreationCodeId is null)
            return;

        int codeIndex = _data.Code.IndexOf(room.CreationCodeId);
        if (codeIndex >= 0)
            NavigateToResource("Code", codeIndex);
    }

    private void RoomInstancesList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (_data is null || e.ClickedItem is not RoomInstanceSummary summary)
            return;

        RoomInstancesList.SelectedItem = summary;
        UpdateRoomInstanceButtons(summary);
        RefreshRoomInstanceEditor(summary);
    }

    private async void RoomInstancesList_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room)
        {
            return;
        }

        bool control = IsVirtualKeyDown(VirtualKey.Control);
        if (control && e.Key == VirtualKey.Z)
        {
            await RestoreLastRoomPreviewUndoAsync();
            e.Handled = true;
        }
        else if (control && e.Key == VirtualKey.C)
        {
            if (RoomInstancesList.SelectedItem is RoomInstanceSummary summary)
            {
                _copiedRoomItem = summary.Instance;
                StatusBox.Text = "Copied room instance.";
                e.Handled = true;
            }
        }
        else if (control && IsRoomPasteKey(e.Key))
        {
            await PasteCopiedRoomInstanceAsync(room);
            e.Handled = true;
        }
        else if (control && e.Key == VirtualKey.Up)
        {
            await MoveSelectedRoomInstanceAsync(-1);
            e.Handled = true;
        }
        else if (control && e.Key == VirtualKey.Down)
        {
            await MoveSelectedRoomInstanceAsync(1);
            e.Handled = true;
        }
        else if (!control && IsMinusKey(e.Key))
        {
            await MoveSelectedRoomInstanceAsync(-1);
            e.Handled = true;
        }
        else if (!control && IsPlusKey(e.Key))
        {
            await MoveSelectedRoomInstanceAsync(1);
            e.Handled = true;
        }
        else if (!control && e.Key == VirtualKey.X)
        {
            await FlipSelectedRoomInstanceAsync(horizontal: true);
            e.Handled = true;
        }
        else if (!control && e.Key == VirtualKey.Y)
        {
            await FlipSelectedRoomInstanceAsync(horizontal: false);
            e.Handled = true;
        }
        else if (!control && e.Key == VirtualKey.Enter)
        {
            RoomOpenInstanceObjectButton_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Delete)
        {
            RoomRemoveInstanceButton_Click(sender, e);
            e.Handled = true;
        }
    }

    private void RoomInstancesList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        RoomOpenInstanceObjectButton_Click(sender, e);
        e.Handled = true;
    }

    private void RoomInstancesList_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement element ||
            !IsMiddlePointerPressed(e, element) ||
            FindDataContextFromOriginalSource<RoomInstanceSummary>(e.OriginalSource) is not { } summary)
        {
            return;
        }

        RoomInstancesList.SelectedItem = summary;
        UpdateRoomInstanceButtons(summary);
        RefreshRoomInstanceEditor(summary);
        OpenRoomInstanceSummary(summary, addTab: true);
        e.Handled = true;
    }

    private void RoomPreviewInstance_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement marker ||
            marker.Tag is not RoomPreviewInstanceMarker markerData)
            return;

        RoomInstanceSummary summary = markerData.Summary;
        RoomInstancesList.SelectedItem = summary;
        UpdateRoomInstanceButtons(summary);
        RefreshRoomInstanceEditor(summary);

        if (_data is not null &&
            !_data.UnsupportedBytecodeVersion &&
            _selectedResource?.Value is UndertaleRoom)
        {
            Point pointerPosition = e.GetCurrentPoint(RoomPreviewCanvas).Position;
            _roomPreviewDragState = new RoomPreviewDragState(
                marker,
                summary,
                e.Pointer.PointerId,
                markerData.Scale,
                RoomPreviewCanvas.Width,
                RoomPreviewCanvas.Height,
                pointerPosition,
                summary.Instance.X,
                summary.Instance.Y);
            marker.CapturePointer(e.Pointer);
        }

        e.Handled = true;
    }

    private void RoomPreviewInstance_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_roomPreviewDragState is null ||
            sender is not FrameworkElement marker ||
            !ReferenceEquals(marker, _roomPreviewDragState.Marker) ||
            e.Pointer.PointerId != _roomPreviewDragState.PointerId)
        {
            return;
        }

        Point pointerPosition = e.GetCurrentPoint(RoomPreviewCanvas).Position;
        double scale = Math.Max(0.01, _roomPreviewDragState.Scale);
        UndertaleRoom.GameObject instance = _roomPreviewDragState.Summary.Instance;
        int newX = _roomPreviewDragState.StartX + (int)Math.Round((pointerPosition.X - _roomPreviewDragState.StartPointer.X) / scale);
        int newY = _roomPreviewDragState.StartY + (int)Math.Round((pointerPosition.Y - _roomPreviewDragState.StartPointer.Y) / scale);

        if (instance.X == newX && instance.Y == newY)
            return;

        instance.X = newX;
        instance.Y = newY;
        _roomPreviewDragState.HasMoved = true;
        Canvas.SetLeft(marker, Math.Clamp(instance.XOffset * scale, 0, Math.Max(0, _roomPreviewDragState.PreviewWidth - 8)));
        Canvas.SetTop(marker, Math.Clamp(instance.YOffset * scale, 0, Math.Max(0, _roomPreviewDragState.PreviewHeight - 8)));
        UpdateRoomInstancePositionEditor(instance);
        MarkDirty();
        StatusBox.Text = $"Moving room instance to {instance.X},{instance.Y}.";
        e.Handled = true;
    }

    private async void RoomPreviewInstance_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_roomPreviewDragState is null ||
            sender is not FrameworkElement marker ||
            !ReferenceEquals(marker, _roomPreviewDragState.Marker) ||
            e.Pointer.PointerId != _roomPreviewDragState.PointerId)
        {
            return;
        }

        RoomPreviewDragState dragState = _roomPreviewDragState;
        _roomPreviewDragState = null;
        marker.ReleasePointerCapture(e.Pointer);
        e.Handled = true;

        if (!dragState.HasMoved || _selectedResource?.Value is not UndertaleRoom room)
            return;

        PushRoomPreviewUndo(new RoomPreviewUndoState(dragState.Summary.Instance, dragState.StartX, dragState.StartY, Layer: null));
        await RefreshRoomInstanceAfterEditAsync(room, dragState.Summary.Instance, "Moved room instance.");
    }

    private void RoomPreviewInstance_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (_roomPreviewDragState is null ||
            sender is not FrameworkElement marker ||
            !ReferenceEquals(marker, _roomPreviewDragState.Marker) ||
            e.Pointer.PointerId != _roomPreviewDragState.PointerId)
        {
            return;
        }

        marker.ReleasePointerCapture(e.Pointer);
        _roomPreviewDragState = null;
        e.Handled = true;
    }

    private void RoomPreviewAsset_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement marker ||
            marker.Tag is not RoomPreviewAssetMarker markerData)
        {
            return;
        }

        object activeInstance = markerData.Instance;
        bool copied = false;
        if (_data is not null &&
            !_data.UnsupportedBytecodeVersion &&
            _selectedResource?.Value is UndertaleRoom &&
            IsVirtualKeyDown(VirtualKey.Menu) &&
            TryCloneRoomPreviewAsset(markerData.Layer, markerData.Instance, _data, out object? clone) &&
            clone is not null)
        {
            activeInstance = clone;
            copied = true;
            marker.Tag = new RoomPreviewAssetMarker(markerData.Layer, activeInstance, markerData.Scale);
            MarkDirty();
            StatusBox.Text = "Copied asset instance.";
        }

        SelectRoomLayerPreviewAsset(markerData.Layer, activeInstance);

        if (_data is not null &&
            !_data.UnsupportedBytecodeVersion &&
            _selectedResource?.Value is UndertaleRoom)
        {
            Point pointerPosition = e.GetCurrentPoint(RoomPreviewCanvas).Position;
            GetRoomPreviewAssetPosition(activeInstance, out int startX, out int startY);
            _roomPreviewAssetDragState = new RoomPreviewAssetDragState(
                marker,
                markerData.Layer,
                activeInstance,
                e.Pointer.PointerId,
                markerData.Scale,
                RoomPreviewCanvas.Width,
                RoomPreviewCanvas.Height,
                pointerPosition,
                startX,
                startY,
                Canvas.GetLeft(marker),
                Canvas.GetTop(marker),
                copied);
            marker.CapturePointer(e.Pointer);
        }

        e.Handled = true;
    }

    private void RoomPreviewAsset_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_roomPreviewAssetDragState is null ||
            sender is not FrameworkElement marker ||
            !ReferenceEquals(marker, _roomPreviewAssetDragState.Marker) ||
            e.Pointer.PointerId != _roomPreviewAssetDragState.PointerId)
        {
            return;
        }

        Point pointerPosition = e.GetCurrentPoint(RoomPreviewCanvas).Position;
        double scale = Math.Max(0.01, _roomPreviewAssetDragState.Scale);
        int newX = _roomPreviewAssetDragState.StartX + (int)Math.Round((pointerPosition.X - _roomPreviewAssetDragState.StartPointer.X) / scale);
        int newY = _roomPreviewAssetDragState.StartY + (int)Math.Round((pointerPosition.Y - _roomPreviewAssetDragState.StartPointer.Y) / scale);
        if (!SetRoomPreviewAssetPosition(_roomPreviewAssetDragState.Instance, newX, newY))
            return;

        _roomPreviewAssetDragState.HasMoved = true;
        double deltaX = (newX - _roomPreviewAssetDragState.StartX) * scale;
        double deltaY = (newY - _roomPreviewAssetDragState.StartY) * scale;
        Canvas.SetLeft(marker, Math.Clamp(_roomPreviewAssetDragState.StartCanvasLeft + deltaX, -_roomPreviewAssetDragState.PreviewWidth, _roomPreviewAssetDragState.PreviewWidth * 2));
        Canvas.SetTop(marker, Math.Clamp(_roomPreviewAssetDragState.StartCanvasTop + deltaY, -_roomPreviewAssetDragState.PreviewHeight, _roomPreviewAssetDragState.PreviewHeight * 2));
        UpdateRoomLayerAssetPositionEditor(_roomPreviewAssetDragState.Instance);
        MarkDirty();
        StatusBox.Text = $"Moving asset instance to {newX},{newY}.";
        e.Handled = true;
    }

    private async void RoomPreviewAsset_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_roomPreviewAssetDragState is null ||
            sender is not FrameworkElement marker ||
            !ReferenceEquals(marker, _roomPreviewAssetDragState.Marker) ||
            e.Pointer.PointerId != _roomPreviewAssetDragState.PointerId)
        {
            return;
        }

        RoomPreviewAssetDragState dragState = _roomPreviewAssetDragState;
        _roomPreviewAssetDragState = null;
        marker.ReleasePointerCapture(e.Pointer);
        e.Handled = true;

        if ((!dragState.HasMoved && !dragState.WasCopied) || _selectedResource?.Value is not UndertaleRoom room)
            return;

        string status = dragState.WasCopied
            ? dragState.HasMoved ? "Copied and moved asset instance." : "Copied asset instance."
            : "Moved asset instance.";
        if (dragState.HasMoved)
            PushRoomPreviewUndo(new RoomPreviewUndoState(dragState.Instance, dragState.StartX, dragState.StartY, dragState.Layer));
        await RefreshRoomLayerAfterEditAsync(room, dragState.Layer, status);
        SelectRoomLayerAssetChild(dragState.Instance);
    }

    private async void RoomPreviewAsset_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (_roomPreviewAssetDragState is null ||
            sender is not FrameworkElement marker ||
            !ReferenceEquals(marker, _roomPreviewAssetDragState.Marker) ||
            e.Pointer.PointerId != _roomPreviewAssetDragState.PointerId)
        {
            return;
        }

        RoomPreviewAssetDragState dragState = _roomPreviewAssetDragState;
        marker.ReleasePointerCapture(e.Pointer);
        _roomPreviewAssetDragState = null;
        e.Handled = true;

        if (dragState.WasCopied && _selectedResource?.Value is UndertaleRoom room)
        {
            await RefreshRoomLayerAfterEditAsync(room, dragState.Layer, "Copied asset instance.");
            SelectRoomLayerAssetChild(dragState.Instance);
        }
    }

    private void PushRoomPreviewUndo(RoomPreviewUndoState state)
    {
        _roomPreviewUndoStack.Push(state);
    }

    private async System.Threading.Tasks.Task RestoreLastRoomPreviewUndoAsync()
    {
        if (_selectedResource?.Value is not UndertaleRoom room)
            return;
        if (!ReferenceEquals(_roomPreviewUndoRoom, room))
        {
            _roomPreviewUndoRoom = room;
            _roomPreviewUndoStack.Clear();
            StatusBox.Text = "Nothing to undo.";
            return;
        }

        while (_roomPreviewUndoStack.TryPop(out RoomPreviewUndoState? state))
        {
            if (!SetRoomPreviewAssetPosition(state.Instance, state.X, state.Y))
            {
                if (state.Instance is UndertaleRoom.GameObject gameObject)
                {
                    gameObject.X = state.X;
                    gameObject.Y = state.Y;
                }
                else if (state.Instance is UndertaleRoom.View view)
                {
                    view.ViewX = state.X;
                    view.ViewY = state.Y;
                }
                else
                {
                    continue;
                }
            }

            if (state.Instance is UndertaleRoom.GameObject instance)
            {
                await RefreshRoomInstanceAfterEditAsync(room, instance, "Undid room preview move.");
            }
            else if (state.Instance is UndertaleRoom.View view)
            {
                view.ViewX = state.X;
                view.ViewY = state.Y;
                await RefreshRoomViewAfterEditAsync(room, view, "Undid room preview view move.");
            }
            else if (state.Layer is not null)
            {
                await RefreshRoomLayerAfterEditAsync(room, state.Layer, "Undid room preview asset move.");
                SelectRoomLayerAssetChild(state.Instance);
            }
            else
            {
                RefreshCurrentDetails();
                StatusBox.Text = "Undid room preview move.";
            }

            return;
        }

        StatusBox.Text = "Nothing to undo.";
    }

    private void SelectRoomLayerPreviewAsset(UndertaleRoom.Layer layer, object instance)
    {
        RoomLayerSummary? layerSummary = (RoomLayersList.ItemsSource as IEnumerable)?
            .OfType<RoomLayerSummary>()
            .FirstOrDefault(summary => ReferenceEquals(summary.Layer, layer));
        if (layerSummary is not null)
        {
            RoomLayersList.SelectedItem = layerSummary;
            RefreshRoomLayerEditor(layerSummary);
        }

        SelectRoomLayerAssetChild(instance);
    }

    private static void GetRoomPreviewAssetPosition(object instance, out int x, out int y)
    {
        switch (instance)
        {
            case UndertaleRoom.SpriteInstance sprite:
                x = sprite.X;
                y = sprite.Y;
                return;
            case UndertaleRoom.SequenceInstance sequence:
                x = sequence.X;
                y = sequence.Y;
                return;
            case UndertaleRoom.ParticleSystemInstance particle:
                x = particle.X;
                y = particle.Y;
                return;
            default:
                x = 0;
                y = 0;
                return;
        }
    }

    private static bool SetRoomPreviewAssetPosition(object instance, int x, int y)
    {
        switch (instance)
        {
            case UndertaleRoom.SpriteInstance sprite when sprite.X != x || sprite.Y != y:
                sprite.X = x;
                sprite.Y = y;
                return true;
            case UndertaleRoom.SequenceInstance sequence when sequence.X != x || sequence.Y != y:
                sequence.X = x;
                sequence.Y = y;
                return true;
            case UndertaleRoom.ParticleSystemInstance particle when particle.X != x || particle.Y != y:
                particle.X = x;
                particle.Y = y;
                return true;
            default:
                return false;
        }
    }

    private static bool TryCloneRoomPreviewAsset(
        UndertaleRoom.Layer layer,
        object source,
        UndertaleData data,
        out object? clone)
    {
        clone = null;
        if (layer.AssetsData is not { } assetsData)
            return false;

        switch (source)
        {
            case UndertaleRoom.SpriteInstance sprite when assetsData.Sprites is not null:
            {
                UndertaleRoom.SpriteInstance duplicate = CloneRoomLayerSpriteInstance(sprite, data, offset: 0);
                int index = assetsData.Sprites.IndexOf(sprite);
                assetsData.Sprites.Insert(index >= 0 ? index + 1 : assetsData.Sprites.Count, duplicate);
                clone = duplicate;
                return true;
            }
            case UndertaleRoom.SequenceInstance sequence when assetsData.Sequences is not null:
            {
                UndertaleRoom.SequenceInstance duplicate = CloneRoomLayerSequenceInstance(sequence, data, offset: 0);
                int index = assetsData.Sequences.IndexOf(sequence);
                assetsData.Sequences.Insert(index >= 0 ? index + 1 : assetsData.Sequences.Count, duplicate);
                clone = duplicate;
                return true;
            }
            case UndertaleRoom.ParticleSystemInstance particle when assetsData.ParticleSystems is not null:
            {
                UndertaleRoom.ParticleSystemInstance duplicate = CloneRoomLayerParticleInstance(particle, data, offset: 0);
                int index = assetsData.ParticleSystems.IndexOf(particle);
                assetsData.ParticleSystems.Insert(index >= 0 ? index + 1 : assetsData.ParticleSystems.Count, duplicate);
                clone = duplicate;
                return true;
            }
            default:
                return false;
        }
    }

    private void UpdateRoomLayerAssetPositionEditor(object instance)
    {
        switch (instance)
        {
            case UndertaleRoom.SpriteInstance sprite:
                RoomLayerAssetSpriteXBox.Text = sprite.X.ToString(CultureInfo.InvariantCulture);
                RoomLayerAssetSpriteYBox.Text = sprite.Y.ToString(CultureInfo.InvariantCulture);
                break;
            case UndertaleRoom.SequenceInstance sequence:
                RoomLayerAssetSequenceXBox.Text = sequence.X.ToString(CultureInfo.InvariantCulture);
                RoomLayerAssetSequenceYBox.Text = sequence.Y.ToString(CultureInfo.InvariantCulture);
                break;
            case UndertaleRoom.ParticleSystemInstance particle:
                RoomLayerAssetParticleXBox.Text = particle.X.ToString(CultureInfo.InvariantCulture);
                RoomLayerAssetParticleYBox.Text = particle.Y.ToString(CultureInfo.InvariantCulture);
                break;
        }
    }

    private void OpenRoomInstanceSummary(RoomInstanceSummary summary, bool addTab = true)
    {
        if (_data is null)
            return;

        if (summary.CreationCode is not null)
        {
            int codeIndex = _data.Code.IndexOf(summary.CreationCode);
            if (codeIndex >= 0)
            {
                NavigateToResource("Code", codeIndex, addTab);
                return;
            }
        }

        if (summary.ObjectDefinition is not null)
        {
            int objectIndex = _data.GameObjects.IndexOf(summary.ObjectDefinition);
            if (objectIndex >= 0)
                NavigateToResource("Objects", objectIndex, addTab);
        }
    }

    private void UpdateRoomInstanceButtons(RoomInstanceSummary? selectedSummary)
    {
        bool canEdit = _data is not null &&
                       !_data.UnsupportedBytecodeVersion &&
                       _selectedResource?.Value is UndertaleRoom &&
                       _data.GameObjects.Count > 0;
        RoomAddInstanceButton.IsEnabled = canEdit;
        RoomDuplicateInstanceButton.IsEnabled = canEdit && selectedSummary is not null;
        RoomRemoveInstanceButton.IsEnabled = canEdit && selectedSummary is not null;
        RoomMoveInstanceUpButton.IsEnabled = canEdit && selectedSummary is not null;
        RoomMoveInstanceDownButton.IsEnabled = canEdit && selectedSummary is not null;
    }

    private void RefreshRoomInstanceEditor(RoomInstanceSummary? summary)
    {
        _isUpdatingRoomInstanceEditor = true;
        if (summary is null)
        {
            HideRoomInstanceEditor();
            _isUpdatingRoomInstanceEditor = false;
            return;
        }

        UndertaleRoom.GameObject instance = summary.Instance;
        RoomInstanceEditorTitleText.Text = summary.Title;
        RoomInstanceXBox.Text = instance.X.ToString(CultureInfo.InvariantCulture);
        RoomInstanceYBox.Text = instance.Y.ToString(CultureInfo.InvariantCulture);
        RoomInstanceScaleXBox.Text = FormatFloat(instance.ScaleX);
        RoomInstanceScaleYBox.Text = FormatFloat(instance.ScaleY);
        RoomInstanceImageIndexBox.Text = instance.ImageIndex.ToString(CultureInfo.InvariantCulture);
        RoomInstanceImageSpeedBox.Text = FormatFloat(instance.ImageSpeed);
        RoomInstanceRotationBox.Text = FormatFloat(instance.Rotation);
        ObjectReferenceItem[] objectItems = _data is null ? [] : BuildObjectReferenceItems(_data).ToArray();
        CodeReferenceItem[] codeItems = _data is null ? [] : BuildCodeReferenceItems(_data, includeNull: true).ToArray();
        RoomInstanceLayerItem[] layerItems = _selectedResource?.Value is UndertaleRoom room
            ? BuildRoomInstanceLayerItems(room).ToArray()
            : [];
        UndertaleRoom.Layer? currentLayer = _selectedResource?.Value is UndertaleRoom selectedRoom
            ? GetRoomInstanceLayer(selectedRoom, instance)
            : null;
        RoomInstanceLayerComboBox.ItemsSource = layerItems;
        RoomInstanceLayerComboBox.SelectedItem = layerItems.FirstOrDefault(item => ReferenceEquals(item.Layer, currentLayer));
        RoomInstanceLayerComboBox.IsEnabled = layerItems.Length > 0;
        RoomInstanceObjectComboBox.ItemsSource = objectItems;
        RoomInstanceObjectComboBox.SelectedItem = objectItems.FirstOrDefault(item => ReferenceEquals(item.Object, instance.ObjectDefinition));
        RoomInstanceCreationCodeComboBox.ItemsSource = codeItems;
        RoomInstanceCreationCodeComboBox.SelectedItem = codeItems.FirstOrDefault(item => ReferenceEquals(item.Code, instance.CreationCode)) ??
                                                        codeItems.FirstOrDefault();
        RoomOpenInstanceObjectButton.IsEnabled = instance.ObjectDefinition is not null;
        RoomOpenInstanceCodeButton.IsEnabled = instance.CreationCode is not null;
        RoomInstanceEditorPanel.Visibility = Visibility.Visible;
        _isUpdatingRoomInstanceEditor = false;
    }

    private void HideRoomInstanceEditor()
    {
        RoomInstanceEditorTitleText.Text = "Selected instance";
        RoomInstanceXBox.Text = string.Empty;
        RoomInstanceYBox.Text = string.Empty;
        RoomInstanceScaleXBox.Text = string.Empty;
        RoomInstanceScaleYBox.Text = string.Empty;
        RoomInstanceImageIndexBox.Text = string.Empty;
        RoomInstanceImageSpeedBox.Text = string.Empty;
        RoomInstanceRotationBox.Text = string.Empty;
        RoomInstanceLayerComboBox.ItemsSource = null;
        RoomInstanceLayerComboBox.SelectedItem = null;
        RoomInstanceLayerComboBox.IsEnabled = false;
        RoomInstanceObjectComboBox.ItemsSource = null;
        RoomInstanceObjectComboBox.SelectedItem = null;
        RoomInstanceCreationCodeComboBox.ItemsSource = null;
        RoomInstanceCreationCodeComboBox.SelectedItem = null;
        RoomOpenInstanceObjectButton.IsEnabled = false;
        RoomOpenInstanceCodeButton.IsEnabled = false;
        RoomInstanceEditorPanel.Visibility = Visibility.Collapsed;
    }

    private void RefreshRoomBackgrounds(UndertaleRoom room, UndertaleRoom.Background? selectedBackground)
    {
        _isUpdatingRoomBackgroundEditor = true;
        try
        {
            RoomBackgroundSummary[] summaries = BuildRoomBackgroundSummaries(room).ToArray();
            RoomBackgroundsList.ItemsSource = summaries;
            RoomBackgroundSummary? selected = selectedBackground is null
                ? summaries.FirstOrDefault()
                : summaries.FirstOrDefault(summary => ReferenceEquals(summary.Background, selectedBackground));
            RoomBackgroundsList.SelectedItem = selected;
            UpdateRoomBackgroundButtons(selected);
            RefreshRoomBackgroundEditor(selected);
        }
        finally
        {
            _isUpdatingRoomBackgroundEditor = false;
        }
    }

    private void UpdateRoomBackgroundButtons(RoomBackgroundSummary? selectedSummary)
    {
        bool canEdit = _data is not null &&
                       !_data.UnsupportedBytecodeVersion &&
                       _selectedResource?.Value is UndertaleRoom &&
                       _data.Backgrounds.Count > 0;
        RoomAddBackgroundButton.IsEnabled = canEdit;
        RoomDuplicateBackgroundButton.IsEnabled = canEdit && selectedSummary is not null;
        RoomRemoveBackgroundButton.IsEnabled = canEdit && selectedSummary is not null && selectedSummary.Background.Enabled;
    }

    private void RefreshRoomBackgroundEditor(RoomBackgroundSummary? summary)
    {
        _isUpdatingRoomBackgroundEditor = true;
        if (summary is null)
        {
            HideRoomBackgroundEditor();
            _isUpdatingRoomBackgroundEditor = false;
            return;
        }

        UndertaleRoom.Background background = summary.Background;
        RoomBackgroundEditorTitleText.Text = summary.Title;
        RoomBackgroundEnabledCheckBox.IsChecked = background.Enabled;
        RoomBackgroundForegroundCheckBox.IsChecked = background.Foreground;
        RoomBackgroundStretchCheckBox.IsChecked = background.Stretch;
        RoomBackgroundTileXCheckBox.IsChecked = background.TiledHorizontally;
        RoomBackgroundTileYCheckBox.IsChecked = background.TiledVertically;
        RoomBackgroundXBox.Text = background.X.ToString(CultureInfo.InvariantCulture);
        RoomBackgroundYBox.Text = background.Y.ToString(CultureInfo.InvariantCulture);
        RoomBackgroundSpeedXBox.Text = background.SpeedX.ToString(CultureInfo.InvariantCulture);
        RoomBackgroundSpeedYBox.Text = background.SpeedY.ToString(CultureInfo.InvariantCulture);
        BackgroundReferenceItem[] backgroundItems = _data is null ? [] : BuildBackgroundReferenceItems(_data, includeNull: true).ToArray();
        RoomBackgroundDefinitionComboBox.ItemsSource = backgroundItems;
        RoomBackgroundDefinitionComboBox.SelectedItem = backgroundItems.FirstOrDefault(item => ReferenceEquals(item.Background, background.BackgroundDefinition)) ??
                                                       backgroundItems.FirstOrDefault();
        RoomOpenBackgroundButton.IsEnabled = background.BackgroundDefinition is not null;
        RoomBackgroundEditorPanel.Visibility = Visibility.Visible;
        _isUpdatingRoomBackgroundEditor = false;
    }

    private void HideRoomBackgroundEditor()
    {
        RoomBackgroundEditorTitleText.Text = "Selected background";
        RoomBackgroundEnabledCheckBox.IsChecked = false;
        RoomBackgroundForegroundCheckBox.IsChecked = false;
        RoomBackgroundStretchCheckBox.IsChecked = false;
        RoomBackgroundTileXCheckBox.IsChecked = false;
        RoomBackgroundTileYCheckBox.IsChecked = false;
        RoomBackgroundXBox.Text = string.Empty;
        RoomBackgroundYBox.Text = string.Empty;
        RoomBackgroundSpeedXBox.Text = string.Empty;
        RoomBackgroundSpeedYBox.Text = string.Empty;
        RoomBackgroundDefinitionComboBox.ItemsSource = null;
        RoomBackgroundDefinitionComboBox.SelectedItem = null;
        RoomOpenBackgroundButton.IsEnabled = false;
        RoomBackgroundEditorPanel.Visibility = Visibility.Collapsed;
    }

    private void RoomBackgroundsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RoomBackgroundSummary summary)
        {
            RoomBackgroundsList.SelectedItem = summary;
            UpdateRoomBackgroundButtons(summary);
            RefreshRoomBackgroundEditor(summary);
        }
    }

    private void RoomBackgroundsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomBackgroundEditor)
            return;

        RoomBackgroundSummary? summary = RoomBackgroundsList.SelectedItem as RoomBackgroundSummary;
        UpdateRoomBackgroundButtons(summary);
        RefreshRoomBackgroundEditor(summary);
    }

    private void RoomBackgroundsList_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            RoomOpenBackgroundButton_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Delete)
        {
            RoomRemoveBackgroundButton_Click(sender, e);
            e.Handled = true;
        }
    }

    private void RoomBackgroundsList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        RoomOpenBackgroundButton_Click(sender, e);
        e.Handled = true;
    }

    private void RoomBackgroundsList_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement element ||
            !IsMiddlePointerPressed(e, element) ||
            FindDataContextFromOriginalSource<RoomBackgroundSummary>(e.OriginalSource) is not { } summary)
        {
            return;
        }

        RoomBackgroundsList.SelectedItem = summary;
        UpdateRoomBackgroundButtons(summary);
        RefreshRoomBackgroundEditor(summary);
        OpenRoomBackgroundSummary(summary, addTab: true);
        e.Handled = true;
    }

    private async void RoomAddBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room)
        {
            return;
        }

        UndertaleBackground? definition =
            RoomBackgroundDefinitionComboBox.SelectedItem is BackgroundReferenceItem selectedBackground
                ? selectedBackground.Background
                : _data.Backgrounds.FirstOrDefault();
        if (definition is null)
        {
            StatusBox.Text = "Cannot add a room background because there are no backgrounds.";
            return;
        }

        UndertaleRoom.Background background = room.Backgrounds.FirstOrDefault(slot => !slot.Enabled && slot.BackgroundDefinition is null) ??
                                              new UndertaleRoom.Background { ParentRoom = room };
        if (!room.Backgrounds.Contains(background))
            room.Backgrounds.Add(background);

        background.ParentRoom = room;
        background.Enabled = true;
        background.Foreground = false;
        background.BackgroundDefinition = definition;
        background.X = 0;
        background.Y = 0;
        background.SpeedX = 0;
        background.SpeedY = 0;
        background.Stretch = false;
        background.TiledHorizontally = true;
        background.TiledVertically = true;
        UpdateRoomBackgroundStretch(background);
        await RefreshRoomBackgroundAfterEditAsync(room, background, "Added room background.");
    }

    private async void RoomDuplicateBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomBackgroundsList.SelectedItem is not RoomBackgroundSummary summary)
        {
            return;
        }

        UndertaleRoom.Background source = summary.Background;
        UndertaleRoom.Background duplicate = room.Backgrounds.FirstOrDefault(slot => !slot.Enabled && slot.BackgroundDefinition is null) ??
                                             new UndertaleRoom.Background { ParentRoom = room };
        if (!room.Backgrounds.Contains(duplicate))
            room.Backgrounds.Add(duplicate);

        duplicate.ParentRoom = room;
        duplicate.Enabled = source.Enabled;
        duplicate.Foreground = source.Foreground;
        duplicate.BackgroundDefinition = source.BackgroundDefinition;
        duplicate.X = source.X;
        duplicate.Y = source.Y;
        duplicate.SpeedX = source.SpeedX;
        duplicate.SpeedY = source.SpeedY;
        duplicate.Stretch = source.Stretch;
        duplicate.TiledHorizontally = source.TiledHorizontally;
        duplicate.TiledVertically = source.TiledVertically;
        UpdateRoomBackgroundStretch(duplicate);
        await RefreshRoomBackgroundAfterEditAsync(room, duplicate, "Duplicated room background.");
    }

    private async void RoomRemoveBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomBackgroundsList.SelectedItem is not RoomBackgroundSummary summary)
        {
            return;
        }

        UndertaleRoom.Background background = summary.Background;
        background.Enabled = false;
        background.BackgroundDefinition = null;
        UpdateRoomBackgroundStretch(background);
        await RefreshRoomBackgroundAfterEditAsync(room, background, "Disabled room background.");
    }

    private void RoomOpenBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            RoomBackgroundsList.SelectedItem is not RoomBackgroundSummary summary)
        {
            return;
        }

        OpenRoomBackgroundSummary(summary);
    }

    private void OpenRoomBackgroundSummary(RoomBackgroundSummary summary, bool addTab = true)
    {
        if (_data is null ||
            summary.Background.BackgroundDefinition is null)
        {
            return;
        }

        int backgroundIndex = _data.Backgrounds.IndexOf(summary.Background.BackgroundDefinition);
        if (backgroundIndex >= 0)
            NavigateToResource("Backgrounds", backgroundIndex, addTab);
    }

    private async void RoomBackgroundBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingRoomBackgroundEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomBackgroundsList.SelectedItem is not RoomBackgroundSummary summary)
        {
            return;
        }

        try
        {
            UndertaleRoom.Background background = summary.Background;
            int x = ParseRoomInstanceInt(RoomBackgroundXBox.Text, "Background X");
            int y = ParseRoomInstanceInt(RoomBackgroundYBox.Text, "Background Y");
            int speedX = ParseRoomInstanceInt(RoomBackgroundSpeedXBox.Text, "Background speed X");
            int speedY = ParseRoomInstanceInt(RoomBackgroundSpeedYBox.Text, "Background speed Y");
            if (background.X == x &&
                background.Y == y &&
                background.SpeedX == speedX &&
                background.SpeedY == speedY)
            {
                return;
            }

            background.X = x;
            background.Y = y;
            background.SpeedX = speedX;
            background.SpeedY = speedY;
            UpdateRoomBackgroundStretch(background);
            await RefreshRoomBackgroundAfterEditAsync(room, background, "Updated room background.");
        }
        catch (Exception ex)
        {
            RefreshRoomBackgroundEditor(summary);
            StatusBox.Text = ex.Message;
        }
    }

    private async void RoomBackgroundCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingRoomBackgroundEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomBackgroundsList.SelectedItem is not RoomBackgroundSummary summary)
        {
            return;
        }

        UndertaleRoom.Background background = summary.Background;
        bool enabled = RoomBackgroundEnabledCheckBox.IsChecked == true;
        bool foreground = RoomBackgroundForegroundCheckBox.IsChecked == true;
        bool stretch = RoomBackgroundStretchCheckBox.IsChecked == true;
        bool tiledHorizontally = RoomBackgroundTileXCheckBox.IsChecked == true;
        bool tiledVertically = RoomBackgroundTileYCheckBox.IsChecked == true;
        if (background.Enabled == enabled &&
            background.Foreground == foreground &&
            background.Stretch == stretch &&
            background.TiledHorizontally == tiledHorizontally &&
            background.TiledVertically == tiledVertically)
        {
            return;
        }

        background.Enabled = enabled;
        background.Foreground = foreground;
        background.Stretch = stretch;
        background.TiledHorizontally = tiledHorizontally;
        background.TiledVertically = tiledVertically;
        UpdateRoomBackgroundStretch(background);
        await RefreshRoomBackgroundAfterEditAsync(room, background, "Updated room background flags.");
    }

    private async void RoomBackgroundDefinitionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomBackgroundEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomBackgroundsList.SelectedItem is not RoomBackgroundSummary summary ||
            RoomBackgroundDefinitionComboBox.SelectedItem is not BackgroundReferenceItem backgroundItem)
        {
            return;
        }

        UndertaleRoom.Background background = summary.Background;
        if (ReferenceEquals(background.BackgroundDefinition, backgroundItem.Background))
            return;

        background.BackgroundDefinition = backgroundItem.Background;
        background.Enabled = backgroundItem.Background is not null && background.Enabled;
        UpdateRoomBackgroundStretch(background);
        await RefreshRoomBackgroundAfterEditAsync(room, background, "Updated room background definition.");
    }

    private async System.Threading.Tasks.Task RefreshRoomBackgroundAfterEditAsync(
        UndertaleRoom room,
        UndertaleRoom.Background background,
        string status)
    {
        MarkDirty();
        RefreshRoomBackgrounds(room, background);
        RoomOverviewText.Text = BuildRoomOverview(room);
        RefreshRoomLayers(room, (RoomLayersList.SelectedItem as RoomLayerSummary)?.Layer);
        RoomInstanceSummary[] instanceSummaries = BuildRoomInstanceSummaries(room).ToArray();
        if (_isRoomPreviewRendered)
            await RefreshRoomPreviewAsync(room, instanceSummaries);
        else
            ClearRoomPreviewSurface(room, instanceSummaries);
        RefreshCurrentDetails();
        StatusBox.Text = status;
    }

    private static void UpdateRoomBackgroundStretch(UndertaleRoom.Background background)
    {
        if (background.BackgroundDefinition?.Texture is null)
        {
            background.CalcScaleX = 1;
            background.CalcScaleY = 1;
            return;
        }

        background.UpdateStretch();
    }

    private static bool UsesLegacyRoomBackgroundSlots(UndertaleRoom room)
    {
        return !room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGMS2) &&
               !room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGM2024_13);
    }

    private void RefreshRoomTiles(UndertaleRoom room, UndertaleRoom.Tile? selectedTile)
    {
        _isUpdatingRoomTileEditor = true;
        try
        {
            RoomTileSummary[] summaries = BuildRoomTileSummaries(room).ToArray();
            RoomTilesList.ItemsSource = summaries;
            RoomTileSummary? selected = selectedTile is null
                ? summaries.FirstOrDefault()
                : summaries.FirstOrDefault(summary => ReferenceEquals(summary.Tile, selectedTile));
            RoomTilesList.SelectedItem = selected;
            UpdateRoomTileButtons(selected);
            RefreshRoomTileEditor(selected);
        }
        finally
        {
            _isUpdatingRoomTileEditor = false;
        }
    }

    private void UpdateRoomTileButtons(RoomTileSummary? selectedSummary)
    {
        bool canEdit = _data is not null &&
                       !_data.UnsupportedBytecodeVersion &&
                       _selectedResource?.Value is UndertaleRoom;
        bool canAdd = canEdit &&
                      _selectedResource?.Value is UndertaleRoom room &&
                      UsesLegacyRoomBackgroundSlots(room) &&
                      _data!.Backgrounds.Count > 0;
        RoomAddTileButton.IsEnabled = canAdd;
        RoomDuplicateTileButton.IsEnabled = canEdit && selectedSummary is not null;
        RoomRemoveTileButton.IsEnabled = canEdit && selectedSummary is not null;
    }

    private void RefreshRoomTileEditor(RoomTileSummary? summary)
    {
        _isUpdatingRoomTileEditor = true;
        if (summary is null)
        {
            HideRoomTileEditor();
            _isUpdatingRoomTileEditor = false;
            return;
        }

        UndertaleRoom.Tile tile = summary.Tile;
        RoomTileEditorTitleText.Text = summary.Title;
        RoomTileXBox.Text = tile.X.ToString(CultureInfo.InvariantCulture);
        RoomTileYBox.Text = tile.Y.ToString(CultureInfo.InvariantCulture);
        RoomTileSourceXBox.Text = tile.SourceX.ToString(CultureInfo.InvariantCulture);
        RoomTileSourceYBox.Text = tile.SourceY.ToString(CultureInfo.InvariantCulture);
        RoomTileWidthBox.Text = tile.Width.ToString(CultureInfo.InvariantCulture);
        RoomTileHeightBox.Text = tile.Height.ToString(CultureInfo.InvariantCulture);
        RoomTileDepthBox.Text = tile.TileDepth.ToString(CultureInfo.InvariantCulture);
        RoomTileInstanceIdBox.Text = tile.InstanceID.ToString(CultureInfo.InvariantCulture);
        RoomTileScaleXBox.Text = FormatFloat(tile.ScaleX);
        RoomTileScaleYBox.Text = FormatFloat(tile.ScaleY);
        RoomTileColorBox.Text = FormatRoomTileColor(tile.Color);

        if (tile.spriteMode)
        {
            RoomTileDefinitionLabelText.Text = "Sprite definition";
            SpriteReferenceItem[] spriteItems = _data is null ? [] : BuildSpriteReferenceItems(_data, includeNull: true).ToArray();
            RoomTileDefinitionComboBox.ItemsSource = spriteItems;
            RoomTileDefinitionComboBox.SelectedItem = spriteItems.FirstOrDefault(item => ReferenceEquals(item.Sprite, tile.SpriteDefinition)) ??
                                                      spriteItems.FirstOrDefault();
            RoomOpenTileDefinitionButton.IsEnabled = tile.SpriteDefinition is not null;
        }
        else
        {
            RoomTileDefinitionLabelText.Text = "Background definition";
            BackgroundReferenceItem[] backgroundItems = _data is null ? [] : BuildBackgroundReferenceItems(_data, includeNull: true).ToArray();
            RoomTileDefinitionComboBox.ItemsSource = backgroundItems;
            RoomTileDefinitionComboBox.SelectedItem = backgroundItems.FirstOrDefault(item => ReferenceEquals(item.Background, tile.BackgroundDefinition)) ??
                                                      backgroundItems.FirstOrDefault();
            RoomOpenTileDefinitionButton.IsEnabled = tile.BackgroundDefinition is not null;
        }

        RefreshRoomTileSourcePreview(summary);
        RoomTileEditorPanel.Visibility = Visibility.Visible;
        _isUpdatingRoomTileEditor = false;
    }

    private void HideRoomTileEditor()
    {
        RoomTileEditorTitleText.Text = "Selected tile";
        RoomTileDefinitionLabelText.Text = "Definition";
        RoomTileXBox.Text = string.Empty;
        RoomTileYBox.Text = string.Empty;
        RoomTileSourceXBox.Text = string.Empty;
        RoomTileSourceYBox.Text = string.Empty;
        RoomTileWidthBox.Text = string.Empty;
        RoomTileHeightBox.Text = string.Empty;
        RoomTileDepthBox.Text = string.Empty;
        RoomTileInstanceIdBox.Text = string.Empty;
        RoomTileScaleXBox.Text = string.Empty;
        RoomTileScaleYBox.Text = string.Empty;
        RoomTileColorBox.Text = string.Empty;
        RoomTileDefinitionComboBox.ItemsSource = null;
        RoomTileDefinitionComboBox.SelectedItem = null;
        RoomOpenTileDefinitionButton.IsEnabled = false;
        ClearRoomTileSourcePreview();
        RoomTileEditorPanel.Visibility = Visibility.Collapsed;
    }

    private void RoomTilesList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RoomTileSummary summary)
        {
            RoomTilesList.SelectedItem = summary;
            UpdateRoomTileButtons(summary);
            RefreshRoomTileEditor(summary);
        }
    }

    private void RoomTilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomTileEditor)
            return;

        RoomTileSummary? summary = RoomTilesList.SelectedItem as RoomTileSummary;
        UpdateRoomTileButtons(summary);
        RefreshRoomTileEditor(summary);
    }

    private void RoomTilesList_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            RoomOpenTileDefinitionButton_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Delete)
        {
            RoomRemoveTileButton_Click(sender, e);
            e.Handled = true;
        }
    }

    private void RoomTilesList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        RoomOpenTileDefinitionButton_Click(sender, e);
        e.Handled = true;
    }

    private void RoomTilesList_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement element ||
            !IsMiddlePointerPressed(e, element) ||
            FindDataContextFromOriginalSource<RoomTileSummary>(e.OriginalSource) is not { } summary)
        {
            return;
        }

        RoomTilesList.SelectedItem = summary;
        UpdateRoomTileButtons(summary);
        RefreshRoomTileEditor(summary);
        OpenRoomTileDefinition(summary, addTab: true);
        e.Handled = true;
    }

    private async void RoomAddTileButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            !UsesLegacyRoomBackgroundSlots(room))
        {
            return;
        }

        UndertaleBackground? definition =
            RoomTileDefinitionComboBox.SelectedItem is BackgroundReferenceItem selectedBackground && selectedBackground.Background is not null
                ? selectedBackground.Background
                : _data.Backgrounds.FirstOrDefault();
        if (definition is null)
        {
            StatusBox.Text = "Cannot add a room tile because there are no backgrounds.";
            return;
        }

        UndertaleTexturePageItem? texture = definition.Texture;
        UndertaleRoom.Tile tile = new()
        {
            spriteMode = false,
            BackgroundDefinition = definition,
            InstanceID = _data.GeneralInfo.LastTile++,
            Width = GetDefaultRoomTileWidth(texture),
            Height = GetDefaultRoomTileHeight(texture),
            ScaleX = 1,
            ScaleY = 1,
            Color = 0xFFFFFFFF
        };

        room.Tiles.Add(tile);
        await RefreshRoomTileAfterEditAsync(room, tile, "Added room tile.");
    }

    private async void RoomDuplicateTileButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomTilesList.SelectedItem is not RoomTileSummary summary)
        {
            return;
        }

        UndertaleRoom.Tile duplicate = CloneRoomTile(summary.Tile, _data.GeneralInfo.LastTile++);
        UndertalePointerList<UndertaleRoom.Tile> tiles = GetRoomTileCollection(room, summary);
        int sourceIndex = tiles.IndexOf(summary.Tile);
        int insertIndex = sourceIndex >= 0 ? sourceIndex + 1 : tiles.Count;
        tiles.Insert(insertIndex, duplicate);
        await RefreshRoomTileAfterEditAsync(room, duplicate, "Duplicated room tile.");
    }

    private async void RoomRemoveTileButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomTilesList.SelectedItem is not RoomTileSummary summary)
        {
            return;
        }

        UndertalePointerList<UndertaleRoom.Tile> tiles = GetRoomTileCollection(room, summary);
        int index = tiles.IndexOf(summary.Tile);
        if (index < 0)
            return;

        tiles.RemoveAt(index);
        RoomTileSummary[] summaries = BuildRoomTileSummaries(room).ToArray();
        RoomTilesList.ItemsSource = summaries;
        RoomTileSummary? nextSelection = summaries.Length == 0 ? null : summaries[Math.Clamp(index, 0, summaries.Length - 1)];
        RoomTilesList.SelectedItem = nextSelection;
        UpdateRoomTileButtons(nextSelection);
        RefreshRoomTileEditor(nextSelection);
        MarkDirty();
        RoomOverviewText.Text = BuildRoomOverview(room);
        RefreshRoomLayers(room, (RoomLayersList.SelectedItem as RoomLayerSummary)?.Layer);
        RoomInstanceSummary[] instanceSummaries = BuildRoomInstanceSummaries(room).ToArray();
        if (_isRoomPreviewRendered)
            await RefreshRoomPreviewAsync(room, instanceSummaries);
        else
            ClearRoomPreviewSurface(room, instanceSummaries);
        RefreshCurrentDetails();
        StatusBox.Text = "Removed room tile.";
    }

    private void RoomOpenTileDefinitionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            RoomTilesList.SelectedItem is not RoomTileSummary summary)
        {
            return;
        }

        OpenRoomTileDefinition(summary);
    }

    private void OpenRoomTileDefinition(RoomTileSummary summary, bool addTab = true)
    {
        if (_data is null)
            return;

        UndertaleRoom.Tile tile = summary.Tile;
        if (tile.spriteMode)
        {
            int spriteIndex = _data.Sprites.IndexOf(tile.SpriteDefinition);
            if (spriteIndex >= 0)
                NavigateToResource("Sprites", spriteIndex, addTab);
        }
        else
        {
            int backgroundIndex = _data.Backgrounds.IndexOf(tile.BackgroundDefinition);
            if (backgroundIndex >= 0)
                NavigateToResource("Backgrounds", backgroundIndex, addTab);
        }
    }

    private async void RoomTileBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingRoomTileEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomTilesList.SelectedItem is not RoomTileSummary summary)
        {
            return;
        }

        try
        {
            UndertaleRoom.Tile tile = summary.Tile;
            int x = ParseRoomInstanceInt(RoomTileXBox.Text, "Tile X");
            int y = ParseRoomInstanceInt(RoomTileYBox.Text, "Tile Y");
            int sourceX = ParseRoomInstanceInt(RoomTileSourceXBox.Text, "Tile source X");
            int sourceY = ParseRoomInstanceInt(RoomTileSourceYBox.Text, "Tile source Y");
            uint width = ParseRoomTileUInt(RoomTileWidthBox.Text, "Tile width");
            uint height = ParseRoomTileUInt(RoomTileHeightBox.Text, "Tile height");
            int depth = ParseRoomInstanceInt(RoomTileDepthBox.Text, "Tile depth");
            uint instanceId = ParseRoomTileUInt(RoomTileInstanceIdBox.Text, "Tile instance ID");
            float scaleX = ParseRoomInstanceFloat(RoomTileScaleXBox.Text, "Tile scale X");
            float scaleY = ParseRoomInstanceFloat(RoomTileScaleYBox.Text, "Tile scale Y");
            uint color = ParseRoomTileColor(RoomTileColorBox.Text);

            if (tile.X == x &&
                tile.Y == y &&
                tile.SourceX == sourceX &&
                tile.SourceY == sourceY &&
                tile.Width == width &&
                tile.Height == height &&
                tile.TileDepth == depth &&
                tile.InstanceID == instanceId &&
                NearlyEqual(tile.ScaleX, scaleX) &&
                NearlyEqual(tile.ScaleY, scaleY) &&
                tile.Color == color)
            {
                return;
            }

            tile.X = x;
            tile.Y = y;
            tile.SourceX = sourceX;
            tile.SourceY = sourceY;
            tile.Width = width;
            tile.Height = height;
            tile.TileDepth = depth;
            tile.InstanceID = instanceId;
            tile.ScaleX = scaleX;
            tile.ScaleY = scaleY;
            tile.Color = color;
            await RefreshRoomTileAfterEditAsync(room, tile, "Updated room tile.");
        }
        catch (Exception ex)
        {
            RefreshRoomTileEditor(summary);
            StatusBox.Text = ex.Message;
        }
    }

    private async void RoomTileDefinitionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomTileEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomTilesList.SelectedItem is not RoomTileSummary summary)
        {
            return;
        }

        UndertaleRoom.Tile tile = summary.Tile;
        if (tile.spriteMode)
        {
            if (RoomTileDefinitionComboBox.SelectedItem is not SpriteReferenceItem spriteItem ||
                ReferenceEquals(tile.SpriteDefinition, spriteItem.Sprite))
            {
                return;
            }

            tile.SpriteDefinition = spriteItem.Sprite;
        }
        else
        {
            if (RoomTileDefinitionComboBox.SelectedItem is not BackgroundReferenceItem backgroundItem ||
                ReferenceEquals(tile.BackgroundDefinition, backgroundItem.Background))
            {
                return;
            }

            tile.BackgroundDefinition = backgroundItem.Background;
        }

        await RefreshRoomTileAfterEditAsync(room, tile, "Updated room tile definition.");
    }

    private async System.Threading.Tasks.Task RefreshRoomTileAfterEditAsync(
        UndertaleRoom room,
        UndertaleRoom.Tile tile,
        string status)
    {
        MarkDirty();
        RefreshRoomTiles(room, tile);
        RoomOverviewText.Text = BuildRoomOverview(room);
        RefreshRoomLayers(room, (RoomLayersList.SelectedItem as RoomLayerSummary)?.Layer);
        RoomInstanceSummary[] instanceSummaries = BuildRoomInstanceSummaries(room).ToArray();
        if (_isRoomPreviewRendered)
            await RefreshRoomPreviewAsync(room, instanceSummaries);
        else
            ClearRoomPreviewSurface(room, instanceSummaries);
        RefreshCurrentDetails();
        StatusBox.Text = status;
    }

    private static UndertalePointerList<UndertaleRoom.Tile> GetRoomTileCollection(
        UndertaleRoom room,
        RoomTileSummary summary)
    {
        return summary.Layer?.AssetsData?.LegacyTiles ?? room.Tiles;
    }

    private static UndertaleRoom.Tile CloneRoomTile(UndertaleRoom.Tile source, uint instanceId)
    {
        UndertaleRoom.Tile duplicate = new()
        {
            X = source.X + 16,
            Y = source.Y + 16,
            spriteMode = source.spriteMode,
            SourceX = source.SourceX,
            SourceY = source.SourceY,
            Width = source.Width,
            Height = source.Height,
            TileDepth = source.TileDepth,
            InstanceID = instanceId,
            ScaleX = source.ScaleX,
            ScaleY = source.ScaleY,
            Color = source.Color
        };

        if (source.spriteMode)
            duplicate.SpriteDefinition = source.SpriteDefinition;
        else
            duplicate.BackgroundDefinition = source.BackgroundDefinition;

        return duplicate;
    }

    private static uint GetDefaultRoomTileWidth(UndertaleTexturePageItem? texture)
    {
        if (texture is null)
            return 0;

        return (uint)Math.Max(texture.SourceWidth, Math.Max(texture.TargetWidth, texture.BoundingWidth));
    }

    private static uint GetDefaultRoomTileHeight(UndertaleTexturePageItem? texture)
    {
        if (texture is null)
            return 0;

        return (uint)Math.Max(texture.SourceHeight, Math.Max(texture.TargetHeight, texture.BoundingHeight));
    }

    private static uint ParseRoomTileUInt(string value, string label)
    {
        if (!uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint result))
            throw new InvalidDataException($"{label} must be a non-negative integer.");

        return result;
    }

    private static uint ParseRoomTileValue(string value, string label)
    {
        string text = value.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (uint.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint hexResult))
                return hexResult;

            throw new InvalidDataException($"{label} must be a uint or hex value.");
        }

        if (uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint decimalResult))
            return decimalResult;

        throw new InvalidDataException($"{label} must be a uint or hex value.");
    }

    private static string FormatRoomTileRawValue(uint value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatRoomTileDetail(uint value)
    {
        uint index = value & RoomTileIndexMask;
        uint flags = (value & RoomTileFlagsMask) >> 28;
        return value == 0
            ? "0 (empty)"
            : $"{FormatRoomTileRawValue(value)} / 0x{value:X8} (index {index}, flags {flags})";
    }

    private static uint MirrorRoomTileValue(uint value)
    {
        return (value & RoomTileRotateFlag) != 0
            ? value ^ RoomTileFlipVerticalFlag
            : value ^ RoomTileFlipHorizontalFlag;
    }

    private static uint FlipRoomTileValue(uint value)
    {
        return (value & RoomTileRotateFlag) != 0
            ? value ^ RoomTileFlipHorizontalFlag
            : value ^ RoomTileFlipVerticalFlag;
    }

    private static uint RotateRoomTileValueClockwise(uint value)
    {
        uint index = value & RoomTileIndexMask;
        uint flags = (value & RoomTileFlagsMask) >> 28;
        return index | (RotateRoomTileFlagsClockwise(flags) << 28);
    }

    private static uint RotateRoomTileValueCounterClockwise(uint value)
    {
        uint index = value & RoomTileIndexMask;
        uint flags = (value & RoomTileFlagsMask) >> 28;
        return index | (RotateRoomTileFlagsCounterClockwise(flags) << 28);
    }

    private static uint RotateRoomTileFlagsClockwise(uint flags)
    {
        return flags switch
        {
            0b000 => 0b100,
            0b100 => 0b011,
            0b011 => 0b111,
            0b111 => 0b000,
            0b110 => 0b001,
            0b010 => 0b110,
            0b101 => 0b010,
            0b001 => 0b101,
            _ => throw new InvalidDataException($"{flags} is not a valid room tile transform.")
        };
    }

    private static uint RotateRoomTileFlagsCounterClockwise(uint flags)
    {
        return flags switch
        {
            0b100 => 0b000,
            0b011 => 0b100,
            0b111 => 0b011,
            0b000 => 0b111,
            0b001 => 0b110,
            0b110 => 0b010,
            0b010 => 0b101,
            0b101 => 0b001,
            _ => throw new InvalidDataException($"{flags} is not a valid room tile transform.")
        };
    }

    private static StackPanel BuildLabeledDialogField(string label, FrameworkElement field)
    {
        return new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock { Text = label },
                field
            }
        };
    }

    private static Border BuildTileVisualPreviewHost(Image image)
    {
        Brush background = Application.Current.Resources.TryGetValue("WinUiPanelBackgroundBrush", out object backgroundResource) &&
                           backgroundResource is Brush backgroundBrush
            ? backgroundBrush
            : new SolidColorBrush(Microsoft.UI.Colors.Transparent);

        return new Border
        {
            MinHeight = 128,
            Padding = new Thickness(8),
            Background = background,
            BorderBrush = Application.Current.Resources.TryGetValue("ControlStrokeColorDefaultBrush", out object borderResource) &&
                          borderResource is Brush borderBrush
                ? borderBrush
                : null,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = image
        };
    }

    private static DataTemplate CreateRoomTileCellEditorTemplate()
    {
        const string xaml = """
<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
    <Border
        MinWidth="92"
        Margin="2"
        Padding="6"
        BorderBrush="{ThemeResource ControlStrokeColorDefaultBrush}"
        BorderThickness="1"
        Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
        CornerRadius="4">
        <StackPanel Spacing="2">
            <TextBlock
                FontSize="12"
                Text="{Binding Coordinates}"
                TextTrimming="CharacterEllipsis" />
            <TextBlock
                FontSize="13"
                FontWeight="SemiBold"
                Text="{Binding ValueText}"
                TextTrimming="CharacterEllipsis" />
            <TextBlock
                FontSize="11"
                Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                Text="{Binding Detail}"
                TextTrimming="CharacterEllipsis" />
        </StackPanel>
    </Border>
</DataTemplate>
""";
        return (DataTemplate)XamlReader.Load(xaml);
    }

    private static DataTemplate CreateRoomTilePaletteEditorTemplate()
    {
        const string xaml = """
<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
    <Border
        MinWidth="148"
        Margin="2"
        Padding="6"
        BorderBrush="{ThemeResource ControlStrokeColorDefaultBrush}"
        BorderThickness="1"
        Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
        CornerRadius="4">
        <Grid ColumnSpacing="8">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <Border
                Width="42"
                Height="42"
                Background="{ThemeResource WinUiPanelBackgroundBrush}"
                BorderBrush="{ThemeResource ControlStrokeColorDefaultBrush}"
                BorderThickness="1"
                CornerRadius="3">
                <Image
                    Margin="3"
                    Source="{Binding PreviewSource}"
                    Stretch="Uniform" />
            </Border>
            <StackPanel
                Grid.Column="1"
                Spacing="1">
                <TextBlock
                    FontSize="12"
                    Text="{Binding Title}"
                    TextTrimming="CharacterEllipsis" />
                <TextBlock
                    FontSize="13"
                    FontWeight="SemiBold"
                    Text="{Binding ValueText}"
                    TextTrimming="CharacterEllipsis" />
                <TextBlock
                    FontSize="11"
                    Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                    Text="{Binding Detail}"
                    TextTrimming="CharacterEllipsis" />
                <TextBlock
                    FontSize="10"
                    Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                    Text="{Binding PreviewStatus}"
                    TextTrimming="CharacterEllipsis" />
            </StackPanel>
        </Grid>
    </Border>
</DataTemplate>
""";
        return (DataTemplate)XamlReader.Load(xaml);
    }

    private static uint ParseRoomTileColor(string value)
    {
        string text = value.Trim();
        bool hexMode = false;
        if (text.StartsWith("#", StringComparison.Ordinal))
        {
            text = text[1..];
            hexMode = true;
        }

        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            text = text[2..];
            hexMode = true;
        }

        if (hexMode && uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint hexResult))
            return hexResult;

        if (uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint decimalResult))
            return decimalResult;

        throw new InvalidDataException("Tile color must be a uint or hex value.");
    }

    private static string FormatRoomTileColor(uint color)
    {
        return $"0x{color:X8}";
    }

    private static bool NearlyEqual(float left, float right)
    {
        return Math.Abs(left - right) < 0.0001f;
    }

    private async void RefreshRoomTileSourcePreview(RoomTileSummary summary)
    {
        int generation = ++_roomTileSourcePreviewGeneration;
        UndertaleRoom.Tile tile = summary.Tile;
        UndertaleTexturePageItem? texture = tile.Tpag;
        if (texture is null || texture.BoundingWidth == 0 || texture.BoundingHeight == 0)
        {
            ClearRoomTileSourcePreview();
            return;
        }

        double width = Math.Max(1, (int)texture.BoundingWidth);
        double height = Math.Max(1, (int)texture.BoundingHeight);
        RoomTileSourcePreviewPanel.Visibility = Visibility.Visible;
        RoomTileSourcePreviewCanvas.Width = width;
        RoomTileSourcePreviewCanvas.Height = height;
        RoomTileSourcePreviewImage.Width = width;
        RoomTileSourcePreviewImage.Height = height;
        RoomTileSourcePreviewSelector.Visibility = Visibility.Visible;
        RoomTileSourcePreviewInfoText.Text = $"Source {tile.SourceX},{tile.SourceY} {tile.Width}x{tile.Height}; texture bounds {texture.BoundingWidth}x{texture.BoundingHeight}.";
        DrawRoomTileSourcePreviewSelector(tile);

        try
        {
            byte[] previewBytes = await System.Threading.Tasks.Task.Run(() => GetCachedTexturePageItemPreviewPng(texture));
            if (generation != _roomTileSourcePreviewGeneration ||
                RoomTilesList.SelectedItem is not RoomTileSummary selected ||
                !ReferenceEquals(selected.Tile, tile))
            {
                return;
            }

            RoomTileSourcePreviewImage.Source = LoadBitmapImage(previewBytes);
            DrawRoomTileSourcePreviewSelector(tile);
        }
        catch (Exception ex)
        {
            if (generation == _roomTileSourcePreviewGeneration)
                RoomTileSourcePreviewInfoText.Text = $"Could not render tile source preview: {ex.Message}";
        }
    }

    private void ClearRoomTileSourcePreview()
    {
        _roomTileSourcePreviewGeneration++;
        _roomTileSourceDragOrigin = null;
        _roomTileSourceDragHadChanges = false;
        RoomTileSourcePreviewImage.Source = null;
        RoomTileSourcePreviewSelector.Visibility = Visibility.Collapsed;
        RoomTileSourcePreviewCanvas.Width = 0;
        RoomTileSourcePreviewCanvas.Height = 0;
        RoomTileSourcePreviewImage.Width = 0;
        RoomTileSourcePreviewImage.Height = 0;
        RoomTileSourcePreviewInfoText.Text = string.Empty;
        RoomTileSourcePreviewPanel.Visibility = Visibility.Collapsed;
    }

    private void DrawRoomTileSourcePreviewSelector(UndertaleRoom.Tile tile)
    {
        UndertaleTexturePageItem? texture = tile.Tpag;
        if (texture is null)
        {
            RoomTileSourcePreviewSelector.Visibility = Visibility.Collapsed;
            return;
        }

        double width = Math.Clamp((double)tile.Width, 1d, Math.Max(1d, (double)texture.BoundingWidth));
        double height = Math.Clamp((double)tile.Height, 1d, Math.Max(1d, (double)texture.BoundingHeight));
        double x = Math.Clamp(tile.SourceX, 0, Math.Max(0, texture.BoundingWidth - width));
        double y = Math.Clamp(tile.SourceY, 0, Math.Max(0, texture.BoundingHeight - height));
        RoomTileSourcePreviewSelector.Width = width;
        RoomTileSourcePreviewSelector.Height = height;
        Canvas.SetLeft(RoomTileSourcePreviewSelector, x);
        Canvas.SetTop(RoomTileSourcePreviewSelector, y);
        RoomTileSourcePreviewSelector.Visibility = Visibility.Visible;
    }

    private async void RoomTileSourcePreviewCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomTilesList.SelectedItem is not RoomTileSummary summary ||
            summary.Tile.Tpag is null)
        {
            return;
        }

        Point snapped = GetRoomTileSourcePoint(e.GetCurrentPoint(RoomTileSourcePreviewCanvas).Position, room, summary.Tile);
        _roomTileSourceDragOrigin = snapped;
        _roomTileSourcePointerId = e.Pointer.PointerId;
        _roomTileSourceDragHadChanges = true;
        RoomTileSourcePreviewCanvas.CapturePointer(e.Pointer);
        ApplyRoomTileSourceSelection(room, summary.Tile, snapped, resizeFromOrigin: false);
        e.Handled = true;
        await System.Threading.Tasks.Task.CompletedTask;
    }

    private void RoomTileSourcePreviewCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_roomTileSourceDragOrigin is null ||
            e.Pointer.PointerId != _roomTileSourcePointerId ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomTilesList.SelectedItem is not RoomTileSummary summary ||
            summary.Tile.Tpag is null)
        {
            return;
        }

        bool resize = IsVirtualKeyDown(VirtualKey.Menu);
        Point snapped = GetRoomTileSourcePoint(e.GetCurrentPoint(RoomTileSourcePreviewCanvas).Position, room, summary.Tile);
        ApplyRoomTileSourceSelection(room, summary.Tile, snapped, resize);
        _roomTileSourceDragHadChanges = true;
        e.Handled = true;
    }

    private async void RoomTileSourcePreviewCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_roomTileSourceDragOrigin is null ||
            e.Pointer.PointerId != _roomTileSourcePointerId)
        {
            return;
        }

        RoomTileSourcePreviewCanvas.ReleasePointerCapture(e.Pointer);
        bool hadChanges = _roomTileSourceDragHadChanges;
        _roomTileSourceDragOrigin = null;
        _roomTileSourceDragHadChanges = false;
        e.Handled = true;

        if (hadChanges &&
            _selectedResource?.Value is UndertaleRoom room &&
            RoomTilesList.SelectedItem is RoomTileSummary summary)
        {
            await RefreshRoomTileAfterEditAsync(room, summary.Tile, "Updated room tile source.");
        }
    }

    private void RoomTileSourcePreviewCanvas_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (_roomTileSourceDragOrigin is null ||
            e.Pointer.PointerId != _roomTileSourcePointerId)
        {
            return;
        }

        RoomTileSourcePreviewCanvas.ReleasePointerCapture(e.Pointer);
        _roomTileSourceDragOrigin = null;
        _roomTileSourceDragHadChanges = false;
        e.Handled = true;
    }

    private void ApplyRoomTileSourceSelection(
        UndertaleRoom room,
        UndertaleRoom.Tile tile,
        Point snapped,
        bool resizeFromOrigin)
    {
        UndertaleTexturePageItem? texture = tile.Tpag;
        if (texture is null)
            return;

        int gridWidth = GetRoomGridWidth(room);
        int gridHeight = GetRoomGridHeight(room);
        if (resizeFromOrigin && _roomTileSourceDragOrigin is { } origin)
        {
            double differenceX = snapped.X - origin.X;
            double differenceY = snapped.Y - origin.Y;
            int sourceX = (int)(differenceX < 0 ? snapped.X : origin.X);
            int sourceY = (int)(differenceY < 0 ? snapped.Y : origin.Y);
            uint width = (uint)Math.Clamp(Math.Abs(differenceX), 0d, (double)texture.BoundingWidth) + (uint)gridWidth;
            uint height = (uint)Math.Clamp(Math.Abs(differenceY), 0d, (double)texture.BoundingHeight) + (uint)gridHeight;
            SetRoomTileSource(tile, sourceX, sourceY, width, height);
        }
        else
        {
            SetRoomTileSource(tile, (int)snapped.X, (int)snapped.Y, (uint)gridWidth, (uint)gridHeight);
        }

        UpdateRoomTileSourceEditor(tile);
        DrawRoomTileSourcePreviewSelector(tile);
        RoomTileSourcePreviewInfoText.Text = $"Source {tile.SourceX},{tile.SourceY} {tile.Width}x{tile.Height}; texture bounds {texture.BoundingWidth}x{texture.BoundingHeight}.";
        MarkDirty();
        StatusBox.Text = $"Updated room tile source to {tile.SourceX},{tile.SourceY}.";
    }

    private static void SetRoomTileSource(
        UndertaleRoom.Tile tile,
        int sourceX,
        int sourceY,
        uint width,
        uint height)
    {
        UndertaleTexturePageItem? texture = tile.Tpag;
        if (texture is null)
            return;

        int requestedWidth = width > int.MaxValue ? int.MaxValue : (int)Math.Max(1u, width);
        int requestedHeight = height > int.MaxValue ? int.MaxValue : (int)Math.Max(1u, height);
        int clampedWidth = Math.Clamp(requestedWidth, 1, Math.Max(1, (int)texture.BoundingWidth));
        int clampedHeight = Math.Clamp(requestedHeight, 1, Math.Max(1, (int)texture.BoundingHeight));
        int clampedX = Math.Clamp(sourceX, 0, Math.Max(0, texture.BoundingWidth - clampedWidth));
        int clampedY = Math.Clamp(sourceY, 0, Math.Max(0, texture.BoundingHeight - clampedHeight));
        tile.SourceX = clampedX;
        tile.SourceY = clampedY;
        tile.Width = (uint)clampedWidth;
        tile.Height = (uint)clampedHeight;
    }

    private void UpdateRoomTileSourceEditor(UndertaleRoom.Tile tile)
    {
        _isUpdatingRoomTileEditor = true;
        RoomTileSourceXBox.Text = tile.SourceX.ToString(CultureInfo.InvariantCulture);
        RoomTileSourceYBox.Text = tile.SourceY.ToString(CultureInfo.InvariantCulture);
        RoomTileWidthBox.Text = tile.Width.ToString(CultureInfo.InvariantCulture);
        RoomTileHeightBox.Text = tile.Height.ToString(CultureInfo.InvariantCulture);
        _isUpdatingRoomTileEditor = false;
    }

    private static Point GetRoomTileSourcePoint(Point point, UndertaleRoom room, UndertaleRoom.Tile tile)
    {
        int gridWidth = GetRoomGridWidth(room);
        int gridHeight = GetRoomGridHeight(room);
        UndertaleTexturePageItem? texture = tile.Tpag;
        double maxX = Math.Max(0, (texture?.BoundingWidth ?? 0) - gridWidth);
        double maxY = Math.Max(0, (texture?.BoundingHeight ?? 0) - gridHeight);
        double x = Math.Floor(Math.Max(0, point.X) / gridWidth) * gridWidth;
        double y = Math.Floor(Math.Max(0, point.Y) / gridHeight) * gridHeight;
        return new Point(Math.Clamp(x, 0, maxX), Math.Clamp(y, 0, maxY));
    }

    private static int GetRoomGridWidth(UndertaleRoom room)
    {
        int gridWidth = Math.Max(Convert.ToInt32(room.GridWidth), 1);
        if (IsVirtualKeyDown(VirtualKey.Control) && gridWidth > 1)
            gridWidth /= 2;
        else if (IsVirtualKeyDown(VirtualKey.Shift))
            gridWidth *= 2;
        return Math.Max(gridWidth, 1);
    }

    private static int GetRoomGridHeight(UndertaleRoom room)
    {
        int gridHeight = Math.Max(Convert.ToInt32(room.GridHeight), 1);
        if (IsVirtualKeyDown(VirtualKey.Control) && gridHeight > 1)
            gridHeight /= 2;
        else if (IsVirtualKeyDown(VirtualKey.Shift))
            gridHeight *= 2;
        return Math.Max(gridHeight, 1);
    }

    private static bool IsVirtualKeyDown(VirtualKey key)
    {
        return (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key) &
                Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;
    }

    private static bool IsMinusKey(VirtualKey key)
    {
        int value = (int)key;
        return key == VirtualKey.Subtract || value == 0xBD;
    }

    private static bool IsPlusKey(VirtualKey key)
    {
        int value = (int)key;
        return key == VirtualKey.Add || value == 0xBB;
    }

    private static bool IsRoomPasteKey(VirtualKey key)
    {
        return key == VirtualKey.V || key == VirtualKey.P;
    }

    private void RefreshRoomLayers(UndertaleRoom room, UndertaleRoom.Layer? selectedLayer)
    {
        _isUpdatingRoomLayerEditor = true;
        try
        {
            RoomLayerSummary[] summaries = BuildRoomLayerSummaries(room).ToArray();
            RoomLayersList.ItemsSource = summaries;
            RoomLayerSummary? selected = selectedLayer is null
                ? summaries.FirstOrDefault()
                : summaries.FirstOrDefault(summary => ReferenceEquals(summary.Layer, selectedLayer));
            RoomLayersList.SelectedItem = selected;
            UpdateRoomLayerButtons(selected);
            RefreshRoomLayerEditor(selected);
        }
        finally
        {
            _isUpdatingRoomLayerEditor = false;
        }
    }

    private void RefreshRoomLayerEditor(RoomLayerSummary? summary)
    {
        _isUpdatingRoomLayerEditor = true;
        if (summary is null)
        {
            HideRoomLayerEditor();
            UpdateRoomLayerButtons(null);
            _isUpdatingRoomLayerEditor = false;
            return;
        }

        UndertaleRoom.Layer layer = summary.Layer;
        RoomLayerEditorTitleText.Text = summary.Title;
        RoomLayerNameBox.Text = layer.LayerName?.Content ?? string.Empty;
        RoomLayerIdBox.Text = layer.LayerId.ToString(CultureInfo.InvariantCulture);
        RoomLayerTypeBox.Text = layer.LayerType.ToString();
        RoomLayerDepthBox.Text = layer.LayerDepth.ToString(CultureInfo.InvariantCulture);
        RoomLayerVisibleCheckBox.IsChecked = layer.IsVisible;
        RoomLayerXOffsetBox.Text = FormatFloat(layer.XOffset);
        RoomLayerYOffsetBox.Text = FormatFloat(layer.YOffset);
        RoomLayerHSpeedBox.Text = FormatFloat(layer.HSpeed);
        RoomLayerVSpeedBox.Text = FormatFloat(layer.VSpeed);
        RoomLayerOffsetSpeedPanel.Visibility = LayerTypeAllowsOffsetSpeed(layer.LayerType) ? Visibility.Visible : Visibility.Collapsed;
        RefreshRoomLayerBackgroundDataEditor(layer.BackgroundData);
        RefreshRoomLayerTilesDataEditor(layer.TilesData);
        RefreshRoomLayerAssetDataEditor(layer.AssetsData);
        UpdateRoomLayerButtons(summary);
        RoomLayerEditorPanel.Visibility = Visibility.Visible;
        _isUpdatingRoomLayerEditor = false;
    }

    private void UpdateRoomLayerButtons(RoomLayerSummary? selectedSummary)
    {
        bool canEdit = _data is not null &&
                       !_data.UnsupportedBytecodeVersion &&
                       _selectedResource?.Value is UndertaleRoom;
        RoomAddInstancesLayerButton.IsEnabled = canEdit;
        RoomAddTilesLayerButton.IsEnabled = canEdit;
        RoomAddBackgroundLayerButton.IsEnabled = canEdit;
        RoomAddAssetsLayerButton.IsEnabled = canEdit;
        RoomRemoveLayerButton.IsEnabled = canEdit && selectedSummary is not null;
    }

    private void HideRoomLayerEditor()
    {
        RoomLayerEditorTitleText.Text = "Selected layer";
        RoomLayerNameBox.Text = string.Empty;
        RoomLayerIdBox.Text = string.Empty;
        RoomLayerTypeBox.Text = string.Empty;
        RoomLayerDepthBox.Text = string.Empty;
        RoomLayerVisibleCheckBox.IsChecked = false;
        RoomLayerXOffsetBox.Text = string.Empty;
        RoomLayerYOffsetBox.Text = string.Empty;
        RoomLayerHSpeedBox.Text = string.Empty;
        RoomLayerVSpeedBox.Text = string.Empty;
        RoomLayerOffsetSpeedPanel.Visibility = Visibility.Collapsed;
        HideRoomLayerBackgroundDataEditor();
        HideRoomLayerTilesDataEditor();
        HideRoomLayerAssetDataEditor();
        RoomLayerEditorPanel.Visibility = Visibility.Collapsed;
    }

    private void RefreshRoomLayerBackgroundDataEditor(UndertaleRoom.Layer.LayerBackgroundData? data)
    {
        if (data is null)
        {
            HideRoomLayerBackgroundDataEditor();
            return;
        }

        RoomLayerBackgroundVisibleCheckBox.IsChecked = data.Visible;
        RoomLayerBackgroundForegroundCheckBox.IsChecked = data.Foreground;
        RoomLayerBackgroundTileXCheckBox.IsChecked = data.TiledHorizontally;
        RoomLayerBackgroundTileYCheckBox.IsChecked = data.TiledVertically;
        RoomLayerBackgroundStretchCheckBox.IsChecked = data.Stretch;
        SpriteReferenceItem[] spriteItems = _data is null ? [] : BuildSpriteReferenceItems(_data, includeNull: true).ToArray();
        RoomLayerBackgroundSpriteComboBox.ItemsSource = spriteItems;
        RoomLayerBackgroundSpriteComboBox.SelectedItem = spriteItems.FirstOrDefault(item => ReferenceEquals(item.Sprite, data.Sprite)) ??
                                                        spriteItems.FirstOrDefault();
        RoomLayerBackgroundColorBox.Text = FormatRoomTileColor(data.Color);
        RoomLayerBackgroundFirstFrameBox.Text = FormatFloat(data.FirstFrame);
        RoomLayerBackgroundAnimationSpeedBox.Text = FormatFloat(data.AnimationSpeed);
        RoomLayerBackgroundAnimationSpeedTypeComboBox.ItemsSource = Enum.GetValues<AnimationSpeedType>();
        RoomLayerBackgroundAnimationSpeedTypeComboBox.SelectedItem = data.AnimationSpeedType;
        RoomLayerBackgroundDataPanel.Visibility = Visibility.Visible;
    }

    private void HideRoomLayerBackgroundDataEditor()
    {
        RoomLayerBackgroundVisibleCheckBox.IsChecked = false;
        RoomLayerBackgroundForegroundCheckBox.IsChecked = false;
        RoomLayerBackgroundTileXCheckBox.IsChecked = false;
        RoomLayerBackgroundTileYCheckBox.IsChecked = false;
        RoomLayerBackgroundStretchCheckBox.IsChecked = false;
        RoomLayerBackgroundSpriteComboBox.ItemsSource = null;
        RoomLayerBackgroundSpriteComboBox.SelectedItem = null;
        RoomLayerBackgroundColorBox.Text = string.Empty;
        RoomLayerBackgroundFirstFrameBox.Text = string.Empty;
        RoomLayerBackgroundAnimationSpeedBox.Text = string.Empty;
        RoomLayerBackgroundAnimationSpeedTypeComboBox.ItemsSource = null;
        RoomLayerBackgroundAnimationSpeedTypeComboBox.SelectedItem = null;
        RoomLayerBackgroundDataPanel.Visibility = Visibility.Collapsed;
    }

    private void RefreshRoomLayerTilesDataEditor(UndertaleRoom.Layer.LayerTilesData? data)
    {
        if (data is null)
        {
            HideRoomLayerTilesDataEditor();
            return;
        }

        RoomLayerTilesXBox.Text = data.TilesX.ToString(CultureInfo.InvariantCulture);
        RoomLayerTilesYBox.Text = data.TilesY.ToString(CultureInfo.InvariantCulture);
        BackgroundReferenceItem[] backgroundItems = _data is null ? [] : BuildBackgroundReferenceItems(_data, includeNull: true).ToArray();
        RoomLayerTilesBackgroundComboBox.ItemsSource = backgroundItems;
        RoomLayerTilesBackgroundComboBox.SelectedItem = backgroundItems.FirstOrDefault(item => ReferenceEquals(item.Background, data.Background)) ??
                                                        backgroundItems.FirstOrDefault();
        RoomLayerTilesDataSummaryText.Text = BuildRoomLayerTilesDataSummary(data);
        RoomLayerTilesDataPanel.Visibility = Visibility.Visible;
    }

    private void HideRoomLayerTilesDataEditor()
    {
        RoomLayerTilesXBox.Text = string.Empty;
        RoomLayerTilesYBox.Text = string.Empty;
        RoomLayerTilesBackgroundComboBox.ItemsSource = null;
        RoomLayerTilesBackgroundComboBox.SelectedItem = null;
        RoomLayerTilesDataSummaryText.Text = string.Empty;
        RoomLayerTilesDataPanel.Visibility = Visibility.Collapsed;
    }

    private void RefreshRoomLayerAssetDataEditor(UndertaleRoom.Layer.LayerAssetsData? data)
    {
        if (data is null)
        {
            HideRoomLayerAssetDataEditor();
            return;
        }

        UndertaleRoom.SpriteInstance? selectedSpriteInstance =
            (RoomLayerAssetSpritesList.SelectedItem as RoomLayerAssetSpriteInstanceSummary)?.Instance;
        UndertaleRoom.SequenceInstance? selectedSequenceInstance =
            (RoomLayerAssetSequencesList.SelectedItem as RoomLayerAssetSequenceInstanceSummary)?.Instance;
        UndertaleRoom.ParticleSystemInstance? selectedParticleInstance =
            (RoomLayerAssetParticlesList.SelectedItem as RoomLayerAssetParticleInstanceSummary)?.Instance;

        RoomLayerAssetsSummaryText.Text = BuildRoomLayerAssetDataSummary(data);

        RoomLayerAssetSpriteInstanceSummary[] spriteSummaries = BuildRoomLayerAssetSpriteSummaries(data).ToArray();
        RoomLayerAssetSequenceInstanceSummary[] sequenceSummaries = BuildRoomLayerAssetSequenceSummaries(data).ToArray();
        RoomLayerAssetParticleInstanceSummary[] particleSummaries = BuildRoomLayerAssetParticleSummaries(data).ToArray();

        RoomLayerAssetSpritesList.ItemsSource = spriteSummaries;
        RoomLayerAssetSequencesList.ItemsSource = sequenceSummaries;
        RoomLayerAssetParticlesList.ItemsSource = particleSummaries;

        RoomLayerAssetSpriteInstanceSummary? selectedSprite = selectedSpriteInstance is null
            ? null
            : spriteSummaries.FirstOrDefault(summary => ReferenceEquals(summary.Instance, selectedSpriteInstance));
        RoomLayerAssetSequenceInstanceSummary? selectedSequence = selectedSequenceInstance is null
            ? null
            : sequenceSummaries.FirstOrDefault(summary => ReferenceEquals(summary.Instance, selectedSequenceInstance));
        RoomLayerAssetParticleInstanceSummary? selectedParticle = selectedParticleInstance is null
            ? null
            : particleSummaries.FirstOrDefault(summary => ReferenceEquals(summary.Instance, selectedParticleInstance));

        if (selectedSprite is null && selectedSequence is null && selectedParticle is null)
        {
            if (spriteSummaries.Length > 0)
                selectedSprite = spriteSummaries[0];
            else if (sequenceSummaries.Length > 0)
                selectedSequence = sequenceSummaries[0];
            else if (particleSummaries.Length > 0)
                selectedParticle = particleSummaries[0];
        }

        RoomLayerAssetSpritesList.SelectedItem = selectedSprite;
        RoomLayerAssetSequencesList.SelectedItem = selectedSequence;
        RoomLayerAssetParticlesList.SelectedItem = selectedParticle;
        RefreshRoomLayerAssetSpriteEditor(selectedSprite);
        RefreshRoomLayerAssetSequenceEditor(selectedSequence);
        RefreshRoomLayerAssetParticleEditor(selectedParticle);
        UpdateRoomLayerAssetOpenButtons();
        RoomLayerAssetsDataPanel.Visibility = Visibility.Visible;
    }

    private void HideRoomLayerAssetDataEditor()
    {
        RoomLayerAssetsSummaryText.Text = string.Empty;
        RoomLayerAssetSpritesList.ItemsSource = null;
        RoomLayerAssetSpritesList.SelectedItem = null;
        RoomLayerAssetSequencesList.ItemsSource = null;
        RoomLayerAssetSequencesList.SelectedItem = null;
        RoomLayerAssetParticlesList.ItemsSource = null;
        RoomLayerAssetParticlesList.SelectedItem = null;
        HideRoomLayerAssetSpriteEditor();
        HideRoomLayerAssetSequenceEditor();
        HideRoomLayerAssetParticleEditor();
        UpdateRoomLayerAssetOpenButtons();
        RoomLayerAssetsDataPanel.Visibility = Visibility.Collapsed;
    }

    private void RefreshRoomLayerAssetSpriteEditor(RoomLayerAssetSpriteInstanceSummary? summary)
    {
        if (summary is null)
        {
            HideRoomLayerAssetSpriteEditor();
            return;
        }

        UndertaleRoom.SpriteInstance instance = summary.Instance;
        RoomLayerAssetSpriteNameBox.Text = instance.Name?.Content ?? string.Empty;
        SpriteReferenceItem[] spriteItems = _data is null ? [] : BuildSpriteReferenceItems(_data).ToArray();
        RoomLayerAssetSpriteDefinitionComboBox.ItemsSource = spriteItems;
        RoomLayerAssetSpriteDefinitionComboBox.SelectedItem =
            spriteItems.FirstOrDefault(item => ReferenceEquals(item.Sprite, instance.Sprite));
        RoomLayerAssetSpriteXBox.Text = instance.X.ToString(CultureInfo.InvariantCulture);
        RoomLayerAssetSpriteYBox.Text = instance.Y.ToString(CultureInfo.InvariantCulture);
        RoomLayerAssetSpriteScaleXBox.Text = FormatFloat(instance.ScaleX);
        RoomLayerAssetSpriteScaleYBox.Text = FormatFloat(instance.ScaleY);
        RoomLayerAssetSpriteColorBox.Text = FormatRoomTileColor(instance.Color);
        RoomLayerAssetSpriteAnimationSpeedBox.Text = FormatFloat(instance.AnimationSpeed);
        RoomLayerAssetSpriteAnimationSpeedTypeComboBox.ItemsSource = Enum.GetValues<AnimationSpeedType>();
        RoomLayerAssetSpriteAnimationSpeedTypeComboBox.SelectedItem = instance.AnimationSpeedType;
        RoomLayerAssetSpriteFrameIndexBox.Text = FormatFloat(instance.FrameIndex);
        RoomLayerAssetSpriteRotationBox.Text = FormatFloat(instance.Rotation);
        RoomLayerAssetSpriteEditorPanel.Visibility = Visibility.Visible;
    }

    private void HideRoomLayerAssetSpriteEditor()
    {
        RoomLayerAssetSpriteNameBox.Text = string.Empty;
        RoomLayerAssetSpriteDefinitionComboBox.ItemsSource = null;
        RoomLayerAssetSpriteDefinitionComboBox.SelectedItem = null;
        RoomLayerAssetSpriteXBox.Text = string.Empty;
        RoomLayerAssetSpriteYBox.Text = string.Empty;
        RoomLayerAssetSpriteScaleXBox.Text = string.Empty;
        RoomLayerAssetSpriteScaleYBox.Text = string.Empty;
        RoomLayerAssetSpriteColorBox.Text = string.Empty;
        RoomLayerAssetSpriteAnimationSpeedBox.Text = string.Empty;
        RoomLayerAssetSpriteAnimationSpeedTypeComboBox.ItemsSource = null;
        RoomLayerAssetSpriteAnimationSpeedTypeComboBox.SelectedItem = null;
        RoomLayerAssetSpriteFrameIndexBox.Text = string.Empty;
        RoomLayerAssetSpriteRotationBox.Text = string.Empty;
        RoomLayerAssetSpriteEditorPanel.Visibility = Visibility.Collapsed;
    }

    private void RefreshRoomLayerAssetSequenceEditor(RoomLayerAssetSequenceInstanceSummary? summary)
    {
        if (summary is null)
        {
            HideRoomLayerAssetSequenceEditor();
            return;
        }

        UndertaleRoom.SequenceInstance instance = summary.Instance;
        RoomLayerAssetSequenceNameBox.Text = instance.Name?.Content ?? string.Empty;
        SequenceReferenceItem[] sequenceItems = _data is null ? [] : BuildSequenceReferenceItems(_data).ToArray();
        RoomLayerAssetSequenceDefinitionComboBox.ItemsSource = sequenceItems;
        RoomLayerAssetSequenceDefinitionComboBox.SelectedItem =
            sequenceItems.FirstOrDefault(item => ReferenceEquals(item.Sequence, instance.Sequence));
        RoomLayerAssetSequenceXBox.Text = instance.X.ToString(CultureInfo.InvariantCulture);
        RoomLayerAssetSequenceYBox.Text = instance.Y.ToString(CultureInfo.InvariantCulture);
        RoomLayerAssetSequenceScaleXBox.Text = FormatFloat(instance.ScaleX);
        RoomLayerAssetSequenceScaleYBox.Text = FormatFloat(instance.ScaleY);
        RoomLayerAssetSequenceColorBox.Text = FormatRoomTileColor(instance.Color);
        RoomLayerAssetSequenceAnimationSpeedBox.Text = FormatFloat(instance.AnimationSpeed);
        RoomLayerAssetSequenceAnimationSpeedTypeComboBox.ItemsSource = Enum.GetValues<AnimationSpeedType>();
        RoomLayerAssetSequenceAnimationSpeedTypeComboBox.SelectedItem = instance.AnimationSpeedType;
        RoomLayerAssetSequenceFrameIndexBox.Text = FormatFloat(instance.FrameIndex);
        RoomLayerAssetSequenceRotationBox.Text = FormatFloat(instance.Rotation);
        RoomLayerAssetSequenceEditorPanel.Visibility = Visibility.Visible;
    }

    private void HideRoomLayerAssetSequenceEditor()
    {
        RoomLayerAssetSequenceNameBox.Text = string.Empty;
        RoomLayerAssetSequenceDefinitionComboBox.ItemsSource = null;
        RoomLayerAssetSequenceDefinitionComboBox.SelectedItem = null;
        RoomLayerAssetSequenceXBox.Text = string.Empty;
        RoomLayerAssetSequenceYBox.Text = string.Empty;
        RoomLayerAssetSequenceScaleXBox.Text = string.Empty;
        RoomLayerAssetSequenceScaleYBox.Text = string.Empty;
        RoomLayerAssetSequenceColorBox.Text = string.Empty;
        RoomLayerAssetSequenceAnimationSpeedBox.Text = string.Empty;
        RoomLayerAssetSequenceAnimationSpeedTypeComboBox.ItemsSource = null;
        RoomLayerAssetSequenceAnimationSpeedTypeComboBox.SelectedItem = null;
        RoomLayerAssetSequenceFrameIndexBox.Text = string.Empty;
        RoomLayerAssetSequenceRotationBox.Text = string.Empty;
        RoomLayerAssetSequenceEditorPanel.Visibility = Visibility.Collapsed;
    }

    private void RefreshRoomLayerAssetParticleEditor(RoomLayerAssetParticleInstanceSummary? summary)
    {
        if (summary is null)
        {
            HideRoomLayerAssetParticleEditor();
            return;
        }

        UndertaleRoom.ParticleSystemInstance instance = summary.Instance;
        RoomLayerAssetParticleNameBox.Text = instance.Name?.Content ?? string.Empty;
        ParticleSystemReferenceItem[] particleItems = _data is null ? [] : BuildParticleSystemReferenceItems(_data).ToArray();
        RoomLayerAssetParticleDefinitionComboBox.ItemsSource = particleItems;
        RoomLayerAssetParticleDefinitionComboBox.SelectedItem =
            particleItems.FirstOrDefault(item => ReferenceEquals(item.ParticleSystem, instance.ParticleSystem));
        RoomLayerAssetParticleInstanceIdBox.Text = instance.InstanceID.ToString(CultureInfo.InvariantCulture);
        RoomLayerAssetParticleXBox.Text = instance.X.ToString(CultureInfo.InvariantCulture);
        RoomLayerAssetParticleYBox.Text = instance.Y.ToString(CultureInfo.InvariantCulture);
        RoomLayerAssetParticleScaleXBox.Text = FormatFloat(instance.ScaleX);
        RoomLayerAssetParticleScaleYBox.Text = FormatFloat(instance.ScaleY);
        RoomLayerAssetParticleRotationBox.Text = FormatFloat(instance.Rotation);
        RoomLayerAssetParticleColorBox.Text = FormatRoomTileColor(instance.Color);
        RoomLayerAssetParticleEditorPanel.Visibility = Visibility.Visible;
    }

    private void HideRoomLayerAssetParticleEditor()
    {
        RoomLayerAssetParticleNameBox.Text = string.Empty;
        RoomLayerAssetParticleDefinitionComboBox.ItemsSource = null;
        RoomLayerAssetParticleDefinitionComboBox.SelectedItem = null;
        RoomLayerAssetParticleInstanceIdBox.Text = string.Empty;
        RoomLayerAssetParticleXBox.Text = string.Empty;
        RoomLayerAssetParticleYBox.Text = string.Empty;
        RoomLayerAssetParticleScaleXBox.Text = string.Empty;
        RoomLayerAssetParticleScaleYBox.Text = string.Empty;
        RoomLayerAssetParticleRotationBox.Text = string.Empty;
        RoomLayerAssetParticleColorBox.Text = string.Empty;
        RoomLayerAssetParticleEditorPanel.Visibility = Visibility.Collapsed;
    }

    private void RoomLayerAssetSpritesList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RoomLayerAssetSpriteInstanceSummary summary)
        {
            RoomLayerAssetSpritesList.SelectedItem = summary;
            SelectRoomLayerAssetSprite(summary);
        }
    }

    private void RoomLayerAssetSpritesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor)
            return;

        SelectRoomLayerAssetSprite(RoomLayerAssetSpritesList.SelectedItem as RoomLayerAssetSpriteInstanceSummary);
    }

    private async void RoomLayerAssetSpritesList_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        await HandleRoomLayerAssetListKeyDownAsync(RoomLayerAssetSpritesList, e);
    }

    private void RoomLayerAssetSpritesList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        RoomLayerAssetOpenSpriteButton_Click(sender, e);
        e.Handled = true;
    }

    private void RoomLayerAssetSpritesList_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement element ||
            !IsMiddlePointerPressed(e, element) ||
            FindDataContextFromOriginalSource<RoomLayerAssetSpriteInstanceSummary>(e.OriginalSource) is not { } summary)
        {
            return;
        }

        RoomLayerAssetSpritesList.SelectedItem = summary;
        SelectRoomLayerAssetSprite(summary);
        OpenRoomLayerAssetSprite(summary, addTab: true);
        e.Handled = true;
    }

    private void SelectRoomLayerAssetSprite(RoomLayerAssetSpriteInstanceSummary? summary)
    {
        ClearOtherRoomLayerAssetSelections(RoomLayerAssetSpritesList);
        RefreshRoomLayerAssetSpriteEditor(summary);
        HideRoomLayerAssetSequenceEditor();
        HideRoomLayerAssetParticleEditor();
        UpdateRoomLayerAssetOpenButtons();
    }

    private void RoomLayerAssetSequencesList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RoomLayerAssetSequenceInstanceSummary summary)
        {
            RoomLayerAssetSequencesList.SelectedItem = summary;
            SelectRoomLayerAssetSequence(summary);
        }
    }

    private void RoomLayerAssetSequencesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor)
            return;

        SelectRoomLayerAssetSequence(RoomLayerAssetSequencesList.SelectedItem as RoomLayerAssetSequenceInstanceSummary);
    }

    private async void RoomLayerAssetSequencesList_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        await HandleRoomLayerAssetListKeyDownAsync(RoomLayerAssetSequencesList, e);
    }

    private void RoomLayerAssetSequencesList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        RoomLayerAssetOpenSequenceButton_Click(sender, e);
        e.Handled = true;
    }

    private void RoomLayerAssetSequencesList_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement element ||
            !IsMiddlePointerPressed(e, element) ||
            FindDataContextFromOriginalSource<RoomLayerAssetSequenceInstanceSummary>(e.OriginalSource) is not { } summary)
        {
            return;
        }

        RoomLayerAssetSequencesList.SelectedItem = summary;
        SelectRoomLayerAssetSequence(summary);
        OpenRoomLayerAssetSequence(summary, addTab: true);
        e.Handled = true;
    }

    private void SelectRoomLayerAssetSequence(RoomLayerAssetSequenceInstanceSummary? summary)
    {
        ClearOtherRoomLayerAssetSelections(RoomLayerAssetSequencesList);
        HideRoomLayerAssetSpriteEditor();
        RefreshRoomLayerAssetSequenceEditor(summary);
        HideRoomLayerAssetParticleEditor();
        UpdateRoomLayerAssetOpenButtons();
    }

    private void RoomLayerAssetParticlesList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RoomLayerAssetParticleInstanceSummary summary)
        {
            RoomLayerAssetParticlesList.SelectedItem = summary;
            SelectRoomLayerAssetParticle(summary);
        }
    }

    private void RoomLayerAssetParticlesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor)
            return;

        SelectRoomLayerAssetParticle(RoomLayerAssetParticlesList.SelectedItem as RoomLayerAssetParticleInstanceSummary);
    }

    private async void RoomLayerAssetParticlesList_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        await HandleRoomLayerAssetListKeyDownAsync(RoomLayerAssetParticlesList, e);
    }

    private void RoomLayerAssetParticlesList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        RoomLayerAssetOpenParticleButton_Click(sender, e);
        e.Handled = true;
    }

    private void RoomLayerAssetParticlesList_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement element ||
            !IsMiddlePointerPressed(e, element) ||
            FindDataContextFromOriginalSource<RoomLayerAssetParticleInstanceSummary>(e.OriginalSource) is not { } summary)
        {
            return;
        }

        RoomLayerAssetParticlesList.SelectedItem = summary;
        SelectRoomLayerAssetParticle(summary);
        OpenRoomLayerAssetParticle(summary, addTab: true);
        e.Handled = true;
    }

    private void SelectRoomLayerAssetParticle(RoomLayerAssetParticleInstanceSummary? summary)
    {
        ClearOtherRoomLayerAssetSelections(RoomLayerAssetParticlesList);
        HideRoomLayerAssetSpriteEditor();
        HideRoomLayerAssetSequenceEditor();
        RefreshRoomLayerAssetParticleEditor(summary);
        UpdateRoomLayerAssetOpenButtons();
    }

    private async System.Threading.Tasks.Task HandleRoomLayerAssetListKeyDownAsync(ListView list, KeyRoutedEventArgs e)
    {
        bool control = IsVirtualKeyDown(VirtualKey.Control);
        if (control && e.Key == VirtualKey.Z)
        {
            await RestoreLastRoomPreviewUndoAsync();
            e.Handled = true;
        }
        else if (control && e.Key == VirtualKey.C)
        {
            object? instance = GetRoomLayerAssetInstanceFromSelection(list.SelectedItem);
            if (instance is not null)
            {
                _copiedRoomItem = instance;
                StatusBox.Text = "Copied asset instance.";
                e.Handled = true;
            }
        }
        else if (control && IsRoomPasteKey(e.Key))
        {
            await PasteCopiedRoomLayerAssetAsync();
            e.Handled = true;
        }
        else if (control && e.Key == VirtualKey.Up)
        {
            await MoveSelectedRoomLayerAssetAsync(-1);
            e.Handled = true;
        }
        else if (control && e.Key == VirtualKey.Down)
        {
            await MoveSelectedRoomLayerAssetAsync(1);
            e.Handled = true;
        }
        else if (!control && IsMinusKey(e.Key))
        {
            await MoveSelectedRoomLayerAssetAsync(-1);
            e.Handled = true;
        }
        else if (!control && IsPlusKey(e.Key))
        {
            await MoveSelectedRoomLayerAssetAsync(1);
            e.Handled = true;
        }
        else if (!control && e.Key == VirtualKey.Enter)
        {
            OpenSelectedRoomLayerAsset();
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Delete)
        {
            await RemoveSelectedRoomLayerAssetAsync();
            e.Handled = true;
        }
    }

    private static object? GetRoomLayerAssetInstanceFromSelection(object? selectedItem)
    {
        return selectedItem switch
        {
            RoomLayerAssetSpriteInstanceSummary summary => summary.Instance,
            RoomLayerAssetSequenceInstanceSummary summary => summary.Instance,
            RoomLayerAssetParticleInstanceSummary summary => summary.Instance,
            _ => null
        };
    }

    private void OpenSelectedRoomLayerAsset()
    {
        if (RoomLayerAssetSpritesList.SelectedItem is RoomLayerAssetSpriteInstanceSummary)
            RoomLayerAssetOpenSpriteButton_Click(RoomLayerAssetSpritesList, new RoutedEventArgs());
        else if (RoomLayerAssetSequencesList.SelectedItem is RoomLayerAssetSequenceInstanceSummary)
            RoomLayerAssetOpenSequenceButton_Click(RoomLayerAssetSequencesList, new RoutedEventArgs());
        else if (RoomLayerAssetParticlesList.SelectedItem is RoomLayerAssetParticleInstanceSummary)
            RoomLayerAssetOpenParticleButton_Click(RoomLayerAssetParticlesList, new RoutedEventArgs());
    }

    private async System.Threading.Tasks.Task PasteCopiedRoomLayerAssetAsync()
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            layerSummary.Layer.AssetsData is not { } assetsData)
        {
            return;
        }

        EnsureRoomLayerAssetLists(layerSummary.Layer, _data);
        switch (_copiedRoomItem)
        {
            case UndertaleRoom.SpriteInstance sprite:
            {
                assetsData.Sprites ??= new UndertalePointerList<UndertaleRoom.SpriteInstance>();
                UndertaleRoom.SpriteInstance duplicate = CloneRoomLayerSpriteInstance(sprite, _data, offset: 16);
                UndertaleRoom.SpriteInstance? after = (RoomLayerAssetSpritesList.SelectedItem as RoomLayerAssetSpriteInstanceSummary)?.Instance ?? sprite;
                InsertAfter(assetsData.Sprites, duplicate, after);
                await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Pasted asset sprite instance.");
                SelectRoomLayerAssetChild(duplicate);
                return;
            }
            case UndertaleRoom.SequenceInstance sequence when _data.IsVersionAtLeast(2, 3):
            {
                assetsData.Sequences ??= new UndertalePointerList<UndertaleRoom.SequenceInstance>();
                UndertaleRoom.SequenceInstance duplicate = CloneRoomLayerSequenceInstance(sequence, _data, offset: 16);
                UndertaleRoom.SequenceInstance? after = (RoomLayerAssetSequencesList.SelectedItem as RoomLayerAssetSequenceInstanceSummary)?.Instance ?? sequence;
                InsertAfter(assetsData.Sequences, duplicate, after);
                await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Pasted asset sequence instance.");
                SelectRoomLayerAssetChild(duplicate);
                return;
            }
            case UndertaleRoom.ParticleSystemInstance particle when _data.IsNonLTSVersionAtLeast(2023, 2):
            {
                assetsData.ParticleSystems ??= new UndertalePointerList<UndertaleRoom.ParticleSystemInstance>();
                UndertaleRoom.ParticleSystemInstance duplicate = CloneRoomLayerParticleInstance(particle, _data, offset: 16);
                UndertaleRoom.ParticleSystemInstance? after = (RoomLayerAssetParticlesList.SelectedItem as RoomLayerAssetParticleInstanceSummary)?.Instance ?? particle;
                InsertAfter(assetsData.ParticleSystems, duplicate, after);
                await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Pasted asset particle instance.");
                SelectRoomLayerAssetChild(duplicate);
                return;
            }
            default:
                StatusBox.Text = "No compatible asset instance copied.";
                return;
        }
    }

    private async System.Threading.Tasks.Task RemoveSelectedRoomLayerAssetAsync()
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            layerSummary.Layer.AssetsData is not { } assetsData)
        {
            return;
        }

        switch (GetRoomLayerAssetInstanceFromSelection(RoomLayerAssetSpritesList.SelectedItem ?? RoomLayerAssetSequencesList.SelectedItem ?? RoomLayerAssetParticlesList.SelectedItem))
        {
            case UndertaleRoom.SpriteInstance sprite when assetsData.Sprites is not null:
                await RemoveRoomLayerAssetInstanceAsync(room, layerSummary.Layer, assetsData.Sprites, sprite, "Removed asset sprite instance.");
                return;
            case UndertaleRoom.SequenceInstance sequence when assetsData.Sequences is not null:
                await RemoveRoomLayerAssetInstanceAsync(room, layerSummary.Layer, assetsData.Sequences, sequence, "Removed asset sequence instance.");
                return;
            case UndertaleRoom.ParticleSystemInstance particle when assetsData.ParticleSystems is not null:
                await RemoveRoomLayerAssetInstanceAsync(room, layerSummary.Layer, assetsData.ParticleSystems, particle, "Removed asset particle instance.");
                return;
        }
    }

    private async System.Threading.Tasks.Task RemoveRoomLayerAssetInstanceAsync<T>(
        UndertaleRoom room,
        UndertaleRoom.Layer layer,
        IList<T> list,
        T instance,
        string status)
    {
        int index = list.IndexOf(instance);
        if (index < 0)
            return;

        list.RemoveAt(index);
        T? nextSelection = list.Count == 0 ? default : list[Math.Clamp(index, 0, list.Count - 1)];
        await RefreshRoomLayerAfterEditAsync(room, layer, status);
        SelectRoomLayerAssetChild(nextSelection);
    }

    private async System.Threading.Tasks.Task MoveSelectedRoomLayerAssetAsync(int delta)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            layerSummary.Layer.AssetsData is not { } assetsData)
        {
            return;
        }

        object? instance = GetRoomLayerAssetInstanceFromSelection(RoomLayerAssetSpritesList.SelectedItem ?? RoomLayerAssetSequencesList.SelectedItem ?? RoomLayerAssetParticlesList.SelectedItem);
        bool moved = instance switch
        {
            UndertaleRoom.SpriteInstance sprite when assetsData.Sprites is not null => MoveListItem(assetsData.Sprites, sprite, delta),
            UndertaleRoom.SequenceInstance sequence when assetsData.Sequences is not null => MoveListItem(assetsData.Sequences, sequence, delta),
            UndertaleRoom.ParticleSystemInstance particle when assetsData.ParticleSystems is not null => MoveListItem(assetsData.ParticleSystems, particle, delta),
            _ => false
        };
        if (!moved)
            return;

        await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, delta < 0 ? "Moved asset instance up." : "Moved asset instance down.");
        SelectRoomLayerAssetChild(instance);
    }

    private static void InsertAfter<T>(IList<T> list, T item, T? after)
    {
        int index = after is null ? -1 : list.IndexOf(after);
        if (index < 0)
            list.Add(item);
        else
            list.Insert(Math.Min(index + 1, list.Count), item);
    }

    private void ClearOtherRoomLayerAssetSelections(ListView activeList)
    {
        bool wasUpdating = _isUpdatingRoomLayerEditor;
        _isUpdatingRoomLayerEditor = true;
        try
        {
            if (!ReferenceEquals(activeList, RoomLayerAssetSpritesList))
                RoomLayerAssetSpritesList.SelectedItem = null;
            if (!ReferenceEquals(activeList, RoomLayerAssetSequencesList))
                RoomLayerAssetSequencesList.SelectedItem = null;
            if (!ReferenceEquals(activeList, RoomLayerAssetParticlesList))
                RoomLayerAssetParticlesList.SelectedItem = null;
        }
        finally
        {
            _isUpdatingRoomLayerEditor = wasUpdating;
        }
    }

    private void UpdateRoomLayerAssetOpenButtons()
    {
        bool canEdit = _data is not null &&
                       !_data.UnsupportedBytecodeVersion &&
                       _selectedResource?.Value is UndertaleRoom &&
                       RoomLayersList.SelectedItem is RoomLayerSummary { Layer.AssetsData: not null };
        bool supportsSequences = _data?.IsVersionAtLeast(2, 3) == true;
        bool supportsParticles = _data?.IsNonLTSVersionAtLeast(2023, 2) == true;

        RoomLayerAssetOpenSpriteButton.IsEnabled =
            (RoomLayerAssetSpritesList.SelectedItem as RoomLayerAssetSpriteInstanceSummary)?.Instance.Sprite is not null;
        RoomLayerAssetOpenSequenceButton.IsEnabled =
            (RoomLayerAssetSequencesList.SelectedItem as RoomLayerAssetSequenceInstanceSummary)?.Instance.Sequence is not null;
        RoomLayerAssetOpenParticleButton.IsEnabled =
            (RoomLayerAssetParticlesList.SelectedItem as RoomLayerAssetParticleInstanceSummary)?.Instance.ParticleSystem is not null;
        RoomLayerAssetAddSpriteButton.IsEnabled = canEdit;
        RoomLayerAssetDuplicateSpriteButton.IsEnabled = canEdit && RoomLayerAssetSpritesList.SelectedItem is RoomLayerAssetSpriteInstanceSummary;
        RoomLayerAssetRemoveSpriteButton.IsEnabled = canEdit && RoomLayerAssetSpritesList.SelectedItem is RoomLayerAssetSpriteInstanceSummary;
        RoomLayerAssetAddSequenceButton.IsEnabled = canEdit && supportsSequences;
        RoomLayerAssetDuplicateSequenceButton.IsEnabled = canEdit && supportsSequences && RoomLayerAssetSequencesList.SelectedItem is RoomLayerAssetSequenceInstanceSummary;
        RoomLayerAssetRemoveSequenceButton.IsEnabled = canEdit && supportsSequences && RoomLayerAssetSequencesList.SelectedItem is RoomLayerAssetSequenceInstanceSummary;
        RoomLayerAssetAddParticleButton.IsEnabled = canEdit && supportsParticles;
        RoomLayerAssetDuplicateParticleButton.IsEnabled = canEdit && supportsParticles && RoomLayerAssetParticlesList.SelectedItem is RoomLayerAssetParticleInstanceSummary;
        RoomLayerAssetRemoveParticleButton.IsEnabled = canEdit && supportsParticles && RoomLayerAssetParticlesList.SelectedItem is RoomLayerAssetParticleInstanceSummary;
    }

    private void RoomLayerAssetOpenSpriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            RoomLayerAssetSpritesList.SelectedItem is not RoomLayerAssetSpriteInstanceSummary summary)
        {
            return;
        }

        OpenRoomLayerAssetSprite(summary);
    }

    private void RoomLayerAssetOpenSequenceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            RoomLayerAssetSequencesList.SelectedItem is not RoomLayerAssetSequenceInstanceSummary summary)
        {
            return;
        }

        OpenRoomLayerAssetSequence(summary);
    }

    private void RoomLayerAssetOpenParticleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            RoomLayerAssetParticlesList.SelectedItem is not RoomLayerAssetParticleInstanceSummary summary)
        {
            return;
        }

        OpenRoomLayerAssetParticle(summary);
    }

    private void OpenRoomLayerAssetSprite(RoomLayerAssetSpriteInstanceSummary summary, bool addTab = true)
    {
        if (_data is null ||
            summary.Instance.Sprite is not { } sprite)
        {
            return;
        }

        NavigateToResource("Sprites", _data.Sprites.IndexOf(sprite), addTab);
    }

    private void OpenRoomLayerAssetSequence(RoomLayerAssetSequenceInstanceSummary summary, bool addTab = true)
    {
        if (_data is null ||
            summary.Instance.Sequence is not { } sequence)
        {
            return;
        }

        NavigateToResource("Sequences", _data.Sequences.IndexOf(sequence), addTab);
    }

    private void OpenRoomLayerAssetParticle(RoomLayerAssetParticleInstanceSummary summary, bool addTab = true)
    {
        if (_data is null ||
            summary.Instance.ParticleSystem is not { } particleSystem)
        {
            return;
        }

        NavigateToResource("Particle systems", _data.ParticleSystems.IndexOf(particleSystem), addTab);
    }

    private async void RoomLayerAssetAddSpriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            layerSummary.Layer.AssetsData is not { } assetsData)
        {
            return;
        }

        EnsureRoomLayerAssetLists(layerSummary.Layer, _data);
        assetsData.Sprites ??= new UndertalePointerList<UndertaleRoom.SpriteInstance>();
        UndertaleRoom.SpriteInstance instance = new()
        {
            Name = UndertaleRoom.SpriteInstance.GenerateRandomName(_data)
        };
        assetsData.Sprites.Add(instance);
        await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Added asset sprite instance.");
        SelectRoomLayerAssetChild(instance);
    }

    private async void RoomLayerAssetDuplicateSpriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            layerSummary.Layer.AssetsData?.Sprites is not { } sprites ||
            RoomLayerAssetSpritesList.SelectedItem is not RoomLayerAssetSpriteInstanceSummary summary)
        {
            return;
        }

        UndertaleRoom.SpriteInstance duplicate = CloneRoomLayerSpriteInstance(summary.Instance, _data, offset: 16);
        int index = sprites.IndexOf(summary.Instance);
        sprites.Insert(index >= 0 ? index + 1 : sprites.Count, duplicate);
        await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Duplicated asset sprite instance.");
        SelectRoomLayerAssetChild(duplicate);
    }

    private async void RoomLayerAssetRemoveSpriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            layerSummary.Layer.AssetsData?.Sprites is not { } sprites ||
            RoomLayerAssetSpritesList.SelectedItem is not RoomLayerAssetSpriteInstanceSummary summary)
        {
            return;
        }

        int index = sprites.IndexOf(summary.Instance);
        if (index < 0)
            return;

        sprites.RemoveAt(index);
        UndertaleRoom.SpriteInstance? nextSelection = sprites.Count == 0 ? null : sprites[Math.Clamp(index, 0, sprites.Count - 1)];
        await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Removed asset sprite instance.");
        SelectRoomLayerAssetChild(nextSelection);
    }

    private async void RoomLayerAssetAddSequenceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            !_data.IsVersionAtLeast(2, 3) ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            layerSummary.Layer.AssetsData is not { } assetsData)
        {
            return;
        }

        EnsureRoomLayerAssetLists(layerSummary.Layer, _data);
        assetsData.Sequences ??= new UndertalePointerList<UndertaleRoom.SequenceInstance>();
        UndertaleRoom.SequenceInstance instance = new()
        {
            Name = GenerateRoomAssetName(_data, "sequence_"),
            ScaleX = 1,
            ScaleY = 1,
            Color = 0xFFFFFFFF,
            AnimationSpeed = 1
        };
        assetsData.Sequences.Add(instance);
        await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Added asset sequence instance.");
        SelectRoomLayerAssetChild(instance);
    }

    private async void RoomLayerAssetDuplicateSequenceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            layerSummary.Layer.AssetsData?.Sequences is not { } sequences ||
            RoomLayerAssetSequencesList.SelectedItem is not RoomLayerAssetSequenceInstanceSummary summary)
        {
            return;
        }

        UndertaleRoom.SequenceInstance duplicate = CloneRoomLayerSequenceInstance(summary.Instance, _data, offset: 16);
        int index = sequences.IndexOf(summary.Instance);
        sequences.Insert(index >= 0 ? index + 1 : sequences.Count, duplicate);
        await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Duplicated asset sequence instance.");
        SelectRoomLayerAssetChild(duplicate);
    }

    private async void RoomLayerAssetRemoveSequenceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            layerSummary.Layer.AssetsData?.Sequences is not { } sequences ||
            RoomLayerAssetSequencesList.SelectedItem is not RoomLayerAssetSequenceInstanceSummary summary)
        {
            return;
        }

        int index = sequences.IndexOf(summary.Instance);
        if (index < 0)
            return;

        sequences.RemoveAt(index);
        UndertaleRoom.SequenceInstance? nextSelection = sequences.Count == 0 ? null : sequences[Math.Clamp(index, 0, sequences.Count - 1)];
        await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Removed asset sequence instance.");
        SelectRoomLayerAssetChild(nextSelection);
    }

    private async void RoomLayerAssetAddParticleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            !_data.IsNonLTSVersionAtLeast(2023, 2) ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            layerSummary.Layer.AssetsData is not { } assetsData)
        {
            return;
        }

        EnsureRoomLayerAssetLists(layerSummary.Layer, _data);
        assetsData.ParticleSystems ??= new UndertalePointerList<UndertaleRoom.ParticleSystemInstance>();
        UndertaleRoom.ParticleSystemInstance instance = new()
        {
            Name = UndertaleRoom.ParticleSystemInstance.GenerateRandomName(_data),
            InstanceID = ++_data.LastParticleSystemInstanceID,
            ScaleX = 1,
            ScaleY = 1,
            Color = 0xFFFFFFFF
        };
        assetsData.ParticleSystems.Add(instance);
        await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Added asset particle instance.");
        SelectRoomLayerAssetChild(instance);
    }

    private async void RoomLayerAssetDuplicateParticleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            layerSummary.Layer.AssetsData?.ParticleSystems is not { } particleSystems ||
            RoomLayerAssetParticlesList.SelectedItem is not RoomLayerAssetParticleInstanceSummary summary)
        {
            return;
        }

        UndertaleRoom.ParticleSystemInstance duplicate = CloneRoomLayerParticleInstance(summary.Instance, _data, offset: 16);
        int index = particleSystems.IndexOf(summary.Instance);
        particleSystems.Insert(index >= 0 ? index + 1 : particleSystems.Count, duplicate);
        await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Duplicated asset particle instance.");
        SelectRoomLayerAssetChild(duplicate);
    }

    private async void RoomLayerAssetRemoveParticleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            layerSummary.Layer.AssetsData?.ParticleSystems is not { } particleSystems ||
            RoomLayerAssetParticlesList.SelectedItem is not RoomLayerAssetParticleInstanceSummary summary)
        {
            return;
        }

        int index = particleSystems.IndexOf(summary.Instance);
        if (index < 0)
            return;

        particleSystems.RemoveAt(index);
        UndertaleRoom.ParticleSystemInstance? nextSelection = particleSystems.Count == 0 ? null : particleSystems[Math.Clamp(index, 0, particleSystems.Count - 1)];
        await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Removed asset particle instance.");
        SelectRoomLayerAssetChild(nextSelection);
    }

    private void SelectRoomLayerAssetChild(object? child)
    {
        switch (child)
        {
            case UndertaleRoom.SpriteInstance spriteInstance:
            {
                RoomLayerAssetSpriteInstanceSummary? summary =
                    (RoomLayerAssetSpritesList.ItemsSource as IEnumerable)?
                    .OfType<RoomLayerAssetSpriteInstanceSummary>()
                    .FirstOrDefault(item => ReferenceEquals(item.Instance, spriteInstance));
                if (summary is not null)
                {
                    RoomLayerAssetSpritesList.SelectedItem = summary;
                    SelectRoomLayerAssetSprite(summary);
                }
                break;
            }
            case UndertaleRoom.SequenceInstance sequenceInstance:
            {
                RoomLayerAssetSequenceInstanceSummary? summary =
                    (RoomLayerAssetSequencesList.ItemsSource as IEnumerable)?
                    .OfType<RoomLayerAssetSequenceInstanceSummary>()
                    .FirstOrDefault(item => ReferenceEquals(item.Instance, sequenceInstance));
                if (summary is not null)
                {
                    RoomLayerAssetSequencesList.SelectedItem = summary;
                    SelectRoomLayerAssetSequence(summary);
                }
                break;
            }
            case UndertaleRoom.ParticleSystemInstance particleInstance:
            {
                RoomLayerAssetParticleInstanceSummary? summary =
                    (RoomLayerAssetParticlesList.ItemsSource as IEnumerable)?
                    .OfType<RoomLayerAssetParticleInstanceSummary>()
                    .FirstOrDefault(item => ReferenceEquals(item.Instance, particleInstance));
                if (summary is not null)
                {
                    RoomLayerAssetParticlesList.SelectedItem = summary;
                    SelectRoomLayerAssetParticle(summary);
                }
                break;
            }
        }
    }

    private static void EnsureRoomLayerAssetLists(UndertaleRoom.Layer layer, UndertaleData data)
    {
        if (layer.AssetsData is not { } assetsData)
            return;

        assetsData.LegacyTiles ??= new UndertalePointerList<UndertaleRoom.Tile>();
        assetsData.Sprites ??= new UndertalePointerList<UndertaleRoom.SpriteInstance>();
        if (data.IsVersionAtLeast(2, 3))
            assetsData.Sequences ??= new UndertalePointerList<UndertaleRoom.SequenceInstance>();
        if (!data.IsVersionAtLeast(2, 3, 2))
            assetsData.NineSlices ??= new UndertalePointerList<UndertaleRoom.SpriteInstance>();
        if (data.IsNonLTSVersionAtLeast(2023, 2))
            assetsData.ParticleSystems ??= new UndertalePointerList<UndertaleRoom.ParticleSystemInstance>();
        if (data.IsVersionAtLeast(2024, 6))
            assetsData.TextItems ??= new UndertalePointerList<UndertaleRoom.TextItemInstance>();
    }

    private static UndertaleString GenerateRoomAssetName(UndertaleData data, string prefix)
    {
        string suffix = ((uint)Random.Shared.Next(-int.MaxValue, int.MaxValue)).ToString("X8", CultureInfo.InvariantCulture);
        return data.Strings.MakeString(prefix + suffix);
    }

    private static UndertaleRoom.SpriteInstance CloneRoomLayerSpriteInstance(
        UndertaleRoom.SpriteInstance source,
        UndertaleData data,
        int offset)
    {
        return new UndertaleRoom.SpriteInstance
        {
            Name = UndertaleRoom.SpriteInstance.GenerateRandomName(data),
            Sprite = source.Sprite,
            X = source.X + offset,
            Y = source.Y + offset,
            ScaleX = source.ScaleX,
            ScaleY = source.ScaleY,
            Color = source.Color,
            AnimationSpeed = source.AnimationSpeed,
            AnimationSpeedType = source.AnimationSpeedType,
            FrameIndex = source.FrameIndex,
            Rotation = source.Rotation
        };
    }

    private static UndertaleRoom.SequenceInstance CloneRoomLayerSequenceInstance(
        UndertaleRoom.SequenceInstance source,
        UndertaleData data,
        int offset)
    {
        return new UndertaleRoom.SequenceInstance
        {
            Name = GenerateRoomAssetName(data, "sequence_"),
            Sequence = source.Sequence,
            X = source.X + offset,
            Y = source.Y + offset,
            ScaleX = source.ScaleX,
            ScaleY = source.ScaleY,
            Color = source.Color,
            AnimationSpeed = source.AnimationSpeed,
            AnimationSpeedType = source.AnimationSpeedType,
            FrameIndex = source.FrameIndex,
            Rotation = source.Rotation
        };
    }

    private static UndertaleRoom.ParticleSystemInstance CloneRoomLayerParticleInstance(
        UndertaleRoom.ParticleSystemInstance source,
        UndertaleData data,
        int offset)
    {
        return new UndertaleRoom.ParticleSystemInstance
        {
            Name = UndertaleRoom.ParticleSystemInstance.GenerateRandomName(data),
            InstanceID = ++data.LastParticleSystemInstanceID,
            ParticleSystem = source.ParticleSystem,
            X = source.X + offset,
            Y = source.Y + offset,
            ScaleX = source.ScaleX,
            ScaleY = source.ScaleY,
            Color = source.Color,
            Rotation = source.Rotation
        };
    }

    private async void RoomLayerAssetSpriteBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            RoomLayerAssetSpritesList.SelectedItem is not RoomLayerAssetSpriteInstanceSummary summary)
        {
            return;
        }

        try
        {
            UndertaleRoom.SpriteInstance instance = summary.Instance;
            bool changed = UpdateUndertaleString(instance.Name, RoomLayerAssetSpriteNameBox.Text, _data, out UndertaleString name);
            int x = ParseRoomInstanceInt(RoomLayerAssetSpriteXBox.Text, "Sprite instance X");
            int y = ParseRoomInstanceInt(RoomLayerAssetSpriteYBox.Text, "Sprite instance Y");
            float scaleX = ParseRoomInstanceFloat(RoomLayerAssetSpriteScaleXBox.Text, "Sprite instance scale X");
            float scaleY = ParseRoomInstanceFloat(RoomLayerAssetSpriteScaleYBox.Text, "Sprite instance scale Y");
            uint color = ParseRoomTileColor(RoomLayerAssetSpriteColorBox.Text);
            float animationSpeed = ParseRoomInstanceFloat(RoomLayerAssetSpriteAnimationSpeedBox.Text, "Sprite instance animation speed");
            float frameIndex = ParseRoomInstanceFloat(RoomLayerAssetSpriteFrameIndexBox.Text, "Sprite instance frame index");
            float rotation = ParseRoomInstanceFloat(RoomLayerAssetSpriteRotationBox.Text, "Sprite instance rotation");

            changed |= !ReferenceEquals(instance.Name, name) ||
                       instance.X != x ||
                       instance.Y != y ||
                       !NearlyEqual(instance.ScaleX, scaleX) ||
                       !NearlyEqual(instance.ScaleY, scaleY) ||
                       instance.Color != color ||
                       !NearlyEqual(instance.AnimationSpeed, animationSpeed) ||
                       !NearlyEqual(instance.FrameIndex, frameIndex) ||
                       !NearlyEqual(instance.Rotation, rotation);

            if (!changed)
                return;

            instance.Name = name;
            instance.X = x;
            instance.Y = y;
            instance.ScaleX = scaleX;
            instance.ScaleY = scaleY;
            instance.Color = color;
            instance.AnimationSpeed = animationSpeed;
            instance.FrameIndex = frameIndex;
            instance.Rotation = rotation;
            await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Updated asset sprite instance.");
        }
        catch (Exception ex)
        {
            RefreshRoomLayerEditor(RoomLayersList.SelectedItem as RoomLayerSummary);
            StatusBox.Text = ex.Message;
        }
    }

    private async void RoomLayerAssetSpriteDefinitionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            RoomLayerAssetSpritesList.SelectedItem is not RoomLayerAssetSpriteInstanceSummary summary ||
            RoomLayerAssetSpriteDefinitionComboBox.SelectedItem is not SpriteReferenceItem item)
        {
            return;
        }

        if (ReferenceEquals(summary.Instance.Sprite, item.Sprite))
            return;

        summary.Instance.Sprite = item.Sprite;
        await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Updated asset sprite definition.");
    }

    private async void RoomLayerAssetSpriteAnimationSpeedTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            RoomLayerAssetSpritesList.SelectedItem is not RoomLayerAssetSpriteInstanceSummary summary ||
            RoomLayerAssetSpriteAnimationSpeedTypeComboBox.SelectedItem is not AnimationSpeedType speedType)
        {
            return;
        }

        if (summary.Instance.AnimationSpeedType == speedType)
            return;

        summary.Instance.AnimationSpeedType = speedType;
        await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Updated asset sprite animation speed type.");
    }

    private async void RoomLayerAssetSequenceBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            RoomLayerAssetSequencesList.SelectedItem is not RoomLayerAssetSequenceInstanceSummary summary)
        {
            return;
        }

        try
        {
            UndertaleRoom.SequenceInstance instance = summary.Instance;
            bool changed = UpdateUndertaleString(instance.Name, RoomLayerAssetSequenceNameBox.Text, _data, out UndertaleString name);
            int x = ParseRoomInstanceInt(RoomLayerAssetSequenceXBox.Text, "Sequence instance X");
            int y = ParseRoomInstanceInt(RoomLayerAssetSequenceYBox.Text, "Sequence instance Y");
            float scaleX = ParseRoomInstanceFloat(RoomLayerAssetSequenceScaleXBox.Text, "Sequence instance scale X");
            float scaleY = ParseRoomInstanceFloat(RoomLayerAssetSequenceScaleYBox.Text, "Sequence instance scale Y");
            uint color = ParseRoomTileColor(RoomLayerAssetSequenceColorBox.Text);
            float animationSpeed = ParseRoomInstanceFloat(RoomLayerAssetSequenceAnimationSpeedBox.Text, "Sequence instance animation speed");
            float frameIndex = ParseRoomInstanceFloat(RoomLayerAssetSequenceFrameIndexBox.Text, "Sequence instance frame index");
            float rotation = ParseRoomInstanceFloat(RoomLayerAssetSequenceRotationBox.Text, "Sequence instance rotation");

            changed |= !ReferenceEquals(instance.Name, name) ||
                       instance.X != x ||
                       instance.Y != y ||
                       !NearlyEqual(instance.ScaleX, scaleX) ||
                       !NearlyEqual(instance.ScaleY, scaleY) ||
                       instance.Color != color ||
                       !NearlyEqual(instance.AnimationSpeed, animationSpeed) ||
                       !NearlyEqual(instance.FrameIndex, frameIndex) ||
                       !NearlyEqual(instance.Rotation, rotation);

            if (!changed)
                return;

            instance.Name = name;
            instance.X = x;
            instance.Y = y;
            instance.ScaleX = scaleX;
            instance.ScaleY = scaleY;
            instance.Color = color;
            instance.AnimationSpeed = animationSpeed;
            instance.FrameIndex = frameIndex;
            instance.Rotation = rotation;
            await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Updated asset sequence instance.");
        }
        catch (Exception ex)
        {
            RefreshRoomLayerEditor(RoomLayersList.SelectedItem as RoomLayerSummary);
            StatusBox.Text = ex.Message;
        }
    }

    private async void RoomLayerAssetSequenceDefinitionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            RoomLayerAssetSequencesList.SelectedItem is not RoomLayerAssetSequenceInstanceSummary summary ||
            RoomLayerAssetSequenceDefinitionComboBox.SelectedItem is not SequenceReferenceItem item)
        {
            return;
        }

        if (ReferenceEquals(summary.Instance.Sequence, item.Sequence))
            return;

        summary.Instance.Sequence = item.Sequence;
        await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Updated asset sequence definition.");
    }

    private async void RoomLayerAssetSequenceAnimationSpeedTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            RoomLayerAssetSequencesList.SelectedItem is not RoomLayerAssetSequenceInstanceSummary summary ||
            RoomLayerAssetSequenceAnimationSpeedTypeComboBox.SelectedItem is not AnimationSpeedType speedType)
        {
            return;
        }

        if (summary.Instance.AnimationSpeedType == speedType)
            return;

        summary.Instance.AnimationSpeedType = speedType;
        await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Updated asset sequence animation speed type.");
    }

    private async void RoomLayerAssetParticleBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            RoomLayerAssetParticlesList.SelectedItem is not RoomLayerAssetParticleInstanceSummary summary)
        {
            return;
        }

        try
        {
            UndertaleRoom.ParticleSystemInstance instance = summary.Instance;
            bool changed = UpdateUndertaleString(instance.Name, RoomLayerAssetParticleNameBox.Text, _data, out UndertaleString name);
            int instanceId = ParseRoomInstanceInt(RoomLayerAssetParticleInstanceIdBox.Text, "Particle instance ID");
            int x = ParseRoomInstanceInt(RoomLayerAssetParticleXBox.Text, "Particle instance X");
            int y = ParseRoomInstanceInt(RoomLayerAssetParticleYBox.Text, "Particle instance Y");
            float scaleX = ParseRoomInstanceFloat(RoomLayerAssetParticleScaleXBox.Text, "Particle instance scale X");
            float scaleY = ParseRoomInstanceFloat(RoomLayerAssetParticleScaleYBox.Text, "Particle instance scale Y");
            float rotation = ParseRoomInstanceFloat(RoomLayerAssetParticleRotationBox.Text, "Particle instance rotation");
            uint color = ParseRoomTileColor(RoomLayerAssetParticleColorBox.Text);

            changed |= !ReferenceEquals(instance.Name, name) ||
                       instance.InstanceID != instanceId ||
                       instance.X != x ||
                       instance.Y != y ||
                       !NearlyEqual(instance.ScaleX, scaleX) ||
                       !NearlyEqual(instance.ScaleY, scaleY) ||
                       !NearlyEqual(instance.Rotation, rotation) ||
                       instance.Color != color;

            if (!changed)
                return;

            instance.Name = name;
            instance.InstanceID = instanceId;
            instance.X = x;
            instance.Y = y;
            instance.ScaleX = scaleX;
            instance.ScaleY = scaleY;
            instance.Rotation = rotation;
            instance.Color = color;
            await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Updated asset particle instance.");
        }
        catch (Exception ex)
        {
            RefreshRoomLayerEditor(RoomLayersList.SelectedItem as RoomLayerSummary);
            StatusBox.Text = ex.Message;
        }
    }

    private async void RoomLayerAssetParticleDefinitionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary layerSummary ||
            RoomLayerAssetParticlesList.SelectedItem is not RoomLayerAssetParticleInstanceSummary summary ||
            RoomLayerAssetParticleDefinitionComboBox.SelectedItem is not ParticleSystemReferenceItem item)
        {
            return;
        }

        if (ReferenceEquals(summary.Instance.ParticleSystem, item.ParticleSystem))
            return;

        summary.Instance.ParticleSystem = item.ParticleSystem;
        await RefreshRoomLayerAfterEditAsync(room, layerSummary.Layer, "Updated asset particle definition.");
    }

    private void RoomLayersList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RoomLayerSummary summary)
        {
            RoomLayersList.SelectedItem = summary;
            RefreshRoomLayerEditor(summary);
        }
    }

    private void RoomLayersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor)
            return;

        RefreshRoomLayerEditor(RoomLayersList.SelectedItem as RoomLayerSummary);
    }

    private void RoomLayersList_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Delete)
            return;

        RoomRemoveLayerButton_Click(sender, e);
        e.Handled = true;
    }

    private async void RoomAddInstancesLayerButton_Click(object sender, RoutedEventArgs e)
    {
        await AddRoomLayerAsync<UndertaleRoom.Layer.LayerInstancesData>(UndertaleRoom.LayerType.Instances, "NewInstancesLayer");
    }

    private async void RoomAddTilesLayerButton_Click(object sender, RoutedEventArgs e)
    {
        await AddRoomLayerAsync<UndertaleRoom.Layer.LayerTilesData>(UndertaleRoom.LayerType.Tiles, "NewTilesLayer");
    }

    private async void RoomAddBackgroundLayerButton_Click(object sender, RoutedEventArgs e)
    {
        await AddRoomLayerAsync<UndertaleRoom.Layer.LayerBackgroundData>(UndertaleRoom.LayerType.Background, "NewBackgroundLayer");
    }

    private async void RoomAddAssetsLayerButton_Click(object sender, RoutedEventArgs e)
    {
        await AddRoomLayerAsync<UndertaleRoom.Layer.LayerAssetsData>(UndertaleRoom.LayerType.Assets, "NewAssetsLayer");
    }

    private async void RoomRemoveLayerButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary summary)
        {
            return;
        }

        UndertaleRoom.Layer layer = summary.Layer;
        int index = room.Layers.IndexOf(layer);
        if (index < 0)
            return;

        if (layer.InstancesData?.Instances is { } instances)
        {
            foreach (UndertaleRoom.GameObject instance in instances.ToArray())
            {
                room.GameObjects.Remove(instance);
                room.InstanceCreationOrderIDs?.InstanceIDs.Remove(instance.InstanceID);
            }
        }

        room.Layers.RemoveAt(index);
        room.UpdateBGColorLayer();
        UpdateRoomLayerZIndexes(room);

        UndertaleRoom.Layer? nextLayer = room.Layers.Count == 0
            ? null
            : room.Layers[Math.Clamp(index, 0, room.Layers.Count - 1)];
        MarkDirty();
        RefreshRoomLayers(room, nextLayer);
        RefreshRoomInstancesAfterLayerMutation(room);
        RefreshRoomTilesPanelAfterLayerMutation(room);
        RoomOverviewText.Text = BuildRoomOverview(room);
        RoomInstanceSummary[] instanceSummaries = BuildRoomInstanceSummaries(room).ToArray();
        if (_isRoomPreviewRendered)
            await RefreshRoomPreviewAsync(room, instanceSummaries);
        else
            ClearRoomPreviewSurface(room, instanceSummaries);
        RefreshCurrentDetails();
        StatusBox.Text = "Removed room layer.";
    }

    private async System.Threading.Tasks.Task AddRoomLayerAsync<TLayerData>(
        UndertaleRoom.LayerType layerType,
        string baseName)
        where TLayerData : UndertaleRoom.Layer.LayerData, new()
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room)
        {
            return;
        }

        UndertaleRoom.Layer layer = new()
        {
            LayerName = _data.Strings.MakeString(MakeUniqueRoomLayerName(room, baseName), createNew: true),
            LayerId = GetNextRoomLayerId(_data),
            LayerType = layerType,
            LayerDepth = GetNextRoomLayerDepth(room),
            Data = new TLayerData(),
            ParentRoom = room
        };

        room.Layers.Add(layer);
        if (layer.LayerType == UndertaleRoom.LayerType.Assets)
            EnsureRoomLayerAssetLists(layer, _data);
        else if (layer.LayerType == UndertaleRoom.LayerType.Tiles)
            layer.TilesData.TileData ??= Array.Empty<uint[]>();

        room.UpdateBGColorLayer();
        UpdateRoomLayerZIndexes(room);
        await RefreshRoomLayerAfterEditAsync(room, layer, $"Added {layer.LayerType.ToString().ToLowerInvariant()} layer.");
    }

    private static uint GetNextRoomLayerId(UndertaleData data)
    {
        uint largestLayerId = 0;
        foreach (UndertaleRoom room in data.Rooms ?? [])
        {
            if (room?.Layers is null)
                continue;

            foreach (UndertaleRoom.Layer layer in room.Layers)
                largestLayerId = Math.Max(largestLayerId, layer.LayerId);
        }

        return largestLayerId + 1;
    }

    private static int GetNextRoomLayerDepth(UndertaleRoom room)
    {
        if (room.Layers.Count == 0)
            return 0;

        int maxDepth = room.Layers.Max(layer => layer.LayerDepth);
        if (maxDepth > int.MaxValue - 100)
            return maxDepth == int.MaxValue ? maxDepth - 1 : maxDepth + 1;

        return maxDepth + 100;
    }

    private static string MakeUniqueRoomLayerName(UndertaleRoom room, string requestedName)
    {
        string name = requestedName;
        string? baseName = null;
        int suffix = 0;
        while (room.Layers.Any(layer => string.Equals(layer.LayerName?.Content, name, StringComparison.Ordinal)))
        {
            if (baseName is null)
            {
                Match match = Regex.Match(name, @"\d+$");
                if (match.Success)
                {
                    baseName = name[..^match.Length];
                    suffix = int.Parse(match.Value, CultureInfo.InvariantCulture) + 1;
                }
                else
                {
                    baseName = name;
                    suffix = 1;
                }
            }
            else
            {
                suffix++;
            }

            name = baseName + suffix.ToString(CultureInfo.InvariantCulture);
        }

        return name;
    }

    private static void UpdateRoomLayerZIndexes(UndertaleRoom room)
    {
        foreach (UndertaleRoom.Layer layer in room.Layers ?? [])
            layer.UpdateZIndex();
    }

    private void RefreshRoomInstancesAfterLayerMutation(UndertaleRoom room)
    {
        RoomInstanceSummary[] summaries = BuildRoomInstanceSummaries(room).ToArray();
        RoomInstancesList.ItemsSource = summaries;
        RoomInstanceSummary? selected = summaries.FirstOrDefault();
        RoomInstancesList.SelectedItem = selected;
        UpdateRoomInstanceButtons(selected);
        RefreshRoomInstanceEditor(selected);
    }

    private void RefreshRoomTilesPanelAfterLayerMutation(UndertaleRoom room)
    {
        RoomTileSummary[] tileSummaries = BuildRoomTileSummaries(room).ToArray();
        bool showRoomTiles = UsesLegacyRoomBackgroundSlots(room) || tileSummaries.Length > 0;
        RoomTilesPanel.Visibility = showRoomTiles ? Visibility.Visible : Visibility.Collapsed;
        if (showRoomTiles)
            RefreshRoomTiles(room, selectedTile: tileSummaries.FirstOrDefault()?.Tile);
        else
            HideRoomTileEditor();
    }

    private async void RoomLayerBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary summary)
        {
            return;
        }

        try
        {
            UndertaleRoom.Layer layer = summary.Layer;
            string name = RoomLayerNameBox.Text;
            uint layerId = ParseRoomTileUInt(RoomLayerIdBox.Text, "Layer ID");
            int depth = ParseRoomInstanceInt(RoomLayerDepthBox.Text, "Layer depth");
            float xOffset = ParseRoomInstanceFloat(RoomLayerXOffsetBox.Text, "Layer offset X");
            float yOffset = ParseRoomInstanceFloat(RoomLayerYOffsetBox.Text, "Layer offset Y");
            float hSpeed = ParseRoomInstanceFloat(RoomLayerHSpeedBox.Text, "Layer speed H");
            float vSpeed = ParseRoomInstanceFloat(RoomLayerVSpeedBox.Text, "Layer speed V");

            if ((layer.LayerName?.Content ?? string.Empty) == name &&
                layer.LayerId == layerId &&
                layer.LayerDepth == depth &&
                NearlyEqual(layer.XOffset, xOffset) &&
                NearlyEqual(layer.YOffset, yOffset) &&
                NearlyEqual(layer.HSpeed, hSpeed) &&
                NearlyEqual(layer.VSpeed, vSpeed))
            {
                return;
            }

            if (layer.LayerName is null)
                layer.LayerName = _data.Strings.MakeString(name, createNew: true);
            else
                layer.LayerName.Content = name;
            layer.LayerId = layerId;
            layer.LayerDepth = depth;
            layer.XOffset = xOffset;
            layer.YOffset = yOffset;
            layer.HSpeed = hSpeed;
            layer.VSpeed = vSpeed;
            await RefreshRoomLayerAfterEditAsync(room, layer, "Updated room layer.");
        }
        catch (Exception ex)
        {
            RefreshRoomLayerEditor(summary);
            StatusBox.Text = ex.Message;
        }
    }

    private async void RoomLayerVisibleCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary summary)
        {
            return;
        }

        bool isVisible = RoomLayerVisibleCheckBox.IsChecked == true;
        if (summary.Layer.IsVisible == isVisible)
            return;

        summary.Layer.IsVisible = isVisible;
        await RefreshRoomLayerAfterEditAsync(room, summary.Layer, "Updated room layer visibility.");
    }

    private async void RoomLayerBackgroundCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary summary ||
            summary.Layer.BackgroundData is not { } data)
        {
            return;
        }

        bool visible = RoomLayerBackgroundVisibleCheckBox.IsChecked == true;
        bool foreground = RoomLayerBackgroundForegroundCheckBox.IsChecked == true;
        bool tileX = RoomLayerBackgroundTileXCheckBox.IsChecked == true;
        bool tileY = RoomLayerBackgroundTileYCheckBox.IsChecked == true;
        bool stretch = RoomLayerBackgroundStretchCheckBox.IsChecked == true;
        if (data.Visible == visible &&
            data.Foreground == foreground &&
            data.TiledHorizontally == tileX &&
            data.TiledVertically == tileY &&
            data.Stretch == stretch)
        {
            return;
        }

        data.Visible = visible;
        data.Foreground = foreground;
        data.TiledHorizontally = tileX;
        data.TiledVertically = tileY;
        data.Stretch = stretch;
        data.UpdateScale();
        await RefreshRoomLayerAfterEditAsync(room, summary.Layer, "Updated background layer flags.");
    }

    private async void RoomLayerBackgroundSpriteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary summary ||
            summary.Layer.BackgroundData is not { } data ||
            RoomLayerBackgroundSpriteComboBox.SelectedItem is not SpriteReferenceItem item)
        {
            return;
        }

        if (ReferenceEquals(data.Sprite, item.Sprite))
            return;

        data.Sprite = item.Sprite;
        data.UpdateScale();
        await RefreshRoomLayerAfterEditAsync(room, summary.Layer, "Updated background layer sprite.");
    }

    private async void RoomLayerBackgroundBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary summary ||
            summary.Layer.BackgroundData is not { } data)
        {
            return;
        }

        try
        {
            uint color = ParseRoomTileColor(RoomLayerBackgroundColorBox.Text);
            float firstFrame = ParseRoomInstanceFloat(RoomLayerBackgroundFirstFrameBox.Text, "Background layer first frame");
            float animationSpeed = ParseRoomInstanceFloat(RoomLayerBackgroundAnimationSpeedBox.Text, "Background layer animation speed");
            if (data.Color == color &&
                NearlyEqual(data.FirstFrame, firstFrame) &&
                NearlyEqual(data.AnimationSpeed, animationSpeed))
            {
                return;
            }

            data.Color = color;
            data.FirstFrame = firstFrame;
            data.AnimationSpeed = animationSpeed;
            await RefreshRoomLayerAfterEditAsync(room, summary.Layer, "Updated background layer animation.");
        }
        catch (Exception ex)
        {
            RefreshRoomLayerEditor(summary);
            StatusBox.Text = ex.Message;
        }
    }

    private async void RoomLayerBackgroundAnimationSpeedTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary summary ||
            summary.Layer.BackgroundData is not { } data ||
            RoomLayerBackgroundAnimationSpeedTypeComboBox.SelectedItem is not AnimationSpeedType speedType)
        {
            return;
        }

        if (data.AnimationSpeedType == speedType)
            return;

        data.AnimationSpeedType = speedType;
        await RefreshRoomLayerAfterEditAsync(room, summary.Layer, "Updated background layer animation speed type.");
    }

    private async void RoomLayerTilesBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary summary ||
            summary.Layer.TilesData is not { } data)
        {
            return;
        }

        try
        {
            uint tilesX = ParseRoomTileUInt(RoomLayerTilesXBox.Text, "Tile layer width");
            uint tilesY = ParseRoomTileUInt(RoomLayerTilesYBox.Text, "Tile layer height");
            if (data.TilesX == tilesX && data.TilesY == tilesY)
                return;

            ResizeRoomLayerTileData(data, tilesX, tilesY);
            await RefreshRoomLayerAfterEditAsync(room, summary.Layer, "Updated tile layer size.");
        }
        catch (Exception ex)
        {
            RefreshRoomLayerEditor(summary);
            StatusBox.Text = ex.Message;
        }
    }

    private async void RoomLayerTilesBackgroundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomLayerEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary summary ||
            summary.Layer.TilesData is not { } data ||
            RoomLayerTilesBackgroundComboBox.SelectedItem is not BackgroundReferenceItem item)
        {
            return;
        }

        if (ReferenceEquals(data.Background, item.Background))
            return;

        data.Background = item.Background;
        await RefreshRoomLayerAfterEditAsync(room, summary.Layer, "Updated tile layer tile set.");
    }

    private async void RoomLayerTilesEditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary summary ||
            summary.Layer.TilesData is not { } data)
        {
            return;
        }

        if (data.TilesX == 0 || data.TilesY == 0)
        {
            StatusBox.Text = "Tile data size cannot be zero.";
            return;
        }

        uint[][] workingData;
        try
        {
            workingData = CloneRoomLayerTileData(data);
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Could not open tile editor: {ex.Message}";
            return;
        }

        const int previewCellLimit = 1600;
        int tilesX = (int)data.TilesX;
        int tilesY = (int)data.TilesY;
        uint[][] originalData = CloneRoomTileArray(workingData, tilesX, tilesY);
        bool changed = false;
        int selectedX = 0;
        int selectedY = 0;
        const int maxUndoSnapshots = 32;
        List<uint[][]> undoSnapshots = [];
        List<uint[][]> redoSnapshots = [];
        ObservableCollection<RoomTileCellEditorItem> visibleCells = [];
        ObservableCollection<RoomTilePaletteEditorItem> paletteItems = [];
        uint[][] brushData = new[] { new[] { workingData[0][0] } };
        int brushWidth = 1;
        int brushHeight = 1;
        int tileVisualPreviewGeneration = 0;
        int palettePreviewGeneration = 0;
        int paletteAutoPreviewCount = 0;
        bool isTileDragPainting = false;
        bool isTileDragPicking = false;
        bool tileDragPickMoved = false;
        bool tileDragPaintUndoPushed = false;
        bool suppressNextTileClick = false;
        DateTimeOffset suppressNextTileClickUntil = DateTimeOffset.MinValue;
        int suppressNextTileClickX = 0;
        int suppressNextTileClickY = 0;
        int tileDragStartX = 0;
        int tileDragStartY = 0;
        HashSet<long> tileDragPaintedCells = [];

        TextBox xBox = new()
        {
            Text = "0",
            MinWidth = 100
        };
        TextBox yBox = new()
        {
            Text = "0",
            MinWidth = 100
        };
        TextBox valueBox = new()
        {
            Text = FormatRoomTileRawValue(workingData[0][0]),
            MinWidth = 180
        };
        TextBox brushWidthBox = new()
        {
            Text = "1",
            MinWidth = 90
        };
        TextBox brushHeightBox = new()
        {
            Text = "1",
            MinWidth = 90
        };
        Brush? secondaryTextBrush = Application.Current.Resources.TryGetValue("TextFillColorSecondaryBrush", out object secondaryTextResource)
            ? secondaryTextResource as Brush
            : null;
        TextBlock summaryText = new()
        {
            Foreground = secondaryTextBrush,
            TextWrapping = TextWrapping.Wrap
        };
        TextBlock editorStatusText = new()
        {
            Foreground = secondaryTextBrush,
            TextWrapping = TextWrapping.Wrap
        };

        GridView cellsView = new()
        {
            IsItemClickEnabled = true,
            ItemTemplate = CreateRoomTileCellEditorTemplate(),
            ItemsSource = visibleCells,
            MaxHeight = 360,
            SelectionMode = ListViewSelectionMode.Single
        };
        ToolTipService.SetToolTip(
            cellsView,
            "Click selects. Shift+click picks a rectangle. Alt+click picks one tile. Ctrl+click fills. Ctrl+Shift+click replaces all matching. Paint clicks stamps the brush.");
        GridView paletteView = new()
        {
            IsItemClickEnabled = true,
            ItemTemplate = CreateRoomTilePaletteEditorTemplate(),
            ItemsSource = paletteItems,
            MaxHeight = 132,
            SelectionMode = ListViewSelectionMode.Single
        };
        CheckBox paintClicksCheckBox = new()
        {
            Content = "Paint clicks"
        };
        TextBlock paletteInfoText = new()
        {
            Foreground = secondaryTextBrush,
            TextWrapping = TextWrapping.Wrap
        };
        TextBlock brushPreviewText = new()
        {
            Foreground = secondaryTextBrush,
            TextWrapping = TextWrapping.Wrap,
            Text = "Brush preview"
        };
        TextBlock selectedCellPreviewText = new()
        {
            Foreground = secondaryTextBrush,
            TextWrapping = TextWrapping.Wrap,
            Text = "Selected cell preview"
        };
        Image brushPreviewImage = new()
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 112,
            MaxHeight = 112
        };
        Image selectedCellPreviewImage = new()
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 112,
            MaxHeight = 112
        };

        StackPanel content = new()
        {
            Spacing = 12,
            Width = 860
        };

        Grid fieldGrid = new()
        {
            ColumnSpacing = 8,
            RowSpacing = 8
        };
        fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        StackPanel xField = BuildLabeledDialogField("X", xBox);
        fieldGrid.Children.Add(xField);
        StackPanel yField = BuildLabeledDialogField("Y", yBox);
        Grid.SetColumn(yField, 1);
        fieldGrid.Children.Add(yField);
        StackPanel valueField = BuildLabeledDialogField("Raw tile value", valueBox);
        Grid.SetColumn(valueField, 2);
        fieldGrid.Children.Add(valueField);

        Grid commandGrid = new()
        {
            ColumnSpacing = 8
        };
        for (int i = 0; i < 8; i++)
            commandGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Button loadButton = new() { Content = "Load cell" };
        Button setButton = new() { Content = "Set cell" };
        Button clearButton = new() { Content = "Clear cell" };
        Button fillButton = new() { Content = "Fill area" };
        Button replaceButton = new() { Content = "Replace all matching" };
        Button undoButton = new() { Content = "Undo", IsEnabled = false };
        Button redoButton = new() { Content = "Redo", IsEnabled = false };
        Button mirrorButton = new() { Content = "Mirror X" };
        Button flipButton = new() { Content = "Flip Y" };
        Button rotateClockwiseButton = new() { Content = "Rotate CW" };
        Button rotateCounterClockwiseButton = new() { Content = "Rotate CCW" };
        Button pickBrushButton = new() { Content = "Pick brush" };
        Button stampBrushButton = new() { Content = "Stamp brush" };
        Button resetBrushButton = new() { Content = "Reset brush" };
        Button eraseBrushButton = new() { Content = "Erase brush" };
        ToolTipService.SetToolTip(mirrorButton, "Mirror the brush around the X axis (X).");
        ToolTipService.SetToolTip(flipButton, "Flip the brush around the Y axis (Y).");
        ToolTipService.SetToolTip(rotateClockwiseButton, "Rotate the brush clockwise (R).");
        ToolTipService.SetToolTip(rotateCounterClockwiseButton, "Rotate the brush counterclockwise (Q).");
        ToolTipService.SetToolTip(pickBrushButton, "Pick a brush rectangle from the current tile grid (P).");
        ToolTipService.SetToolTip(stampBrushButton, "Stamp the picked brush at the current X/Y cell (Enter or Space).");
        ToolTipService.SetToolTip(resetBrushButton, "Reset the brush to the current raw tile value.");
        ToolTipService.SetToolTip(eraseBrushButton, "Set the brush to an empty tile (E).");
        ToolTipService.SetToolTip(undoButton, "Undo tile edits in this dialog (Ctrl+Z).");
        ToolTipService.SetToolTip(redoButton, "Redo tile edits in this dialog (Ctrl+Y).");
        commandGrid.Children.Add(loadButton);
        Grid.SetColumn(setButton, 1);
        commandGrid.Children.Add(setButton);
        Grid.SetColumn(clearButton, 2);
        commandGrid.Children.Add(clearButton);
        Grid.SetColumn(fillButton, 3);
        commandGrid.Children.Add(fillButton);
        Grid.SetColumn(replaceButton, 4);
        commandGrid.Children.Add(replaceButton);
        Grid.SetColumn(undoButton, 5);
        commandGrid.Children.Add(undoButton);
        Grid.SetColumn(redoButton, 6);
        commandGrid.Children.Add(redoButton);
        Grid.SetColumn(paintClicksCheckBox, 7);
        commandGrid.Children.Add(paintClicksCheckBox);

        Grid transformGrid = new()
        {
            ColumnSpacing = 8
        };
        for (int i = 0; i < 4; i++)
            transformGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        transformGrid.Children.Add(mirrorButton);
        Grid.SetColumn(flipButton, 1);
        transformGrid.Children.Add(flipButton);
        Grid.SetColumn(rotateClockwiseButton, 2);
        transformGrid.Children.Add(rotateClockwiseButton);
        Grid.SetColumn(rotateCounterClockwiseButton, 3);
        transformGrid.Children.Add(rotateCounterClockwiseButton);

        Grid brushGrid = new()
        {
            ColumnSpacing = 8
        };
        for (int i = 0; i < 6; i++)
            brushGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        StackPanel brushWidthField = BuildLabeledDialogField("Brush W", brushWidthBox);
        brushGrid.Children.Add(brushWidthField);
        StackPanel brushHeightField = BuildLabeledDialogField("Brush H", brushHeightBox);
        Grid.SetColumn(brushHeightField, 1);
        brushGrid.Children.Add(brushHeightField);
        Grid.SetColumn(pickBrushButton, 2);
        brushGrid.Children.Add(pickBrushButton);
        Grid.SetColumn(stampBrushButton, 3);
        brushGrid.Children.Add(stampBrushButton);
        Grid.SetColumn(resetBrushButton, 4);
        brushGrid.Children.Add(resetBrushButton);
        Grid.SetColumn(eraseBrushButton, 5);
        brushGrid.Children.Add(eraseBrushButton);

        StackPanel palettePanel = new()
        {
            Spacing = 6,
            Children =
            {
                paletteInfoText,
                paletteView
            }
        };

        Grid visualPreviewGrid = new()
        {
            ColumnSpacing = 12
        };
        visualPreviewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        visualPreviewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Border brushPreviewHost = BuildTileVisualPreviewHost(brushPreviewImage);
        Border selectedCellPreviewHost = BuildTileVisualPreviewHost(selectedCellPreviewImage);
        StackPanel brushPreviewPanel = new()
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Brush preview", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                brushPreviewText,
                brushPreviewHost
            }
        };
        StackPanel selectedCellPreviewPanel = new()
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Selected cell", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                selectedCellPreviewText,
                selectedCellPreviewHost
            }
        };
        Grid.SetColumn(selectedCellPreviewPanel, 1);
        visualPreviewGrid.Children.Add(brushPreviewPanel);
        visualPreviewGrid.Children.Add(selectedCellPreviewPanel);

        content.Children.Add(summaryText);
        content.Children.Add(fieldGrid);
        content.Children.Add(commandGrid);
        content.Children.Add(transformGrid);
        content.Children.Add(brushGrid);
        content.Children.Add(editorStatusText);
        content.Children.Add(visualPreviewGrid);
        content.Children.Add(palettePanel);
        content.Children.Add(cellsView);

        uint[][] CloneWorkingData()
        {
            return CloneRoomTileArray(workingData, tilesX, tilesY);
        }

        void UpdateUndoButtons()
        {
            undoButton.IsEnabled = undoSnapshots.Count > 0;
            redoButton.IsEnabled = redoSnapshots.Count > 0;
        }

        void PushUndoSnapshot()
        {
            if (undoSnapshots.Count >= maxUndoSnapshots)
                undoSnapshots.RemoveAt(0);

            undoSnapshots.Add(CloneWorkingData());
            redoSnapshots.Clear();
            UpdateUndoButtons();
        }

        void RestoreWorkingData(uint[][] snapshot, string status)
        {
            workingData = CloneRoomTileArray(snapshot, tilesX, tilesY);
            changed = true;
            RefreshVisibleCells();
            SelectCell(Math.Clamp(selectedX, 0, tilesX - 1), Math.Clamp(selectedY, 0, tilesY - 1), status);
            UpdateUndoButtons();
        }

        void RefreshPaletteItems()
        {
            palettePreviewGeneration++;
            paletteAutoPreviewCount = 0;
            paletteItems.Clear();
            UndertaleBackground? background = data.Background;
            if (background?.GMS2TileIds is null || background.GMS2TileIds.Count == 0)
            {
                paletteInfoText.Text = "No tile palette is available for the selected tile set.";
                return;
            }

            const int paletteLimit = 768;
            int step = Math.Max(1, (int)background.GMS2ItemsPerTileCount);
            int paletteCount = (background.GMS2TileIds.Count + step - 1) / step;
            int added = 0;
            for (int index = 0; index < background.GMS2TileIds.Count && added < paletteLimit; index += step)
            {
                uint value = background.GMS2TileIds[index].ID;
                paletteItems.Add(new RoomTilePaletteEditorItem(added, value));
                added++;
            }

            paletteInfoText.Text = paletteCount > paletteLimit
                ? $"Palette shows first {paletteLimit} tile(s). Thumbnails load lazily; type a raw value for tiles beyond this preview."
                : $"Palette contains {added} tile(s). Thumbnails load lazily; select one to use it as the brush value.";
        }

        void RefreshVisibleCells()
        {
            visibleCells.Clear();
            int added = 0;
            for (int y = 0; y < tilesY && added < previewCellLimit; y++)
            {
                for (int x = 0; x < tilesX && added < previewCellLimit; x++)
                {
                    visibleCells.Add(new RoomTileCellEditorItem(x, y, workingData[y][x]));
                    added++;
                }
            }

            long totalCells = (long)tilesX * tilesY;
            int visibleTiles = CountVisibleRoomTiles(workingData);
            summaryText.Text = totalCells > previewCellLimit
                ? $"{tilesX}x{tilesY} cells, {visibleTiles} non-empty tile(s). Showing first {previewCellLimit} cells; coordinates still edit the full layer."
                : $"{tilesX}x{tilesY} cells, {visibleTiles} non-empty tile(s).";
        }

        void RefreshTileVisualPreviews()
        {
            int generation = ++tileVisualPreviewGeneration;
            string brushLabel = brushWidth == 1 && brushHeight == 1 ? "Brush" : $"Brush {brushWidth}x{brushHeight}";
            _ = UpdateTileVisualPreviewAsync(brushData[0][0], brushPreviewImage, brushPreviewText, brushLabel, generation);
            _ = UpdateTileVisualPreviewAsync(workingData[selectedY][selectedX], selectedCellPreviewImage, selectedCellPreviewText, "Selected cell", generation);
        }

        async System.Threading.Tasks.Task EnsurePalettePreviewAsync(RoomTilePaletteEditorItem item, bool force)
        {
            const int paletteAutoPreviewLimit = 96;
            if (!force && paletteAutoPreviewCount >= paletteAutoPreviewLimit)
            {
                item.SetPreviewDeferred();
                return;
            }

            if (!item.TryBeginPreviewLoad())
                return;

            if (!force)
                paletteAutoPreviewCount++;

            int generation = palettePreviewGeneration;
            UndertaleBackground? background = data.Background;
            UndertaleTexturePageItem? texture = background?.Texture;
            uint tileId = item.Value & RoomTileIndexMask;
            if (tileId == 0)
            {
                item.SetPreview(null, "empty");
                return;
            }

            if (background is null || texture is null)
            {
                item.SetPreview(null, "no texture");
                return;
            }

            if (!TryGetGms2TileSource(background, tileId, out int sourceX, out int sourceY))
            {
                item.SetPreview(null, "source unavailable");
                return;
            }

            try
            {
                int tileWidth = (int)Math.Max(1, background.GMS2TileWidth);
                int tileHeight = (int)Math.Max(1, background.GMS2TileHeight);
                uint transform = (item.Value & RoomTileFlagsMask) >> 28;
                RoomPreviewTileKey key = new(texture, sourceX, sourceY, tileWidth, tileHeight, transform);
                byte[] bytes = await System.Threading.Tasks.Task.Run(() => GetCachedRoomTilePreviewPng(key));
                if (generation != palettePreviewGeneration)
                    return;

                item.SetPreview(LoadBitmapImage(bytes), "preview ready");
            }
            catch (Exception ex)
            {
                if (generation != palettePreviewGeneration)
                    return;

                item.SetPreview(null, $"preview failed: {ex.Message}");
            }
        }

        async System.Threading.Tasks.Task UpdateTileVisualPreviewAsync(
            uint value,
            Image image,
            TextBlock text,
            string label,
            int generation)
        {
            UndertaleBackground? background = data.Background;
            UndertaleTexturePageItem? texture = background?.Texture;
            uint tileId = value & RoomTileIndexMask;
            if (tileId == 0)
            {
                if (generation != tileVisualPreviewGeneration)
                    return;

                image.Source = null;
                text.Text = $"{label}: empty.";
                return;
            }

            if (background is null || texture is null)
            {
                if (generation != tileVisualPreviewGeneration)
                    return;

                image.Source = null;
                text.Text = $"{label}: no tile set texture.";
                return;
            }

            if (!TryGetGms2TileSource(background, tileId, out int sourceX, out int sourceY))
            {
                if (generation != tileVisualPreviewGeneration)
                    return;

                image.Source = null;
                text.Text = $"{label}: tile source unavailable.";
                return;
            }

            try
            {
                int tileWidth = (int)Math.Max(1, background.GMS2TileWidth);
                int tileHeight = (int)Math.Max(1, background.GMS2TileHeight);
                uint transform = (value & RoomTileFlagsMask) >> 28;
                RoomPreviewTileKey key = new(texture, sourceX, sourceY, tileWidth, tileHeight, transform);
                byte[] bytes = await System.Threading.Tasks.Task.Run(() => GetCachedRoomTilePreviewPng(key));
                if (generation != tileVisualPreviewGeneration)
                    return;

                image.Source = LoadBitmapImage(bytes);
                text.Text = $"{label}: {FormatRoomTileDetail(value)}.";
            }
            catch (Exception ex)
            {
                if (generation != tileVisualPreviewGeneration)
                    return;

                image.Source = null;
                text.Text = $"{label}: preview failed: {ex.Message}";
            }
        }

        void SelectCell(int x, int y, string? status = null)
        {
            selectedX = x;
            selectedY = y;
            xBox.Text = x.ToString(CultureInfo.InvariantCulture);
            yBox.Text = y.ToString(CultureInfo.InvariantCulture);
            cellsView.SelectedItem = visibleCells.FirstOrDefault(item => item.X == x && item.Y == y);
            editorStatusText.Text = status ?? $"Selected cell {x},{y}: {FormatRoomTileDetail(workingData[y][x])}.";
            RefreshTileVisualPreviews();
        }

        bool TryReadCoordinates(out int x, out int y)
        {
            x = 0;
            y = 0;
            try
            {
                uint parsedX = ParseRoomTileUInt(xBox.Text, "Tile cell X");
                uint parsedY = ParseRoomTileUInt(yBox.Text, "Tile cell Y");
                if (parsedX >= data.TilesX || parsedY >= data.TilesY)
                    throw new InvalidDataException($"Tile cell must be inside 0,0 to {tilesX - 1},{tilesY - 1}.");

                x = (int)parsedX;
                y = (int)parsedY;
                return true;
            }
            catch (Exception ex)
            {
                editorStatusText.Text = ex.Message;
                return false;
            }
        }

        bool TryReadTileValue(out uint value)
        {
            value = 0;
            try
            {
                value = ParseRoomTileValue(valueBox.Text, "Tile value");
                return true;
            }
            catch (Exception ex)
            {
                editorStatusText.Text = ex.Message;
                return false;
            }
        }

        bool TryReadBrushSize(out int width, out int height)
        {
            width = 1;
            height = 1;
            try
            {
                uint parsedWidth = ParseRoomTileUInt(brushWidthBox.Text, "Brush width");
                uint parsedHeight = ParseRoomTileUInt(brushHeightBox.Text, "Brush height");
                if (parsedWidth == 0 || parsedHeight == 0)
                    throw new InvalidDataException("Brush size must be at least 1x1.");

                if (parsedWidth > tilesX || parsedHeight > tilesY)
                    throw new InvalidDataException($"Brush size must fit inside the {tilesX}x{tilesY} tile layer.");

                width = (int)parsedWidth;
                height = (int)parsedHeight;
                return true;
            }
            catch (Exception ex)
            {
                editorStatusText.Text = ex.Message;
                return false;
            }
        }

        void SetBrushData(uint[][] value, int width, int height, string status)
        {
            brushWidth = Math.Max(1, width);
            brushHeight = Math.Max(1, height);
            brushData = CloneRoomTileArray(value, brushWidth, brushHeight);
            brushWidthBox.Text = brushWidth.ToString(CultureInfo.InvariantCulture);
            brushHeightBox.Text = brushHeight.ToString(CultureInfo.InvariantCulture);
            valueBox.Text = FormatRoomTileRawValue(brushData[0][0]);
            paletteView.SelectedItem = paletteItems.FirstOrDefault(item => item.Value == (brushData[0][0] & RoomTileIndexMask));
            editorStatusText.Text = status;
            RefreshTileVisualPreviews();
        }

        void SetSingleTileBrush(uint value, string status)
        {
            SetBrushData(new[] { new[] { value } }, 1, 1, status);
        }

        void SetBrushValue(uint value, string status)
        {
            SetSingleTileBrush(value, status);
        }

        void MirrorBrush()
        {
            uint[][] mirrored = new uint[brushHeight][];
            for (int y = 0; y < brushHeight; y++)
            {
                mirrored[y] = new uint[brushWidth];
                for (int x = 0; x < brushWidth; x++)
                    mirrored[y][brushWidth - x - 1] = MirrorRoomTileValue(brushData[y][x]);
            }

            SetBrushData(mirrored, brushWidth, brushHeight, $"Mirrored brush on X: {brushWidth}x{brushHeight}.");
        }

        void FlipBrush()
        {
            uint[][] flipped = new uint[brushHeight][];
            for (int y = 0; y < brushHeight; y++)
            {
                flipped[brushHeight - y - 1] = new uint[brushWidth];
                for (int x = 0; x < brushWidth; x++)
                    flipped[brushHeight - y - 1][x] = FlipRoomTileValue(brushData[y][x]);
            }

            SetBrushData(flipped, brushWidth, brushHeight, $"Flipped brush on Y: {brushWidth}x{brushHeight}.");
        }

        void RotateBrushClockwise()
        {
            uint[][] rotated = new uint[brushWidth][];
            for (int y = 0; y < brushWidth; y++)
                rotated[y] = new uint[brushHeight];

            for (int y = 0; y < brushHeight; y++)
            {
                for (int x = 0; x < brushWidth; x++)
                    rotated[x][brushHeight - y - 1] = RotateRoomTileValueClockwise(brushData[y][x]);
            }

            SetBrushData(rotated, brushHeight, brushWidth, $"Rotated brush clockwise: {brushHeight}x{brushWidth}.");
        }

        void RotateBrushCounterClockwise()
        {
            uint[][] rotated = new uint[brushWidth][];
            for (int y = 0; y < brushWidth; y++)
                rotated[y] = new uint[brushHeight];

            for (int y = 0; y < brushHeight; y++)
            {
                for (int x = 0; x < brushWidth; x++)
                    rotated[brushWidth - x - 1][y] = RotateRoomTileValueCounterClockwise(brushData[y][x]);
            }

            SetBrushData(rotated, brushHeight, brushWidth, $"Rotated brush counterclockwise: {brushHeight}x{brushWidth}.");
        }

        void LoadSelectedCellIntoBrush()
        {
            SetSingleTileBrush(workingData[selectedY][selectedX], $"Loaded selected cell {selectedX},{selectedY} into brush.");
        }

        void PickBrushFromLayer()
        {
            if (!TryReadCoordinates(out int x, out int y) || !TryReadBrushSize(out int width, out int height))
                return;

            PickBrushRectangle(x, y, width, height, $"Picked brush {width}x{height} from {x},{y}.");
        }

        void PickBrushRectangle(int x, int y, int width, int height, string status)
        {
            if (x + width > tilesX || y + height > tilesY)
            {
                editorStatusText.Text = $"Brush rectangle {x},{y} {width}x{height} exceeds the tile layer bounds.";
                return;
            }

            uint[][] picked = new uint[height][];
            for (int brushY = 0; brushY < height; brushY++)
            {
                picked[brushY] = new uint[width];
                Array.Copy(workingData[y + brushY], x, picked[brushY], 0, width);
            }

            SetBrushData(picked, width, height, status);
        }

        void PickBrushBetweenCells(int startX, int startY, int endX, int endY)
        {
            int x = Math.Min(startX, endX);
            int y = Math.Min(startY, endY);
            int width = Math.Abs(endX - startX) + 1;
            int height = Math.Abs(endY - startY) + 1;
            string status = $"Picked brush {width}x{height} from {x},{y} to {Math.Max(startX, endX)},{Math.Max(startY, endY)}.";
            PickBrushRectangle(x, y, width, height, status);
            SelectCell(endX, endY, status);
        }

        int ApplyBrushStampAt(int x, int y, bool pushUndo, string unchangedStatus, Func<int, string> changedStatus)
        {
            int maxX = Math.Min(tilesX, x + brushWidth);
            int maxY = Math.Min(tilesY, y + brushHeight);
            int changedCount = 0;
            for (int stampY = y; stampY < maxY; stampY++)
            {
                for (int stampX = x; stampX < maxX; stampX++)
                {
                    uint value = brushData[stampY - y][stampX - x];
                    if (workingData[stampY][stampX] != value)
                        changedCount++;
                }
            }

            if (changedCount == 0)
            {
                SelectCell(x, y, unchangedStatus);
                return 0;
            }

            if (pushUndo)
                PushUndoSnapshot();

            for (int stampY = y; stampY < maxY; stampY++)
            {
                for (int stampX = x; stampX < maxX; stampX++)
                    workingData[stampY][stampX] = brushData[stampY - y][stampX - x];
            }

            changed = true;
            RefreshVisibleCells();
            SelectCell(x, y, changedStatus(changedCount));
            return changedCount;
        }

        void StampBrushAt(int x, int y)
        {
            ApplyBrushStampAt(
                x,
                y,
                pushUndo: true,
                unchangedStatus: "Stamp did not change any cells.",
                changedStatus: changedCount => $"Stamped {changedCount} cell(s) from brush {brushWidth}x{brushHeight}.");
        }

        void StampBrushAtSelectedCell()
        {
            if (TryReadCoordinates(out int x, out int y))
                StampBrushAt(x, y);
        }

        void RefreshBrushFromValueText()
        {
            if (TryReadTileValue(out uint value))
                SetSingleTileBrush(value, $"Brush value set to {FormatRoomTileDetail(value)}.");
        }

        void SetEmptyBrush()
        {
            SetSingleTileBrush(0, "Brush set to empty tile.");
        }

        void FillBrushAt(int x, int y, bool global)
        {
            uint value = brushData[0][0];
            if (workingData[y][x] == value)
            {
                SelectCell(x, y, global ? "Replace did not change any cells." : "Fill did not change any cells.");
                return;
            }

            PushUndoSnapshot();
            int changedCount = global
                ? ReplaceMatchingCellValues(x, y, value)
                : FloodFillCellValue(x, y, value);
            if (changedCount == 0)
            {
                SelectCell(x, y, global ? "Replace did not change any cells." : "Fill did not change any cells.");
                return;
            }

            changed = true;
            RefreshVisibleCells();
            string action = global ? "Replaced" : "Filled";
            SelectCell(x, y, $"{action} {changedCount} cell(s) with brush value {FormatRoomTileDetail(value)}.");
        }

        void SetCellValue(int x, int y, uint value, string status)
        {
            if (workingData[y][x] == value)
            {
                SelectCell(x, y, $"Cell {x},{y} already contains {FormatRoomTileDetail(value)}.");
                return;
            }

            PushUndoSnapshot();
            workingData[y][x] = value;
            changed = true;
            RefreshVisibleCells();
            SelectCell(x, y, status);
        }

        int FloodFillCellValue(int startX, int startY, uint value)
        {
            uint replace = workingData[startY][startX];
            if (replace == value)
                return 0;

            int changedCount = 0;
            HashSet<long> handled = [];
            Queue<(int X, int Y)> queue = new();
            queue.Enqueue((startX, startY));
            while (queue.Count > 0)
            {
                (int x, int y) = queue.Dequeue();
                if (x < 0 || y < 0 || x >= tilesX || y >= tilesY)
                    continue;

                long key = ((long)y * tilesX) + x;
                if (!handled.Add(key))
                    continue;

                if (workingData[y][x] != replace)
                    continue;

                workingData[y][x] = value;
                changedCount++;
                if (x > 0)
                    queue.Enqueue((x - 1, y));
                if (x < tilesX - 1)
                    queue.Enqueue((x + 1, y));
                if (y > 0)
                    queue.Enqueue((x, y - 1));
                if (y < tilesY - 1)
                    queue.Enqueue((x, y + 1));
            }

            return changedCount;
        }

        int ReplaceMatchingCellValues(int startX, int startY, uint value)
        {
            uint replace = workingData[startY][startX];
            if (replace == value)
                return 0;

            int changedCount = 0;
            for (int y = 0; y < tilesY; y++)
            {
                for (int x = 0; x < tilesX; x++)
                {
                    if (workingData[y][x] != replace)
                        continue;

                    workingData[y][x] = value;
                    changedCount++;
                }
            }

            return changedCount;
        }

        void UndoTileEdit()
        {
            if (undoSnapshots.Count == 0)
                return;

            if (redoSnapshots.Count >= maxUndoSnapshots)
                redoSnapshots.RemoveAt(0);

            redoSnapshots.Add(CloneWorkingData());
            int index = undoSnapshots.Count - 1;
            uint[][] snapshot = undoSnapshots[index];
            undoSnapshots.RemoveAt(index);
            RestoreWorkingData(snapshot, "Undid tile edit.");
        }

        void RedoTileEdit()
        {
            if (redoSnapshots.Count == 0)
                return;

            if (undoSnapshots.Count >= maxUndoSnapshots)
                undoSnapshots.RemoveAt(0);

            undoSnapshots.Add(CloneWorkingData());
            int index = redoSnapshots.Count - 1;
            uint[][] snapshot = redoSnapshots[index];
            redoSnapshots.RemoveAt(index);
            RestoreWorkingData(snapshot, "Redid tile edit.");
        }

        long GetTileCellKey(RoomTileCellEditorItem item)
        {
            return ((long)item.Y * tilesX) + item.X;
        }

        RoomTileCellEditorItem? GetTileCellFromPointer(PointerRoutedEventArgs args)
        {
            return FindDataContextFromOriginalSource<RoomTileCellEditorItem>(args.OriginalSource);
        }

        void StopTileDrag(PointerRoutedEventArgs args)
        {
            cellsView.ReleasePointerCapture(args.Pointer);
            isTileDragPainting = false;
            isTileDragPicking = false;
            tileDragPickMoved = false;
            tileDragPaintUndoPushed = false;
            tileDragPaintedCells.Clear();
        }

        void PaintDraggedTileCell(RoomTileCellEditorItem item)
        {
            if (!tileDragPaintedCells.Add(GetTileCellKey(item)))
                return;

            int changedCount = ApplyBrushStampAt(
                item.X,
                item.Y,
                pushUndo: !tileDragPaintUndoPushed,
                unchangedStatus: "Drag paint did not change any cells.",
                changedStatus: count => $"Painted {count} cell(s) at {item.X},{item.Y} from brush {brushWidth}x{brushHeight}.");
            if (changedCount > 0)
                tileDragPaintUndoPushed = true;
        }

        void PreviewTileDragPick(RoomTileCellEditorItem item)
        {
            if (item.X == tileDragStartX && item.Y == tileDragStartY)
                return;

            tileDragPickMoved = true;
            int x = Math.Min(tileDragStartX, item.X);
            int y = Math.Min(tileDragStartY, item.Y);
            int width = Math.Abs(item.X - tileDragStartX) + 1;
            int height = Math.Abs(item.Y - tileDragStartY) + 1;
            editorStatusText.Text = $"Release to pick brush {width}x{height} from {x},{y}.";
        }

        void HandleTileDragOver(PointerRoutedEventArgs args)
        {
            if (!isTileDragPainting && !isTileDragPicking)
                return;

            if (GetTileCellFromPointer(args) is not { } item)
                return;

            if (isTileDragPainting)
                PaintDraggedTileCell(item);
            else
                PreviewTileDragPick(item);

            args.Handled = true;
        }

        cellsView.AddHandler(
            UIElement.PointerPressedEvent,
            new PointerEventHandler((_, args) =>
            {
                if (GetTileCellFromPointer(args) is not { } item)
                    return;

                var point = args.GetCurrentPoint(cellsView);
                if (!point.Properties.IsLeftButtonPressed)
                    return;

                bool alt = IsVirtualKeyDown(VirtualKey.Menu);
                bool control = IsVirtualKeyDown(VirtualKey.Control);
                bool shift = IsVirtualKeyDown(VirtualKey.Shift);
                if (paintClicksCheckBox.IsChecked == true && !alt && !control && !shift)
                {
                    isTileDragPainting = true;
                    tileDragPaintUndoPushed = false;
                    tileDragPaintedCells.Clear();
                    cellsView.CapturePointer(args.Pointer);
                    PaintDraggedTileCell(item);
                    args.Handled = true;
                    return;
                }

                if (shift && !alt && !control)
                {
                    isTileDragPicking = true;
                    tileDragPickMoved = false;
                    tileDragStartX = item.X;
                    tileDragStartY = item.Y;
                    cellsView.CapturePointer(args.Pointer);
                }
            }),
            true);
        cellsView.AddHandler(
            UIElement.PointerMovedEvent,
            new PointerEventHandler((_, args) => HandleTileDragOver(args)),
            true);
        cellsView.AddHandler(
            UIElement.PointerEnteredEvent,
            new PointerEventHandler((_, args) => HandleTileDragOver(args)),
            true);
        cellsView.AddHandler(
            UIElement.PointerReleasedEvent,
            new PointerEventHandler((_, args) =>
            {
                if (!isTileDragPainting && !isTileDragPicking)
                    return;

                if (isTileDragPicking && tileDragPickMoved && GetTileCellFromPointer(args) is { } item)
                {
                    PickBrushBetweenCells(tileDragStartX, tileDragStartY, item.X, item.Y);
                    suppressNextTileClick = true;
                    suppressNextTileClickUntil = DateTimeOffset.Now.AddMilliseconds(500);
                    suppressNextTileClickX = item.X;
                    suppressNextTileClickY = item.Y;
                }

                StopTileDrag(args);
                args.Handled = true;
            }),
            true);

        cellsView.ItemClick += (_, args) =>
        {
            if (suppressNextTileClick)
            {
                bool shouldSuppress =
                    DateTimeOffset.Now <= suppressNextTileClickUntil &&
                    args.ClickedItem is RoomTileCellEditorItem suppressedItem &&
                    suppressedItem.X == suppressNextTileClickX &&
                    suppressedItem.Y == suppressNextTileClickY;
                suppressNextTileClick = false;
                if (shouldSuppress)
                    return;
            }

            if (args.ClickedItem is RoomTileCellEditorItem item)
            {
                bool alt = IsVirtualKeyDown(VirtualKey.Menu);
                bool control = IsVirtualKeyDown(VirtualKey.Control);
                bool shift = IsVirtualKeyDown(VirtualKey.Shift);
                if (alt)
                {
                    SelectCell(item.X, item.Y);
                    LoadSelectedCellIntoBrush();
                    return;
                }

                if (control)
                {
                    FillBrushAt(item.X, item.Y, global: shift);
                    return;
                }

                if (shift)
                {
                    PickBrushBetweenCells(selectedX, selectedY, item.X, item.Y);
                    return;
                }

                if (paintClicksCheckBox.IsChecked == true)
                {
                    StampBrushAt(item.X, item.Y);
                    return;
                }

                SelectCell(item.X, item.Y);
            }
        };
        paletteView.ItemClick += (sender, args) =>
        {
            if (args.ClickedItem is not RoomTilePaletteEditorItem item)
                return;

            SetBrushValue(item.Value, $"Brush value set to {FormatRoomTileDetail(item.Value)}.");
            _ = EnsurePalettePreviewAsync(item, force: true);
        };
        paletteView.ContainerContentChanging += (sender, args) =>
        {
            if (args.Item is RoomTilePaletteEditorItem item)
                _ = EnsurePalettePreviewAsync(item, force: false);
        };
        mirrorButton.Click += (_, _) => MirrorBrush();
        flipButton.Click += (_, _) => FlipBrush();
        rotateClockwiseButton.Click += (_, _) => RotateBrushClockwise();
        rotateCounterClockwiseButton.Click += (_, _) => RotateBrushCounterClockwise();
        pickBrushButton.Click += (_, _) => PickBrushFromLayer();
        stampBrushButton.Click += (_, _) => StampBrushAtSelectedCell();
        resetBrushButton.Click += (_, _) => RefreshBrushFromValueText();
        eraseBrushButton.Click += (_, _) => SetEmptyBrush();
        valueBox.LostFocus += (_, _) => RefreshBrushFromValueText();
        valueBox.KeyDown += (_, args) =>
        {
            if (args.Key != VirtualKey.Enter)
                return;

            RefreshBrushFromValueText();
            args.Handled = true;
        };
        cellsView.KeyDown += (_, args) =>
        {
            bool control = IsVirtualKeyDown(VirtualKey.Control);
            if (control && args.Key == VirtualKey.Z)
            {
                UndoTileEdit();
                args.Handled = true;
                return;
            }

            if (control && args.Key == VirtualKey.Y)
            {
                RedoTileEdit();
                args.Handled = true;
                return;
            }

            if (control)
                return;

            switch (args.Key)
            {
                case VirtualKey.X:
                    MirrorBrush();
                    args.Handled = true;
                    break;
                case VirtualKey.Y:
                case VirtualKey.Z:
                    FlipBrush();
                    args.Handled = true;
                    break;
                case VirtualKey.R:
                    RotateBrushClockwise();
                    args.Handled = true;
                    break;
                case VirtualKey.Q:
                    RotateBrushCounterClockwise();
                    args.Handled = true;
                    break;
                case VirtualKey.P:
                    PickBrushFromLayer();
                    args.Handled = true;
                    break;
                case VirtualKey.B:
                    LoadSelectedCellIntoBrush();
                    args.Handled = true;
                    break;
                case VirtualKey.E:
                    SetEmptyBrush();
                    args.Handled = true;
                    break;
                case VirtualKey.Enter:
                case VirtualKey.Space:
                    StampBrushAtSelectedCell();
                    args.Handled = true;
                    break;
                case VirtualKey.Delete:
                    SetCellValue(selectedX, selectedY, 0, $"Cleared cell {selectedX},{selectedY}.");
                    args.Handled = true;
                    break;
            }
        };
        loadButton.Click += (_, _) =>
        {
            if (TryReadCoordinates(out int x, out int y))
            {
                SelectCell(x, y);
                LoadSelectedCellIntoBrush();
            }
        };
        setButton.Click += (_, _) =>
        {
            if (!TryReadCoordinates(out int x, out int y) || !TryReadTileValue(out uint value))
                return;

            SetCellValue(x, y, value, $"Set cell {x},{y} to {FormatRoomTileDetail(value)}.");
        };
        clearButton.Click += (_, _) =>
        {
            if (!TryReadCoordinates(out int x, out int y))
                return;

            SetCellValue(x, y, 0, $"Cleared cell {x},{y}.");
        };
        fillButton.Click += (_, _) =>
        {
            if (!TryReadCoordinates(out int x, out int y) || !TryReadTileValue(out uint value))
                return;

            if (workingData[y][x] == value)
            {
                SelectCell(x, y, "Fill did not change any cells.");
                return;
            }

            PushUndoSnapshot();
            int changedCount = FloodFillCellValue(x, y, value);
            if (changedCount == 0)
            {
                SelectCell(x, y, "Fill did not change any cells.");
                return;
            }

            changed = true;
            RefreshVisibleCells();
            SelectCell(x, y, $"Filled {changedCount} connected cell(s) with {FormatRoomTileDetail(value)}.");
        };
        replaceButton.Click += (_, _) =>
        {
            if (!TryReadCoordinates(out int x, out int y) || !TryReadTileValue(out uint value))
                return;

            if (workingData[y][x] == value)
            {
                SelectCell(x, y, "Replace did not change any cells.");
                return;
            }

            PushUndoSnapshot();
            int changedCount = ReplaceMatchingCellValues(x, y, value);
            if (changedCount == 0)
            {
                SelectCell(x, y, "Replace did not change any cells.");
                return;
            }

            changed = true;
            RefreshVisibleCells();
            SelectCell(x, y, $"Replaced {changedCount} matching cell(s) with {FormatRoomTileDetail(value)}.");
        };
        undoButton.Click += (_, _) =>
        {
            UndoTileEdit();
        };
        redoButton.Click += (_, _) =>
        {
            RedoTileEdit();
        };

        RefreshPaletteItems();
        RefreshVisibleCells();
        SelectCell(0, 0);

        ContentDialog dialog = new()
        {
            Title = $"Edit tile cells - {summary.Title}",
            Content = content,
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        if (!changed || RoomTileArraysEqual(workingData, originalData, tilesX, tilesY))
        {
            StatusBox.Text = "No tile cell changes were applied.";
            return;
        }

        data.TileData = workingData;
        data.TileDataUpdated();
        await RefreshRoomLayerAfterEditAsync(room, summary.Layer, "Edited tile layer cells.");
    }

    private async void RoomLayerTilesAutoSizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary summary ||
            summary.Layer.TilesData is not { } data)
        {
            return;
        }

        if (data.Background is null)
        {
            StatusBox.Text = "The layer must have a tile set selected.";
            return;
        }

        uint tileWidth = Math.Max(1u, data.Background.GMS2TileWidth);
        uint tileHeight = Math.Max(1u, data.Background.GMS2TileHeight);
        uint tilesX = (uint)Math.Ceiling(room.Width / (double)tileWidth);
        uint tilesY = (uint)Math.Ceiling(room.Height / (double)tileHeight);
        ResizeRoomLayerTileData(data, tilesX, tilesY);
        await RefreshRoomLayerAfterEditAsync(room, summary.Layer, "Auto-sized tile layer.");
    }

    private async void RoomLayerTilesExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedResource?.Value is not UndertaleRoom ||
            RoomLayersList.SelectedItem is not RoomLayerSummary summary ||
            summary.Layer.TilesData is not { } data)
        {
            return;
        }

        if (data.TileData is null || data.TileData.Length == 0)
        {
            StatusBox.Text = "Tile data is empty.";
            return;
        }

        string directory = Path.GetDirectoryName(_currentFilePath ?? string.Empty) ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string fileName = $"{SafeFileName(summary.Layer.LayerName?.Content, "layer")}_tiledata.csv";
        string path = Path.Combine(directory, fileName);
        try
        {
            await File.WriteAllTextAsync(path, ExportRoomLayerTileDataCsv(data), Encoding.UTF8);
            StatusBox.Text = $"Exported tile data to {path}.";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to export tile data: {ex.Message}";
        }
    }

    private async void RoomLayerTilesImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomLayersList.SelectedItem is not RoomLayerSummary summary ||
            summary.Layer.TilesData is not { } data)
        {
            return;
        }

        if (data.TilesX == 0 || data.TilesY == 0)
        {
            StatusBox.Text = "Tile data size cannot be zero.";
            return;
        }

        FileOpenPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add(".csv");
        picker.FileTypeFilter.Add("*");
        InitializePickerWithMainWindow(picker);
        StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        try
        {
            string[] lines = await File.ReadAllLinesAsync(file.Path, Encoding.UTF8);
            bool importAsTiled = lines.FirstOrDefault()?.Count(character => character == ',') > 1;
            if (importAsTiled)
            {
                ContentDialog dialog = new()
                {
                    Title = "Import tile data",
                    Content = "Was the data exported from Tiled?",
                    PrimaryButtonText = "Yes",
                    SecondaryButtonText = "No",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = XamlRoot
                };
                if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                {
                    StatusBox.Text = "The selected CSV format was not imported.";
                    return;
                }
            }

            data.TileData = ImportRoomLayerTileDataCsv(lines, data.TilesX, data.TilesY, importAsTiled);
            await RefreshRoomLayerAfterEditAsync(room, summary.Layer, "Imported tile layer CSV.");
        }
        catch (Exception ex)
        {
            RefreshRoomLayerEditor(summary);
            StatusBox.Text = $"Failed to import tile data: {ex.Message}";
        }
    }

    private async System.Threading.Tasks.Task RefreshRoomLayerAfterEditAsync(
        UndertaleRoom room,
        UndertaleRoom.Layer layer,
        string status)
    {
        MarkDirty();
        RefreshRoomLayers(room, layer);
        RoomOverviewText.Text = BuildRoomOverview(room);
        RoomInstanceSummary[] instanceSummaries = BuildRoomInstanceSummaries(room).ToArray();
        if (_isRoomPreviewRendered)
            await RefreshRoomPreviewAsync(room, instanceSummaries);
        else
            ClearRoomPreviewSurface(room, instanceSummaries);
        RefreshCurrentDetails();
        StatusBox.Text = status;
    }

    private static bool LayerTypeAllowsOffsetSpeed(UndertaleRoom.LayerType layerType)
    {
        return layerType is not UndertaleRoom.LayerType.Instances;
    }

    private static string BuildRoomLayerTilesDataSummary(UndertaleRoom.Layer.LayerTilesData data)
    {
        int visibleTiles = CountVisibleRoomTiles(data.TileData);
        string tileSet = FormatTitle(data.Background?.Name?.Content);
        return $"{data.TilesX}x{data.TilesY} cells, {visibleTiles} non-empty tile(s), tile set {tileSet}.";
    }

    private static uint[][] CloneRoomLayerTileData(UndertaleRoom.Layer.LayerTilesData data)
    {
        if (data.TilesX > int.MaxValue || data.TilesY > int.MaxValue)
            throw new InvalidDataException("Tile layer size is too large.");

        return CloneRoomTileArray(data.TileData ?? [], (int)data.TilesX, (int)data.TilesY);
    }

    private static uint[][] CloneRoomTileArray(uint[][] source, int tilesX, int tilesY)
    {
        uint[][] clone = new uint[tilesY][];
        for (int y = 0; y < tilesY; y++)
        {
            clone[y] = new uint[tilesX];
            if (y >= source.Length)
                continue;

            uint[] sourceRow = source[y] ?? [];
            Array.Copy(sourceRow, clone[y], Math.Min(sourceRow.Length, clone[y].Length));
        }

        return clone;
    }

    private static bool RoomTileArraysEqual(uint[][] left, uint[][] right, int tilesX, int tilesY)
    {
        for (int y = 0; y < tilesY; y++)
        {
            uint[] leftRow = y < left.Length ? left[y] ?? [] : [];
            uint[] rightRow = y < right.Length ? right[y] ?? [] : [];
            for (int x = 0; x < tilesX; x++)
            {
                uint leftValue = x < leftRow.Length ? leftRow[x] : 0;
                uint rightValue = x < rightRow.Length ? rightRow[x] : 0;
                if (leftValue != rightValue)
                    return false;
            }
        }

        return true;
    }

    private static int CountVisibleRoomTiles(uint[][]? tileData)
    {
        int visibleTiles = 0;
        foreach (uint[] row in tileData ?? [])
        {
            foreach (uint tile in row)
            {
                if ((tile & RoomTileIndexMask) != 0)
                    visibleTiles++;
            }
        }

        return visibleTiles;
    }

    private static void ResizeRoomLayerTileData(
        UndertaleRoom.Layer.LayerTilesData data,
        uint tilesX,
        uint tilesY)
    {
        if (tilesX > int.MaxValue || tilesY > int.MaxValue)
            throw new InvalidDataException("Tile layer size is too large.");

        uint[][] previous = data.TileData ?? [];
        uint[][] resized = new uint[(int)tilesY][];
        for (int y = 0; y < resized.Length; y++)
        {
            resized[y] = new uint[(int)tilesX];
            if (y >= previous.Length)
                continue;

            uint[] previousRow = previous[y] ?? [];
            Array.Copy(previousRow, resized[y], Math.Min(previousRow.Length, resized[y].Length));
        }

        data.TileData = resized;
        data.TilesX = tilesX;
        data.TilesY = tilesY;
        data.TileDataUpdated();
    }

    private static string ExportRoomLayerTileDataCsv(UndertaleRoom.Layer.LayerTilesData data)
    {
        StringBuilder builder = new();
        foreach (uint[] row in data.TileData ?? [])
            builder.AppendLine(string.Join(";", row.Select(value => value.ToString(CultureInfo.InvariantCulture))));

        return builder.ToString();
    }

    private static uint[][] ImportRoomLayerTileDataCsv(
        string[] lines,
        uint tilesX,
        uint tilesY,
        bool importAsTiled)
    {
        if (tilesX > int.MaxValue || tilesY > int.MaxValue)
            throw new InvalidDataException("Tile layer size is too large.");

        if (lines.Length != (int)tilesY)
            throw new InvalidDataException("Selected file line count does not match tile layer height.");

        char delimiter = importAsTiled ? ',' : ';';
        uint[][] tileData = new uint[(int)tilesY][];
        for (int y = 0; y < lines.Length; y++)
        {
            string[] columns = lines[y].Split(delimiter);
            if (columns.Length != (int)tilesX)
                throw new InvalidDataException($"Length of line {y + 1} is not equal to the tile data width.");

            tileData[y] = new uint[(int)tilesX];
            for (int x = 0; x < columns.Length; x++)
                tileData[y][x] = importAsTiled ? ConvertTiledTileToRoomTile(columns[x]) : uint.Parse(columns[x], CultureInfo.InvariantCulture);
        }

        return tileData;
    }

    private static uint ConvertTiledTileToRoomTile(string value)
    {
        int parsed = int.Parse(value, CultureInfo.InvariantCulture);
        uint tiledValue = unchecked((uint)parsed);
        if (tiledValue == uint.MaxValue)
            return 0;

        uint id = tiledValue & 0x0FFFFFFF;
        uint flags = tiledValue & 0xF0000000;
        flags = flags switch
        {
            0 => 0,
            2147483648 => 1,
            1073741824 => 2,
            3221225472 => 3,
            2684354560 => 4,
            3758096384 => 5,
            536870912 => 6,
            1610612736 => 7,
            _ => throw new InvalidDataException($"{flags} is not a valid tile flag value.")
        };
        return id | (flags << 28);
    }

    private void RefreshRoomViews(UndertaleRoom room, UndertaleRoom.View? selectedView)
    {
        _isUpdatingRoomViewEditor = true;
        try
        {
            RoomViewSummary[] summaries = BuildRoomViewSummaries(room).ToArray();
            RoomViewsList.ItemsSource = summaries;
            RoomViewSummary? selected = selectedView is null
                ? summaries.FirstOrDefault()
                : summaries.FirstOrDefault(summary => ReferenceEquals(summary.View, selectedView));
            RoomViewsList.SelectedItem = selected;
            UpdateRoomViewButtons(selected);
            RefreshRoomViewEditor(selected);
        }
        finally
        {
            _isUpdatingRoomViewEditor = false;
        }
    }

    private void UpdateRoomViewButtons(RoomViewSummary? selectedSummary)
    {
        bool canEdit = _data is not null &&
                       !_data.UnsupportedBytecodeVersion &&
                       _selectedResource?.Value is UndertaleRoom;
        RoomDisableViewButton.IsEnabled = canEdit && selectedSummary is not null && selectedSummary.View.Enabled;
    }

    private void RefreshRoomViewEditor(RoomViewSummary? summary)
    {
        _isUpdatingRoomViewEditor = true;
        if (summary is null)
        {
            HideRoomViewEditor();
            _isUpdatingRoomViewEditor = false;
            return;
        }

        UndertaleRoom.View view = summary.View;
        RoomViewEditorTitleText.Text = summary.Title;
        RoomViewEnabledCheckBox.IsChecked = view.Enabled;
        RoomViewXBox.Text = view.ViewX.ToString(CultureInfo.InvariantCulture);
        RoomViewYBox.Text = view.ViewY.ToString(CultureInfo.InvariantCulture);
        RoomViewWidthBox.Text = view.ViewWidth.ToString(CultureInfo.InvariantCulture);
        RoomViewHeightBox.Text = view.ViewHeight.ToString(CultureInfo.InvariantCulture);
        RoomViewPortXBox.Text = view.PortX.ToString(CultureInfo.InvariantCulture);
        RoomViewPortYBox.Text = view.PortY.ToString(CultureInfo.InvariantCulture);
        RoomViewPortWidthBox.Text = view.PortWidth.ToString(CultureInfo.InvariantCulture);
        RoomViewPortHeightBox.Text = view.PortHeight.ToString(CultureInfo.InvariantCulture);
        RoomViewBorderXBox.Text = view.BorderX.ToString(CultureInfo.InvariantCulture);
        RoomViewBorderYBox.Text = view.BorderY.ToString(CultureInfo.InvariantCulture);
        RoomViewSpeedXBox.Text = view.SpeedX.ToString(CultureInfo.InvariantCulture);
        RoomViewSpeedYBox.Text = view.SpeedY.ToString(CultureInfo.InvariantCulture);
        ObjectReferenceItem[] objectItems = _data is null ? [] : BuildObjectReferenceItems(_data, includeNull: true).ToArray();
        RoomViewObjectComboBox.ItemsSource = objectItems;
        RoomViewObjectComboBox.SelectedItem = objectItems.FirstOrDefault(item => ReferenceEquals(item.Object, view.ObjectId)) ??
                                             objectItems.FirstOrDefault();
        RoomOpenViewObjectButton.IsEnabled = view.ObjectId is not null;
        RoomViewEditorPanel.Visibility = Visibility.Visible;
        _isUpdatingRoomViewEditor = false;
    }

    private void HideRoomViewEditor()
    {
        RoomViewEditorTitleText.Text = "Selected view";
        RoomViewEnabledCheckBox.IsChecked = false;
        RoomViewXBox.Text = string.Empty;
        RoomViewYBox.Text = string.Empty;
        RoomViewWidthBox.Text = string.Empty;
        RoomViewHeightBox.Text = string.Empty;
        RoomViewPortXBox.Text = string.Empty;
        RoomViewPortYBox.Text = string.Empty;
        RoomViewPortWidthBox.Text = string.Empty;
        RoomViewPortHeightBox.Text = string.Empty;
        RoomViewBorderXBox.Text = string.Empty;
        RoomViewBorderYBox.Text = string.Empty;
        RoomViewSpeedXBox.Text = string.Empty;
        RoomViewSpeedYBox.Text = string.Empty;
        RoomViewObjectComboBox.ItemsSource = null;
        RoomViewObjectComboBox.SelectedItem = null;
        RoomOpenViewObjectButton.IsEnabled = false;
        RoomViewEditorPanel.Visibility = Visibility.Collapsed;
    }

    private void UpdateRoomViewPositionEditor(UndertaleRoom.View view)
    {
        RoomViewXBox.Text = view.ViewX.ToString(CultureInfo.InvariantCulture);
        RoomViewYBox.Text = view.ViewY.ToString(CultureInfo.InvariantCulture);
    }

    private void RoomViewsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RoomViewSummary summary)
        {
            RoomViewsList.SelectedItem = summary;
            UpdateRoomViewButtons(summary);
            RefreshRoomViewEditor(summary);
        }
    }

    private void RoomViewsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomViewEditor)
            return;

        RoomViewSummary? summary = RoomViewsList.SelectedItem as RoomViewSummary;
        UpdateRoomViewButtons(summary);
        RefreshRoomViewEditor(summary);
    }

    private void RoomViewsList_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Delete)
            return;

        RoomDisableViewButton_Click(sender, e);
        e.Handled = true;
    }

    private async void RoomDisableViewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomViewsList.SelectedItem is not RoomViewSummary summary)
        {
            return;
        }

        summary.View.Enabled = false;
        await RefreshRoomViewAfterEditAsync(room, summary.View, "Disabled room view.");
    }

    private void RoomOpenViewObjectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            RoomViewsList.SelectedItem is not RoomViewSummary summary ||
            summary.View.ObjectId is null)
        {
            return;
        }

        int objectIndex = _data.GameObjects.IndexOf(summary.View.ObjectId);
        if (objectIndex >= 0)
            NavigateToResource("Objects", objectIndex);
    }

    private async void RoomViewEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingRoomViewEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomViewsList.SelectedItem is not RoomViewSummary summary)
        {
            return;
        }

        bool enabled = RoomViewEnabledCheckBox.IsChecked == true;
        if (summary.View.Enabled == enabled)
            return;

        summary.View.Enabled = enabled;
        await RefreshRoomViewAfterEditAsync(room, summary.View, "Updated room view enabled state.");
    }

    private async void RoomViewBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingRoomViewEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomViewsList.SelectedItem is not RoomViewSummary summary)
        {
            return;
        }

        try
        {
            UndertaleRoom.View view = summary.View;
            int viewX = ParseRoomInstanceInt(RoomViewXBox.Text, "View X");
            int viewY = ParseRoomInstanceInt(RoomViewYBox.Text, "View Y");
            int viewWidth = ParseRoomInstanceInt(RoomViewWidthBox.Text, "View width");
            int viewHeight = ParseRoomInstanceInt(RoomViewHeightBox.Text, "View height");
            int portX = ParseRoomInstanceInt(RoomViewPortXBox.Text, "Port X");
            int portY = ParseRoomInstanceInt(RoomViewPortYBox.Text, "Port Y");
            int portWidth = ParseRoomInstanceInt(RoomViewPortWidthBox.Text, "Port width");
            int portHeight = ParseRoomInstanceInt(RoomViewPortHeightBox.Text, "Port height");
            uint borderX = ParseRoomViewUInt(RoomViewBorderXBox.Text, "Border X");
            uint borderY = ParseRoomViewUInt(RoomViewBorderYBox.Text, "Border Y");
            int speedX = ParseRoomInstanceInt(RoomViewSpeedXBox.Text, "Speed X");
            int speedY = ParseRoomInstanceInt(RoomViewSpeedYBox.Text, "Speed Y");

            if (view.ViewX == viewX &&
                view.ViewY == viewY &&
                view.ViewWidth == viewWidth &&
                view.ViewHeight == viewHeight &&
                view.PortX == portX &&
                view.PortY == portY &&
                view.PortWidth == portWidth &&
                view.PortHeight == portHeight &&
                view.BorderX == borderX &&
                view.BorderY == borderY &&
                view.SpeedX == speedX &&
                view.SpeedY == speedY)
            {
                return;
            }

            view.ViewX = viewX;
            view.ViewY = viewY;
            view.ViewWidth = viewWidth;
            view.ViewHeight = viewHeight;
            view.PortX = portX;
            view.PortY = portY;
            view.PortWidth = portWidth;
            view.PortHeight = portHeight;
            view.BorderX = borderX;
            view.BorderY = borderY;
            view.SpeedX = speedX;
            view.SpeedY = speedY;
            await RefreshRoomViewAfterEditAsync(room, view, "Updated room view.");
        }
        catch (Exception ex)
        {
            RefreshRoomViewEditor(summary);
            StatusBox.Text = ex.Message;
        }
    }

    private async void RoomViewObjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomViewEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomViewsList.SelectedItem is not RoomViewSummary summary ||
            RoomViewObjectComboBox.SelectedItem is not ObjectReferenceItem objectItem)
        {
            return;
        }

        if (ReferenceEquals(summary.View.ObjectId, objectItem.Object))
            return;

        summary.View.ObjectId = objectItem.Object;
        await RefreshRoomViewAfterEditAsync(room, summary.View, "Updated room view follow object.");
    }

    private async System.Threading.Tasks.Task RefreshRoomViewAfterEditAsync(
        UndertaleRoom room,
        UndertaleRoom.View view,
        string status)
    {
        MarkDirty();
        RefreshRoomViews(room, view);
        RoomOverviewText.Text = BuildRoomOverview(room);
        RoomInstanceSummary[] instanceSummaries = BuildRoomInstanceSummaries(room).ToArray();
        if (_isRoomPreviewRendered)
            await RefreshRoomPreviewAsync(room, instanceSummaries);
        else
            ClearRoomPreviewSurface(room, instanceSummaries);
        RefreshCurrentDetails();
        StatusBox.Text = status;
    }

    private static uint ParseRoomViewUInt(string value, string label)
    {
        if (!uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint result))
            throw new InvalidDataException($"{label} must be a non-negative integer.");

        return result;
    }

    private void UpdateRoomInstancePositionEditor(UndertaleRoom.GameObject instance)
    {
        _isUpdatingRoomInstanceEditor = true;
        RoomInstanceXBox.Text = instance.X.ToString(CultureInfo.InvariantCulture);
        RoomInstanceYBox.Text = instance.Y.ToString(CultureInfo.InvariantCulture);
        _isUpdatingRoomInstanceEditor = false;
    }

    private void RoomOpenInstanceObjectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            RoomInstancesList.SelectedItem is not RoomInstanceSummary summary ||
            summary.ObjectDefinition is null)
        {
            return;
        }

        int objectIndex = _data.GameObjects.IndexOf(summary.ObjectDefinition);
        if (objectIndex >= 0)
            NavigateToResource("Objects", objectIndex);
    }

    private void RoomOpenInstanceCodeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            RoomInstancesList.SelectedItem is not RoomInstanceSummary summary ||
            summary.CreationCode is null)
        {
            return;
        }

        int codeIndex = _data.Code.IndexOf(summary.CreationCode);
        if (codeIndex >= 0)
            NavigateToResource("Code", codeIndex);
    }

    private async void RoomAddInstanceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room)
        {
            return;
        }

        UndertaleGameObject? objectDefinition =
            RoomInstanceObjectComboBox.SelectedItem is ObjectReferenceItem selectedObject
                ? selectedObject.Object
                : _data.GameObjects.FirstOrDefault();
        if (objectDefinition is null)
        {
            StatusBox.Text = "Cannot add a room instance because there are no objects.";
            return;
        }

        UndertaleRoom.GameObject instance = new()
        {
            X = 0,
            Y = 0,
            ObjectDefinition = objectDefinition,
            InstanceID = _data.GeneralInfo.LastObj++,
            ScaleX = 1,
            ScaleY = 1,
            Color = 0xFFFFFFFF,
            ImageSpeed = 1,
            ImageIndex = 0
        };
        room.GameObjects.Add(instance);
        AddRoomInstanceCreationOrder(room, instance);
        AddRoomInstanceToDefaultLayer(room, instance, preferredLayer: GetSelectedRoomInstanceLayer());
        await RefreshRoomInstanceAfterEditAsync(room, instance, "Added room instance.");
    }

    private async void RoomDuplicateInstanceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomInstancesList.SelectedItem is not RoomInstanceSummary summary)
        {
            return;
        }

        UndertaleRoom.GameObject source = summary.Instance;
        UndertaleRoom.GameObject duplicate = new()
        {
            X = source.X + 16,
            Y = source.Y + 16,
            ObjectDefinition = source.ObjectDefinition,
            InstanceID = _data.GeneralInfo.LastObj++,
            CreationCode = source.CreationCode,
            ScaleX = source.ScaleX,
            ScaleY = source.ScaleY,
            Color = source.Color,
            Rotation = source.Rotation,
            PreCreateCode = source.PreCreateCode,
            ImageSpeed = source.ImageSpeed,
            ImageIndex = source.ImageIndex
        };

        int sourceIndex = room.GameObjects.IndexOf(source);
        int insertIndex = sourceIndex >= 0 ? sourceIndex + 1 : room.GameObjects.Count;
        room.GameObjects.Insert(insertIndex, duplicate);
        AddRoomInstanceCreationOrder(room, duplicate, source.InstanceID);
        AddRoomInstanceToDefaultLayer(room, duplicate, source, GetSelectedRoomInstanceLayer());
        await RefreshRoomInstanceAfterEditAsync(room, duplicate, "Duplicated room instance.");
    }

    private async void RoomRemoveInstanceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomInstancesList.SelectedItem is not RoomInstanceSummary summary)
        {
            return;
        }

        UndertaleRoom.GameObject instance = summary.Instance;
        int index = room.GameObjects.IndexOf(instance);
        if (index < 0)
            return;

        room.GameObjects.RemoveAt(index);
        RemoveRoomInstanceFromLayerReferences(room, instance);
        room.InstanceCreationOrderIDs?.InstanceIDs.Remove(instance.InstanceID);

        RoomInstanceSummary[] summaries = BuildRoomInstanceSummaries(room).ToArray();
        RoomInstancesList.ItemsSource = summaries;
        RoomInstanceSummary? nextSelection = summaries.Length == 0 ? null : summaries[Math.Clamp(index, 0, summaries.Length - 1)];
        RoomInstancesList.SelectedItem = nextSelection;
        UpdateRoomInstanceButtons(nextSelection);
        RefreshRoomInstanceEditor(nextSelection);
        MarkDirty();
        if (_isRoomPreviewRendered)
            await RefreshRoomPreviewAsync(room, summaries);
        else
            ClearRoomPreviewSurface(room, summaries);
        RefreshCurrentDetails();
        StatusBox.Text = "Removed room instance.";
    }

    private async void RoomMoveInstanceUpButton_Click(object sender, RoutedEventArgs e)
    {
        await MoveSelectedRoomInstanceAsync(-1);
    }

    private async void RoomMoveInstanceDownButton_Click(object sender, RoutedEventArgs e)
    {
        await MoveSelectedRoomInstanceAsync(1);
    }

    private async System.Threading.Tasks.Task PasteCopiedRoomInstanceAsync(UndertaleRoom room)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _copiedRoomItem is not UndertaleRoom.GameObject source)
        {
            StatusBox.Text = "No room instance copied.";
            return;
        }

        RoomInstanceSummary? selectedSummary = RoomInstancesList.SelectedItem as RoomInstanceSummary;
        UndertaleRoom.GameObject? insertAfter = selectedSummary?.Instance ?? source;
        UndertaleRoom.GameObject duplicate = new()
        {
            X = source.X + 16,
            Y = source.Y + 16,
            ObjectDefinition = source.ObjectDefinition,
            InstanceID = _data.GeneralInfo.LastObj++,
            CreationCode = source.CreationCode,
            ScaleX = source.ScaleX,
            ScaleY = source.ScaleY,
            Color = source.Color,
            Rotation = source.Rotation,
            PreCreateCode = source.PreCreateCode,
            ImageSpeed = source.ImageSpeed,
            ImageIndex = source.ImageIndex
        };

        int insertIndex = insertAfter is null ? -1 : room.GameObjects.IndexOf(insertAfter);
        if (insertIndex < 0)
            insertIndex = room.GameObjects.IndexOf(source);
        if (insertIndex < 0)
            room.GameObjects.Add(duplicate);
        else
            room.GameObjects.Insert(Math.Min(insertIndex + 1, room.GameObjects.Count), duplicate);

        AddRoomInstanceCreationOrder(room, duplicate, insertAfter?.InstanceID ?? source.InstanceID);
        AddRoomInstanceToDefaultLayer(room, duplicate, insertAfter ?? source, GetSelectedRoomInstanceLayer());
        await RefreshRoomInstanceAfterEditAsync(room, duplicate, "Pasted room instance.");
    }

    private async System.Threading.Tasks.Task MoveSelectedRoomInstanceAsync(int delta)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomInstancesList.SelectedItem is not RoomInstanceSummary summary)
        {
            return;
        }

        UndertaleRoom.GameObject instance = summary.Instance;
        bool moved = MoveListItem(room.GameObjects, instance, delta);
        UndertaleRoom.Layer? layer = GetRoomInstanceLayer(room, instance);
        if (layer?.InstancesData?.Instances is not null)
            moved |= MoveListItem(layer.InstancesData.Instances, instance, delta);
        if (room.InstanceCreationOrderIDs?.InstanceIDs is { } orderIds)
            moved |= MoveListItem(orderIds, instance.InstanceID, delta);
        if (!moved)
            return;

        await RefreshRoomInstanceAfterEditAsync(room, instance, delta < 0 ? "Moved room instance up." : "Moved room instance down.");
    }

    private async System.Threading.Tasks.Task FlipSelectedRoomInstanceAsync(bool horizontal)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomInstancesList.SelectedItem is not RoomInstanceSummary summary)
        {
            return;
        }

        UndertaleRoom.GameObject instance = summary.Instance;
        UndertaleSprite? sprite = instance.ObjectDefinition?.Sprite;
        if (sprite is null)
        {
            StatusBox.Text = "Cannot flip room instance without a sprite.";
            return;
        }

        if (horizontal)
        {
            instance.ScaleX *= -1;
            instance.X -= ((int)sprite.Width - sprite.OriginX) * (int)instance.ScaleX;
        }
        else
        {
            instance.ScaleY *= -1;
            instance.Y -= ((int)sprite.Height - sprite.OriginY) * (int)instance.ScaleY;
        }

        await RefreshRoomInstanceAfterEditAsync(room, instance, horizontal ? "Flipped room instance horizontally." : "Flipped room instance vertically.");
    }

    private async void RoomInstanceEditorBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingRoomInstanceEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomInstancesList.SelectedItem is not RoomInstanceSummary summary ||
            sender is not TextBox textBox)
        {
            return;
        }

        UndertaleRoom.GameObject instance = summary.Instance;
        try
        {
            if (textBox == RoomInstanceXBox)
                instance.X = ParseRoomInstanceInt(textBox.Text, nameof(instance.X));
            else if (textBox == RoomInstanceYBox)
                instance.Y = ParseRoomInstanceInt(textBox.Text, nameof(instance.Y));
            else if (textBox == RoomInstanceScaleXBox)
                instance.ScaleX = ParseRoomInstanceFloat(textBox.Text, nameof(instance.ScaleX));
            else if (textBox == RoomInstanceScaleYBox)
                instance.ScaleY = ParseRoomInstanceFloat(textBox.Text, nameof(instance.ScaleY));
            else if (textBox == RoomInstanceImageIndexBox)
                instance.ImageIndex = ParseRoomInstanceInt(textBox.Text, nameof(instance.ImageIndex));
            else if (textBox == RoomInstanceImageSpeedBox)
                instance.ImageSpeed = ParseRoomInstanceFloat(textBox.Text, nameof(instance.ImageSpeed));
            else if (textBox == RoomInstanceRotationBox)
                instance.Rotation = ParseRoomInstanceFloat(textBox.Text, nameof(instance.Rotation));
            else
                return;
        }
        catch (Exception ex)
        {
            StatusBox.Text = ex.Message;
            RefreshRoomInstanceEditor(summary);
            return;
        }

        await RefreshRoomInstanceAfterEditAsync(room, instance, "Updated room instance.");
    }

    private async void RoomInstanceReferenceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomInstanceEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomInstancesList.SelectedItem is not RoomInstanceSummary summary)
        {
            return;
        }

        UndertaleRoom.GameObject instance = summary.Instance;
        if (ReferenceEquals(sender, RoomInstanceObjectComboBox) &&
            RoomInstanceObjectComboBox.SelectedItem is ObjectReferenceItem objectItem &&
            objectItem.Object is not null)
        {
            instance.ObjectDefinition = objectItem.Object;
        }
        else if (ReferenceEquals(sender, RoomInstanceCreationCodeComboBox) &&
                 RoomInstanceCreationCodeComboBox.SelectedItem is CodeReferenceItem codeItem)
        {
            instance.CreationCode = codeItem.Code;
        }
        else
        {
            return;
        }

        await RefreshRoomInstanceAfterEditAsync(room, instance, "Updated room instance reference.");
    }

    private async void RoomInstanceLayerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRoomInstanceEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleRoom room ||
            RoomInstancesList.SelectedItem is not RoomInstanceSummary summary ||
            RoomInstanceLayerComboBox.SelectedItem is not RoomInstanceLayerItem layerItem)
        {
            return;
        }

        UndertaleRoom.GameObject instance = summary.Instance;
        UndertaleRoom.Layer? currentLayer = GetRoomInstanceLayer(room, instance);
        if (ReferenceEquals(currentLayer, layerItem.Layer))
            return;

        MoveRoomInstanceToLayer(room, instance, layerItem.Layer);
        await RefreshRoomInstanceAfterEditAsync(room, instance, "Moved room instance to layer.");
    }

    private async System.Threading.Tasks.Task RefreshRoomInstanceAfterEditAsync(
        UndertaleRoom room,
        UndertaleRoom.GameObject instance,
        string status)
    {
        MarkDirty();
        RoomInstanceSummary[] summaries = BuildRoomInstanceSummaries(room).ToArray();
        RoomInstancesList.ItemsSource = summaries;
        RoomInstanceSummary? updatedSummary = summaries.FirstOrDefault(item => ReferenceEquals(item.Instance, instance));
        RoomInstancesList.SelectedItem = updatedSummary;
        UpdateRoomInstanceButtons(updatedSummary);
        RefreshRoomInstanceEditor(updatedSummary);
        if (_isRoomPreviewRendered)
            await RefreshRoomPreviewAsync(room, summaries);
        else
            ClearRoomPreviewSurface(room, summaries);
        RefreshCurrentDetails();
        StatusBox.Text = status;
    }

    private static void AddRoomInstanceCreationOrder(
        UndertaleRoom room,
        UndertaleRoom.GameObject instance,
        uint? afterInstanceId = null)
    {
        if (room.InstanceCreationOrderIDs is null)
            return;

        int insertIndex = afterInstanceId is null
            ? room.InstanceCreationOrderIDs.InstanceIDs.Count
            : room.InstanceCreationOrderIDs.InstanceIDs.IndexOf(afterInstanceId.Value) + 1;
        if (insertIndex <= 0 || insertIndex > room.InstanceCreationOrderIDs.InstanceIDs.Count)
            room.InstanceCreationOrderIDs.InstanceIDs.Add(instance.InstanceID);
        else
            room.InstanceCreationOrderIDs.InstanceIDs.Insert(insertIndex, instance.InstanceID);
    }

    private UndertaleRoom.Layer? GetSelectedRoomInstanceLayer()
    {
        return RoomInstanceLayerComboBox.SelectedItem is RoomInstanceLayerItem item
            ? item.Layer
            : null;
    }

    private static UndertaleRoom.Layer? GetRoomInstanceLayer(UndertaleRoom room, UndertaleRoom.GameObject instance)
    {
        return room.Layers?
                   .FirstOrDefault(layer => layer.InstancesData?.Instances?.Contains(instance) == true);
    }

    private static void RemoveRoomInstanceFromLayerReferences(UndertaleRoom room, UndertaleRoom.GameObject instance)
    {
        foreach (UndertaleRoom.Layer layer in room.Layers ?? [])
        {
            if (layer.InstancesData is null)
                continue;

            layer.InstancesData.Instances?.Remove(instance);
        }
    }

    private static void MoveRoomInstanceToLayer(
        UndertaleRoom room,
        UndertaleRoom.GameObject instance,
        UndertaleRoom.Layer targetLayer)
    {
        RemoveRoomInstanceFromLayerReferences(room, instance);
        if (targetLayer.InstancesData?.Instances is null)
            return;

        if (!targetLayer.InstancesData.Instances.Contains(instance))
            targetLayer.InstancesData.Instances.Add(instance);
    }

    private static void AddRoomInstanceToDefaultLayer(
        UndertaleRoom room,
        UndertaleRoom.GameObject instance,
        UndertaleRoom.GameObject? source = null,
        UndertaleRoom.Layer? preferredLayer = null)
    {
        UndertaleRoom.Layer? targetLayer = preferredLayer?.InstancesData is not null ? preferredLayer : null;
        if (source is not null)
        {
            targetLayer ??= GetRoomInstanceLayer(room, source);
        }

        targetLayer ??= room.Layers?
                            .Where(layer => layer.LayerType == UndertaleRoom.LayerType.Instances)
                            .OrderBy(layer => layer.LayerDepth)
                            .FirstOrDefault();

        if (targetLayer?.InstancesData?.Instances is null)
            return;

        int insertIndex = source is null ? targetLayer.InstancesData.Instances.Count : targetLayer.InstancesData.Instances.IndexOf(source) + 1;
        if (insertIndex <= 0 || insertIndex > targetLayer.InstancesData.Instances.Count)
            targetLayer.InstancesData.Instances.Add(instance);
        else
            targetLayer.InstancesData.Instances.Insert(insertIndex, instance);
    }

    private static bool MoveListItem<T>(IList<T> list, T item, int delta)
    {
        int index = list.IndexOf(item);
        if (index < 0)
            return false;

        int newIndex = Math.Clamp(index + delta, 0, list.Count - 1);
        if (newIndex == index)
            return false;

        (list[index], list[newIndex]) = (list[newIndex], list[index]);
        return true;
    }

    private static int ParseRoomInstanceInt(string value, string label)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            throw new InvalidDataException($"{label} must be an integer.");

        return result;
    }

    private static float ParseRoomInstanceFloat(string value, string label)
    {
        if (!TryParseFloat(value, out float result))
            throw new InvalidDataException($"{label} must be a number.");

        return result;
    }

    private async void RoomPreviewOption_Changed(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingRoomPreviewOptions || _selectedResource?.Value is not UndertaleRoom room)
            return;

        RoomInstanceSummary[] summaries = BuildRoomInstanceSummaries(room).ToArray();
        if (_isRoomPreviewRendered)
            await RefreshRoomPreviewAsync(room, summaries);
        else
            ClearRoomPreviewSurface(room, summaries);
    }

    private async System.Threading.Tasks.Task RefreshRoomPreviewAsync(UndertaleRoom room, IReadOnlyList<RoomInstanceSummary> instanceSummaries)
    {
        _roomPreviewCts?.Cancel();
        CancellationTokenSource previewCts = new();
        _roomPreviewCts = previewCts;
        CancellationToken token = previewCts.Token;
        int generation = ++_roomPreviewGeneration;
        _isRoomPreviewRendered = true;
        RoomRenderPreviewButton.IsEnabled = false;
        RoomExportPreviewButton.IsEnabled = false;
        SetRoomPreviewZoomControlsEnabled(false);
        bool previewCanvasBuilt = false;

        try
        {
            await System.Threading.Tasks.Task.Delay(75, token);
            if (token.IsCancellationRequested || generation != _roomPreviewGeneration)
                return;

            RoomPreviewCanvas.Children.Clear();

            double roomWidth = Math.Max(1, room.Width);
            double roomHeight = Math.Max(1, room.Height);
            double scale = Math.Min(1, Math.Min(760 / roomWidth, 320 / roomHeight));
            scale = Math.Max(0.1, scale);
            double previewWidth = Math.Max(240, roomWidth * scale);
            double previewHeight = Math.Max(160, roomHeight * scale);
            RoomPreviewCanvas.Width = previewWidth;
            RoomPreviewCanvas.Height = previewHeight;
            _roomPreviewScale = scale;
            _lastRoomPreviewPointerPosition = null;

            bool showBackgrounds = RoomPreviewBackgroundsCheckBox.IsChecked == true;
            bool showInstances = RoomPreviewInstancesCheckBox.IsChecked == true;
            bool showTiles = RoomPreviewTilesCheckBox.IsChecked == true;
            bool showViews = RoomPreviewViewsCheckBox.IsChecked == true;
            bool showLabels = RoomPreviewInstanceLabelsCheckBox.IsChecked == true;
            IReadOnlyList<RoomInstanceSummary> previewInstanceSummaries = instanceSummaries;
            int skippedLegacyInstances = 0;
            if (showInstances && instanceSummaries.Count > RoomPreviewLegacyInstanceLimit)
            {
                previewInstanceSummaries = instanceSummaries.Take(RoomPreviewLegacyInstanceLimit).ToArray();
                skippedLegacyInstances = instanceSummaries.Count - previewInstanceSummaries.Count;
            }

            IReadOnlyList<RoomPreviewTileSummary> previewTileSummaries = showTiles
                ? BuildRoomPreviewTileSummaries(room, RoomPreviewTileLimit + 1)
                : [];
            bool skippedTiles = previewTileSummaries.Count > RoomPreviewTileLimit;
            if (skippedTiles)
                previewTileSummaries = previewTileSummaries.Take(RoomPreviewTileLimit).ToArray();

            IReadOnlyList<RoomViewSummary> viewSummaries = showViews
                ? BuildRoomViewSummaries(room).Where(summary => summary.View.Enabled).ToArray()
                : [];
            IReadOnlyList<RoomPreviewAssetSpriteSummary> assetSpriteSummaries = showInstances
                ? BuildRoomPreviewAssetSpriteSummaries(room)
                : [];
            IReadOnlyList<RoomPreviewSequenceSummary> sequenceSummaries = showInstances
                ? BuildRoomPreviewSequenceSummaries(room)
                : [];
            IReadOnlyList<RoomPreviewParticleSummary> particleSummaries = showInstances
                ? BuildRoomPreviewParticleSummaries(room)
                : [];
            RoomPreviewInfoText.Text = $"Showing {previewInstanceSummaries.Count} of {instanceSummaries.Count} legacy instance(s), {assetSpriteSummaries.Count} asset sprite(s), {sequenceSummaries.Count} sequence marker(s), {particleSummaries.Count} particle marker(s), and {viewSummaries.Count} view(s) at {FormatFloat((float)(scale * 100))}% scale.";
            if (skippedLegacyInstances > 0 || skippedTiles)
            {
                List<string> cappedParts = [];
                if (skippedLegacyInstances > 0)
                    cappedParts.Add($"{skippedLegacyInstances} legacy instance marker(s)");
                if (skippedTiles)
                    cappedParts.Add("additional tile preview(s)");

                RoomPreviewInfoText.Text += $" Preview capped; skipped {string.Join(", ", cappedParts)}.";
            }

            Microsoft.UI.Xaml.Shapes.Rectangle background = new()
            {
                Width = previewWidth,
                Height = previewHeight,
                Fill = new SolidColorBrush(Microsoft.UI.Colors.Black),
                Stroke = new SolidColorBrush(Microsoft.UI.Colors.DimGray),
                StrokeThickness = 1
            };
            RoomPreviewCanvas.Children.Add(background);
            previewCanvasBuilt = true;

            List<RoomPreviewImageRequest> imageRequests = new();
            if (showBackgrounds)
            {
                foreach (RoomPreviewBackgroundSummary summary in BuildRoomPreviewBackgroundSummaries(room, foreground: false))
                    AddRoomPreviewBackground(summary, scale, previewWidth, previewHeight, imageRequests);
            }

            List<RoomPreviewSpriteRequest> spriteRequests = new();
            List<RoomPreviewTileRequest> tileRequests = new();
            if (showTiles)
            {
                foreach (RoomPreviewTileSummary summary in previewTileSummaries)
                    AddRoomPreviewTile(summary, scale, previewWidth, previewHeight, tileRequests);
            }

            if (showInstances)
            {
                foreach (RoomPreviewAssetSpriteSummary summary in assetSpriteSummaries)
                    AddRoomPreviewAssetSprite(summary, scale, previewWidth, previewHeight, spriteRequests);

                foreach (RoomPreviewSequenceSummary summary in sequenceSummaries)
                    AddRoomPreviewSequenceMarker(summary, scale, previewWidth, previewHeight);

                foreach (RoomPreviewParticleSummary summary in particleSummaries)
                    AddRoomPreviewParticleMarker(summary, scale, previewWidth, previewHeight);

                foreach (RoomInstanceSummary summary in previewInstanceSummaries)
                {
                    UndertaleRoom.GameObject instance = summary.Instance;
                    double x = instance.XOffset * scale;
                    double y = instance.YOffset * scale;
                    double width = GetRoomInstancePreviewWidth(instance) * scale;
                    double height = GetRoomInstancePreviewHeight(instance) * scale;
                    width = Math.Clamp(width, 8, Math.Max(8, previewWidth));
                    height = Math.Clamp(height, 8, Math.Max(8, previewHeight));

                    Grid marker = new()
                    {
                        Width = width,
                        Height = height,
                        MinWidth = 8,
                        MinHeight = 8,
                        Tag = new RoomPreviewInstanceMarker(summary, scale),
                        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
                    };
                    marker.PointerPressed += RoomPreviewInstance_PointerPressed;
                    marker.PointerMoved += RoomPreviewInstance_PointerMoved;
                    marker.PointerReleased += RoomPreviewInstance_PointerReleased;
                    marker.PointerCanceled += RoomPreviewInstance_PointerCanceled;
                    marker.PointerCaptureLost += RoomPreviewInstance_PointerCanceled;

                    if (TryGetRoomInstancePreviewTexture(instance, out UndertaleTexturePageItem? texture) && texture is not null)
                    {
                        Image image = new()
                        {
                            Stretch = Stretch.Fill,
                            Opacity = 0.9
                        };
                        marker.Children.Add(image);
                        spriteRequests.Add(new RoomPreviewSpriteRequest(image, texture));
                    }

                    marker.Children.Add(new Microsoft.UI.Xaml.Shapes.Rectangle
                    {
                        Stroke = CreateAccentBrush(),
                        Fill = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                        StrokeThickness = 1
                    });

                    if (showLabels && width >= 36 && height >= 18)
                    {
                        marker.Children.Add(new TextBlock
                        {
                            Text = summary.PreviewLabel,
                            Margin = new Thickness(4, 1, 4, 1),
                            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                            TextTrimming = Microsoft.UI.Xaml.TextTrimming.CharacterEllipsis
                        });
                    }

                    Canvas.SetLeft(marker, Math.Clamp(x, 0, Math.Max(0, previewWidth - 8)));
                    Canvas.SetTop(marker, Math.Clamp(y, 0, Math.Max(0, previewHeight - 8)));
                    RoomPreviewCanvas.Children.Add(marker);
                }
            }

            if (showBackgrounds)
            {
                foreach (RoomPreviewBackgroundSummary summary in BuildRoomPreviewBackgroundSummaries(room, foreground: true))
                    AddRoomPreviewBackground(summary, scale, previewWidth, previewHeight, imageRequests);
            }

            foreach (RoomViewSummary summary in viewSummaries)
                AddRoomPreviewViewMarker(summary, scale, previewWidth, previewHeight, showLabels);

            if (spriteRequests.Count == 0 && imageRequests.Count == 0 && tileRequests.Count == 0)
            {
                if (sequenceSummaries.Count == 0 && particleSummaries.Count == 0 && viewSummaries.Count == 0)
                    RoomPreviewInfoText.Text += " No preview textures were available for rendering.";
                else
                    RoomPreviewInfoText.Text += $" Rendered {sequenceSummaries.Count} sequence marker(s), {particleSummaries.Count} particle marker(s), and {viewSummaries.Count} view marker(s).";
                return;
            }

            try
            {
                UndertaleTexturePageItem[] textures = spriteRequests.Select(request => request.Texture)
                                                                    .Concat(imageRequests.Select(request => request.Texture))
                                                                    .Distinct()
                                                                    .ToArray();
                RoomPreviewTileKey[] tileKeys = tileRequests.Select(request => request.TileKey).Distinct().ToArray();
                (Dictionary<UndertaleTexturePageItem, byte[]> textureBytes, Dictionary<RoomPreviewTileKey, byte[]> tileBytes) = await System.Threading.Tasks.Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    Dictionary<UndertaleTexturePageItem, byte[]> result = new();
                    foreach (UndertaleTexturePageItem texture in textures)
                    {
                        token.ThrowIfCancellationRequested();
                        result[texture] = GetCachedTexturePageItemPreviewPng(texture);
                    }

                    Dictionary<RoomPreviewTileKey, byte[]> tileResult = new();
                    foreach (RoomPreviewTileKey key in tileKeys)
                    {
                        token.ThrowIfCancellationRequested();
                        tileResult[key] = GetCachedRoomTilePreviewPng(key);
                    }
                    return (result, tileResult);
                }, token);

                if (token.IsCancellationRequested || generation != _roomPreviewGeneration)
                    return;

                int renderedCount = 0;
                foreach (RoomPreviewSpriteRequest request in spriteRequests)
                {
                    if (!textureBytes.TryGetValue(request.Texture, out byte[]? bytes))
                        continue;

                    request.Image.Source = LoadBitmapImage(bytes);
                    renderedCount++;
                }

                int renderedBackgroundCount = 0;
                foreach (RoomPreviewImageRequest request in imageRequests)
                {
                    if (!textureBytes.TryGetValue(request.Texture, out byte[]? bytes))
                        continue;

                    request.Image.Source = LoadBitmapImage(bytes);
                    renderedBackgroundCount++;
                }

                int renderedTileCount = 0;
                foreach (RoomPreviewTileRequest request in tileRequests)
                {
                    if (!tileBytes.TryGetValue(request.TileKey, out byte[]? bytes))
                        continue;

                    request.Image.Source = LoadBitmapImage(bytes);
                    renderedTileCount++;
                }

                RoomPreviewInfoText.Text += $" Rendered {renderedBackgroundCount} background preview(s), {renderedTileCount} tile preview(s), {renderedCount} sprite preview(s), {sequenceSummaries.Count} sequence marker(s), {particleSummaries.Count} particle marker(s), and {viewSummaries.Count} view marker(s).";
            }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested || generation != _roomPreviewGeneration)
                    return;

                RoomPreviewInfoText.Text += $" Texture rendering failed: {ex.Message}";
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(_roomPreviewCts, previewCts))
            {
                _roomPreviewCts = null;
                if (ReferenceEquals(_selectedResource?.Value, room))
                {
                    RoomRenderPreviewButton.IsEnabled = true;
                    RoomExportPreviewButton.IsEnabled = _isRoomPreviewRendered &&
                                                        previewCanvasBuilt &&
                                                        generation == _roomPreviewGeneration &&
                                                        RoomPreviewCanvas.Children.Count > 0;
                    SetRoomPreviewZoomControlsEnabled(RoomExportPreviewButton.IsEnabled);
                }
            }

            previewCts.Dispose();
        }
    }

    private void AddRoomPreviewBackground(
        RoomPreviewBackgroundSummary summary,
        double scale,
        double previewWidth,
        double previewHeight,
        List<RoomPreviewImageRequest> imageRequests)
    {
        double x = summary.X * scale;
        double y = summary.Y * scale;
        double width = Math.Max(8, summary.Width * scale);
        double height = Math.Max(8, summary.Height * scale);
        Image image = new()
        {
            Width = Math.Clamp(width, 8, Math.Max(8, previewWidth * 4)),
            Height = Math.Clamp(height, 8, Math.Max(8, previewHeight * 4)),
            Stretch = Stretch.Fill,
            Opacity = summary.Foreground ? 0.65 : 0.9
        };
        Canvas.SetLeft(image, Math.Clamp(x, -previewWidth, previewWidth));
        Canvas.SetTop(image, Math.Clamp(y, -previewHeight, previewHeight));
        RoomPreviewCanvas.Children.Add(image);
        imageRequests.Add(new RoomPreviewImageRequest(image, summary.Texture));

        if (!summary.TiledHorizontally && !summary.TiledVertically)
            return;

        int tileCount = 1;
        double startX = summary.TiledHorizontally ? x % width : x;
        double startY = summary.TiledVertically ? y % height : y;
        if (summary.TiledHorizontally && startX > 0)
            startX -= width;
        if (summary.TiledVertically && startY > 0)
            startY -= height;

        double tileY = summary.TiledVertically ? startY : y;
        while (tileY < previewHeight && tileCount < 256)
        {
            double tileX = summary.TiledHorizontally ? startX : x;
            while (tileX < previewWidth && tileCount < 256)
            {
                bool isOriginal = Math.Abs(tileX - x) < 0.001 && Math.Abs(tileY - y) < 0.001;
                if (!isOriginal)
                {
                    Image tileImage = new()
                    {
                        Width = Math.Clamp(width, 8, Math.Max(8, previewWidth * 4)),
                        Height = Math.Clamp(height, 8, Math.Max(8, previewHeight * 4)),
                        Stretch = Stretch.Fill,
                        Opacity = summary.Foreground ? 0.65 : 0.9
                    };
                    Canvas.SetLeft(tileImage, tileX);
                    Canvas.SetTop(tileImage, tileY);
                    RoomPreviewCanvas.Children.Add(tileImage);
                    imageRequests.Add(new RoomPreviewImageRequest(tileImage, summary.Texture));
                    tileCount++;
                }

                if (!summary.TiledHorizontally)
                    break;
                tileX += width;
            }

            if (!summary.TiledVertically)
                break;
            tileY += height;
        }
    }

    private void AddRoomPreviewTile(
        RoomPreviewTileSummary summary,
        double scale,
        double previewWidth,
        double previewHeight,
        List<RoomPreviewTileRequest> tileRequests)
    {
        double x = summary.X * scale;
        double y = summary.Y * scale;
        double width = Math.Max(4, summary.Width * scale);
        double height = Math.Max(4, summary.Height * scale);
        if (x > previewWidth || y > previewHeight || x + width < 0 || y + height < 0)
            return;

        Image image = new()
        {
            Width = Math.Clamp(width, 4, Math.Max(4, previewWidth)),
            Height = Math.Clamp(height, 4, Math.Max(4, previewHeight)),
            Stretch = Stretch.Fill,
            Opacity = 0.85
        };
        Canvas.SetLeft(image, Math.Clamp(x, -previewWidth, previewWidth));
        Canvas.SetTop(image, Math.Clamp(y, -previewHeight, previewHeight));
        RoomPreviewCanvas.Children.Add(image);
        tileRequests.Add(new RoomPreviewTileRequest(image, summary.TileKey));
    }

    private void AddRoomPreviewAssetSprite(
        RoomPreviewAssetSpriteSummary summary,
        double scale,
        double previewWidth,
        double previewHeight,
        List<RoomPreviewSpriteRequest> spriteRequests)
    {
        double x = summary.X * scale;
        double y = summary.Y * scale;
        double width = Math.Max(1, summary.Width * scale);
        double height = Math.Max(1, summary.Height * scale);
        double maxDimension = Math.Max(previewWidth, previewHeight) * 4;
        if (x > previewWidth + maxDimension || y > previewHeight + maxDimension || x + width < -maxDimension || y + height < -maxDimension)
            return;

        Image image = new()
        {
            Width = Math.Clamp(width, 1, Math.Max(1, maxDimension)),
            Height = Math.Clamp(height, 1, Math.Max(1, maxDimension)),
            Stretch = Stretch.Fill,
            Opacity = summary.Opacity,
            Tag = new RoomPreviewAssetMarker(summary.Layer, summary.Instance, scale),
            RenderTransform = new TransformGroup
            {
                Children =
                {
                    new ScaleTransform
                    {
                        ScaleX = summary.ScaleX,
                        ScaleY = summary.ScaleY,
                        CenterX = summary.TransformCenterX * scale,
                        CenterY = summary.TransformCenterY * scale
                    },
                    new RotateTransform
                    {
                        Angle = summary.OppositeRotation,
                        CenterX = summary.TransformCenterX * scale,
                        CenterY = summary.TransformCenterY * scale
                    }
                }
            }
        };
        image.PointerPressed += RoomPreviewAsset_PointerPressed;
        image.PointerMoved += RoomPreviewAsset_PointerMoved;
        image.PointerReleased += RoomPreviewAsset_PointerReleased;
        image.PointerCanceled += RoomPreviewAsset_PointerCanceled;
        image.PointerCaptureLost += RoomPreviewAsset_PointerCanceled;
        Canvas.SetLeft(image, Math.Clamp(x, -maxDimension, previewWidth + maxDimension));
        Canvas.SetTop(image, Math.Clamp(y, -maxDimension, previewHeight + maxDimension));
        RoomPreviewCanvas.Children.Add(image);
        spriteRequests.Add(new RoomPreviewSpriteRequest(image, summary.Texture));
    }

    private void AddRoomPreviewSequenceMarker(
        RoomPreviewSequenceSummary summary,
        double scale,
        double previewWidth,
        double previewHeight)
    {
        double width = Math.Clamp(Math.Max(24, summary.Width * scale), 24, Math.Max(24, previewWidth));
        double height = Math.Clamp(Math.Max(18, summary.Height * scale), 18, Math.Max(18, previewHeight));
        double x = (summary.X - summary.OriginX) * scale;
        double y = (summary.Y - summary.OriginY) * scale;
        double margin = Math.Max(previewWidth, previewHeight);
        if (x > previewWidth + margin || y > previewHeight + margin || x + width < -margin || y + height < -margin)
            return;

        Grid marker = new()
        {
            Width = width,
            Height = height,
            Opacity = summary.Opacity,
            Tag = new RoomPreviewAssetMarker(summary.Layer, summary.Instance, scale),
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            RenderTransform = new TransformGroup
            {
                Children =
                {
                    new ScaleTransform
                    {
                        ScaleX = summary.ScaleX,
                        ScaleY = summary.ScaleY,
                        CenterX = summary.OriginX * scale,
                        CenterY = summary.OriginY * scale
                    },
                    new RotateTransform
                    {
                        Angle = summary.OppositeRotation,
                        CenterX = summary.OriginX * scale,
                        CenterY = summary.OriginY * scale
                    }
                }
            }
        };
        marker.PointerPressed += RoomPreviewAsset_PointerPressed;
        marker.PointerMoved += RoomPreviewAsset_PointerMoved;
        marker.PointerReleased += RoomPreviewAsset_PointerReleased;
        marker.PointerCanceled += RoomPreviewAsset_PointerCanceled;
        marker.PointerCaptureLost += RoomPreviewAsset_PointerCanceled;

        marker.Children.Add(new Microsoft.UI.Xaml.Shapes.Rectangle
        {
            Stroke = new SolidColorBrush(Microsoft.UI.Colors.MediumPurple),
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 4, 3 },
            Fill = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
        });
        marker.Children.Add(new TextBlock
        {
            Text = "SEQ",
            Margin = new Thickness(4, 1, 4, 1),
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            TextTrimming = Microsoft.UI.Xaml.TextTrimming.CharacterEllipsis
        });

        Canvas.SetLeft(marker, Math.Clamp(x, -margin, previewWidth + margin));
        Canvas.SetTop(marker, Math.Clamp(y, -margin, previewHeight + margin));
        RoomPreviewCanvas.Children.Add(marker);
    }

    private void AddRoomPreviewParticleMarker(
        RoomPreviewParticleSummary summary,
        double scale,
        double previewWidth,
        double previewHeight)
    {
        double markerSize = Math.Max(6, 16 * scale);
        double markerX = summary.X * scale - markerSize / 2;
        double markerY = summary.Y * scale - markerSize / 2;
        double margin = Math.Max(previewWidth, previewHeight);
        if (markerX > previewWidth + margin || markerY > previewHeight + margin || markerX + markerSize < -margin || markerY + markerSize < -margin)
            return;

        Canvas marker = new()
        {
            Width = markerSize,
            Height = markerSize,
            Opacity = summary.Opacity,
            Tag = new RoomPreviewAssetMarker(summary.Layer, summary.Instance, scale),
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            RenderTransform = new TransformGroup
            {
                Children =
                {
                    new ScaleTransform
                    {
                        ScaleX = summary.ScaleX,
                        ScaleY = summary.ScaleY,
                        CenterX = markerSize / 2,
                        CenterY = markerSize / 2
                    },
                    new RotateTransform
                    {
                        Angle = summary.OppositeRotation,
                        CenterX = markerSize / 2,
                        CenterY = markerSize / 2
                    }
                }
            }
        };
        marker.PointerPressed += RoomPreviewAsset_PointerPressed;
        marker.PointerMoved += RoomPreviewAsset_PointerMoved;
        marker.PointerReleased += RoomPreviewAsset_PointerReleased;
        marker.PointerCanceled += RoomPreviewAsset_PointerCanceled;
        marker.PointerCaptureLost += RoomPreviewAsset_PointerCanceled;

        marker.Children.Add(new Microsoft.UI.Xaml.Shapes.Ellipse
        {
            Width = markerSize,
            Height = markerSize,
            Fill = new SolidColorBrush(Microsoft.UI.Colors.Silver)
        });

        AddParticleMarkerDot(marker, markerSize, 0.38, 0.38, 0.18);
        AddParticleMarkerLine(marker, markerSize, 0.22, 0.42, 0.56, 0.42);
        AddParticleMarkerLine(marker, markerSize, 0.42, 0.22, 0.42, 0.56);
        AddParticleMarkerDot(marker, markerSize, 0.63, 0.63, 0.14);
        AddParticleMarkerLine(marker, markerSize, 0.50, 0.65, 0.78, 0.65);
        AddParticleMarkerLine(marker, markerSize, 0.65, 0.50, 0.65, 0.78);

        if (summary.BoundsWidth > 0 && summary.BoundsHeight > 0)
        {
            Microsoft.UI.Xaml.Shapes.Rectangle bounds = new()
            {
                Width = Math.Max(1, summary.BoundsWidth * scale),
                Height = Math.Max(1, summary.BoundsHeight * scale),
                Stroke = new SolidColorBrush(Microsoft.UI.Colors.DarkCyan),
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };
            Canvas.SetLeft(bounds, summary.BoundsX * scale);
            Canvas.SetTop(bounds, summary.BoundsY * scale);
            marker.Children.Add(bounds);
        }

        Canvas.SetLeft(marker, Math.Clamp(markerX, -margin, previewWidth + margin));
        Canvas.SetTop(marker, Math.Clamp(markerY, -margin, previewHeight + margin));
        RoomPreviewCanvas.Children.Add(marker);
    }

    private void AddRoomPreviewViewMarker(
        RoomViewSummary summary,
        double scale,
        double previewWidth,
        double previewHeight,
        bool showLabels)
    {
        UndertaleRoom.View view = summary.View;
        double x = view.ViewX * scale;
        double y = view.ViewY * scale;
        double width = Math.Clamp(view.ViewWidth * scale, 8, Math.Max(8, previewWidth * 2));
        double height = Math.Clamp(view.ViewHeight * scale, 8, Math.Max(8, previewHeight * 2));

        Grid marker = new()
        {
            Width = width,
            Height = height,
            MinWidth = 8,
            MinHeight = 8,
            Tag = new RoomPreviewViewMarker(summary, scale),
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
        };
        marker.PointerPressed += RoomPreviewView_PointerPressed;
        marker.PointerMoved += RoomPreviewView_PointerMoved;
        marker.PointerReleased += RoomPreviewView_PointerReleased;
        marker.PointerCanceled += RoomPreviewView_PointerCanceled;
        marker.PointerCaptureLost += RoomPreviewView_PointerCanceled;

        marker.Children.Add(new Microsoft.UI.Xaml.Shapes.Rectangle
        {
            Stroke = new SolidColorBrush(Microsoft.UI.Colors.Silver),
            Fill = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(38, 255, 255, 255)),
            StrokeThickness = 1
        });

        if (showLabels && width >= 40 && height >= 18)
        {
            marker.Children.Add(new TextBlock
            {
                Text = $"View {summary.Index}",
                Margin = new Thickness(4, 1, 4, 1),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                TextTrimming = Microsoft.UI.Xaml.TextTrimming.CharacterEllipsis
            });
        }

        Canvas.SetLeft(marker, x);
        Canvas.SetTop(marker, y);
        Canvas.SetZIndex(marker, 2000);
        RoomPreviewCanvas.Children.Add(marker);
    }

    private void RoomPreviewView_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement markerElement ||
            markerElement.Tag is not RoomPreviewViewMarker marker)
            return;

        SelectRoomView(marker.Summary.View);

        if (_data is not null &&
            !_data.UnsupportedBytecodeVersion &&
            _selectedResource?.Value is UndertaleRoom)
        {
            Point pointerPosition = e.GetCurrentPoint(RoomPreviewCanvas).Position;
            _roomPreviewViewDragState = new RoomPreviewViewDragState(
                markerElement,
                marker.Summary,
                e.Pointer.PointerId,
                marker.Scale,
                pointerPosition,
                marker.Summary.View.ViewX,
                marker.Summary.View.ViewY,
                Canvas.GetLeft(markerElement),
                Canvas.GetTop(markerElement));
            markerElement.CapturePointer(e.Pointer);
        }

        e.Handled = true;
    }

    private void RoomPreviewView_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_roomPreviewViewDragState is null ||
            sender is not FrameworkElement marker ||
            !ReferenceEquals(marker, _roomPreviewViewDragState.Marker) ||
            e.Pointer.PointerId != _roomPreviewViewDragState.PointerId)
        {
            return;
        }

        Point pointerPosition = e.GetCurrentPoint(RoomPreviewCanvas).Position;
        double scale = Math.Max(0.01, _roomPreviewViewDragState.Scale);
        UndertaleRoom.View view = _roomPreviewViewDragState.Summary.View;
        int newX = _roomPreviewViewDragState.StartX + (int)Math.Round((pointerPosition.X - _roomPreviewViewDragState.StartPointer.X) / scale);
        int newY = _roomPreviewViewDragState.StartY + (int)Math.Round((pointerPosition.Y - _roomPreviewViewDragState.StartPointer.Y) / scale);

        if (view.ViewX == newX && view.ViewY == newY)
            return;

        view.ViewX = newX;
        view.ViewY = newY;
        _roomPreviewViewDragState.HasMoved = true;
        Canvas.SetLeft(marker, _roomPreviewViewDragState.StartCanvasLeft + (newX - _roomPreviewViewDragState.StartX) * scale);
        Canvas.SetTop(marker, _roomPreviewViewDragState.StartCanvasTop + (newY - _roomPreviewViewDragState.StartY) * scale);
        UpdateRoomViewPositionEditor(view);
        MarkDirty();
        StatusBox.Text = $"Moving room view to {newX},{newY}.";
        e.Handled = true;
    }

    private async void RoomPreviewView_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_roomPreviewViewDragState is null ||
            sender is not FrameworkElement marker ||
            !ReferenceEquals(marker, _roomPreviewViewDragState.Marker) ||
            e.Pointer.PointerId != _roomPreviewViewDragState.PointerId)
        {
            return;
        }

        RoomPreviewViewDragState dragState = _roomPreviewViewDragState;
        _roomPreviewViewDragState = null;
        marker.ReleasePointerCapture(e.Pointer);
        e.Handled = true;

        if (!dragState.HasMoved || _selectedResource?.Value is not UndertaleRoom room)
            return;

        PushRoomPreviewUndo(new RoomPreviewUndoState(dragState.Summary.View, dragState.StartX, dragState.StartY, Layer: null));
        await RefreshRoomViewAfterEditAsync(room, dragState.Summary.View, "Moved room view.");
    }

    private void RoomPreviewView_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (_roomPreviewViewDragState is null ||
            sender is not FrameworkElement marker ||
            !ReferenceEquals(marker, _roomPreviewViewDragState.Marker) ||
            e.Pointer.PointerId != _roomPreviewViewDragState.PointerId)
        {
            return;
        }

        marker.ReleasePointerCapture(e.Pointer);
        _roomPreviewViewDragState = null;
        e.Handled = true;
    }

    private void SelectRoomView(UndertaleRoom.View view)
    {
        RoomViewSummary? selected = (RoomViewsList.ItemsSource as IEnumerable)?
            .OfType<RoomViewSummary>()
            .FirstOrDefault(summary => ReferenceEquals(summary.View, view));

        if (selected is null)
            return;

        RoomViewsList.SelectedItem = selected;
        UpdateRoomViewButtons(selected);
        RefreshRoomViewEditor(selected);
    }

    private static void AddParticleMarkerDot(Canvas marker, double markerSize, double centerX, double centerY, double radius)
    {
        double size = Math.Max(1, markerSize * radius);
        Microsoft.UI.Xaml.Shapes.Ellipse dot = new()
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(Microsoft.UI.Colors.White)
        };
        Canvas.SetLeft(dot, markerSize * centerX - size / 2);
        Canvas.SetTop(dot, markerSize * centerY - size / 2);
        marker.Children.Add(dot);
    }

    private static void AddParticleMarkerLine(
        Canvas marker,
        double markerSize,
        double x1,
        double y1,
        double x2,
        double y2)
    {
        marker.Children.Add(new Microsoft.UI.Xaml.Shapes.Line
        {
            X1 = markerSize * x1,
            Y1 = markerSize * y1,
            X2 = markerSize * x2,
            Y2 = markerSize * y2,
            Stroke = new SolidColorBrush(Microsoft.UI.Colors.White),
            StrokeThickness = Math.Max(1, markerSize * 0.06)
        });
    }


    private void ShowBackgroundSummaryFor(ResourceItem item)
    {
        if (item.Value is not UndertaleBackground background)
        {
            HideBackgroundSummary();
            return;
        }

        _isUpdatingBackgroundEditor = true;
        try
        {
            BackgroundSummaryPanel.Visibility = Visibility.Visible;
            BackgroundSummaryText.Text = BuildBackgroundSummary(background);
            BackgroundOpenTextureButton.IsEnabled = background.Texture is not null;
            BackgroundOpenExportedSpriteButton.IsEnabled = background.GMS2ExportedSprite is not null;
            BackgroundAddTileIdButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion;
            RefreshBackgroundTileIds(background, selectedTileId: null);
        }
        finally
        {
            _isUpdatingBackgroundEditor = false;
        }
    }

    private void HideBackgroundSummary()
    {
        _isUpdatingBackgroundEditor = true;
        BackgroundSummaryText.Text = string.Empty;
        BackgroundOpenTextureButton.IsEnabled = false;
        BackgroundOpenExportedSpriteButton.IsEnabled = false;
        BackgroundAddTileIdButton.IsEnabled = false;
        BackgroundDuplicateTileIdButton.IsEnabled = false;
        BackgroundRemoveTileIdButton.IsEnabled = false;
        BackgroundTileIdsList.ItemsSource = null;
        BackgroundTileIdsList.SelectedItem = null;
        BackgroundTileIdBox.Text = string.Empty;
        BackgroundTileIdBox.Visibility = Visibility.Collapsed;
        BackgroundSummaryPanel.Visibility = Visibility.Collapsed;
        _isUpdatingBackgroundEditor = false;
    }

    private void BackgroundOpenTextureButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _selectedResource?.Value is not UndertaleBackground background || background.Texture is null)
            return;

        int textureIndex = _data.TexturePageItems.IndexOf(background.Texture);
        if (textureIndex >= 0)
            NavigateToResource("Texture page items", textureIndex);
    }

    private void BackgroundOpenExportedSpriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _selectedResource?.Value is not UndertaleBackground background || background.GMS2ExportedSprite is null)
            return;

        int spriteIndex = _data.Sprites.IndexOf(background.GMS2ExportedSprite);
        if (spriteIndex >= 0)
            NavigateToResource("Sprites", spriteIndex);
    }

    private void BackgroundTileIdsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is BackgroundTileIdSummary summary)
        {
            BackgroundTileIdsList.SelectedItem = summary;
            UpdateBackgroundTileIdEditor(summary);
        }
    }

    private void BackgroundTileIdsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingBackgroundEditor)
            return;

        UpdateBackgroundTileIdEditor(BackgroundTileIdsList.SelectedItem as BackgroundTileIdSummary);
    }

    private void BackgroundAddTileIdButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleBackground background)
        {
            return;
        }

        background.GMS2TileIds ??= new UndertaleObservableList<UndertaleBackground.TileID>();
        uint id = BackgroundTileIdsList.SelectedItem is BackgroundTileIdSummary selected
            ? selected.TileId.ID + 1
            : (uint)background.GMS2TileIds.Count;
        UndertaleBackground.TileID tileId = new() { ID = id };
        background.GMS2TileIds.Add(tileId);
        MarkDirty();
        RefreshBackgroundTileIds(background, tileId);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Added background tile ID.";
    }

    private void BackgroundDuplicateTileIdButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleBackground background ||
            BackgroundTileIdsList.SelectedItem is not BackgroundTileIdSummary summary)
        {
            return;
        }

        background.GMS2TileIds ??= new UndertaleObservableList<UndertaleBackground.TileID>();
        UndertaleBackground.TileID tileId = new() { ID = summary.TileId.ID };
        int insertIndex = Math.Clamp(summary.Index + 1, 0, background.GMS2TileIds.Count);
        background.GMS2TileIds.Insert(insertIndex, tileId);
        MarkDirty();
        RefreshBackgroundTileIds(background, tileId);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Duplicated background tile ID.";
    }

    private void BackgroundRemoveTileIdButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleBackground background ||
            BackgroundTileIdsList.SelectedItem is not BackgroundTileIdSummary summary ||
            summary.Index < 0 ||
            summary.Index >= background.GMS2TileIds.Count)
        {
            return;
        }

        int index = summary.Index;
        background.GMS2TileIds.RemoveAt(index);
        UndertaleBackground.TileID? nextSelection = background.GMS2TileIds.Count == 0
            ? null
            : background.GMS2TileIds[Math.Clamp(index, 0, background.GMS2TileIds.Count - 1)];
        MarkDirty();
        RefreshBackgroundTileIds(background, nextSelection);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Removed background tile ID.";
    }

    private void BackgroundTileIdBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingBackgroundEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleBackground background ||
            BackgroundTileIdsList.SelectedItem is not BackgroundTileIdSummary summary)
        {
            return;
        }

        if (!uint.TryParse(BackgroundTileIdBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint id))
        {
            UpdateBackgroundTileIdEditor(summary);
            StatusBox.Text = "Invalid tile ID. Use a non-negative integer.";
            return;
        }

        if (summary.TileId.ID == id)
            return;

        summary.TileId.ID = id;
        MarkDirty();
        RefreshBackgroundTileIds(background, summary.TileId);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        StatusBox.Text = "Updated background tile ID.";
    }

    private void RefreshBackgroundTileIds(UndertaleBackground background, UndertaleBackground.TileID? selectedTileId)
    {
        _isUpdatingBackgroundEditor = true;
        try
        {
            BackgroundTileIdSummary[] tileIds = BuildBackgroundTileIdSummaries(background).ToArray();
            BackgroundTileIdsList.ItemsSource = tileIds;
            BackgroundTileIdSummary? selected = selectedTileId is null
                ? tileIds.FirstOrDefault()
                : tileIds.FirstOrDefault(summary => ReferenceEquals(summary.TileId, selectedTileId));
            BackgroundTileIdsList.SelectedItem = selected;
            UpdateBackgroundTileIdEditor(selected);
            BackgroundSummaryText.Text = BuildBackgroundSummary(background);
            BackgroundOpenExportedSpriteButton.IsEnabled = background.GMS2ExportedSprite is not null;
        }
        finally
        {
            _isUpdatingBackgroundEditor = false;
        }
    }

    private void UpdateBackgroundTileIdEditor(BackgroundTileIdSummary? summary)
    {
        bool canEdit = _data is not null &&
                       !_data.UnsupportedBytecodeVersion &&
                       _selectedResource?.Value is UndertaleBackground;
        _isUpdatingBackgroundEditor = true;
        try
        {
            BackgroundDuplicateTileIdButton.IsEnabled = canEdit && summary is not null;
            BackgroundRemoveTileIdButton.IsEnabled = canEdit && summary is not null;
            if (summary is null)
            {
                BackgroundTileIdBox.Text = string.Empty;
                BackgroundTileIdBox.Visibility = Visibility.Collapsed;
                return;
            }

            BackgroundTileIdBox.Text = summary.TileId.ID.ToString(CultureInfo.InvariantCulture);
            BackgroundTileIdBox.Visibility = Visibility.Visible;
        }
        finally
        {
            _isUpdatingBackgroundEditor = false;
        }
    }

    private void ShowPathSummaryFor(ResourceItem item)
    {
        if (item.Value is not UndertalePath path)
        {
            HidePathSummary();
            return;
        }

        PathSummaryPanel.Visibility = Visibility.Visible;
        PathAddPointButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion;
        RefreshPathEditor(path, selectedPoint: null);
    }

    private void HidePathSummary()
    {
        _isUpdatingPathPointEditor = true;
        PathSummaryText.Text = string.Empty;
        PathPointsList.ItemsSource = null;
        PathPointsList.SelectedItem = null;
        PathPreviewCanvas.Children.Clear();
        PathPointXBox.Text = string.Empty;
        PathPointYBox.Text = string.Empty;
        PathPointSpeedBox.Text = string.Empty;
        PathPointEditorPanel.Visibility = Visibility.Collapsed;
        PathAddPointButton.IsEnabled = false;
        PathDuplicatePointButton.IsEnabled = false;
        PathRemovePointButton.IsEnabled = false;
        PathSummaryPanel.Visibility = Visibility.Collapsed;
        _isUpdatingPathPointEditor = false;
    }

    private void RefreshPathEditor(UndertalePath path, UndertalePath.PathPoint? selectedPoint)
    {
        PathSummaryText.Text = BuildPathSummary(path);

        PathPointSummary[] summaries = BuildPathPointSummaries(path).ToArray();
        PathPointsList.ItemsSource = summaries;

        PathPointSummary? selectedSummary = selectedPoint is null
            ? summaries.FirstOrDefault()
            : summaries.FirstOrDefault(summary => ReferenceEquals(summary.Point, selectedPoint));
        PathPointsList.SelectedItem = selectedSummary;

        bool canMutate = _data is not null && !_data.UnsupportedBytecodeVersion;
        PathDuplicatePointButton.IsEnabled = canMutate && selectedSummary is not null;
        PathRemovePointButton.IsEnabled = canMutate && selectedSummary is not null;
        UpdatePathPointEditor(selectedSummary);
        RenderPathPreview(path, selectedSummary?.Point);
    }

    private void UpdatePathPointEditor(PathPointSummary? summary)
    {
        _isUpdatingPathPointEditor = true;
        if (summary is null)
        {
            PathPointXBox.Text = string.Empty;
            PathPointYBox.Text = string.Empty;
            PathPointSpeedBox.Text = string.Empty;
            PathPointEditorPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            PathPointXBox.Text = FormatFloat(summary.Point.X);
            PathPointYBox.Text = FormatFloat(summary.Point.Y);
            PathPointSpeedBox.Text = FormatFloat(summary.Point.Speed);
            PathPointEditorPanel.Visibility = Visibility.Visible;
        }

        _isUpdatingPathPointEditor = false;
    }

    private void UpdatePathPointPositionEditor(UndertalePath.PathPoint point)
    {
        _isUpdatingPathPointEditor = true;
        PathPointXBox.Text = FormatFloat(point.X);
        PathPointYBox.Text = FormatFloat(point.Y);
        _isUpdatingPathPointEditor = false;
    }

    private void SelectPathPoint(UndertalePath path, PathPointSummary summary)
    {
        PathPointsList.SelectedItem = summary;
        PathDuplicatePointButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion;
        PathRemovePointButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion;
        UpdatePathPointEditor(summary);
        RenderPathPreview(path, summary.Point);
    }

    private PathPointSummary? FindPathPointSummary(UndertalePath.PathPoint point)
    {
        return PathPointsList.ItemsSource is IEnumerable<PathPointSummary> summaries
            ? summaries.FirstOrDefault(summary => ReferenceEquals(summary.Point, point))
            : null;
    }

    private void PathPointsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (_selectedResource?.Value is not UndertalePath path || e.ClickedItem is not PathPointSummary summary)
            return;

        SelectPathPoint(path, summary);
    }

    private void PathPreviewPoint_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement marker ||
            marker.Tag is not PathPreviewPointMarker markerData ||
            _selectedResource?.Value is not UndertalePath path)
        {
            return;
        }

        PathPointSummary? summary = FindPathPointSummary(markerData.Point);
        if (summary is not null)
            SelectPathPoint(path, summary);

        if (_data is not null && !_data.UnsupportedBytecodeVersion)
        {
            _pathPreviewDragState = new PathPreviewDragState(
                marker,
                markerData,
                e.Pointer.PointerId,
                e.GetCurrentPoint(PathPreviewCanvas).Position,
                markerData.Point.X,
                markerData.Point.Y);
            marker.CapturePointer(e.Pointer);
        }

        e.Handled = true;
    }

    private void PathPreviewPoint_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_pathPreviewDragState is null ||
            sender is not FrameworkElement marker ||
            !ReferenceEquals(marker, _pathPreviewDragState.Marker) ||
            e.Pointer.PointerId != _pathPreviewDragState.PointerId)
        {
            return;
        }

        Point pointerPosition = e.GetCurrentPoint(PathPreviewCanvas).Position;
        double scale = Math.Max(0.01, _pathPreviewDragState.MarkerData.Scale);
        UndertalePath.PathPoint point = _pathPreviewDragState.MarkerData.Point;
        float newX = _pathPreviewDragState.StartX + (float)((pointerPosition.X - _pathPreviewDragState.StartPointer.X) / scale);
        float newY = _pathPreviewDragState.StartY + (float)((pointerPosition.Y - _pathPreviewDragState.StartPointer.Y) / scale);

        if (Math.Abs(point.X - newX) < 0.001f && Math.Abs(point.Y - newY) < 0.001f)
            return;

        point.X = newX;
        point.Y = newY;
        _pathPreviewDragState.HasMoved = true;
        Canvas.SetLeft(marker, _pathPreviewDragState.MarkerData.MapX(point) - marker.Width / 2);
        Canvas.SetTop(marker, _pathPreviewDragState.MarkerData.MapY(point) - marker.Height / 2);
        UpdatePathPointPositionEditor(point);
        MarkDirty();
        StatusBox.Text = $"Moving path point to {FormatFloat(point.X)}, {FormatFloat(point.Y)}.";
        e.Handled = true;
    }

    private void PathPreviewPoint_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_pathPreviewDragState is null ||
            sender is not FrameworkElement marker ||
            !ReferenceEquals(marker, _pathPreviewDragState.Marker) ||
            e.Pointer.PointerId != _pathPreviewDragState.PointerId)
        {
            return;
        }

        PathPreviewDragState dragState = _pathPreviewDragState;
        _pathPreviewDragState = null;
        marker.ReleasePointerCapture(e.Pointer);
        e.Handled = true;

        if (!dragState.HasMoved || _selectedResource?.Value is not UndertalePath path)
            return;

        RefreshAfterPathMutation(path, dragState.MarkerData.Point, "Moved path point.");
    }

    private void PathPreviewPoint_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (_pathPreviewDragState is null ||
            sender is not FrameworkElement marker ||
            !ReferenceEquals(marker, _pathPreviewDragState.Marker) ||
            e.Pointer.PointerId != _pathPreviewDragState.PointerId)
        {
            return;
        }

        marker.ReleasePointerCapture(e.Pointer);
        _pathPreviewDragState = null;
        e.Handled = true;
    }

    private void PathAddPointButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _data.UnsupportedBytecodeVersion || _selectedResource?.Value is not UndertalePath path)
            return;

        UndertalePath.PathPoint point = new();
        if (path.Points.Count > 0)
        {
            UndertalePath.PathPoint previous = path.Points[^1];
            point.X = previous.X + 16;
            point.Y = previous.Y;
            point.Speed = previous.Speed;
        }

        path.Points.Add(point);
        MarkDirty();
        RefreshAfterPathMutation(path, point, "Added path point.");
    }

    private void PathDuplicatePointButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertalePath path ||
            PathPointsList.SelectedItem is not PathPointSummary summary)
        {
            return;
        }

        UndertalePath.PathPoint point = new()
        {
            X = summary.Point.X,
            Y = summary.Point.Y,
            Speed = summary.Point.Speed
        };

        int insertIndex = Math.Clamp(summary.Index + 1, 0, path.Points.Count);
        path.Points.Insert(insertIndex, point);
        MarkDirty();
        RefreshAfterPathMutation(path, point, "Duplicated path point.");
    }

    private void PathRemovePointButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertalePath path ||
            PathPointsList.SelectedItem is not PathPointSummary summary)
        {
            return;
        }

        int index = path.Points.IndexOf(summary.Point);
        if (index < 0)
            return;

        path.Points.RemoveAt(index);
        UndertalePath.PathPoint? nextSelection = path.Points.Count == 0
            ? null
            : path.Points[Math.Clamp(index, 0, path.Points.Count - 1)];
        MarkDirty();
        RefreshAfterPathMutation(path, nextSelection, "Removed path point.");
    }

    private void PathPointBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingPathPointEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertalePath path ||
            PathPointsList.SelectedItem is not PathPointSummary summary)
        {
            return;
        }

        if (!TryParseFloat(PathPointXBox.Text, out float x) ||
            !TryParseFloat(PathPointYBox.Text, out float y) ||
            !TryParseFloat(PathPointSpeedBox.Text, out float speed))
        {
            StatusBox.Text = "Invalid path point value. Use decimal numbers.";
            UpdatePathPointEditor(summary);
            return;
        }

        if (summary.Point.X == x && summary.Point.Y == y && summary.Point.Speed == speed)
            return;

        summary.Point.X = x;
        summary.Point.Y = y;
        summary.Point.Speed = speed;
        MarkDirty();
        RefreshAfterPathMutation(path, summary.Point, "Updated path point.");
    }

    private void RefreshAfterPathMutation(UndertalePath path, UndertalePath.PathPoint? selectedPoint, string status)
    {
        DetailsList.ItemsSource = _selectedResource is null ? null : BuildDetails(_selectedResource).ToArray();
        RefreshPathEditor(path, selectedPoint);
        StatusBox.Text = status;
    }

    private void ShowFontSummaryFor(ResourceItem item)
    {
        if (item.Value is not UndertaleFont font)
        {
            HideFontSummary();
            return;
        }

        FontSummaryPanel.Visibility = Visibility.Visible;
        FontSummaryText.Text = BuildFontSummary(font);
        IReadOnlyList<FontGlyphSummary> glyphs = BuildFontGlyphSummaries(font);
        FontGlyphsList.ItemsSource = glyphs;
        FontGlyphSummary? selectedGlyph = glyphs.FirstOrDefault();
        FontGlyphsList.SelectedItem = selectedGlyph;
        UpdateFontGlyphEditor(selectedGlyph);
        RefreshFontKerningForGlyph(selectedGlyph);
        UpdateFontActionButtons();
    }

    private void HideFontSummary()
    {
        FontSummaryText.Text = string.Empty;
        FontOpenTextureButton.IsEnabled = false;
        FontSortGlyphsButton.IsEnabled = false;
        FontUpdateRangeButton.IsEnabled = false;
        FontCreateGlyphButton.IsEnabled = false;
        FontAddKerningButton.IsEnabled = false;
        FontRemoveKerningButton.IsEnabled = false;
        FontGlyphsList.ItemsSource = null;
        UpdateFontGlyphEditor(null);
        FontKerningList.ItemsSource = null;
        FontKerningTitleText.Text = "Kerning";
        FontKerningCharacterBox.Text = string.Empty;
        FontKerningShiftBox.Text = string.Empty;
        FontKerningEditorPanel.Visibility = Visibility.Collapsed;
        FontKerningPanel.Visibility = Visibility.Collapsed;
        FontSummaryPanel.Visibility = Visibility.Collapsed;
    }

    private void UpdateFontActionButtons()
    {
        bool canMutate = _data is not null && !_data.UnsupportedBytecodeVersion && _selectedResource?.Value is UndertaleFont;
        FontOpenTextureButton.IsEnabled = _selectedResource?.Value is UndertaleFont { Texture: not null };
        FontSortGlyphsButton.IsEnabled = canMutate;
        FontUpdateRangeButton.IsEnabled = canMutate && _selectedResource?.Value is UndertaleFont { Glyphs.Count: > 0 };
        FontCreateGlyphButton.IsEnabled = canMutate;
        FontAddKerningButton.IsEnabled = canMutate && FontGlyphsList.SelectedItem is FontGlyphSummary;
        FontRemoveKerningButton.IsEnabled = canMutate && FontKerningList.SelectedItem is FontKerningSummary;
        FontKerningCharacterBox.IsReadOnly = !canMutate;
        FontKerningShiftBox.IsReadOnly = !canMutate;
        FontGlyphCharacterBox.IsReadOnly = !canMutate;
        FontGlyphSourceXBox.IsReadOnly = !canMutate;
        FontGlyphSourceYBox.IsReadOnly = !canMutate;
        FontGlyphSourceWidthBox.IsReadOnly = !canMutate;
        FontGlyphSourceHeightBox.IsReadOnly = !canMutate;
        FontGlyphShiftBox.IsReadOnly = !canMutate;
        FontGlyphOffsetBox.IsReadOnly = !canMutate;
        FontGlyphUnknownZeroBox.IsReadOnly = !canMutate;
    }

    private void FontOpenTextureButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _selectedResource?.Value is not UndertaleFont font || font.Texture is null)
            return;

        int textureIndex = _data.TexturePageItems.IndexOf(font.Texture);
        if (textureIndex >= 0)
            NavigateToResource("Texture page items", textureIndex);
    }

    private void FontGlyphsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not FontGlyphSummary summary)
            return;

        SelectFontGlyph(summary);
        StatusBox.Text = $"Selected {summary.Title}.";
    }

    private void SelectFontGlyph(FontGlyphSummary summary, UndertaleFont.Glyph.GlyphKerning? selectedKerning = null)
    {
        FontGlyphsList.SelectedItem = summary;
        UpdateFontGlyphEditor(summary);
        RefreshFontKerningForGlyph(summary, selectedKerning);
        UpdateFontActionButtons();
    }

    private void UpdateFontGlyphEditor(FontGlyphSummary? summary)
    {
        _isUpdatingFontGlyphEditor = true;
        try
        {
            if (summary is null)
            {
                FontGlyphCharacterBox.Text = string.Empty;
                FontGlyphSourceXBox.Text = string.Empty;
                FontGlyphSourceYBox.Text = string.Empty;
                FontGlyphSourceWidthBox.Text = string.Empty;
                FontGlyphSourceHeightBox.Text = string.Empty;
                FontGlyphShiftBox.Text = string.Empty;
                FontGlyphOffsetBox.Text = string.Empty;
                FontGlyphUnknownZeroBox.Text = string.Empty;
                FontGlyphEditorPanel.Visibility = Visibility.Collapsed;
                return;
            }

            UndertaleFont.Glyph glyph = summary.Glyph;
            FontGlyphCharacterBox.Text = glyph.Character.ToString(CultureInfo.InvariantCulture);
            FontGlyphSourceXBox.Text = glyph.SourceX.ToString(CultureInfo.InvariantCulture);
            FontGlyphSourceYBox.Text = glyph.SourceY.ToString(CultureInfo.InvariantCulture);
            FontGlyphSourceWidthBox.Text = glyph.SourceWidth.ToString(CultureInfo.InvariantCulture);
            FontGlyphSourceHeightBox.Text = glyph.SourceHeight.ToString(CultureInfo.InvariantCulture);
            FontGlyphShiftBox.Text = glyph.Shift.ToString(CultureInfo.InvariantCulture);
            FontGlyphOffsetBox.Text = glyph.Offset.ToString(CultureInfo.InvariantCulture);
            FontGlyphUnknownZeroBox.Text = glyph.UnknownAlwaysZero.ToString(CultureInfo.InvariantCulture);
            FontGlyphEditorPanel.Visibility = Visibility.Visible;
        }
        finally
        {
            _isUpdatingFontGlyphEditor = false;
        }
    }

    private void FontSortGlyphsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _data.UnsupportedBytecodeVersion || _selectedResource?.Value is not UndertaleFont font)
            return;

        UndertaleFont.Glyph? selectedGlyph = (FontGlyphsList.SelectedItem as FontGlyphSummary)?.Glyph;
        List<UndertaleFont.Glyph> sortedGlyphs = font.Glyphs.ToList();
        sortedGlyphs.Sort((x, y) => x.Character.CompareTo(y.Character));
        font.Glyphs.Clear();
        foreach (UndertaleFont.Glyph glyph in sortedGlyphs)
            font.Glyphs.Add(glyph);

        MarkDirty();
        RefreshAfterFontMutation(font, selectedGlyph, "Sorted font glyphs.");
    }

    private void FontUpdateRangeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _data.UnsupportedBytecodeVersion || _selectedResource?.Value is not UndertaleFont font)
            return;

        if (font.Glyphs.Count == 0)
        {
            StatusBox.Text = "Cannot update font range because this font has no glyphs.";
            return;
        }

        font.RangeStart = font.Glyphs.Min(glyph => glyph.Character);
        font.RangeEnd = font.Glyphs.Max(glyph => glyph.Character);
        MarkDirty();
        RefreshAfterFontMutation(font, (FontGlyphsList.SelectedItem as FontGlyphSummary)?.Glyph, "Updated font range from glyphs.");
    }

    private void FontCreateGlyphButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _data.UnsupportedBytecodeVersion || _selectedResource?.Value is not UndertaleFont font)
            return;

        if (font.Glyphs.Count > 0)
        {
            UndertaleFont.Glyph lastGlyph = font.Glyphs[font.Glyphs.Count - 1];
            if (lastGlyph.SourceWidth == 0 || lastGlyph.SourceHeight == 0)
            {
                StatusBox.Text = "The last glyph has zero size. Adjust it before creating another empty glyph.";
                return;
            }
        }

        UndertaleFont.Glyph glyph = new();
        font.Glyphs.Add(glyph);
        MarkDirty();
        RefreshAfterFontMutation(font, glyph, "Created empty font glyph.");
    }

    private void FontGlyphBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingFontGlyphEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleFont font ||
            FontGlyphsList.SelectedItem is not FontGlyphSummary glyphSummary)
        {
            return;
        }

        if (!TryParseGlyphCharacter(FontGlyphCharacterBox.Text, out ushort character) ||
            !ushort.TryParse(FontGlyphSourceXBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort sourceX) ||
            !ushort.TryParse(FontGlyphSourceYBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort sourceY) ||
            !ushort.TryParse(FontGlyphSourceWidthBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort sourceWidth) ||
            !ushort.TryParse(FontGlyphSourceHeightBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort sourceHeight) ||
            !short.TryParse(FontGlyphShiftBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out short shift) ||
            !short.TryParse(FontGlyphOffsetBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out short offset) ||
            !short.TryParse(FontGlyphUnknownZeroBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out short unknownAlwaysZero))
        {
            UpdateFontGlyphEditor(glyphSummary);
            StatusBox.Text = "Could not update font glyph: expected numeric glyph fields.";
            return;
        }

        UndertaleFont.Glyph glyph = glyphSummary.Glyph;
        if (glyph.Character == character &&
            glyph.SourceX == sourceX &&
            glyph.SourceY == sourceY &&
            glyph.SourceWidth == sourceWidth &&
            glyph.SourceHeight == sourceHeight &&
            glyph.Shift == shift &&
            glyph.Offset == offset &&
            glyph.UnknownAlwaysZero == unknownAlwaysZero)
        {
            return;
        }

        glyph.Character = character;
        glyph.SourceX = sourceX;
        glyph.SourceY = sourceY;
        glyph.SourceWidth = sourceWidth;
        glyph.SourceHeight = sourceHeight;
        glyph.Shift = shift;
        glyph.Offset = offset;
        glyph.UnknownAlwaysZero = unknownAlwaysZero;
        MarkDirty();
        RefreshAfterFontMutation(font, glyph, "Updated font glyph.");
    }

    private void FontAddKerningButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleFont font ||
            FontGlyphsList.SelectedItem is not FontGlyphSummary glyphSummary)
        {
            return;
        }

        UndertaleFont.Glyph.GlyphKerning kerning = new();
        glyphSummary.Glyph.Kerning.Add(kerning);
        MarkDirty();
        RefreshAfterFontMutation(font, glyphSummary.Glyph, "Added font kerning pair.", kerning);
    }

    private void FontRemoveKerningButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleFont font ||
            FontGlyphsList.SelectedItem is not FontGlyphSummary glyphSummary ||
            FontKerningList.SelectedItem is not FontKerningSummary kerningSummary)
        {
            return;
        }

        int index = glyphSummary.Glyph.Kerning.IndexOf(kerningSummary.Kerning);
        if (index < 0)
            return;

        glyphSummary.Glyph.Kerning.RemoveAt(index);
        MarkDirty();
        RefreshAfterFontMutation(font, glyphSummary.Glyph, "Removed font kerning pair.");
    }

    private void FontKerningList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not FontKerningSummary summary)
            return;

        FontKerningList.SelectedItem = summary;
        UpdateFontKerningEditor(summary);
        UpdateFontActionButtons();
        StatusBox.Text = $"Selected {summary.Title}.";
    }

    private void FontKerningBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingFontKerningEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleFont font ||
            FontGlyphsList.SelectedItem is not FontGlyphSummary glyphSummary ||
            FontKerningList.SelectedItem is not FontKerningSummary kerningSummary)
        {
            return;
        }

        if (!TryParseKerningCharacter(FontKerningCharacterBox.Text, out short character) ||
            !short.TryParse(FontKerningShiftBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out short shiftModifier))
        {
            UpdateFontKerningEditor(kerningSummary);
            StatusBox.Text = "Could not update font kerning pair: expected numeric character code and shift.";
            return;
        }

        kerningSummary.Kerning.Character = character;
        kerningSummary.Kerning.ShiftModifier = shiftModifier;
        MarkDirty();
        RefreshAfterFontMutation(font, glyphSummary.Glyph, "Updated font kerning pair.", kerningSummary.Kerning);
    }

    private void RefreshAfterFontMutation(
        UndertaleFont font,
        UndertaleFont.Glyph? selectedGlyph,
        string status,
        UndertaleFont.Glyph.GlyphKerning? selectedKerning = null)
    {
        DetailsList.ItemsSource = _selectedResource is null ? null : BuildDetails(_selectedResource).ToArray();
        FontSummaryText.Text = BuildFontSummary(font);
        IReadOnlyList<FontGlyphSummary> glyphs = BuildFontGlyphSummaries(font);
        FontGlyphsList.ItemsSource = glyphs;
        FontGlyphSummary? selectedSummary = selectedGlyph is null
            ? glyphs.FirstOrDefault()
            : glyphs.FirstOrDefault(summary => ReferenceEquals(summary.Glyph, selectedGlyph));
        FontGlyphsList.SelectedItem = selectedSummary;
        UpdateFontGlyphEditor(selectedSummary);
        RefreshFontKerningForGlyph(selectedSummary, selectedKerning);
        UpdateFontActionButtons();
        StatusBox.Text = status;
    }

    private void RefreshFontKerningForGlyph(FontGlyphSummary? glyphSummary, UndertaleFont.Glyph.GlyphKerning? selectedKerning = null)
    {
        _isUpdatingFontKerningEditor = true;
        try
        {
            if (glyphSummary is null)
            {
                FontKerningPanel.Visibility = Visibility.Collapsed;
                FontKerningList.ItemsSource = null;
                UpdateFontKerningEditor(null);
                return;
            }

            FontKerningPanel.Visibility = Visibility.Visible;
            FontKerningTitleText.Text = $"Kerning for {glyphSummary.Title}";
            IReadOnlyList<FontKerningSummary> kerningSummaries = BuildFontKerningSummaries(glyphSummary.Glyph);
            FontKerningList.ItemsSource = kerningSummaries;
            FontKerningSummary? selectedSummary = selectedKerning is null
                ? kerningSummaries.FirstOrDefault()
                : kerningSummaries.FirstOrDefault(summary => ReferenceEquals(summary.Kerning, selectedKerning));
            FontKerningList.SelectedItem = selectedSummary;
            UpdateFontKerningEditor(selectedSummary);
        }
        finally
        {
            _isUpdatingFontKerningEditor = false;
        }
    }

    private void UpdateFontKerningEditor(FontKerningSummary? summary)
    {
        _isUpdatingFontKerningEditor = true;
        try
        {
            if (summary is null)
            {
                FontKerningCharacterBox.Text = string.Empty;
                FontKerningShiftBox.Text = string.Empty;
                FontKerningEditorPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                FontKerningCharacterBox.Text = summary.Kerning.Character.ToString(CultureInfo.InvariantCulture);
                FontKerningShiftBox.Text = summary.Kerning.ShiftModifier.ToString(CultureInfo.InvariantCulture);
                FontKerningEditorPanel.Visibility = Visibility.Visible;
            }
        }
        finally
        {
            _isUpdatingFontKerningEditor = false;
        }
    }

    private void ShowShaderEditorFor(ResourceItem item)
    {
        if (item.Value is not UndertaleShader shader)
        {
            HideShaderEditor();
            return;
        }

        bool canEdit = _data is not null && !_data.UnsupportedBytecodeVersion;
        _isUpdatingShaderEditor = true;
        ShaderEditorPanel.Visibility = Visibility.Visible;
        ShaderSourceComboBox.ItemsSource = BuildShaderSourceItems(shader);
        ShaderSourceComboBox.SelectedIndex = 0;
        ShaderTypeComboBox.ItemsSource = Enum.GetValues<UndertaleShader.ShaderType>();
        ShaderTypeComboBox.SelectedItem = shader.Type;
        ShaderSourceTextBox.IsReadOnly = !canEdit;
        ShaderImportSourceButton.IsEnabled = canEdit;
        ShaderAddAttributeButton.IsEnabled = canEdit;
        ShaderSummaryText.Text = BuildShaderSummary(shader);
        _isUpdatingShaderEditor = false;

        UpdateShaderSourceEditor();
        RefreshShaderAttributes(shader, selectedAttribute: null);
    }

    private void HideShaderEditor()
    {
        _isUpdatingShaderEditor = true;
        ShaderSourceComboBox.ItemsSource = null;
        ShaderTypeComboBox.ItemsSource = null;
        ShaderSourceTextBox.Text = string.Empty;
        ShaderSummaryText.Text = string.Empty;
        ShaderAttributesList.ItemsSource = null;
        ShaderAttributesList.SelectedItem = null;
        ShaderAttributeNameBox.Text = string.Empty;
        ShaderAttributeEditorPanel.Visibility = Visibility.Collapsed;
        ShaderImportSourceButton.IsEnabled = false;
        ShaderExportSourceButton.IsEnabled = false;
        ShaderAddAttributeButton.IsEnabled = false;
        ShaderRemoveAttributeButton.IsEnabled = false;
        ShaderEditorPanel.Visibility = Visibility.Collapsed;
        _isUpdatingShaderEditor = false;
    }

    private void ShaderSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingShaderEditor)
            return;

        UpdateShaderSourceEditor();
    }

    private void UpdateShaderSourceEditor()
    {
        if (_selectedResource?.Value is not UndertaleShader ||
            ShaderSourceComboBox.SelectedItem is not ShaderSourceItem source)
        {
            ShaderSourceTextBox.Text = string.Empty;
            ShaderExportSourceButton.IsEnabled = false;
            return;
        }

        _isUpdatingShaderEditor = true;
        ShaderSourceTextBox.Text = source.Source.Content ?? string.Empty;
        ShaderExportSourceButton.IsEnabled = true;
        _isUpdatingShaderEditor = false;
    }

    private void ShaderTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingShaderEditor ||
            _selectedResource?.Value is not UndertaleShader shader ||
            ShaderTypeComboBox.SelectedItem is not UndertaleShader.ShaderType shaderType)
        {
            return;
        }

        if (shader.Type == shaderType)
            return;

        shader.Type = shaderType;
        ShaderSummaryText.Text = BuildShaderSummary(shader);
        DetailsList.ItemsSource = _selectedResource is null ? null : BuildDetails(_selectedResource).ToArray();
        MarkDirty();
    }

    private void ShaderSourceTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingShaderEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            ShaderSourceComboBox.SelectedItem is not ShaderSourceItem source)
        {
            return;
        }

        if ((source.Source.Content ?? string.Empty) == ShaderSourceTextBox.Text)
            return;

        source.Source.Content = ShaderSourceTextBox.Text;
        MarkDirty();
        StatusBox.Text = $"Updated {source.Title}.";
    }

    private async void ShaderImportSourceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            ShaderSourceComboBox.SelectedItem is not ShaderSourceItem source)
        {
            return;
        }

        ResourceItem? selectedResource = _selectedResource;
        FileOpenPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add(".glsl");
        picker.FileTypeFilter.Add(".hlsl");
        picker.FileTypeFilter.Add(".txt");
        picker.FileTypeFilter.Add("*");

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        ShaderImportSourceButton.IsEnabled = false;
        try
        {
            string text = await File.ReadAllTextAsync(file.Path);
            source.Source.Content = text;
            if (ReferenceEquals(_selectedResource, selectedResource) &&
                ReferenceEquals(ShaderSourceComboBox.SelectedItem, source))
            {
                ShaderSourceTextBox.Text = text;
            }
            MarkDirty();
            StatusBox.Text = $"Imported {source.Title} from {file.Path}.";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to import shader source: {ex}";
        }
        finally
        {
            if (ReferenceEquals(_selectedResource, selectedResource) &&
                ReferenceEquals(ShaderSourceComboBox.SelectedItem, source))
            {
                ShaderImportSourceButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion;
            }
        }
    }

    private async void ShaderExportSourceButton_Click(object sender, RoutedEventArgs e)
    {
        if (ShaderSourceComboBox.SelectedItem is not ShaderSourceItem source)
            return;

        ResourceItem? selectedResource = _selectedResource;
        FileSavePicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = SafeFileName(source.Title, "shader")
        };
        picker.FileTypeChoices.Add("Text file", [".txt"]);

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFile? file = await picker.PickSaveFileAsync();
        if (file is null)
            return;

        ShaderExportSourceButton.IsEnabled = false;
        try
        {
            await File.WriteAllTextAsync(file.Path, source.Source.Content ?? string.Empty);
            StatusBox.Text = $"Exported {source.Title} to {file.Path}.";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to export shader source: {ex}";
        }
        finally
        {
            if (ReferenceEquals(_selectedResource, selectedResource) &&
                ReferenceEquals(ShaderSourceComboBox.SelectedItem, source))
            {
                ShaderExportSourceButton.IsEnabled = true;
            }
        }
    }

    private void RefreshShaderAttributes(
        UndertaleShader shader,
        UndertaleShader.VertexShaderAttribute? selectedAttribute)
    {
        ShaderAttributeSummary[] summaries = BuildShaderAttributeSummaries(shader).ToArray();
        ShaderAttributesList.ItemsSource = summaries;

        ShaderAttributeSummary? selectedSummary = selectedAttribute is null
            ? summaries.FirstOrDefault()
            : summaries.FirstOrDefault(summary => ReferenceEquals(summary.Attribute, selectedAttribute));
        ShaderAttributesList.SelectedItem = selectedSummary;
        UpdateShaderAttributeEditor(selectedSummary);
    }

    private void UpdateShaderAttributeEditor(ShaderAttributeSummary? summary)
    {
        bool canEdit = _data is not null && !_data.UnsupportedBytecodeVersion;
        _isUpdatingShaderEditor = true;
        ShaderAttributeNameBox.Text = summary?.Attribute.Name?.Content ?? string.Empty;
        ShaderAttributeEditorPanel.Visibility = summary is null ? Visibility.Collapsed : Visibility.Visible;
        ShaderRemoveAttributeButton.IsEnabled = canEdit && summary is not null;
        _isUpdatingShaderEditor = false;
    }

    private void ShaderAttributesList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ShaderAttributeSummary summary)
            UpdateShaderAttributeEditor(summary);
    }

    private void ShaderAddAttributeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _data.UnsupportedBytecodeVersion || _selectedResource?.Value is not UndertaleShader shader)
            return;

        UndertaleShader.VertexShaderAttribute attribute = new()
        {
            Name = new UndertaleString($"in_Position{shader.VertexShaderAttributes.Count}")
        };
        shader.VertexShaderAttributes.Add(attribute);
        MarkDirty();
        RefreshShaderAttributes(shader, attribute);
        StatusBox.Text = "Added vertex shader attribute.";
    }

    private void ShaderRemoveAttributeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleShader shader ||
            ShaderAttributesList.SelectedItem is not ShaderAttributeSummary summary)
        {
            return;
        }

        int index = shader.VertexShaderAttributes.IndexOf(summary.Attribute);
        if (index < 0)
            return;

        shader.VertexShaderAttributes.RemoveAt(index);
        UndertaleShader.VertexShaderAttribute? nextSelection = shader.VertexShaderAttributes.Count == 0
            ? null
            : shader.VertexShaderAttributes[Math.Clamp(index, 0, shader.VertexShaderAttributes.Count - 1)];
        MarkDirty();
        RefreshShaderAttributes(shader, nextSelection);
        StatusBox.Text = "Removed vertex shader attribute.";
    }

    private void ShaderAttributeNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingShaderEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleShader shader ||
            ShaderAttributesList.SelectedItem is not ShaderAttributeSummary summary)
        {
            return;
        }

        summary.Attribute.Name ??= new UndertaleString(string.Empty);
        if (summary.Attribute.Name.Content == ShaderAttributeNameBox.Text)
            return;

        summary.Attribute.Name.Content = ShaderAttributeNameBox.Text;
        MarkDirty();
        RefreshShaderAttributes(shader, summary.Attribute);
        StatusBox.Text = "Updated vertex shader attribute.";
    }

    private void ShowTimelineEditorFor(ResourceItem item)
    {
        if (item.Value is not UndertaleTimeline timeline)
        {
            HideTimelineEditor();
            return;
        }

        TimelineEditorPanel.Visibility = Visibility.Visible;
        TimelineAddMomentButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion;
        RefreshTimelineEditor(timeline, selectedMoment: null, selectedAction: null);
    }

    private void HideTimelineEditor()
    {
        _isUpdatingTimelineEditor = true;
        TimelineSummaryText.Text = string.Empty;
        TimelineMomentsList.ItemsSource = null;
        TimelineMomentsList.SelectedItem = null;
        TimelineMomentStepBox.Text = string.Empty;
        TimelineActionsList.ItemsSource = null;
        TimelineActionsList.SelectedItem = null;
        TimelineActionCodeComboBox.ItemsSource = null;
        TimelineActionCodeComboBox.SelectedItem = null;
        TimelineActionEditorPanel.Visibility = Visibility.Collapsed;
        TimelineMomentEditorPanel.Visibility = Visibility.Collapsed;
        TimelineAddMomentButton.IsEnabled = false;
        TimelineDuplicateMomentButton.IsEnabled = false;
        TimelineRemoveMomentButton.IsEnabled = false;
        TimelineAddActionButton.IsEnabled = false;
        TimelineOpenActionCodeButton.IsEnabled = false;
        TimelineRemoveActionButton.IsEnabled = false;
        TimelineEditorPanel.Visibility = Visibility.Collapsed;
        _isUpdatingTimelineEditor = false;
    }

    private void RefreshTimelineEditor(
        UndertaleTimeline timeline,
        UndertaleTimeline.UndertaleTimelineMoment? selectedMoment,
        UndertaleGameObject.EventAction? selectedAction)
    {
        TimelineSummaryText.Text = BuildTimelineSummary(timeline);

        TimelineMomentSummary[] momentSummaries = BuildTimelineMomentSummaries(timeline).ToArray();
        TimelineMomentsList.ItemsSource = momentSummaries;

        TimelineMomentSummary? selectedMomentSummary = selectedMoment is null
            ? momentSummaries.FirstOrDefault()
            : momentSummaries.FirstOrDefault(summary => ReferenceEquals(summary.Moment, selectedMoment));
        TimelineMomentsList.SelectedItem = selectedMomentSummary;
        UpdateTimelineMomentEditor(selectedMomentSummary, selectedAction);
    }

    private void UpdateTimelineMomentEditor(
        TimelineMomentSummary? summary,
        UndertaleGameObject.EventAction? selectedAction)
    {
        bool canEdit = _data is not null && !_data.UnsupportedBytecodeVersion;
        _isUpdatingTimelineEditor = true;
        TimelineDuplicateMomentButton.IsEnabled = canEdit && summary is not null;
        TimelineRemoveMomentButton.IsEnabled = canEdit && summary is not null;
        TimelineAddActionButton.IsEnabled = canEdit && summary is not null;

        if (summary is null)
        {
            TimelineMomentStepBox.Text = string.Empty;
            TimelineActionsList.ItemsSource = null;
            TimelineActionCodeComboBox.ItemsSource = null;
            TimelineActionEditorPanel.Visibility = Visibility.Collapsed;
            TimelineMomentEditorPanel.Visibility = Visibility.Collapsed;
            TimelineOpenActionCodeButton.IsEnabled = false;
            TimelineRemoveActionButton.IsEnabled = false;
            _isUpdatingTimelineEditor = false;
            return;
        }

        TimelineMomentStepBox.Text = summary.Moment.Step.ToString(CultureInfo.InvariantCulture);
        TimelineMomentEditorPanel.Visibility = Visibility.Visible;
        _isUpdatingTimelineEditor = false;
        RefreshTimelineActions(summary.Moment, selectedAction);
    }

    private void RefreshTimelineActions(
        UndertaleTimeline.UndertaleTimelineMoment moment,
        UndertaleGameObject.EventAction? selectedAction)
    {
        TimelineActionSummary[] actionSummaries = BuildTimelineActionSummaries(moment).ToArray();
        TimelineActionsList.ItemsSource = actionSummaries;

        TimelineActionSummary? selectedActionSummary = selectedAction is null
            ? actionSummaries.FirstOrDefault()
            : actionSummaries.FirstOrDefault(summary => ReferenceEquals(summary.Action, selectedAction));
        TimelineActionsList.SelectedItem = selectedActionSummary;
        UpdateTimelineActionEditor(selectedActionSummary);
    }

    private void UpdateTimelineActionEditor(TimelineActionSummary? summary)
    {
        bool canEdit = _data is not null && !_data.UnsupportedBytecodeVersion;
        _isUpdatingTimelineEditor = true;
        TimelineOpenActionCodeButton.IsEnabled = summary?.Action.CodeId is not null;
        TimelineRemoveActionButton.IsEnabled = canEdit && summary is not null;

        if (_data is null || summary is null)
        {
            TimelineActionCodeComboBox.ItemsSource = null;
            TimelineActionCodeComboBox.SelectedItem = null;
            TimelineActionEditorPanel.Visibility = Visibility.Collapsed;
            _isUpdatingTimelineEditor = false;
            return;
        }

        CodeReferenceItem[] codeItems = BuildCodeReferenceItems(_data).ToArray();
        TimelineActionCodeComboBox.ItemsSource = codeItems;
        TimelineActionCodeComboBox.SelectedItem = codeItems.FirstOrDefault(item => ReferenceEquals(item.Code, summary.Action.CodeId)) ?? codeItems.FirstOrDefault();
        TimelineActionEditorPanel.Visibility = Visibility.Visible;
        _isUpdatingTimelineEditor = false;
    }

    private void TimelineMomentsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is TimelineMomentSummary summary)
            UpdateTimelineMomentEditor(summary, selectedAction: null);
    }

    private void TimelineActionsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is TimelineActionSummary summary)
            UpdateTimelineActionEditor(summary);
    }

    private void TimelineMomentStepBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingTimelineEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleTimeline timeline ||
            TimelineMomentsList.SelectedItem is not TimelineMomentSummary summary)
        {
            return;
        }

        if (!uint.TryParse(TimelineMomentStepBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint step))
        {
            TimelineMomentStepBox.Text = summary.Moment.Step.ToString(CultureInfo.InvariantCulture);
            StatusBox.Text = "Invalid timeline step. Use a non-negative integer.";
            return;
        }

        if (summary.Moment.Step == step)
            return;

        summary.Moment.Step = step;
        MarkDirty();
        RefreshTimelineEditor(timeline, summary.Moment, selectedAction: null);
        StatusBox.Text = "Updated timeline moment step.";
    }

    private void TimelineAddMomentButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _data.UnsupportedBytecodeVersion || _selectedResource?.Value is not UndertaleTimeline timeline)
            return;

        uint step = timeline.Moments.Count == 0 ? 0 : timeline.Moments.Max(moment => moment.Step) + 1;
        UndertaleTimeline.UndertaleTimelineMoment moment = CreateTimelineMoment(step, addEmptyAction: true);
        timeline.Moments.Add(moment);
        MarkDirty();
        RefreshTimelineEditor(timeline, moment, selectedAction: moment.Event.FirstOrDefault());
        StatusBox.Text = "Added timeline moment.";
    }

    private void TimelineDuplicateMomentButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleTimeline timeline ||
            TimelineMomentsList.SelectedItem is not TimelineMomentSummary summary)
        {
            return;
        }

        UndertaleTimeline.UndertaleTimelineMoment moment = CreateTimelineMoment(summary.Moment.Step + 1, addEmptyAction: false);
        foreach (UndertaleGameObject.EventAction action in summary.Moment.Event ?? [])
        {
            moment.Event.Add(new UndertaleGameObject.EventAction
            {
                CodeId = action.CodeId
            });
        }

        if (moment.Event.Count == 0)
            moment.Event.Add(new UndertaleGameObject.EventAction());

        int insertIndex = Math.Clamp(summary.Index + 1, 0, timeline.Moments.Count);
        timeline.Moments.Insert(insertIndex, moment);
        MarkDirty();
        RefreshTimelineEditor(timeline, moment, selectedAction: moment.Event.FirstOrDefault());
        StatusBox.Text = "Duplicated timeline moment.";
    }

    private void TimelineRemoveMomentButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleTimeline timeline ||
            TimelineMomentsList.SelectedItem is not TimelineMomentSummary summary)
        {
            return;
        }

        int index = timeline.Moments.IndexOf(summary.Moment);
        if (index < 0)
            return;

        timeline.Moments.RemoveAt(index);
        UndertaleTimeline.UndertaleTimelineMoment? nextSelection = timeline.Moments.Count == 0
            ? null
            : timeline.Moments[Math.Clamp(index, 0, timeline.Moments.Count - 1)];
        MarkDirty();
        RefreshTimelineEditor(timeline, nextSelection, selectedAction: null);
        StatusBox.Text = "Removed timeline moment.";
    }

    private void TimelineAddActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleTimeline timeline ||
            TimelineMomentsList.SelectedItem is not TimelineMomentSummary summary)
        {
            return;
        }

        summary.Moment.Event ??= new UndertalePointerList<UndertaleGameObject.EventAction>();
        UndertaleGameObject.EventAction action = new();
        summary.Moment.Event.Add(action);
        MarkDirty();
        RefreshTimelineEditor(timeline, summary.Moment, action);
        StatusBox.Text = "Added timeline action.";
    }

    private void TimelineRemoveActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleTimeline timeline ||
            TimelineMomentsList.SelectedItem is not TimelineMomentSummary momentSummary ||
            TimelineActionsList.SelectedItem is not TimelineActionSummary actionSummary ||
            momentSummary.Moment.Event is null)
        {
            return;
        }

        int index = momentSummary.Moment.Event.IndexOf(actionSummary.Action);
        if (index < 0)
            return;

        momentSummary.Moment.Event.RemoveAt(index);
        UndertaleGameObject.EventAction? nextSelection = momentSummary.Moment.Event.Count == 0
            ? null
            : momentSummary.Moment.Event[Math.Clamp(index, 0, momentSummary.Moment.Event.Count - 1)];
        MarkDirty();
        RefreshTimelineEditor(timeline, momentSummary.Moment, nextSelection);
        StatusBox.Text = "Removed timeline action.";
    }

    private void TimelineOpenActionCodeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            TimelineActionsList.SelectedItem is not TimelineActionSummary summary ||
            summary.Action.CodeId is null)
        {
            return;
        }

        int codeIndex = _data.Code.IndexOf(summary.Action.CodeId);
        if (codeIndex >= 0)
            NavigateToResource("Code", codeIndex);
    }

    private void TimelineActionCodeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingTimelineEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleTimeline timeline ||
            TimelineMomentsList.SelectedItem is not TimelineMomentSummary momentSummary ||
            TimelineActionsList.SelectedItem is not TimelineActionSummary actionSummary ||
            TimelineActionCodeComboBox.SelectedItem is not CodeReferenceItem codeItem)
        {
            return;
        }

        if (ReferenceEquals(actionSummary.Action.CodeId, codeItem.Code))
            return;

        actionSummary.Action.CodeId = codeItem.Code;
        MarkDirty();
        RefreshTimelineEditor(timeline, momentSummary.Moment, actionSummary.Action);
        StatusBox.Text = "Updated timeline action code.";
    }

    private void ShowExtensionEditorFor(ResourceItem item)
    {
        if (item.Value is not UndertaleExtension extension)
        {
            HideExtensionEditor();
            return;
        }

        ExtensionEditorPanel.Visibility = Visibility.Visible;
        ExtensionAddFileButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion;
        RefreshExtensionEditor(extension, selectedFile: null, selectedFunction: null, selectedArgument: null);
    }

    private void HideExtensionEditor()
    {
        _isUpdatingExtensionEditor = true;
        ExtensionSummaryText.Text = string.Empty;
        ExtensionFilesList.ItemsSource = null;
        ExtensionFilesList.SelectedItem = null;
        ExtensionOptionsList.ItemsSource = null;
        ExtensionOptionsList.SelectedItem = null;
        ExtensionFunctionsList.ItemsSource = null;
        ExtensionFunctionsList.SelectedItem = null;
        ExtensionArgumentsList.ItemsSource = null;
        ExtensionArgumentsList.SelectedItem = null;
        ExtensionOptionNameBox.Text = string.Empty;
        ExtensionOptionValueBox.Text = string.Empty;
        ExtensionOptionKindComboBox.ItemsSource = null;
        ExtensionOptionEditorPanel.Visibility = Visibility.Collapsed;
        ExtensionFileNameBox.Text = string.Empty;
        ExtensionFileInitBox.Text = string.Empty;
        ExtensionFileCleanupBox.Text = string.Empty;
        ExtensionFileKindComboBox.ItemsSource = null;
        ExtensionFunctionNameBox.Text = string.Empty;
        ExtensionFunctionExternalNameBox.Text = string.Empty;
        ExtensionFunctionIdBox.Text = string.Empty;
        ExtensionFunctionKindBox.Text = string.Empty;
        ExtensionFunctionReturnTypeComboBox.ItemsSource = null;
        ExtensionArgumentTypeComboBox.ItemsSource = null;
        ExtensionArgumentEditorPanel.Visibility = Visibility.Collapsed;
        ExtensionFunctionEditorPanel.Visibility = Visibility.Collapsed;
        ExtensionFileEditorPanel.Visibility = Visibility.Collapsed;
        ExtensionAddFileButton.IsEnabled = false;
        ExtensionDuplicateFileButton.IsEnabled = false;
        ExtensionRemoveFileButton.IsEnabled = false;
        ExtensionAddOptionButton.IsEnabled = false;
        ExtensionDuplicateOptionButton.IsEnabled = false;
        ExtensionRemoveOptionButton.IsEnabled = false;
        ExtensionAddFunctionButton.IsEnabled = false;
        ExtensionDuplicateFunctionButton.IsEnabled = false;
        ExtensionRemoveFunctionButton.IsEnabled = false;
        ExtensionAddArgumentButton.IsEnabled = false;
        ExtensionRemoveArgumentButton.IsEnabled = false;
        ExtensionEditorPanel.Visibility = Visibility.Collapsed;
        _isUpdatingExtensionEditor = false;
    }

    private void RefreshExtensionEditor(
        UndertaleExtension extension,
        UndertaleExtensionFile? selectedFile,
        UndertaleExtensionFunction? selectedFunction,
        UndertaleExtensionFunctionArg? selectedArgument)
    {
        ExtensionSummaryText.Text = BuildExtensionSummary(extension);
        ExtensionAddOptionButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion;

        ExtensionFileSummary[] fileSummaries = BuildExtensionFileSummaries(extension).ToArray();
        ExtensionFilesList.ItemsSource = fileSummaries;

        RefreshExtensionOptions(extension, selectedOption: null);

        ExtensionFileSummary? selectedFileSummary = selectedFile is null
            ? fileSummaries.FirstOrDefault()
            : fileSummaries.FirstOrDefault(summary => ReferenceEquals(summary.File, selectedFile));
        ExtensionFilesList.SelectedItem = selectedFileSummary;
        UpdateExtensionFileEditor(selectedFileSummary, selectedFunction, selectedArgument);
    }

    private void UpdateExtensionFileEditor(
        ExtensionFileSummary? summary,
        UndertaleExtensionFunction? selectedFunction,
        UndertaleExtensionFunctionArg? selectedArgument)
    {
        bool canEdit = _data is not null && !_data.UnsupportedBytecodeVersion;
        _isUpdatingExtensionEditor = true;
        ExtensionDuplicateFileButton.IsEnabled = canEdit && summary is not null;
        ExtensionRemoveFileButton.IsEnabled = canEdit && summary is not null;
        ExtensionAddFunctionButton.IsEnabled = canEdit && summary is not null;

        if (summary is null)
        {
            ExtensionFileNameBox.Text = string.Empty;
            ExtensionFileInitBox.Text = string.Empty;
            ExtensionFileCleanupBox.Text = string.Empty;
            ExtensionFileKindComboBox.ItemsSource = null;
            ExtensionFunctionsList.ItemsSource = null;
            ExtensionFunctionEditorPanel.Visibility = Visibility.Collapsed;
            ExtensionFileEditorPanel.Visibility = Visibility.Collapsed;
            ExtensionDuplicateFunctionButton.IsEnabled = false;
            ExtensionRemoveFunctionButton.IsEnabled = false;
            _isUpdatingExtensionEditor = false;
            return;
        }

        ExtensionFileNameBox.Text = summary.File.Filename?.Content ?? string.Empty;
        ExtensionFileInitBox.Text = summary.File.InitScript?.Content ?? string.Empty;
        ExtensionFileCleanupBox.Text = summary.File.CleanupScript?.Content ?? string.Empty;
        ExtensionFileKindComboBox.ItemsSource = Enum.GetValues<UndertaleExtensionKind>();
        ExtensionFileKindComboBox.SelectedItem = summary.File.Kind;
        ExtensionFileEditorPanel.Visibility = Visibility.Visible;
        _isUpdatingExtensionEditor = false;

        RefreshExtensionFunctions(summary.File, selectedFunction, selectedArgument);
    }

    private void RefreshExtensionFunctions(
        UndertaleExtensionFile file,
        UndertaleExtensionFunction? selectedFunction,
        UndertaleExtensionFunctionArg? selectedArgument)
    {
        ExtensionFunctionSummary[] functionSummaries = BuildExtensionFunctionSummaries(file).ToArray();
        ExtensionFunctionsList.ItemsSource = functionSummaries;

        ExtensionFunctionSummary? selectedFunctionSummary = selectedFunction is null
            ? functionSummaries.FirstOrDefault()
            : functionSummaries.FirstOrDefault(summary => ReferenceEquals(summary.Function, selectedFunction));
        ExtensionFunctionsList.SelectedItem = selectedFunctionSummary;
        UpdateExtensionFunctionEditor(selectedFunctionSummary, selectedArgument);
    }

    private void UpdateExtensionFunctionEditor(
        ExtensionFunctionSummary? summary,
        UndertaleExtensionFunctionArg? selectedArgument)
    {
        bool canEdit = _data is not null && !_data.UnsupportedBytecodeVersion;
        _isUpdatingExtensionEditor = true;
        ExtensionDuplicateFunctionButton.IsEnabled = canEdit && summary is not null;
        ExtensionRemoveFunctionButton.IsEnabled = canEdit && summary is not null;
        ExtensionAddArgumentButton.IsEnabled = canEdit && summary is not null;

        if (summary is null)
        {
            ExtensionFunctionNameBox.Text = string.Empty;
            ExtensionFunctionExternalNameBox.Text = string.Empty;
            ExtensionFunctionIdBox.Text = string.Empty;
            ExtensionFunctionKindBox.Text = string.Empty;
            ExtensionFunctionReturnTypeComboBox.ItemsSource = null;
            ExtensionArgumentsList.ItemsSource = null;
            ExtensionArgumentEditorPanel.Visibility = Visibility.Collapsed;
            ExtensionFunctionEditorPanel.Visibility = Visibility.Collapsed;
            ExtensionRemoveArgumentButton.IsEnabled = false;
            _isUpdatingExtensionEditor = false;
            return;
        }

        ExtensionFunctionNameBox.Text = summary.Function.Name?.Content ?? string.Empty;
        ExtensionFunctionExternalNameBox.Text = summary.Function.ExtName?.Content ?? string.Empty;
        ExtensionFunctionIdBox.Text = summary.Function.ID.ToString(CultureInfo.InvariantCulture);
        ExtensionFunctionKindBox.Text = summary.Function.Kind.ToString(CultureInfo.InvariantCulture);
        ExtensionFunctionReturnTypeComboBox.ItemsSource = Enum.GetValues<UndertaleExtensionVarType>();
        ExtensionFunctionReturnTypeComboBox.SelectedItem = summary.Function.RetType;
        ExtensionFunctionEditorPanel.Visibility = Visibility.Visible;
        _isUpdatingExtensionEditor = false;

        RefreshExtensionArguments(summary.Function, selectedArgument);
    }

    private void RefreshExtensionArguments(
        UndertaleExtensionFunction function,
        UndertaleExtensionFunctionArg? selectedArgument)
    {
        ExtensionArgumentSummary[] argumentSummaries = BuildExtensionArgumentSummaries(function).ToArray();
        ExtensionArgumentsList.ItemsSource = argumentSummaries;

        ExtensionArgumentSummary? selectedArgumentSummary = selectedArgument is null
            ? argumentSummaries.FirstOrDefault()
            : argumentSummaries.FirstOrDefault(summary => ReferenceEquals(summary.Argument, selectedArgument));
        ExtensionArgumentsList.SelectedItem = selectedArgumentSummary;
        UpdateExtensionArgumentEditor(selectedArgumentSummary);
    }

    private void UpdateExtensionArgumentEditor(ExtensionArgumentSummary? summary)
    {
        bool canEdit = _data is not null && !_data.UnsupportedBytecodeVersion;
        _isUpdatingExtensionEditor = true;
        ExtensionRemoveArgumentButton.IsEnabled = canEdit && summary is not null;

        if (summary is null)
        {
            ExtensionArgumentTypeComboBox.ItemsSource = null;
            ExtensionArgumentEditorPanel.Visibility = Visibility.Collapsed;
            _isUpdatingExtensionEditor = false;
            return;
        }

        ExtensionArgumentTypeComboBox.ItemsSource = Enum.GetValues<UndertaleExtensionVarType>();
        ExtensionArgumentTypeComboBox.SelectedItem = summary.Argument.Type;
        ExtensionArgumentEditorPanel.Visibility = Visibility.Visible;
        _isUpdatingExtensionEditor = false;
    }

    private void ExtensionFilesList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ExtensionFileSummary summary)
            UpdateExtensionFileEditor(summary, selectedFunction: null, selectedArgument: null);
    }

    private void ExtensionFunctionsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ExtensionFunctionSummary summary)
            UpdateExtensionFunctionEditor(summary, selectedArgument: null);
    }

    private void ExtensionArgumentsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ExtensionArgumentSummary summary)
            UpdateExtensionArgumentEditor(summary);
    }

    private void RefreshExtensionOptions(UndertaleExtension extension, UndertaleExtensionOption? selectedOption)
    {
        ExtensionOptionSummary[] optionSummaries = BuildExtensionOptionSummaries(extension).ToArray();
        ExtensionOptionsList.ItemsSource = optionSummaries;

        ExtensionOptionSummary? selectedSummary = selectedOption is null
            ? optionSummaries.FirstOrDefault()
            : optionSummaries.FirstOrDefault(summary => ReferenceEquals(summary.Option, selectedOption));
        ExtensionOptionsList.SelectedItem = selectedSummary;
        UpdateExtensionOptionEditor(selectedSummary);
    }

    private void UpdateExtensionOptionEditor(ExtensionOptionSummary? summary)
    {
        bool canEdit = _data is not null && !_data.UnsupportedBytecodeVersion;
        _isUpdatingExtensionEditor = true;
        ExtensionDuplicateOptionButton.IsEnabled = canEdit && summary is not null;
        ExtensionRemoveOptionButton.IsEnabled = canEdit && summary is not null;

        if (summary is null)
        {
            ExtensionOptionNameBox.Text = string.Empty;
            ExtensionOptionValueBox.Text = string.Empty;
            ExtensionOptionKindComboBox.ItemsSource = null;
            ExtensionOptionEditorPanel.Visibility = Visibility.Collapsed;
            _isUpdatingExtensionEditor = false;
            return;
        }

        ExtensionOptionNameBox.Text = summary.Option.Name?.Content ?? string.Empty;
        ExtensionOptionValueBox.Text = summary.Option.Value?.Content ?? string.Empty;
        ExtensionOptionKindComboBox.ItemsSource = Enum.GetValues<UndertaleExtensionOption.OptionKind>();
        ExtensionOptionKindComboBox.SelectedItem = summary.Option.Kind;
        ExtensionOptionEditorPanel.Visibility = Visibility.Visible;
        _isUpdatingExtensionEditor = false;
    }

    private void ExtensionOptionsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ExtensionOptionSummary summary)
            UpdateExtensionOptionEditor(summary);
    }

    private void ExtensionAddOptionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _data.UnsupportedBytecodeVersion || _selectedResource?.Value is not UndertaleExtension extension)
            return;

        int index = extension.Options.Count;
        UndertaleExtensionOption option = new()
        {
            Name = _data.Strings.MakeString($"extensionOption{index}"),
            Value = _data.Strings.MakeString(string.Empty, createNew: true),
            Kind = UndertaleExtensionOption.OptionKind.String
        };
        extension.Options.Add(option);
        MarkDirty();
        RefreshExtensionEditor(extension, GetSelectedExtensionFile(), GetSelectedExtensionFunction(), GetSelectedExtensionArgument());
        RefreshExtensionOptions(extension, option);
        StatusBox.Text = "Added extension option.";
    }

    private void ExtensionDuplicateOptionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleExtension extension ||
            ExtensionOptionsList.SelectedItem is not ExtensionOptionSummary summary)
        {
            return;
        }

        UndertaleExtensionOption option = CloneExtensionOption(summary.Option, _data);
        int insertIndex = Math.Clamp(summary.Index + 1, 0, extension.Options.Count);
        extension.Options.Insert(insertIndex, option);
        MarkDirty();
        RefreshExtensionEditor(extension, GetSelectedExtensionFile(), GetSelectedExtensionFunction(), GetSelectedExtensionArgument());
        RefreshExtensionOptions(extension, option);
        StatusBox.Text = "Duplicated extension option.";
    }

    private void ExtensionRemoveOptionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleExtension extension ||
            ExtensionOptionsList.SelectedItem is not ExtensionOptionSummary summary)
        {
            return;
        }

        int index = extension.Options.IndexOf(summary.Option);
        if (index < 0)
            return;

        extension.Options.RemoveAt(index);
        UndertaleExtensionOption? nextSelection = extension.Options.Count == 0
            ? null
            : extension.Options[Math.Clamp(index, 0, extension.Options.Count - 1)];
        MarkDirty();
        RefreshExtensionEditor(extension, GetSelectedExtensionFile(), GetSelectedExtensionFunction(), GetSelectedExtensionArgument());
        RefreshExtensionOptions(extension, nextSelection);
        StatusBox.Text = "Removed extension option.";
    }

    private void ExtensionOptionBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingExtensionEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleExtension extension ||
            ExtensionOptionsList.SelectedItem is not ExtensionOptionSummary summary)
        {
            return;
        }

        string normalizedValue = NormalizeExtensionOptionValue(ExtensionOptionValueBox.Text, summary.Option.Kind);
        if (normalizedValue != ExtensionOptionValueBox.Text)
            ExtensionOptionValueBox.Text = normalizedValue;

        bool changed = false;
        changed |= UpdateUndertaleString(summary.Option.Name, ExtensionOptionNameBox.Text, _data, out UndertaleString name);
        summary.Option.Name = name;
        changed |= UpdateUndertaleString(summary.Option.Value, normalizedValue, _data, out UndertaleString value);
        summary.Option.Value = value;

        if (!changed)
            return;

        MarkDirty();
        RefreshExtensionEditor(extension, GetSelectedExtensionFile(), GetSelectedExtensionFunction(), GetSelectedExtensionArgument());
        RefreshExtensionOptions(extension, summary.Option);
        StatusBox.Text = "Updated extension option.";
    }

    private void ExtensionOptionKindComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingExtensionEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleExtension extension ||
            ExtensionOptionsList.SelectedItem is not ExtensionOptionSummary summary ||
            ExtensionOptionKindComboBox.SelectedItem is not UndertaleExtensionOption.OptionKind kind)
        {
            return;
        }

        string normalizedValue = NormalizeExtensionOptionValue(summary.Option.Value?.Content ?? string.Empty, kind);
        bool changed = summary.Option.Kind != kind;
        summary.Option.Kind = kind;
        changed |= UpdateUndertaleString(summary.Option.Value, normalizedValue, _data, out UndertaleString value);
        summary.Option.Value = value;

        if (!changed)
            return;

        MarkDirty();
        RefreshExtensionEditor(extension, GetSelectedExtensionFile(), GetSelectedExtensionFunction(), GetSelectedExtensionArgument());
        RefreshExtensionOptions(extension, summary.Option);
        StatusBox.Text = "Updated extension option kind.";
    }

    private void ExtensionAddFileButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _data.UnsupportedBytecodeVersion || _selectedResource?.Value is not UndertaleExtension extension)
            return;

        int index = extension.Files.Count;
        UndertaleExtensionFile file = new()
        {
            Kind = UndertaleExtensionKind.Dll,
            Filename = _data.Strings.MakeString($"NewExtensionFile{index}.dll"),
            InitScript = _data.Strings.MakeString(string.Empty),
            CleanupScript = _data.Strings.MakeString(string.Empty),
            Functions = new UndertalePointerList<UndertaleExtensionFunction>()
        };
        extension.Files.Add(file);
        MarkDirty();
        RefreshExtensionEditor(extension, file, selectedFunction: null, selectedArgument: null);
        StatusBox.Text = "Added extension file.";
    }

    private void ExtensionDuplicateFileButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleExtension extension ||
            ExtensionFilesList.SelectedItem is not ExtensionFileSummary summary)
        {
            return;
        }

        UndertaleExtensionFile file = CloneExtensionFile(summary.File, _data);
        int insertIndex = Math.Clamp(summary.Index + 1, 0, extension.Files.Count);
        extension.Files.Insert(insertIndex, file);
        MarkDirty();
        RefreshExtensionEditor(extension, file, selectedFunction: null, selectedArgument: null);
        StatusBox.Text = "Duplicated extension file.";
    }

    private void ExtensionRemoveFileButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleExtension extension ||
            ExtensionFilesList.SelectedItem is not ExtensionFileSummary summary)
        {
            return;
        }

        int index = extension.Files.IndexOf(summary.File);
        if (index < 0)
            return;

        extension.Files.RemoveAt(index);
        UndertaleExtensionFile? nextSelection = extension.Files.Count == 0
            ? null
            : extension.Files[Math.Clamp(index, 0, extension.Files.Count - 1)];
        MarkDirty();
        RefreshExtensionEditor(extension, nextSelection, selectedFunction: null, selectedArgument: null);
        StatusBox.Text = "Removed extension file.";
    }

    private void ExtensionFileBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingExtensionEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleExtension extension ||
            ExtensionFilesList.SelectedItem is not ExtensionFileSummary summary)
        {
            return;
        }

        bool changed = false;
        changed |= UpdateUndertaleString(summary.File.Filename, ExtensionFileNameBox.Text, _data, out UndertaleString filename);
        summary.File.Filename = filename;
        changed |= UpdateUndertaleString(summary.File.InitScript, ExtensionFileInitBox.Text, _data, out UndertaleString initScript);
        summary.File.InitScript = initScript;
        changed |= UpdateUndertaleString(summary.File.CleanupScript, ExtensionFileCleanupBox.Text, _data, out UndertaleString cleanupScript);
        summary.File.CleanupScript = cleanupScript;

        if (!changed)
            return;

        MarkDirty();
        RefreshExtensionEditor(extension, summary.File, selectedFunction: null, selectedArgument: null);
        StatusBox.Text = "Updated extension file.";
    }

    private void ExtensionFileKindComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingExtensionEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleExtension extension ||
            ExtensionFilesList.SelectedItem is not ExtensionFileSummary summary ||
            ExtensionFileKindComboBox.SelectedItem is not UndertaleExtensionKind kind)
        {
            return;
        }

        if (summary.File.Kind == kind)
            return;

        summary.File.Kind = kind;
        MarkDirty();
        RefreshExtensionEditor(extension, summary.File, selectedFunction: null, selectedArgument: null);
        StatusBox.Text = "Updated extension file kind.";
    }

    private void ExtensionAddFunctionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleExtension extension ||
            ExtensionFilesList.SelectedItem is not ExtensionFileSummary fileSummary)
        {
            return;
        }

        UndertaleExtensionFunction function = CreateExtensionFunction(_data, fileSummary.File.Functions.Count);
        fileSummary.File.Functions.Add(function);
        MarkDirty();
        RefreshExtensionEditor(extension, fileSummary.File, function, selectedArgument: null);
        StatusBox.Text = "Added extension function.";
    }

    private void ExtensionDuplicateFunctionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleExtension extension ||
            ExtensionFilesList.SelectedItem is not ExtensionFileSummary fileSummary ||
            ExtensionFunctionsList.SelectedItem is not ExtensionFunctionSummary functionSummary)
        {
            return;
        }

        UndertaleExtensionFunction function = CloneExtensionFunction(functionSummary.Function, _data);
        int insertIndex = Math.Clamp(functionSummary.Index + 1, 0, fileSummary.File.Functions.Count);
        fileSummary.File.Functions.Insert(insertIndex, function);
        MarkDirty();
        RefreshExtensionEditor(extension, fileSummary.File, function, selectedArgument: null);
        StatusBox.Text = "Duplicated extension function.";
    }

    private void ExtensionRemoveFunctionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleExtension extension ||
            ExtensionFilesList.SelectedItem is not ExtensionFileSummary fileSummary ||
            ExtensionFunctionsList.SelectedItem is not ExtensionFunctionSummary functionSummary)
        {
            return;
        }

        int index = fileSummary.File.Functions.IndexOf(functionSummary.Function);
        if (index < 0)
            return;

        fileSummary.File.Functions.RemoveAt(index);
        UndertaleExtensionFunction? nextSelection = fileSummary.File.Functions.Count == 0
            ? null
            : fileSummary.File.Functions[Math.Clamp(index, 0, fileSummary.File.Functions.Count - 1)];
        MarkDirty();
        RefreshExtensionEditor(extension, fileSummary.File, nextSelection, selectedArgument: null);
        StatusBox.Text = "Removed extension function.";
    }

    private void ExtensionFunctionBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingExtensionEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleExtension extension ||
            ExtensionFilesList.SelectedItem is not ExtensionFileSummary fileSummary ||
            ExtensionFunctionsList.SelectedItem is not ExtensionFunctionSummary functionSummary)
        {
            return;
        }

        if (!uint.TryParse(ExtensionFunctionIdBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint id) ||
            !uint.TryParse(ExtensionFunctionKindBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint kind))
        {
            UpdateExtensionFunctionEditor(functionSummary, selectedArgument: null);
            StatusBox.Text = "Invalid extension function ID or kind. Use non-negative integers.";
            return;
        }

        bool changed = false;
        changed |= UpdateUndertaleString(functionSummary.Function.Name, ExtensionFunctionNameBox.Text, _data, out UndertaleString name);
        functionSummary.Function.Name = name;
        changed |= UpdateUndertaleString(functionSummary.Function.ExtName, ExtensionFunctionExternalNameBox.Text, _data, out UndertaleString extName);
        functionSummary.Function.ExtName = extName;
        if (functionSummary.Function.ID != id)
        {
            functionSummary.Function.ID = id;
            changed = true;
        }
        if (functionSummary.Function.Kind != kind)
        {
            functionSummary.Function.Kind = kind;
            changed = true;
        }

        if (!changed)
            return;

        MarkDirty();
        RefreshExtensionEditor(extension, fileSummary.File, functionSummary.Function, selectedArgument: null);
        StatusBox.Text = "Updated extension function.";
    }

    private void ExtensionFunctionReturnTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingExtensionEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleExtension extension ||
            ExtensionFilesList.SelectedItem is not ExtensionFileSummary fileSummary ||
            ExtensionFunctionsList.SelectedItem is not ExtensionFunctionSummary functionSummary ||
            ExtensionFunctionReturnTypeComboBox.SelectedItem is not UndertaleExtensionVarType returnType)
        {
            return;
        }

        if (functionSummary.Function.RetType == returnType)
            return;

        functionSummary.Function.RetType = returnType;
        MarkDirty();
        RefreshExtensionEditor(extension, fileSummary.File, functionSummary.Function, selectedArgument: null);
        StatusBox.Text = "Updated extension function return type.";
    }

    private void ExtensionAddArgumentButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleExtension extension ||
            ExtensionFilesList.SelectedItem is not ExtensionFileSummary fileSummary ||
            ExtensionFunctionsList.SelectedItem is not ExtensionFunctionSummary functionSummary)
        {
            return;
        }

        UndertaleExtensionFunctionArg argument = new(UndertaleExtensionVarType.Double);
        functionSummary.Function.Arguments.Add(argument);
        MarkDirty();
        RefreshExtensionEditor(extension, fileSummary.File, functionSummary.Function, argument);
        StatusBox.Text = "Added extension function argument.";
    }

    private void ExtensionRemoveArgumentButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleExtension extension ||
            ExtensionFilesList.SelectedItem is not ExtensionFileSummary fileSummary ||
            ExtensionFunctionsList.SelectedItem is not ExtensionFunctionSummary functionSummary ||
            ExtensionArgumentsList.SelectedItem is not ExtensionArgumentSummary argumentSummary)
        {
            return;
        }

        int index = functionSummary.Function.Arguments.IndexOf(argumentSummary.Argument);
        if (index < 0)
            return;

        functionSummary.Function.Arguments.RemoveAt(index);
        UndertaleExtensionFunctionArg? nextSelection = functionSummary.Function.Arguments.Count == 0
            ? null
            : functionSummary.Function.Arguments[Math.Clamp(index, 0, functionSummary.Function.Arguments.Count - 1)];
        MarkDirty();
        RefreshExtensionEditor(extension, fileSummary.File, functionSummary.Function, nextSelection);
        StatusBox.Text = "Removed extension function argument.";
    }

    private void ExtensionArgumentTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingExtensionEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleExtension extension ||
            ExtensionFilesList.SelectedItem is not ExtensionFileSummary fileSummary ||
            ExtensionFunctionsList.SelectedItem is not ExtensionFunctionSummary functionSummary ||
            ExtensionArgumentsList.SelectedItem is not ExtensionArgumentSummary argumentSummary ||
            ExtensionArgumentTypeComboBox.SelectedItem is not UndertaleExtensionVarType argType)
        {
            return;
        }

        if (argumentSummary.Argument.Type == argType)
            return;

        argumentSummary.Argument.Type = argType;
        MarkDirty();
        RefreshExtensionEditor(extension, fileSummary.File, functionSummary.Function, argumentSummary.Argument);
        StatusBox.Text = "Updated extension argument type.";
    }

    private UndertaleExtensionFile? GetSelectedExtensionFile()
    {
        return ExtensionFilesList.SelectedItem is ExtensionFileSummary summary ? summary.File : null;
    }

    private UndertaleExtensionFunction? GetSelectedExtensionFunction()
    {
        return ExtensionFunctionsList.SelectedItem is ExtensionFunctionSummary summary ? summary.Function : null;
    }

    private UndertaleExtensionFunctionArg? GetSelectedExtensionArgument()
    {
        return ExtensionArgumentsList.SelectedItem is ExtensionArgumentSummary summary ? summary.Argument : null;
    }

    private void ShowParticleSystemEditorFor(ResourceItem item)
    {
        if (item.Value is not UndertaleParticleSystem particleSystem)
        {
            HideParticleSystemEditor();
            return;
        }

        ParticleSystemEditorPanel.Visibility = Visibility.Visible;
        ParticleAddNewEmitterButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion;
        ParticleLinkEmitterButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion && _data.ParticleSystemEmitters.Count > 0;
        RefreshParticleSystemEditor(particleSystem, selectedEmitter: null);
    }

    private void HideParticleSystemEditor()
    {
        ParticleSystemSummaryText.Text = string.Empty;
        ParticleEmittersList.ItemsSource = null;
        ParticleEmittersList.SelectedItem = null;
        ParticleEmitterLinkComboBox.ItemsSource = null;
        ParticleEmitterLinkComboBox.SelectedItem = null;
        ParticleAddNewEmitterButton.IsEnabled = false;
        ParticleLinkEmitterButton.IsEnabled = false;
        ParticleOpenEmitterButton.IsEnabled = false;
        ParticleUnlinkEmitterButton.IsEnabled = false;
        ParticleSystemEditorPanel.Visibility = Visibility.Collapsed;
    }

    private void RefreshParticleSystemEditor(UndertaleParticleSystem particleSystem, UndertaleParticleSystemEmitter? selectedEmitter)
    {
        if (_data is null)
            return;

        ParticleSystemSummaryText.Text = BuildParticleSystemSummary(particleSystem);
        ParticleEmitterSummary[] summaries = BuildParticleEmitterSummaries(particleSystem, _data).ToArray();
        ParticleEmittersList.ItemsSource = summaries;

        ParticleEmitterSummary? selectedSummary = selectedEmitter is null
            ? summaries.FirstOrDefault()
            : summaries.FirstOrDefault(summary => ReferenceEquals(summary.Emitter, selectedEmitter));
        ParticleEmittersList.SelectedItem = selectedSummary;

        ParticleEmitterReferenceItem[] referenceItems = BuildParticleEmitterReferenceItems(_data).ToArray();
        ParticleEmitterLinkComboBox.ItemsSource = referenceItems;
        ParticleEmitterLinkComboBox.SelectedItem = referenceItems.FirstOrDefault(item => selectedSummary is not null && ReferenceEquals(item.Emitter, selectedSummary.Emitter)) ??
                                                   referenceItems.FirstOrDefault();

        bool canEdit = !_data.UnsupportedBytecodeVersion;
        ParticleAddNewEmitterButton.IsEnabled = canEdit;
        ParticleLinkEmitterButton.IsEnabled = canEdit && referenceItems.Length > 0;
        ParticleOpenEmitterButton.IsEnabled = selectedSummary?.Emitter is not null;
        ParticleUnlinkEmitterButton.IsEnabled = canEdit && selectedSummary is not null;
    }

    private void ParticleEmittersList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (_selectedResource?.Value is not UndertaleParticleSystem particleSystem || e.ClickedItem is not ParticleEmitterSummary summary)
            return;

        RefreshParticleSystemEditor(particleSystem, summary.Emitter);
    }

    private void ParticleAddNewEmitterButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _data.UnsupportedBytecodeVersion || _selectedResource?.Value is not UndertaleParticleSystem particleSystem)
            return;

        UndertaleParticleSystemEmitter emitter = CreateParticleSystemEmitter(_data, _data.ParticleSystemEmitters.Count);
        _data.ParticleSystemEmitters.Add(emitter);
        particleSystem.Emitters.Add(new UndertaleResourceById<UndertaleParticleSystemEmitter, UndertaleChunkPSEM>(emitter));
        MarkDirty();
        RefreshParticleSystemEditor(particleSystem, emitter);
        StatusBox.Text = "Created and linked particle system emitter.";
    }

    private void ParticleLinkEmitterButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleParticleSystem particleSystem ||
            ParticleEmitterLinkComboBox.SelectedItem is not ParticleEmitterReferenceItem item ||
            item.Emitter is null)
        {
            return;
        }

        particleSystem.Emitters.Add(new UndertaleResourceById<UndertaleParticleSystemEmitter, UndertaleChunkPSEM>(item.Emitter));
        MarkDirty();
        RefreshParticleSystemEditor(particleSystem, item.Emitter);
        StatusBox.Text = "Linked particle system emitter.";
    }

    private void ParticleOpenEmitterButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || ParticleEmittersList.SelectedItem is not ParticleEmitterSummary summary || summary.Emitter is null)
            return;

        int emitterIndex = _data.ParticleSystemEmitters.IndexOf(summary.Emitter);
        if (emitterIndex >= 0)
            NavigateToResource("Particle system emitters", emitterIndex);
    }

    private void ParticleUnlinkEmitterButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleParticleSystem particleSystem ||
            ParticleEmittersList.SelectedItem is not ParticleEmitterSummary summary)
        {
            return;
        }

        if (summary.Index < 0 || summary.Index >= particleSystem.Emitters.Count)
            return;

        particleSystem.Emitters.RemoveAt(summary.Index);
        UndertaleParticleSystemEmitter? nextSelection = particleSystem.Emitters.Count == 0
            ? null
            : particleSystem.Emitters[Math.Clamp(summary.Index, 0, particleSystem.Emitters.Count - 1)].Resource;
        MarkDirty();
        RefreshParticleSystemEditor(particleSystem, nextSelection);
        StatusBox.Text = "Unlinked particle system emitter.";
    }

    private void ShowParticleEmitterEditorFor(ResourceItem item)
    {
        if (_data is null || item.Value is not UndertaleParticleSystemEmitter emitter)
        {
            HideParticleEmitterEditor();
            return;
        }

        ParticleEmitterEditorPanel.Visibility = Visibility.Visible;
        RefreshParticleEmitterEditor(emitter);
    }

    private void HideParticleEmitterEditor()
    {
        _isUpdatingParticleEmitterEditor = true;
        ParticleEmitterSummaryText.Text = string.Empty;
        ParticleEmitterModeComboBox.ItemsSource = null;
        ParticleEmitterDistributionComboBox.ItemsSource = null;
        ParticleEmitterShapeComboBox.ItemsSource = null;
        ParticleEmitterTextureComboBox.ItemsSource = null;
        ParticleEmitterDelayUnitComboBox.ItemsSource = null;
        ParticleEmitterIntervalUnitComboBox.ItemsSource = null;
        ParticleEmitterSpriteComboBox.ItemsSource = null;
        ParticleEmitterSpriteComboBox.SelectedItem = null;
        ParticleEmitterSpawnOnDeathComboBox.ItemsSource = null;
        ParticleEmitterSpawnOnDeathComboBox.SelectedItem = null;
        ParticleEmitterSpawnOnUpdateComboBox.ItemsSource = null;
        ParticleEmitterSpawnOnUpdateComboBox.SelectedItem = null;
        ParticleEmitterSpawnOnDeathCountBox.Text = string.Empty;
        ParticleEmitterSpawnOnUpdateCountBox.Text = string.Empty;
        ParticleEmitterOpenSpriteButton.IsEnabled = false;
        ParticleEmitterOpenSpawnOnDeathButton.IsEnabled = false;
        ParticleEmitterOpenSpawnOnUpdateButton.IsEnabled = false;
        ParticleEmitterEditorPanel.Visibility = Visibility.Collapsed;
        _isUpdatingParticleEmitterEditor = false;
    }

    private void RefreshParticleEmitterEditor(UndertaleParticleSystemEmitter emitter)
    {
        if (_data is null)
            return;

        _isUpdatingParticleEmitterEditor = true;
        ParticleEmitterSummaryText.Text = BuildParticleEmitterSummary(emitter, _data);

        ParticleEmitterModeComboBox.ItemsSource = Enum.GetValues<UndertaleParticleSystemEmitter.EmitMode>();
        ParticleEmitterModeComboBox.SelectedItem = emitter.Mode;
        ParticleEmitterDistributionComboBox.ItemsSource = Enum.GetValues<UndertaleParticleSystemEmitter.DistributionEnum>();
        ParticleEmitterDistributionComboBox.SelectedItem = emitter.Distribution;
        ParticleEmitterShapeComboBox.ItemsSource = Enum.GetValues<UndertaleParticleSystemEmitter.EmitterShape>();
        ParticleEmitterShapeComboBox.SelectedItem = emitter.Shape;
        ParticleEmitterTextureComboBox.ItemsSource = Enum.GetValues<UndertaleParticleSystemEmitter.TextureEnum>();
        ParticleEmitterTextureComboBox.SelectedItem = emitter.Texture;
        ParticleEmitterDelayUnitComboBox.ItemsSource = Enum.GetValues<UndertaleParticleSystemEmitter.TimeUnitEnum>();
        ParticleEmitterDelayUnitComboBox.SelectedItem = emitter.DelayUnit;
        ParticleEmitterIntervalUnitComboBox.ItemsSource = Enum.GetValues<UndertaleParticleSystemEmitter.TimeUnitEnum>();
        ParticleEmitterIntervalUnitComboBox.SelectedItem = emitter.IntervalUnit;

        SpriteReferenceItem[] spriteItems = BuildSpriteReferenceItems(_data, includeNull: true).ToArray();
        ParticleEmitterSpriteComboBox.ItemsSource = spriteItems;
        ParticleEmitterSpriteComboBox.SelectedItem = spriteItems.FirstOrDefault(item => ReferenceEquals(item.Sprite, emitter.Sprite)) ??
                                                     spriteItems.FirstOrDefault();

        ParticleEmitterReferenceItem[] emitterItems = BuildParticleEmitterReferenceItems(_data, includeNull: true).ToArray();
        ParticleEmitterSpawnOnDeathComboBox.ItemsSource = emitterItems;
        ParticleEmitterSpawnOnDeathComboBox.SelectedItem = emitterItems.FirstOrDefault(item => ReferenceEquals(item.Emitter, emitter.SpawnOnDeath)) ??
                                                           emitterItems.FirstOrDefault();
        ParticleEmitterSpawnOnUpdateComboBox.ItemsSource = emitterItems;
        ParticleEmitterSpawnOnUpdateComboBox.SelectedItem = emitterItems.FirstOrDefault(item => ReferenceEquals(item.Emitter, emitter.SpawnOnUpdate)) ??
                                                            emitterItems.FirstOrDefault();

        ParticleEmitterSpawnOnDeathCountBox.Text = emitter.SpawnOnDeathCount.ToString(CultureInfo.InvariantCulture);
        ParticleEmitterSpawnOnUpdateCountBox.Text = emitter.SpawnOnUpdateCount.ToString(CultureInfo.InvariantCulture);
        ParticleEmitterOpenSpriteButton.IsEnabled = emitter.Sprite is not null;
        ParticleEmitterOpenSpawnOnDeathButton.IsEnabled = emitter.SpawnOnDeath is not null;
        ParticleEmitterOpenSpawnOnUpdateButton.IsEnabled = emitter.SpawnOnUpdate is not null;
        _isUpdatingParticleEmitterEditor = false;
    }

    private void ParticleEmitterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingParticleEmitterEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleParticleSystemEmitter emitter)
        {
            return;
        }

        if (ReferenceEquals(sender, ParticleEmitterModeComboBox) && ParticleEmitterModeComboBox.SelectedItem is UndertaleParticleSystemEmitter.EmitMode mode)
            emitter.Mode = mode;
        else if (ReferenceEquals(sender, ParticleEmitterDistributionComboBox) && ParticleEmitterDistributionComboBox.SelectedItem is UndertaleParticleSystemEmitter.DistributionEnum distribution)
            emitter.Distribution = distribution;
        else if (ReferenceEquals(sender, ParticleEmitterShapeComboBox) && ParticleEmitterShapeComboBox.SelectedItem is UndertaleParticleSystemEmitter.EmitterShape shape)
            emitter.Shape = shape;
        else if (ReferenceEquals(sender, ParticleEmitterTextureComboBox) && ParticleEmitterTextureComboBox.SelectedItem is UndertaleParticleSystemEmitter.TextureEnum texture)
            emitter.Texture = texture;
        else if (ReferenceEquals(sender, ParticleEmitterDelayUnitComboBox) && ParticleEmitterDelayUnitComboBox.SelectedItem is UndertaleParticleSystemEmitter.TimeUnitEnum delayUnit)
            emitter.DelayUnit = delayUnit;
        else if (ReferenceEquals(sender, ParticleEmitterIntervalUnitComboBox) && ParticleEmitterIntervalUnitComboBox.SelectedItem is UndertaleParticleSystemEmitter.TimeUnitEnum intervalUnit)
            emitter.IntervalUnit = intervalUnit;
        else
            return;

        MarkDirty();
        RefreshParticleEmitterEditor(emitter);
        RefreshCurrentDetails();
        StatusBox.Text = "Updated particle emitter.";
    }

    private void ParticleEmitterSpriteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingParticleEmitterEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleParticleSystemEmitter emitter ||
            ParticleEmitterSpriteComboBox.SelectedItem is not SpriteReferenceItem item)
        {
            return;
        }

        emitter.Sprite = item.Sprite;
        MarkDirty();
        RefreshParticleEmitterEditor(emitter);
        RefreshCurrentDetails();
        StatusBox.Text = "Updated particle emitter sprite.";
    }

    private void ParticleEmitterOpenSpriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _selectedResource?.Value is not UndertaleParticleSystemEmitter emitter || emitter.Sprite is null)
            return;

        int spriteIndex = _data.Sprites.IndexOf(emitter.Sprite);
        if (spriteIndex >= 0)
            NavigateToResource("Sprites", spriteIndex);
    }

    private void ParticleEmitterSpawnComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingParticleEmitterEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleParticleSystemEmitter emitter)
        {
            return;
        }

        if (ReferenceEquals(sender, ParticleEmitterSpawnOnDeathComboBox) &&
            ParticleEmitterSpawnOnDeathComboBox.SelectedItem is ParticleEmitterReferenceItem deathItem)
        {
            emitter.SpawnOnDeath = deathItem.Emitter;
        }
        else if (ReferenceEquals(sender, ParticleEmitterSpawnOnUpdateComboBox) &&
                 ParticleEmitterSpawnOnUpdateComboBox.SelectedItem is ParticleEmitterReferenceItem updateItem)
        {
            emitter.SpawnOnUpdate = updateItem.Emitter;
        }
        else
        {
            return;
        }

        MarkDirty();
        RefreshParticleEmitterEditor(emitter);
        RefreshCurrentDetails();
        StatusBox.Text = "Updated particle emitter spawn link.";
    }

    private void ParticleEmitterSpawnCountBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingParticleEmitterEditor ||
            _data is null ||
            _data.UnsupportedBytecodeVersion ||
            _selectedResource?.Value is not UndertaleParticleSystemEmitter emitter ||
            sender is not TextBox textBox)
        {
            return;
        }

        if (!int.TryParse(textBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            StatusBox.Text = "Particle emitter spawn count must be an integer.";
            RefreshParticleEmitterEditor(emitter);
            return;
        }

        if (textBox == ParticleEmitterSpawnOnDeathCountBox)
            emitter.SpawnOnDeathCount = value;
        else if (textBox == ParticleEmitterSpawnOnUpdateCountBox)
            emitter.SpawnOnUpdateCount = value;
        else
            return;

        MarkDirty();
        RefreshParticleEmitterEditor(emitter);
        RefreshCurrentDetails();
        StatusBox.Text = "Updated particle emitter spawn count.";
    }

    private void ParticleEmitterOpenSpawnButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _selectedResource?.Value is not UndertaleParticleSystemEmitter emitter)
            return;

        UndertaleParticleSystemEmitter? target = ReferenceEquals(sender, ParticleEmitterOpenSpawnOnDeathButton)
            ? emitter.SpawnOnDeath
            : emitter.SpawnOnUpdate;
        if (target is null)
            return;

        int emitterIndex = _data.ParticleSystemEmitters.IndexOf(target);
        if (emitterIndex >= 0)
            NavigateToResource("Particle system emitters", emitterIndex);
    }

    private void ShowTexturePageItemReferencesFor(ResourceItem item)
    {
        if (_data is null || item.Value is not UndertaleTexturePageItem texture)
        {
            HideTexturePageItemReferences();
            return;
        }

        TexturePageItemReferenceItem[] references = BuildTexturePageItemReferences(texture, out bool truncated).ToArray();
        TexturePageItemReferencesPanel.Visibility = Visibility.Visible;
        TexturePageItemReferencesList.ItemsSource = references;
        TexturePageItemReferencesList.SelectedIndex = references.Length > 0 ? 0 : -1;
        TexturePageItemOpenSelectedReferenceButton.IsEnabled = references.Length > 0;
        TexturePageItemOpenTexturePageButton.IsEnabled = texture.TexturePage is not null &&
                                                         _data.EmbeddedTextures.IndexOf(texture.TexturePage) >= 0;

        string textureName = FormatTitle(texture.Name?.Content);
        if (references.Length == 0)
        {
            TexturePageItemReferencesSummaryText.Text = $"{textureName} has no sprite, background, font, or room references.";
        }
        else if (truncated)
        {
            TexturePageItemReferencesSummaryText.Text =
                $"Showing first {references.Length.ToString(CultureInfo.InvariantCulture)} references for {textureName}.";
        }
        else
        {
            TexturePageItemReferencesSummaryText.Text =
                $"{references.Length.ToString(CultureInfo.InvariantCulture)} reference(s) use {textureName}.";
        }
    }

    private void HideTexturePageItemReferences()
    {
        TexturePageItemReferencesSummaryText.Text = string.Empty;
        TexturePageItemReferencesList.ItemsSource = null;
        TexturePageItemReferencesList.SelectedItem = null;
        TexturePageItemOpenSelectedReferenceButton.IsEnabled = false;
        TexturePageItemOpenTexturePageButton.IsEnabled = false;
        TexturePageItemReferencesPanel.Visibility = Visibility.Collapsed;
    }

    private IEnumerable<TexturePageItemReferenceItem> BuildTexturePageItemReferences(
        UndertaleTexturePageItem target,
        out bool truncated)
    {
        truncated = false;
        bool wasTruncated = false;
        List<TexturePageItemReferenceItem> references = new();

        bool Add(string title, string subtitle, string categoryLabel, int itemIndex, int spriteFrameIndex = -1)
        {
            if (references.Count >= TexturePageItemReferenceDisplayLimit)
            {
                wasTruncated = true;
                return false;
            }

            references.Add(new TexturePageItemReferenceItem(title, subtitle, categoryLabel, itemIndex, spriteFrameIndex));
            return true;
        }

        for (int spriteIndex = 0; spriteIndex < _data!.Sprites.Count; spriteIndex++)
        {
            UndertaleSprite sprite = _data.Sprites[spriteIndex];
            for (int frameIndex = 0; frameIndex < sprite.Textures.Count; frameIndex++)
            {
                if (!ReferenceEquals(sprite.Textures[frameIndex]?.Texture, target))
                    continue;

                string spriteName = FormatNamedResourceTitle(sprite.Name?.Content, "Sprite", spriteIndex);
                if (!Add(
                    $"Sprite #{spriteIndex}: {spriteName}",
                    $"Frame {frameIndex}; {sprite.Width}x{sprite.Height}, origin {sprite.OriginXWrapper},{sprite.OriginYWrapper}",
                    "Sprites",
                    spriteIndex,
                    frameIndex))
                {
                    truncated = wasTruncated;
                    return references;
                }
            }
        }

        for (int backgroundIndex = 0; backgroundIndex < _data.Backgrounds.Count; backgroundIndex++)
        {
            UndertaleBackground background = _data.Backgrounds[backgroundIndex];
            if (!ReferenceEquals(background.Texture, target))
                continue;

            string backgroundName = FormatNamedResourceTitle(background.Name?.Content, "Background", backgroundIndex);
            if (!Add(
                $"Background #{backgroundIndex}: {backgroundName}",
                $"{background.GMS2TileWidth}x{background.GMS2TileHeight} tile size; {background.GMS2TileCount} tile(s)",
                "Backgrounds",
                backgroundIndex))
            {
                truncated = wasTruncated;
                return references;
            }
        }

        for (int fontIndex = 0; fontIndex < _data.Fonts.Count; fontIndex++)
        {
            UndertaleFont font = _data.Fonts[fontIndex];
            if (!ReferenceEquals(font.Texture, target))
                continue;

            string fontName = FormatNamedResourceTitle(font.Name?.Content, "Font", fontIndex);
            if (!Add(
                $"Font #{fontIndex}: {fontName}",
                $"{font.Glyphs?.Count ?? 0} glyph(s), em size {font.EmSize}",
                "Fonts",
                fontIndex))
            {
                truncated = wasTruncated;
                return references;
            }
        }

        for (int roomIndex = 0; roomIndex < _data.Rooms.Count; roomIndex++)
        {
            UndertaleRoom room = _data.Rooms[roomIndex];
            if (!AddRoomTextureReferences(room, roomIndex, target, Add))
            {
                truncated = wasTruncated;
                return references;
            }
        }

        truncated = wasTruncated;
        return references;
    }

    private static bool AddRoomTextureReferences(
        UndertaleRoom room,
        int roomIndex,
        UndertaleTexturePageItem target,
        Func<string, string, string, int, int, bool> add)
    {
        string roomName = FormatNamedResourceTitle(room.Name?.Content, "Room", roomIndex);

        if (room.Backgrounds is not null)
        {
            for (int backgroundIndex = 0; backgroundIndex < room.Backgrounds.Count; backgroundIndex++)
            {
                UndertaleRoom.Background background = room.Backgrounds[backgroundIndex];
                if (!ReferenceEquals(background.BackgroundDefinition?.Texture, target))
                    continue;

                if (!add(
                    $"Room #{roomIndex}: {roomName}",
                    $"Background slot {backgroundIndex}; offset {background.X},{background.Y}; foreground {background.Foreground}",
                    "Rooms",
                    roomIndex,
                    -1))
                {
                    return false;
                }
            }
        }

        if (room.Tiles is not null)
        {
            for (int tileIndex = 0; tileIndex < room.Tiles.Count; tileIndex++)
            {
                UndertaleRoom.Tile tile = room.Tiles[tileIndex];
                if (!ReferenceEquals(tile.Tpag, target))
                    continue;

                if (!add(
                    $"Room #{roomIndex}: {roomName}",
                    $"Legacy tile {tileIndex}; position {tile.X},{tile.Y}; size {tile.Width}x{tile.Height}",
                    "Rooms",
                    roomIndex,
                    -1))
                {
                    return false;
                }
            }
        }

        foreach (UndertaleRoom.Layer layer in room.Layers ?? [])
        {
            string layerName = FormatTitle(layer.LayerName?.Content);
            if (layer.LayerType == UndertaleRoom.LayerType.Background &&
                ReferenceEquals(layer.BackgroundData?.Sprite?.Textures.FirstOrDefault()?.Texture, target))
            {
                if (!add(
                    $"Room #{roomIndex}: {roomName}",
                    $"Background layer {layerName}; depth {layer.LayerDepth}",
                    "Rooms",
                    roomIndex,
                    -1))
                {
                    return false;
                }
            }
            else if (layer.LayerType == UndertaleRoom.LayerType.Assets && layer.AssetsData?.LegacyTiles is not null)
            {
                for (int tileIndex = 0; tileIndex < layer.AssetsData.LegacyTiles.Count; tileIndex++)
                {
                    UndertaleRoom.Tile tile = layer.AssetsData.LegacyTiles[tileIndex];
                    if (!ReferenceEquals(tile.Tpag, target))
                        continue;

                    if (!add(
                        $"Room #{roomIndex}: {roomName}",
                        $"Layer {layerName} legacy tile {tileIndex}; position {tile.X},{tile.Y}; size {tile.Width}x{tile.Height}",
                        "Rooms",
                        roomIndex,
                        -1))
                    {
                        return false;
                    }
                }
            }
            else if (layer.LayerType == UndertaleRoom.LayerType.Tiles &&
                     ReferenceEquals(layer.TilesData?.Background?.Texture, target) &&
                     RoomTileLayerHasVisibleTiles(layer.TilesData))
            {
                if (!add(
                    $"Room #{roomIndex}: {roomName}",
                    $"Tile layer {layerName}; {layer.TilesData.TilesX}x{layer.TilesData.TilesY} cells",
                    "Rooms",
                    roomIndex,
                    -1))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool RoomTileLayerHasVisibleTiles(UndertaleRoom.Layer.LayerTilesData? tilesData)
    {
        if (tilesData?.TileData is null)
            return false;

        foreach (uint[] row in tilesData.TileData)
        {
            foreach (uint tile in row)
            {
                if ((tile & RoomTileIndexMask) != 0)
                    return true;
            }
        }

        return false;
    }

    private void TexturePageItemReferencesList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not TexturePageItemReferenceItem reference)
            return;

        OpenTexturePageItemReference(reference);
    }

    private void TexturePageItemReferencesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        TexturePageItemOpenSelectedReferenceButton.IsEnabled =
            TexturePageItemReferencesList.SelectedItem is TexturePageItemReferenceItem;
    }

    private void TexturePageItemOpenSelectedReferenceButton_Click(object sender, RoutedEventArgs e)
    {
        if (TexturePageItemReferencesList.SelectedItem is TexturePageItemReferenceItem reference)
            OpenTexturePageItemReference(reference);
    }

    private void OpenTexturePageItemReference(TexturePageItemReferenceItem reference)
    {
        if (!NavigateToResource(reference.CategoryLabel, reference.ItemIndex))
            return;

        if (reference.CategoryLabel == "Sprites" &&
            reference.SpriteFrameIndex >= 0 &&
            _selectedResource?.Value is UndertaleSprite sprite &&
            SpriteFrameComboBox.Items.Count > 0)
        {
            int frameIndex = Math.Clamp(reference.SpriteFrameIndex, 0, SpriteFrameComboBox.Items.Count - 1);
            _isUpdatingSpriteFrame = true;
            SpriteFrameComboBox.SelectedIndex = frameIndex;
            _isUpdatingSpriteFrame = false;
            if (SpriteTexturesList.Items.Count > 0)
                SpriteTexturesList.SelectedIndex = frameIndex;
            SelectSpriteEditorMode(FramesTab);
            RenderOrResetSpritePreview(sprite);
            StatusBox.Text = $"Opened {reference.Title}, frame {reference.SpriteFrameIndex}.";
        }
        else
        {
            StatusBox.Text = $"Opened {reference.Title}.";
        }
    }

    private void TexturePageItemOpenTexturePageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _selectedResource?.Value is not UndertaleTexturePageItem item || item.TexturePage is null)
            return;

        int texturePageIndex = _data.EmbeddedTextures.IndexOf(item.TexturePage);
        if (texturePageIndex < 0)
        {
            StatusBox.Text = "Could not open source embedded texture for this page item.";
            return;
        }

        _pendingTexturePreviewPageItemSelection = item;
        NavigateToResource("Embedded textures", texturePageIndex);
        StatusBox.Text = $"Opened source texture page for {FormatTitle(item.Name?.Content)}.";
    }

    private async void ShowTexturePreviewFor(ResourceItem item, bool forceRender = false)
    {
        if (!TryGetPreviewableTextureValue(item.Value, out object? textureValue) || textureValue is null)
        {
            _pendingTexturePreviewPageItemSelection = null;
            HideTexturePreview();
            return;
        }

        if (_pendingTexturePreviewPageItemSelection is { } pendingSelection &&
            (textureValue is not UndertaleEmbeddedTexture embeddedTexture ||
             !ReferenceEquals(pendingSelection.TexturePage, embeddedTexture)))
        {
            _pendingTexturePreviewPageItemSelection = null;
        }

        _texturePreviewCts?.Cancel();
        _texturePreviewCts = null;
        int generation = ++_texturePreviewGeneration;
        TexturePreviewPanel.Visibility = Visibility.Visible;
        TexturePreviewImage.Source = null;
        UpdateImagePreviewOpenStates();
        ClearTexturePreviewAtlasOverlay();
        ResetTexturePreviewViewport();
        TexturePreviewInfoText.Text = BuildTexturePreviewInfo(textureValue);
        ToolTipService.SetToolTip(
            TexturePreviewImage,
            textureValue is UndertaleEmbeddedTexture
                ? "Click a highlighted region to open its texture page item. Click empty space to open full preview."
                : "Open full preview");
        TextureRenderPreviewButton.IsEnabled = true;
        TextureImportButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion;
        TextureExportButton.IsEnabled = true;
        TextureOpenPreviewButton.IsEnabled = false;
        TexturePreviewZoomSlider.IsEnabled = false;
        TexturePreviewActualSizeButton.IsEnabled = false;
        TexturePreviewFitButton.IsEnabled = false;

        if (!forceRender && !ShouldAutoRenderPreviews())
        {
            TexturePreviewInfoText.Text = $"{TexturePreviewInfoText.Text}{Environment.NewLine}Auto preview render is disabled.";
            return;
        }

        if (!forceRender && ShouldDeferTexturePreview(textureValue, out long pixelCount))
        {
            TexturePreviewInfoText.Text = $"{TexturePreviewInfoText.Text}{Environment.NewLine}Large preview deferred ({FormatPixelCount(pixelCount)}).";
            return;
        }

        CancellationTokenSource previewCts = new();
        _texturePreviewCts = previewCts;
        CancellationToken token = previewCts.Token;
        TextureRenderPreviewButton.IsEnabled = false;
        TextureImportButton.IsEnabled = false;
        TextureExportButton.IsEnabled = false;

        try
        {
            await System.Threading.Tasks.Task.Delay(75, token);
            byte[] previewBytes = await System.Threading.Tasks.Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                byte[] bytes = GetCachedTexturePreviewPng(textureValue);
                token.ThrowIfCancellationRequested();
                return bytes;
            }, token);
            if (token.IsCancellationRequested || generation != _texturePreviewGeneration)
                return;

            BitmapImage preview = LoadBitmapImage(previewBytes);
            if (token.IsCancellationRequested || generation != _texturePreviewGeneration)
                return;

            TexturePreviewImage.Source = preview;
            UpdateTexturePreviewAtlasOverlay(textureValue, preview);
            TexturePreviewZoomSlider.IsEnabled = true;
            TexturePreviewActualSizeButton.IsEnabled = true;
            TexturePreviewFitButton.IsEnabled = true;
            TextureRenderPreviewButton.IsEnabled = true;
            TextureImportButton.IsEnabled = !_data?.UnsupportedBytecodeVersion ?? false;
            TextureExportButton.IsEnabled = true;
            TextureOpenPreviewButton.IsEnabled = true;
            UpdateImagePreviewOpenStates();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (generation != _texturePreviewGeneration)
                return;

            TexturePreviewInfoText.Text = $"Could not render texture preview: {ex.Message}";
            TextureRenderPreviewButton.IsEnabled = true;
            TextureImportButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion;
            TextureExportButton.IsEnabled = true;
        }
        finally
        {
            if (ReferenceEquals(_texturePreviewCts, previewCts))
                _texturePreviewCts = null;
            previewCts.Dispose();
        }
    }

    private void HideTexturePreview()
    {
        _texturePreviewCts?.Cancel();
        _texturePreviewGeneration++;
        TexturePreviewImage.Source = null;
        UpdateImagePreviewOpenStates();
        ClearTexturePreviewAtlasOverlay();
        ResetTexturePreviewViewport();
        TexturePreviewInfoText.Text = string.Empty;
        ToolTipService.SetToolTip(TexturePreviewImage, "Open full preview");
        TextureRenderPreviewButton.IsEnabled = false;
        TextureImportButton.IsEnabled = false;
        TextureExportButton.IsEnabled = false;
        TextureOpenPreviewButton.IsEnabled = false;
        TexturePreviewZoomSlider.IsEnabled = false;
        TexturePreviewActualSizeButton.IsEnabled = false;
        TexturePreviewFitButton.IsEnabled = false;
        TexturePreviewPanel.Visibility = Visibility.Collapsed;
        UpdateImagePreviewOpenStates();
    }

    private void TextureRenderPreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedResource is null)
            return;

        ShowTexturePreviewFor(_selectedResource, forceRender: true);
    }

    private void ResetTexturePreviewViewport()
    {
        _isUpdatingTexturePreviewZoom = true;
        TexturePreviewZoomSlider.Value = 1;
        _isUpdatingTexturePreviewZoom = false;
        TexturePreviewScrollViewer.ChangeView(0, 0, 1);
    }

    private void TexturePreviewZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isUpdatingTexturePreviewZoom || TexturePreviewScrollViewer is null)
            return;

        TexturePreviewScrollViewer.ChangeView(null, null, (float)e.NewValue);
    }

    private void TexturePreviewScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (!TexturePreviewZoomSlider.IsEnabled)
            return;

        int wheelDelta = e.GetCurrentPoint(TexturePreviewScrollViewer).Properties.MouseWheelDelta;
        if (wheelDelta == 0)
            return;

        double factor = wheelDelta > 0 ? 1.15 : 1 / 1.15;
        SetTexturePreviewZoom(TexturePreviewZoomSlider.Value * factor, resetOffset: false);
        e.Handled = true;
    }

    private void TexturePreviewActualSizeButton_Click(object sender, RoutedEventArgs e)
    {
        SetTexturePreviewZoom(1);
    }

    private void TexturePreviewFitButton_Click(object sender, RoutedEventArgs e)
    {
        if (TexturePreviewImage.Source is not BitmapImage bitmap ||
            bitmap.PixelWidth <= 0 ||
            bitmap.PixelHeight <= 0 ||
            TexturePreviewScrollViewer.ViewportWidth <= 0 ||
            TexturePreviewScrollViewer.ViewportHeight <= 0)
        {
            SetTexturePreviewZoom(1);
            return;
        }

        double availableWidth = Math.Max(1, TexturePreviewScrollViewer.ViewportWidth - 32);
        double availableHeight = Math.Max(1, TexturePreviewScrollViewer.ViewportHeight - 32);
        double scale = Math.Min(availableWidth / bitmap.PixelWidth, availableHeight / bitmap.PixelHeight);
        scale = Math.Clamp(scale, TexturePreviewZoomSlider.Minimum, TexturePreviewZoomSlider.Maximum);
        SetTexturePreviewZoom(scale);
    }

    private void SetTexturePreviewZoom(double zoom, bool resetOffset = true)
    {
        zoom = Math.Clamp(zoom, TexturePreviewZoomSlider.Minimum, TexturePreviewZoomSlider.Maximum);
        _isUpdatingTexturePreviewZoom = true;
        TexturePreviewZoomSlider.Value = zoom;
        _isUpdatingTexturePreviewZoom = false;
        TexturePreviewScrollViewer.ChangeView(resetOffset ? 0 : null, resetOffset ? 0 : null, (float)zoom);
    }

    private void UpdateTexturePreviewAtlasOverlay(object textureValue, BitmapImage preview)
    {
        ClearTexturePreviewAtlasOverlay();
        if (textureValue is not UndertaleEmbeddedTexture texture ||
            _data is null ||
            preview.PixelWidth <= 0 ||
            preview.PixelHeight <= 0)
        {
            return;
        }

        _activeTexturePreviewAtlas = texture;
        if (_pendingTexturePreviewPageItemSelection is { } pendingSelection &&
            ReferenceEquals(pendingSelection.TexturePage, texture))
        {
            _selectedTexturePreviewPageItem = pendingSelection;
            _pendingTexturePreviewPageItemSelection = null;
        }

        TexturePreviewSurface.Width = preview.PixelWidth;
        TexturePreviewSurface.Height = preview.PixelHeight;
        TexturePreviewImage.Width = preview.PixelWidth;
        TexturePreviewImage.Height = preview.PixelHeight;
        TexturePreviewOverlayCanvas.Width = preview.PixelWidth;
        TexturePreviewOverlayCanvas.Height = preview.PixelHeight;
        DrawTexturePreviewAtlasOverlay(texture);
    }

    private void ClearTexturePreviewAtlasOverlay()
    {
        _activeTexturePreviewAtlas = null;
        _selectedTexturePreviewPageItem = null;
        TexturePreviewOverlayCanvas.Children.Clear();
        TexturePreviewSurface.Width = double.NaN;
        TexturePreviewSurface.Height = double.NaN;
        TexturePreviewImage.Width = double.NaN;
        TexturePreviewImage.Height = double.NaN;
        TexturePreviewOverlayCanvas.Width = 0;
        TexturePreviewOverlayCanvas.Height = 0;
    }

    private void DrawTexturePreviewAtlasOverlay(UndertaleEmbeddedTexture texture)
    {
        TexturePreviewOverlayCanvas.Children.Clear();
        if (_data is null)
            return;

        foreach (UndertaleTexturePageItem item in _data.TexturePageItems)
        {
            if (!ReferenceEquals(item.TexturePage, texture) ||
                item.SourceWidth == 0 ||
                item.SourceHeight == 0)
            {
                continue;
            }

            bool selected = ReferenceEquals(item, _selectedTexturePreviewPageItem);
            SolidColorBrush stroke = selected
                ? CreateAccentBrush()
                : new SolidColorBrush(Microsoft.UI.Colors.White);
            stroke.Opacity = selected ? 0.95 : 0.35;

            SolidColorBrush fill = selected
                ? CreateAccentBrush()
                : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            fill.Opacity = selected ? 0.18 : 0;

            Microsoft.UI.Xaml.Shapes.Rectangle rectangle = new()
            {
                Width = item.SourceWidth,
                Height = item.SourceHeight,
                Stroke = stroke,
                StrokeThickness = selected ? 2 : 1,
                Fill = fill
            };

            Canvas.SetLeft(rectangle, item.SourceX);
            Canvas.SetTop(rectangle, item.SourceY);
            TexturePreviewOverlayCanvas.Children.Add(rectangle);
        }
    }

    private async void TexturePreviewSurface_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_isImagePreviewDialogOpen)
        {
            e.Handled = true;
            return;
        }

        if (_data is null || _activeTexturePreviewAtlas is null || TexturePreviewImage.Source is null)
            return;

        Point position = e.GetCurrentPoint(TexturePreviewOverlayCanvas).Position;
        if (position.X < 0 ||
            position.Y < 0 ||
            position.X > TexturePreviewOverlayCanvas.Width ||
            position.Y > TexturePreviewOverlayCanvas.Height)
        {
            return;
        }

        UndertaleTexturePageItem? item = FindTexturePageItemAt(_activeTexturePreviewAtlas, position.X, position.Y);
        if (item is null)
        {
            e.Handled = true;
            _suppressNextTexturePreviewTap = true;
            _ = ClearTexturePreviewTapSuppressionAsync();
            await ShowImagePreviewDialogAsync(TexturePreviewImage.Source);
            return;
        }

        e.Handled = true;
        _suppressNextTexturePreviewTap = true;
        _ = ClearTexturePreviewTapSuppressionAsync();
        _selectedTexturePreviewPageItem = item;
        DrawTexturePreviewAtlasOverlay(_activeTexturePreviewAtlas);

        int itemIndex = _data.TexturePageItems.IndexOf(item);
        if (itemIndex < 0)
        {
            StatusBox.Text = "Selected texture page item is no longer in the loaded data.";
            return;
        }

        StatusBox.Text = $"Selected texture page item #{itemIndex}: {FormatTitle(item.Name?.Content)}.";
        DispatcherQueue.TryEnqueue(() => NavigateToResource("Texture page items", itemIndex));
    }

    private async System.Threading.Tasks.Task ClearTexturePreviewTapSuppressionAsync()
    {
        await System.Threading.Tasks.Task.Delay(250);
        _suppressNextTexturePreviewTap = false;
    }

    private UndertaleTexturePageItem? FindTexturePageItemAt(UndertaleEmbeddedTexture texture, double x, double y)
    {
        if (_data is null)
            return null;

        UndertaleTexturePageItem? bestItem = null;
        long bestArea = long.MaxValue;
        foreach (UndertaleTexturePageItem item in _data.TexturePageItems)
        {
            if (!ReferenceEquals(item.TexturePage, texture) ||
                item.SourceWidth == 0 ||
                item.SourceHeight == 0 ||
                x < item.SourceX ||
                y < item.SourceY ||
                x >= item.SourceX + item.SourceWidth ||
                y >= item.SourceY + item.SourceHeight)
            {
                continue;
            }

            long area = (long)item.SourceWidth * item.SourceHeight;
            if (area >= bestArea)
                continue;

            bestArea = area;
            bestItem = item;
        }

        return bestItem;
    }

    private bool LoadSoundAudioPlayer(MediaPlayerElement player, SoundAudioSource source, string? name, string fallbackName)
    {
        if (source.EmbeddedAudio is not null)
            return LoadAudioPlayer(player, source.EmbeddedAudio, name, fallbackName);

        if (source.FilePath is not null)
            return LoadAudioPlayerFromFile(player, source.FilePath);

        player.Source = null;
        player.Visibility = Visibility.Collapsed;
        return false;
    }

    private bool LoadAudioPlayer(MediaPlayerElement player, UndertaleEmbeddedAudio? audio, string? name, string fallbackName)
    {
        player.Source = null;
        player.Visibility = Visibility.Collapsed;

        if (audio is null || audio.Data.Length == 0)
            return false;

        if (!CanPreviewEmbeddedAudio(audio.Data))
            return false;

        string extension = GetEmbeddedAudioExtension(audio.Data);
        try
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), "UndertaleModTool.WinUI", "AudioPreview");
            Directory.CreateDirectory(tempDirectory);

            string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(audio.Data))[..12];
            string fileName = $"{SafeFileName(name, fallbackName)}_{hash}{extension}";
            string tempPath = Path.Combine(tempDirectory, fileName);
            if (!File.Exists(tempPath) || new FileInfo(tempPath).Length != audio.Data.Length)
                File.WriteAllBytes(tempPath, audio.Data);

            player.Source = MediaSource.CreateFromUri(new Uri(tempPath));
            player.Visibility = Visibility.Visible;
            return true;
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Could not prepare audio preview: {ex.Message}";
            return false;
        }
    }

    private bool LoadAudioPlayerFromFile(MediaPlayerElement player, string path)
    {
        player.Source = null;
        player.Visibility = Visibility.Collapsed;

        if (!File.Exists(path) || !CanPreviewAudioFile(path))
            return false;

        try
        {
            player.Source = MediaSource.CreateFromUri(new Uri(path));
            player.Visibility = Visibility.Visible;
            return true;
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Could not prepare audio preview: {ex.Message}";
            return false;
        }
    }

    private SoundAudioSource ResolveSoundAudioSource(UndertaleSound sound)
    {
        if (_data is null)
            return SoundAudioSource.FromError("No data file is loaded.");

        if (IsStreamedSoundFile(sound))
            return ResolveStreamedSoundFile(sound);

        int builtinGroupId = _data.GetBuiltinSoundGroupID();
        if (sound.GroupID == builtinGroupId)
        {
            UndertaleEmbeddedAudio? audio = GetBuiltinEmbeddedAudioForSound(sound);
            if (audio is null)
                return SoundAudioSource.FromError($"Built-in embedded audio #{sound.AudioID} was not found.");

            int audioIndex = _data.EmbeddedAudio.IndexOf(audio);
            if (audioIndex < 0)
                audioIndex = sound.AudioID;

            return new SoundAudioSource(audio, null, builtinGroupId, audioIndex, $"built-in embedded audio #{audioIndex}", null);
        }

        if (sound.AudioID < 0)
            return SoundAudioSource.FromError($"External audio group {sound.GroupID} has no valid audio id.");

        if (!TryResolveAudioGroupPath(sound.GroupID, sound.AudioGroup, out string? audioGroupPath, out string relativePath, out string? pathError))
            return SoundAudioSource.FromError(pathError ?? $"Could not resolve audio group {sound.GroupID}.");

        if (!File.Exists(audioGroupPath))
            return SoundAudioSource.FromError($"Audio group file was not found: {relativePath}");

        try
        {
            UndertaleData audioGroupData = LoadExternalAudioGroup(sound.GroupID, audioGroupPath);
            IList<UndertaleEmbeddedAudio> embeddedAudio = audioGroupData.EmbeddedAudio;
            if (sound.AudioID >= embeddedAudio.Count)
                return SoundAudioSource.FromError($"Audio #{sound.AudioID} was not found in {relativePath}.");

            UndertaleEmbeddedAudio audio = embeddedAudio[sound.AudioID];
            return new SoundAudioSource(audio, null, sound.GroupID, sound.AudioID, $"{relativePath} audio #{sound.AudioID}", null);
        }
        catch (Exception ex)
        {
            return SoundAudioSource.FromError($"Could not load {relativePath}: {ex.Message}");
        }
    }

    private SoundAudioSource ResolveStreamedSoundFile(UndertaleSound sound)
    {
        string? fileName = GetStreamedSoundFileName(sound);
        if (fileName is null)
            return SoundAudioSource.FromError("Streamed sound has no file name.");

        if (!TryResolveSidecarFilePath(fileName, out string? path, out string? error))
            return SoundAudioSource.FromError(error ?? $"Could not resolve streamed audio file {fileName}.");

        if (!File.Exists(path))
            return SoundAudioSource.FromError($"Streamed audio file was not found: {fileName}");

        return new SoundAudioSource(null, path, sound.GroupID, sound.AudioID, $"streamed file {fileName}", null);
    }

    private UndertaleData LoadExternalAudioGroup(int groupId, string path)
    {
        string fullPath = Path.GetFullPath(path);
        if (_externalAudioGroupCache.TryGetValue(groupId, out ExternalAudioGroupCacheEntry? cached))
        {
            if (string.Equals(cached.Path, fullPath, StringComparison.OrdinalIgnoreCase))
                return cached.Data;

            cached.Data.Dispose();
            _externalAudioGroupCache.Remove(groupId);
        }

        using FileStream stream = new(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        UndertaleData data = UndertaleIO.Read(stream, (warning, _) =>
        {
            throw new InvalidDataException(warning);
        });
        _externalAudioGroupCache[groupId] = new ExternalAudioGroupCacheEntry(fullPath, data);
        return data;
    }

    private void ClearExternalAudioGroupCache()
    {
        foreach (ExternalAudioGroupCacheEntry entry in _externalAudioGroupCache.Values)
            entry.Data.Dispose();

        _externalAudioGroupCache.Clear();
    }

    private bool TryResolveAudioGroupPath(
        int groupId,
        UndertaleAudioGroup? audioGroup,
        out string? path,
        out string relativePath,
        out string? error)
    {
        relativePath = audioGroup?.Path?.Content is { Length: > 0 } customPath
            ? customPath
            : $"audiogroup{groupId}.dat";
        return TryResolveSidecarFilePath(relativePath, out path, out error);
    }

    private bool TryResolveSidecarFilePath(string relativePath, out string? path, out string? error)
    {
        path = null;
        error = null;

        string? dataDirectory = Path.GetDirectoryName(_currentFilePath ?? string.Empty);
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            error = "Cannot resolve sidecar audio because the data file has not been saved or loaded from disk.";
            return false;
        }

        path = Paths.TryJoinVerifyWithinDirectory(dataDirectory, relativePath);
        if (path is null)
        {
            error = $"Audio path escapes the data file directory: {relativePath}";
            return false;
        }

        return true;
    }

    private static bool IsStreamedSoundFile(UndertaleSound sound)
    {
        return !sound.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsEmbedded) &&
               !sound.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsCompressed);
    }

    private static string? GetStreamedSoundFileName(UndertaleSound sound)
    {
        string? fileName = sound.File?.Content;
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        return fileName.Contains('.', StringComparison.Ordinal)
            ? fileName
            : fileName + ".ogg";
    }

    private static bool CanPreviewAudioFile(string path)
    {
        string extension = Path.GetExtension(path);
        return string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".ogg", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase);
    }

    private void PlayAudioPlayer(MediaPlayerElement player)
    {
        if (player.Source is null)
            return;

        try
        {
            var session = player.MediaPlayer.PlaybackSession;
            if (session.NaturalDuration > TimeSpan.Zero &&
                session.Position >= session.NaturalDuration - TimeSpan.FromMilliseconds(50))
            {
                session.Position = TimeSpan.Zero;
            }

            player.MediaPlayer.Play();
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Could not play audio preview: {ex.Message}";
        }
    }

    private void StopAudioPlayer(MediaPlayerElement player)
    {
        if (player.Source is null)
            return;

        try
        {
            player.MediaPlayer.Pause();
            player.MediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Could not stop audio preview: {ex.Message}";
        }
    }

    private void ShowSoundAudioEditorFor(ResourceItem item)
    {
        if (item.Value is not UndertaleSound sound)
        {
            HideSoundAudioEditor();
            return;
        }

        SoundAudioSource source = ResolveSoundAudioSource(sound);
        UndertaleEmbeddedAudio? builtinAudio = GetBuiltinEmbeddedAudioForSound(sound);
        SoundAudioPanel.Visibility = Visibility.Visible;
        SoundAudioInfoText.Text = BuildSoundAudioInfo(sound, source);
        SoundAudioNavigateButton.IsEnabled = builtinAudio is not null;
        SoundAudioImportButton.IsEnabled = builtinAudio is not null && _data is not null && !_data.UnsupportedBytecodeVersion;
        SoundAudioExportButton.IsEnabled = CanExportSoundAudio(source);
        bool canPlay = LoadSoundAudioPlayer(SoundAudioPlayer, source, sound.File?.Content ?? sound.Name?.Content, "sound");
        SoundAudioPlayButton.IsEnabled = canPlay;
        SoundAudioStopButton.IsEnabled = canPlay;
    }

    private void HideSoundAudioEditor()
    {
        SoundAudioPlayer.Source = null;
        SoundAudioPlayer.Visibility = Visibility.Collapsed;
        SoundAudioPlayButton.IsEnabled = false;
        SoundAudioStopButton.IsEnabled = false;
        SoundAudioInfoText.Text = string.Empty;
        SoundAudioNavigateButton.IsEnabled = false;
        SoundAudioImportButton.IsEnabled = false;
        SoundAudioExportButton.IsEnabled = false;
        SoundAudioPanel.Visibility = Visibility.Collapsed;
    }

    private void SoundAudioPlayButton_Click(object sender, RoutedEventArgs e)
    {
        PlayAudioPlayer(SoundAudioPlayer);
    }

    private void SoundAudioStopButton_Click(object sender, RoutedEventArgs e)
    {
        StopAudioPlayer(SoundAudioPlayer);
    }

    private void SoundAudioNavigateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _selectedResource?.Value is not UndertaleSound sound)
            return;

        UndertaleEmbeddedAudio? audio = GetBuiltinEmbeddedAudioForSound(sound);
        if (audio is null)
            return;

        int audioIndex = _data.EmbeddedAudio.IndexOf(audio);
        if (audioIndex < 0)
            audioIndex = sound.AudioID;

        NavigateToResource("Embedded audio", audioIndex);
    }

    private async void SoundAudioImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _selectedResource?.Value is not UndertaleSound sound || _data.UnsupportedBytecodeVersion)
            return;

        ResourceItem selectedResource = _selectedResource;
        UndertaleEmbeddedAudio? audio = GetBuiltinEmbeddedAudioForSound(sound);
        if (audio is null)
            return;

        FileOpenPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.MusicLibrary
        };
        picker.FileTypeFilter.Add(".wav");
        picker.FileTypeFilter.Add(".ogg");
        picker.FileTypeFilter.Add("*");

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        SoundAudioImportButton.IsEnabled = false;
        SoundAudioExportButton.IsEnabled = false;
        try
        {
            byte[] data = await File.ReadAllBytesAsync(file.Path);
            string? warning = ValidateImportedAudio(audio.Data, data);
            audio.Data = data;
            if (ReferenceEquals(_selectedResource, selectedResource))
            {
                DetailsList.ItemsSource = BuildDetails(selectedResource).ToArray();
                SoundAudioSource source = ResolveSoundAudioSource(sound);
                SoundAudioInfoText.Text = BuildSoundAudioInfo(sound, source);
                bool canPlay = LoadSoundAudioPlayer(SoundAudioPlayer, source, sound.File?.Content ?? sound.Name?.Content, "sound");
                SoundAudioPlayButton.IsEnabled = canPlay;
                SoundAudioStopButton.IsEnabled = canPlay;
                SoundAudioExportButton.IsEnabled = true;
            }
            MarkDirty();
            StatusBox.Text = warning is null
                ? $"Imported audio for {FormatTitle(sound.Name?.Content)} from {file.Path}"
                : $"Imported audio for {FormatTitle(sound.Name?.Content)} from {file.Path}{Environment.NewLine}{warning}";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to import sound audio: {ex}";
        }
        finally
        {
            if (ReferenceEquals(_selectedResource, selectedResource))
            {
                SoundAudioImportButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion;
                SoundAudioExportButton.IsEnabled = audio.Data.Length > 0;
            }
        }
    }

    private async void SoundAudioExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedResource?.Value is not UndertaleSound sound)
            return;

        ResourceItem selectedResource = _selectedResource;
        SoundAudioSource source = ResolveSoundAudioSource(sound);
        if (!CanExportSoundAudio(source))
            return;

        FileSavePicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.MusicLibrary,
            SuggestedFileName = GetSoundAudioSuggestedFileName(sound, source)
        };

        string extension = GetSoundAudioSourceExtension(source);
        picker.FileTypeChoices.Add(extension switch
        {
            ".wav" => "WAV audio",
            ".ogg" => "OGG audio",
            ".mp3" => "MP3 audio",
            _ => "Audio data"
        }, [extension]);

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFile? file = await picker.PickSaveFileAsync();
        if (file is null)
            return;

        SoundAudioExportButton.IsEnabled = false;
        try
        {
            if (source.EmbeddedAudio is not null)
                await File.WriteAllBytesAsync(file.Path, source.EmbeddedAudio.Data);
            else if (source.FilePath is not null)
                File.Copy(source.FilePath, file.Path, overwrite: true);
            StatusBox.Text = $"Exported audio for {FormatTitle(sound.Name?.Content)} to {file.Path}";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to export sound audio: {ex}";
        }
        finally
        {
            if (ReferenceEquals(_selectedResource, selectedResource))
                SoundAudioExportButton.IsEnabled = CanExportSoundAudio(ResolveSoundAudioSource(sound));
        }
    }

    private void ShowEmbeddedAudioEditorFor(ResourceItem item)
    {
        if (item.Value is not UndertaleEmbeddedAudio audio)
        {
            HideEmbeddedAudioEditor();
            return;
        }

        EmbeddedAudioPanel.Visibility = Visibility.Visible;
        AudioInfoText.Text = BuildEmbeddedAudioInfo(audio);
        AudioImportButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion;
        AudioExportButton.IsEnabled = audio.Data.Length > 0;
        bool canPlay = LoadAudioPlayer(EmbeddedAudioPlayer, audio, audio.Name?.Content, "audio");
        AudioPlayButton.IsEnabled = canPlay;
        AudioStopButton.IsEnabled = canPlay;
    }

    private void HideEmbeddedAudioEditor()
    {
        EmbeddedAudioPlayer.Source = null;
        EmbeddedAudioPlayer.Visibility = Visibility.Collapsed;
        AudioPlayButton.IsEnabled = false;
        AudioStopButton.IsEnabled = false;
        AudioInfoText.Text = string.Empty;
        AudioImportButton.IsEnabled = false;
        AudioExportButton.IsEnabled = false;
        EmbeddedAudioPanel.Visibility = Visibility.Collapsed;
    }

    private void AudioPlayButton_Click(object sender, RoutedEventArgs e)
    {
        PlayAudioPlayer(EmbeddedAudioPlayer);
    }

    private void AudioStopButton_Click(object sender, RoutedEventArgs e)
    {
        StopAudioPlayer(EmbeddedAudioPlayer);
    }

    private async void AudioImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _selectedResource?.Value is not UndertaleEmbeddedAudio audio || _data.UnsupportedBytecodeVersion)
            return;

        ResourceItem selectedResource = _selectedResource;
        FileOpenPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.MusicLibrary
        };
        picker.FileTypeFilter.Add(".wav");
        picker.FileTypeFilter.Add(".ogg");
        picker.FileTypeFilter.Add("*");

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        AudioImportButton.IsEnabled = false;
        AudioExportButton.IsEnabled = false;
        try
        {
            byte[] data = await File.ReadAllBytesAsync(file.Path);
            string? warning = ValidateImportedAudio(audio.Data, data);
            audio.Data = data;
            if (ReferenceEquals(_selectedResource, selectedResource))
            {
                DetailsList.ItemsSource = BuildDetails(selectedResource).ToArray();
                AudioInfoText.Text = BuildEmbeddedAudioInfo(audio);
                bool canPlay = LoadAudioPlayer(EmbeddedAudioPlayer, audio, audio.Name?.Content, "audio");
                AudioPlayButton.IsEnabled = canPlay;
                AudioStopButton.IsEnabled = canPlay;
                AudioExportButton.IsEnabled = true;
            }
            MarkDirty();
            StatusBox.Text = warning is null
                ? $"Imported embedded audio from {file.Path}"
                : $"Imported embedded audio from {file.Path}{Environment.NewLine}{warning}";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to import embedded audio: {ex}";
        }
        finally
        {
            if (ReferenceEquals(_selectedResource, selectedResource))
            {
                AudioImportButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion;
                AudioExportButton.IsEnabled = audio.Data.Length > 0;
            }
        }
    }

    private async void AudioExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedResource?.Value is not UndertaleEmbeddedAudio audio)
            return;

        ResourceItem selectedResource = _selectedResource;
        FileSavePicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.MusicLibrary,
            SuggestedFileName = SafeFileName(audio.Name?.Content, "audio")
        };

        string extension = GetEmbeddedAudioExtension(audio.Data);
        picker.FileTypeChoices.Add(extension switch
        {
            ".wav" => "WAV audio",
            ".ogg" => "OGG audio",
            _ => "Audio data"
        }, [extension]);

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFile? file = await picker.PickSaveFileAsync();
        if (file is null)
            return;

        AudioExportButton.IsEnabled = false;
        try
        {
            await File.WriteAllBytesAsync(file.Path, audio.Data);
            StatusBox.Text = $"Exported embedded audio to {file.Path}";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to export embedded audio: {ex}";
        }
        finally
        {
            if (ReferenceEquals(_selectedResource, selectedResource))
                AudioExportButton.IsEnabled = audio.Data.Length > 0;
        }
    }

    private async void TextureImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _selectedResource is null || _data.UnsupportedBytecodeVersion ||
            !TryGetPreviewableTextureValue(_selectedResource.Value, out object? textureValue) || textureValue is null)
            return;

        ResourceItem selectedResource = _selectedResource;
        FileOpenPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary
        };
        picker.FileTypeFilter.Add(".png");

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        bool previewWasRendered = TexturePreviewImage.Source is not null;
        TextureImportButton.IsEnabled = false;
        TextureExportButton.IsEnabled = false;
        try
        {
            string? warning = await System.Threading.Tasks.Task.Run(() => ImportTexture(textureValue, file.Path));
            ClearPreviewCaches();
            if (ReferenceEquals(_selectedResource, selectedResource))
            {
                DetailsList.ItemsSource = BuildDetails(selectedResource).ToArray();
                TexturePreviewInfoText.Text = BuildTexturePreviewInfo(textureValue);
                if (selectedResource.Value is UndertaleBackground background)
                    BackgroundSummaryText.Text = BuildBackgroundSummary(background);
                ShowTexturePreviewFor(selectedResource, forceRender: previewWasRendered);
            }
            MarkDirty();
            StatusBox.Text = warning is null
                ? $"Imported texture from {file.Path}"
                : $"Imported texture from {file.Path}{Environment.NewLine}{warning}";
        }
        catch (Exception ex)
        {
            if (ReferenceEquals(_selectedResource, selectedResource))
            {
                TextureImportButton.IsEnabled = _data is not null && !_data.UnsupportedBytecodeVersion;
                TextureExportButton.IsEnabled = true;
            }
            StatusBox.Text = $"Failed to import texture: {ex}";
        }
    }

    private async void TextureOpenPreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isImagePreviewDialogOpen)
            return;

        if (TexturePreviewImage.Source is not ImageSource source)
            return;

        await ShowImagePreviewDialogAsync(source);
    }

    private async void TextureExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedResource is null ||
            !TryGetPreviewableTextureValue(_selectedResource.Value, out object? textureValue) || textureValue is null)
            return;

        ResourceItem selectedResource = _selectedResource;
        FileSavePicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            SuggestedFileName = SafeFileName(GetTextureName(textureValue), "texture")
        };
        picker.FileTypeChoices.Add("PNG image", [".png"]);

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFile? file = await picker.PickSaveFileAsync();
        if (file is null)
            return;

        TextureExportButton.IsEnabled = false;
        try
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                ExportTexture(textureValue, file.Path);
            });
            StatusBox.Text = $"Exported texture to {file.Path}";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to export texture: {ex}";
        }
        finally
        {
            if (ReferenceEquals(_selectedResource, selectedResource))
                TextureExportButton.IsEnabled = true;
        }
    }

    private void CodeViewModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_data is null || _selectedResource?.Value is not UndertaleCode code || CodeViewerPanel.Visibility != Visibility.Visible)
            return;

        UpdateCodeViewer(_data, code);
    }

    private void UpdateCodeViewer(UndertaleData data, UndertaleCode code)
    {
        bool decompiled = CodeViewModeBox.SelectedIndex == 1;
        CodeViewerTitle.Text = decompiled ? "Code decompiled view" : "Code disassembly";
        CodeEditToggle.Content = decompiled ? "Edit GML" : "Edit disassembly";
        CodeEditToggle.IsEnabled = code.ParentEntry is null && !data.UnsupportedBytecodeVersion;
        CodeImportButton.IsEnabled = code.ParentEntry is null && !data.UnsupportedBytecodeVersion;
        CodeExportButton.IsEnabled = true;
        if (code.ParentEntry is not null)
            SetCodeEditToggle(false);

        CodeApplyButton.IsEnabled = CodeEditToggle.IsChecked == true && code.ParentEntry is null;
        CodeTextBox.IsReadOnly = CodeEditToggle.IsChecked != true;
        CodeTextBox.Text = decompiled ? BuildCodeDecompiledText(data, code) : BuildCodeDisassembly(data, code);
        RefreshCodeSearch(selectFirstMatch: false);
    }

    private void CodeEditToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingCodeEditor)
            return;

        bool canEdit = _data is not null &&
                       _selectedResource?.Value is UndertaleCode { ParentEntry: null } &&
                       !_data.UnsupportedBytecodeVersion;
        bool enabled = canEdit && CodeEditToggle.IsChecked == true;
        CodeTextBox.IsReadOnly = !enabled;
        CodeApplyButton.IsEnabled = enabled;
    }

    private async void CodeApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _selectedResource?.Value is not UndertaleCode code || code.ParentEntry is not null)
            return;

        bool decompiled = CodeViewModeBox.SelectedIndex == 1;
        string source = CodeTextBox.Text;
        CodeApplyButton.IsEnabled = false;
        CodeTextBox.IsReadOnly = true;
        StatusBox.Text = decompiled ? "Compiling GML..." : "Assembling disassembly...";

        try
        {
            string? compileErrors = await System.Threading.Tasks.Task.Run(() =>
                decompiled
                    ? ApplyDecompiledGml(_data, code, source)
                    : ApplyDisassembly(_data, code, source));
            if (compileErrors is not null)
            {
                StatusBox.Text = $"Compiler error:{Environment.NewLine}{compileErrors}";
                CodeTextBox.IsReadOnly = false;
                CodeApplyButton.IsEnabled = true;
                return;
            }

            MarkDirty();
            UpdateCodeViewer(_data, code);
            StatusBox.Text = decompiled
                ? $"Compiled GML changes for {FormatTitle(code.Name?.Content)}."
                : $"Applied disassembly changes to {FormatTitle(code.Name?.Content)}.";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"{(decompiled ? "Compiler" : "Assembler")} error:{Environment.NewLine}{ex}";
            CodeTextBox.IsReadOnly = false;
            CodeApplyButton.IsEnabled = true;
        }
    }

    private async void CodeImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _selectedResource?.Value is not UndertaleCode code || code.ParentEntry is not null || _data.UnsupportedBytecodeVersion)
            return;

        FileOpenPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add(".txt");
        picker.FileTypeFilter.Add(".gml");
        picker.FileTypeFilter.Add(".asm");

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        try
        {
            CodeTextBox.Text = await File.ReadAllTextAsync(file.Path);
            SetCodeEditToggle(true);
            CodeTextBox.IsReadOnly = false;
            CodeApplyButton.IsEnabled = true;
            RefreshCodeSearch(selectFirstMatch: false);
            StatusBox.Text = $"Imported code text from {file.Path}.";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to import code text:{Environment.NewLine}{ex}";
        }
    }

    private async void CodeExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedResource?.Value is not UndertaleCode code)
            return;

        bool decompiled = CodeViewModeBox.SelectedIndex == 1;
        FileSavePicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = SafeFileName(code.Name?.Content, "code")
        };
        picker.FileTypeChoices.Add(decompiled ? "GML file" : "Assembly text", [decompiled ? ".gml" : ".asm"]);
        picker.FileTypeChoices.Add("Text file", [".txt"]);

        if (App.MainWindow is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        }

        Windows.Storage.StorageFile? file = await picker.PickSaveFileAsync();
        if (file is null)
            return;

        try
        {
            await File.WriteAllTextAsync(file.Path, CodeTextBox.Text);
            StatusBox.Text = $"Exported code text to {file.Path}.";
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to export code text:{Environment.NewLine}{ex}";
        }
    }

    private void CodeSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingCodeSearch)
            return;

        RefreshCodeSearch(selectFirstMatch: true);
    }

    private void CodeSearchPreviousButton_Click(object sender, RoutedEventArgs e)
    {
        MoveCodeSearchSelection(-1);
    }

    private void CodeSearchNextButton_Click(object sender, RoutedEventArgs e)
    {
        MoveCodeSearchSelection(1);
    }

    private void RefreshCodeSearch(bool selectFirstMatch)
    {
        string query = CodeSearchBox.Text;
        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(CodeTextBox.Text))
        {
            _codeSearchMatches = [];
            _codeSearchMatchIndex = -1;
            UpdateCodeSearchControls();
            return;
        }

        List<int> matches = [];
        int index = 0;
        while ((index = CodeTextBox.Text.IndexOf(query, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            matches.Add(index);
            index += query.Length;
        }

        _codeSearchMatches = matches;
        _codeSearchMatchIndex = matches.Count == 0 ? -1 : 0;
        UpdateCodeSearchControls();

        if (selectFirstMatch && _codeSearchMatchIndex >= 0)
            SelectCodeSearchMatch();
    }

    private void MoveCodeSearchSelection(int direction)
    {
        if (_codeSearchMatches.Count == 0)
            return;

        _codeSearchMatchIndex = (_codeSearchMatchIndex + direction + _codeSearchMatches.Count) % _codeSearchMatches.Count;
        SelectCodeSearchMatch();
        UpdateCodeSearchControls();
    }

    private void SelectCodeSearchMatch()
    {
        if (_codeSearchMatchIndex < 0 || _codeSearchMatchIndex >= _codeSearchMatches.Count)
            return;

        CodeTextBox.Focus(FocusState.Programmatic);
        CodeTextBox.Select(_codeSearchMatches[_codeSearchMatchIndex], CodeSearchBox.Text.Length);
    }

    private void UpdateCodeSearchControls()
    {
        bool hasMatches = _codeSearchMatches.Count > 0;
        CodeSearchPreviousButton.IsEnabled = hasMatches;
        CodeSearchNextButton.IsEnabled = hasMatches;
        CodeSearchStatusText.Text = hasMatches
            ? $"{_codeSearchMatchIndex + 1} of {_codeSearchMatches.Count}"
            : "0 matches";
    }

    private void SetCodeSearchText(string text)
    {
        _isUpdatingCodeSearch = true;
        CodeSearchBox.Text = text;
        _isUpdatingCodeSearch = false;
    }

    private void SetCodeEditToggle(bool isChecked)
    {
        _isUpdatingCodeEditor = true;
        CodeEditToggle.IsChecked = isChecked;
        _isUpdatingCodeEditor = false;
    }

    private void StringContentBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingStringEditor || _selectedResource?.Value is not UndertaleString str)
            return;

        if (StringContentBox.Text == (str.Content ?? string.Empty))
            return;

        str.Content = StringContentBox.Text;
        DetailsTitleText.Text = FormatTitle(str.Content);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        MarkDirty();
        RefreshSelectedResourceTitle();
        RefreshSelectedResourceDependentPanels();
    }

    private void ResourceNameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingNamedResourceEditor || _selectedResource?.Value is not UndertaleNamedResource named)
            return;

        if (named.Name is null)
            return;

        if (ResourceNameBox.Text == (named.Name.Content ?? string.Empty))
            return;

        named.Name.Content = ResourceNameBox.Text;
        DetailsTitleText.Text = FormatTitle(named.Name.Content);
        DetailsList.ItemsSource = BuildDetails(_selectedResource).ToArray();
        MarkDirty();
        RefreshSelectedResourceTitle();
    }

    private void ScalarPropertyBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingScalarEditor || sender is not TextBox textBox || textBox.DataContext is not EditablePropertyRow row)
            return;

        if (textBox.Text == row.Value)
            return;

        try
        {
            object? parsedValue = ParseEditableValue(textBox.Text, row.Property.PropertyType);
            object? currentValue = row.Property.GetValue(row.Owner);
            if (string.Equals(FormatEditableValue(currentValue), FormatEditableValue(parsedValue), StringComparison.Ordinal))
            {
                row.Value = FormatEditableValue(currentValue);
                textBox.Text = row.Value;
                return;
            }

            row.Property.SetValue(row.Owner, parsedValue);
            row.Value = FormatEditableValue(parsedValue);
            textBox.Text = row.Value;
            DetailsList.ItemsSource = _selectedResource is null ? null : BuildDetails(_selectedResource).ToArray();
            MarkDirty();
            RefreshSelectedResourceDependentPanels();
        }
        catch (Exception ex)
        {
            textBox.Text = row.Value;
            StatusBox.Text = $"Could not update {row.Label}: {ex.Message}";
        }
    }

    private void MarkDirty(bool markProjectAsset = true)
    {
        if (_data is null)
            return;

        if (markProjectAsset && _project is not null && _selectedResource?.Value is IProjectAsset projectAsset)
            _project.MarkAssetForExport(projectAsset);

        _isDirty = true;
        UpdateWindowTitle();
        SetSaveButtonContent("Save*");
        SaveButton.IsEnabled = !_data.UnsupportedBytecodeVersion;
        SaveAsButton.IsEnabled = !_data.UnsupportedBytecodeVersion;
        UpdateResourceCommandButtons();
        UpdateCommandStates();
    }

    private void RegisterKeyboardAccelerators()
    {
        KeyboardAccelerators.Add(CreateKeyboardAccelerator(VirtualKey.N, VirtualKeyModifiers.Control, async () =>
        {
            if (NewDataFileMenuItem.IsEnabled)
                await CreateNewDataFileAsync();
        }));
        KeyboardAccelerators.Add(CreateKeyboardAccelerator(VirtualKey.F5, VirtualKeyModifiers.None, async () =>
        {
            if (TempRunGameMenuItem.IsEnabled)
                await TempRunGameAsync();
        }));
        KeyboardAccelerators.Add(CreateKeyboardAccelerator(VirtualKey.F5, VirtualKeyModifiers.Menu, async () =>
        {
            if (RunWithOtherRunnerMenuItem.IsEnabled)
                await RunWithOtherRunnerAsync();
        }));
        KeyboardAccelerators.Add(CreateKeyboardAccelerator(VirtualKey.F5, VirtualKeyModifiers.Shift, async () =>
        {
            if (RunGmsDebuggerMenuItem.IsEnabled)
                await RunUnderGmsDebuggerAsync();
        }));
        KeyboardAccelerators.Add(CreateKeyboardAccelerator(VirtualKey.O, VirtualKeyModifiers.Control, async () =>
        {
            if (OpenButton.IsEnabled)
                await PickAndOpenDataFileAsync();
        }));
        KeyboardAccelerators.Add(CreateKeyboardAccelerator(VirtualKey.S, VirtualKeyModifiers.Control, async () =>
        {
            if (SaveButton.IsEnabled)
                await SaveCurrentFileAsync();
        }));
        KeyboardAccelerators.Add(CreateKeyboardAccelerator(VirtualKey.Q, VirtualKeyModifiers.Control, () =>
        {
            App.MainWindow?.Close();
            return System.Threading.Tasks.Task.CompletedTask;
        }));
        KeyboardAccelerators.Add(CreateKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu, () =>
        {
            if (BackNavigationMenuItem.IsEnabled)
                NavigateResourceHistory(-1);
            return System.Threading.Tasks.Task.CompletedTask;
        }));
        KeyboardAccelerators.Add(CreateKeyboardAccelerator(VirtualKey.Right, VirtualKeyModifiers.Menu, () =>
        {
            if (ForwardNavigationMenuItem.IsEnabled)
                NavigateResourceHistory(1);
            return System.Threading.Tasks.Task.CompletedTask;
        }));
        KeyboardAccelerators.Add(CreateKeyboardAccelerator(
            VirtualKey.S,
            VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
            async () =>
            {
                if (SaveAsButton.IsEnabled)
                    await SaveCurrentFileAsAsync();
            }));
        KeyboardAccelerators.Add(CreateKeyboardAccelerator(VirtualKey.F, VirtualKeyModifiers.Control, () =>
        {
            if (FindResourceMenuItem.IsEnabled)
                FocusPrimaryFindTarget();
            return System.Threading.Tasks.Task.CompletedTask;
        }));
        KeyboardAccelerators.Add(CreateKeyboardAccelerator(
            VirtualKey.F,
            VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
            () =>
            {
                if (FindCodeMenuItem.IsEnabled)
                    FindCodeMenuItem_Click(this, new RoutedEventArgs());
                return System.Threading.Tasks.Task.CompletedTask;
            }));
        KeyboardAccelerators.Add(CreateKeyboardAccelerator(VirtualKey.W, VirtualKeyModifiers.Control, () =>
        {
            if (CloseTabMenuItem.IsEnabled)
                CloseTabMenuItem_Click(this, new RoutedEventArgs());
            return System.Threading.Tasks.Task.CompletedTask;
        }));
        KeyboardAccelerators.Add(CreateKeyboardAccelerator(
            VirtualKey.W,
            VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
            () =>
            {
                if (CloseAllTabsMenuItem.IsEnabled)
                    CloseAllTabsMenuItem_Click(this, new RoutedEventArgs());
                return System.Threading.Tasks.Task.CompletedTask;
            }));
        KeyboardAccelerators.Add(CreateKeyboardAccelerator(
            VirtualKey.T,
            VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
            () =>
            {
                if (RestoreClosedTabMenuItem.IsEnabled)
                    RestoreClosedTabMenuItem_Click(this, new RoutedEventArgs());
                return System.Threading.Tasks.Task.CompletedTask;
            }));
        KeyboardAccelerators.Add(CreateKeyboardAccelerator(VirtualKey.Tab, VirtualKeyModifiers.Control, () =>
        {
            if (NextTabMenuItem.IsEnabled)
                NextTabMenuItem_Click(this, new RoutedEventArgs());
            return System.Threading.Tasks.Task.CompletedTask;
        }));
        KeyboardAccelerators.Add(CreateKeyboardAccelerator(
            VirtualKey.Tab,
            VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
            () =>
            {
                if (PreviousTabMenuItem.IsEnabled)
                    PreviousTabMenuItem_Click(this, new RoutedEventArgs());
                return System.Threading.Tasks.Task.CompletedTask;
            }));
    }

    private static KeyboardAccelerator CreateKeyboardAccelerator(
        VirtualKey key,
        VirtualKeyModifiers modifiers,
        Func<System.Threading.Tasks.Task> handler)
    {
        KeyboardAccelerator accelerator = new()
        {
            Key = key,
            Modifiers = modifiers
        };
        accelerator.Invoked += async (_, args) =>
        {
            args.Handled = true;
            await handler();
        };
        return accelerator;
    }

    private void UpdateWindowTitle()
    {
        string title = "UndertaleModTool.WinUI";
        if (_data is not null)
        {
            string gameName = FormatTitle(_data.GeneralInfo?.Name?.Content);
            string fileName = string.IsNullOrWhiteSpace(_currentFilePath)
                ? "Untitled"
                : Path.GetFileName(_currentFilePath);
            title = $"{gameName} - {fileName} - UndertaleModTool.WinUI";
            if (_isDirty)
                title = $"* {title}";
        }

        if (App.MainWindow is MainWindow window)
            window.SetDocumentTitle(title);
        else if (App.MainWindow is not null)
            App.MainWindow.Title = title;
    }

    private void RefreshCurrentDetails()
    {
        DetailsList.ItemsSource = _selectedResource is null ? null : BuildDetails(_selectedResource).ToArray();
    }

    private void UpdateResourceCommandButtons()
    {
        bool canEdit = _data is not null && !_data.UnsupportedBytecodeVersion;
        bool canAddResource = canEdit && CanAddResourceToCategory(_selectedCategory);
        bool canSwapResources = canEdit && CanSwapResourcesInCategory(_selectedCategory);
        bool isStringsCategory = _selectedCategory?.Label == "Strings";

        AddResourceButton.Visibility = canAddResource ? Visibility.Visible : Visibility.Collapsed;
        DuplicateStringButton.Visibility = isStringsCategory ? Visibility.Visible : Visibility.Collapsed;
        AddResourceButton.IsEnabled = canAddResource;
        DuplicateStringButton.IsEnabled = canEdit && isStringsCategory && _selectedResource?.Value is UndertaleString;
        ResourceList.CanDragItems = canSwapResources;
    }

    private static bool CanAddResourceToCategory(ResourceCategory? category)
    {
        if (category?.SourceList is not IList list)
            return false;

        Type? itemType = GetListItemType(list);
        if (itemType is null)
            return false;

        return (itemType == typeof(UndertaleString) || typeof(UndertaleResource).IsAssignableFrom(itemType)) &&
               itemType.GetConstructor(Type.EmptyTypes) is not null;
    }

    private bool CanDeleteResourceItem(ResourceCategory? category, ResourceItem? item)
    {
        if (_data is null ||
            _data.UnsupportedBytecodeVersion ||
            category?.SourceList is not IList list ||
            item is null ||
            list.IsFixedSize ||
            list.IsReadOnly ||
            item.Value is not UndertaleObject)
        {
            return false;
        }

        return list.IndexOf(item.Value) >= 0;
    }

    private static bool CanSwapResourcesInCategory(ResourceCategory? category)
    {
        if (!WinUiToolSettings.Instance.AssetOrderSwappingEnabled ||
            category?.SourceList is not IList { Count: > 1 } list ||
            list.IsFixedSize ||
            list.IsReadOnly)
        {
            return false;
        }

        Type? itemType = GetListItemType(list);
        return itemType == typeof(UndertaleString) || typeof(UndertaleResource).IsAssignableFrom(itemType);
    }

    private static Type? GetListItemType(IList list)
    {
        Type listType = list.GetType();
        if (listType.IsGenericType)
            return listType.GetGenericArguments().FirstOrDefault();

        return listType.GetInterfaces()
                       .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
                       .Select(type => type.GetGenericArguments()[0])
                       .FirstOrDefault();
    }

    private static bool IsValidAssetIdentifier(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        char firstChar = name[0];
        if (!char.IsAsciiLetter(firstChar) && firstChar != '_')
            return false;

        foreach (char c in name.Skip(1))
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '_')
                return false;
        }

        return true;
    }

    private void ApplySettingsToUi()
    {
        WinUiToolSettings.EnsureLoaded();
        RunGmsDebuggerMenuItem.Visibility = WinUiToolSettings.Instance.ShowDebuggerOption
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateCommandStates()
    {
        UndertaleData? data = _data;
        bool hasData = data is not null;
        bool canWrite = data is not null && !data.UnsupportedBytecodeVersion;
        bool hasTabs = _openResourceTabs.Count > 0;
        bool canRunCommand = hasData && !_isRunningScript && !string.IsNullOrWhiteSpace(CommandBox.Text);
        int codeCount = data?.Code.Count ?? 0;
        int stringCount = data?.Strings.Count ?? 0;
        bool canGoBack = _resourceNavigationHistoryPosition > 0;
        bool canGoForward = _resourceNavigationHistoryPosition >= 0 &&
                            _resourceNavigationHistoryPosition < _resourceNavigationHistory.Count - 1;

        NewDataFileMenuItem.IsEnabled = OpenButton.IsEnabled;
        OpenMenuItem.IsEnabled = OpenButton.IsEnabled;
        OpenRecentMenuItem.IsEnabled = OpenButton.IsEnabled && _recentFilePaths.Count > 0;
        SaveMenuItem.IsEnabled = SaveButton.IsEnabled;
        SaveAsMenuItem.IsEnabled = SaveAsButton.IsEnabled;
        TempRunGameMenuItem.IsEnabled = canWrite && _currentFilePath is not null && !_isTempRunningGame;
        RunWithOtherRunnerMenuItem.IsEnabled = canWrite && _currentFilePath is not null && !_isTempRunningGame;
        RunGmsDebuggerMenuItem.IsEnabled = canWrite && _currentFilePath is not null && !_isTempRunningGame;
        GenerateOffsetMapMenuItem.IsEnabled = !_isTempRunningGame && !_isGeneratingOffsetMap;
        FindResourceMenuItem.IsEnabled = hasData;
        FindCodeMenuItem.IsEnabled = codeCount > 0;
        FindStringsMenuItem.IsEnabled = stringCount > 0;
        FindUnreferencedAssetsMenuItem.IsEnabled = hasData && !_isFindingReferences;
        ResourceBackButton.IsEnabled = canGoBack;
        ResourceForwardButton.IsEnabled = canGoForward;
        BackNavigationMenuItem.IsEnabled = canGoBack;
        ForwardNavigationMenuItem.IsEnabled = canGoForward;
        PreviousTabMenuItem.IsEnabled = hasTabs;
        NextTabMenuItem.IsEnabled = hasTabs;
        CloseTabMenuItem.IsEnabled = hasTabs;
        CloseAllTabsMenuItem.IsEnabled = hasTabs;
        RestoreClosedTabMenuItem.IsEnabled = _closedResourceTabsHistory.Count > 0;
        NewProjectMenuItem.IsEnabled = OpenButton.IsEnabled && !_isProjectOperation;
        OpenProjectMenuItem.IsEnabled = OpenButton.IsEnabled && !_isProjectOperation;
        SaveProjectMenuItem.IsEnabled = _project is not null && !_isProjectOperation;
        ViewProjectAssetsMenuItem.IsEnabled = _project is not null && !_isProjectOperation;
        CloseProjectMenuItem.IsEnabled = _project is not null && !_isProjectOperation;
        ExportAllCodeMenuItem.IsEnabled = codeCount > 0;
        ExportStringsMenuItem.IsEnabled = stringCount > 0;
        ImportStringsMenuItemMenu.IsEnabled = canWrite && stringCount > 0;
        CommandRunButton.IsEnabled = canRunCommand;
    }

    private static List<string> ReadRecentFilePaths()
    {
        try
        {
            object? recentValue = ApplicationData.Current.LocalSettings.Values[RecentFilePathsSetting];
            if (recentValue is string recentJson && !string.IsNullOrWhiteSpace(recentJson))
            {
                string[]? paths = JsonSerializer.Deserialize<string[]>(recentJson);
                if (paths is not null)
                    return NormalizeRecentFilePaths(paths).ToList();
            }

            object? lastValue = ApplicationData.Current.LocalSettings.Values[LastOpenedFilePathSetting];
            if (lastValue is string lastPath && !string.IsNullOrWhiteSpace(lastPath))
                return NormalizeRecentFilePaths([lastPath]).ToList();
        }
        catch
        {
        }

        return [];
    }

    private void RememberOpenedFile(string path)
    {
        _recentFilePaths = NormalizeRecentFilePaths([path, .. _recentFilePaths]).ToList();
        _lastOpenedFilePath = _recentFilePaths.FirstOrDefault();
        SaveRecentFilePaths();
        UpdateRecentFileUi();
        UpdateCommandStates();
    }

    private void SaveRecentFilePaths()
    {
        try
        {
            ApplicationData.Current.LocalSettings.Values[RecentFilePathsSetting] = JsonSerializer.Serialize(_recentFilePaths);
            if (_lastOpenedFilePath is not null)
                ApplicationData.Current.LocalSettings.Values[LastOpenedFilePathSetting] = _lastOpenedFilePath;
            else
                ApplicationData.Current.LocalSettings.Values.Remove(LastOpenedFilePathSetting);
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Could not remember recent files:{Environment.NewLine}{ex.Message}";
        }
    }

    private void UpdateRecentFileUi()
    {
        _lastOpenedFilePath = _recentFilePaths.FirstOrDefault();
        bool hasRecent = _recentFilePaths.Count > 0;
        OpenRecentMenuItem.IsEnabled = hasRecent && OpenButton.IsEnabled;
        RecentFileItem[] items = _recentFilePaths.Select(BuildRecentFileItem).ToArray();
        RecentFilesItems.ItemsSource = items;
        RecentFilesItems.IsHitTestVisible = OpenButton.IsEnabled;
        NoRecentFilesPanel.Visibility = items.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        ClearRecentFilesButton.IsEnabled = hasRecent && OpenButton.IsEnabled;
        RefreshRecentFilesMenu(items);
    }

    private void RefreshRecentFilesMenu(IReadOnlyList<RecentFileItem> items)
    {
        if (OpenRecentMenuItem is not MenuFlyoutSubItem openRecentMenuItem)
            return;

        openRecentMenuItem.Items.Clear();
        if (items.Count == 0)
        {
            openRecentMenuItem.Items.Add(new MenuFlyoutItem
            {
                Text = "No recent files",
                IsEnabled = false
            });
            return;
        }

        foreach (RecentFileItem item in items)
        {
            MenuFlyoutItem menuItem = new()
            {
                Text = item.Title,
                Tag = item.Path,
                IsEnabled = OpenButton.IsEnabled && item.Status != "Missing"
            };
            ToolTipService.SetToolTip(menuItem, item.Path);
            menuItem.Click += OpenRecentMenuItem_Click;
            openRecentMenuItem.Items.Add(menuItem);
        }

        openRecentMenuItem.Items.Add(new MenuFlyoutSeparator());
        MenuFlyoutItem clearItem = new()
        {
            Text = "Clear recent files",
            IsEnabled = OpenButton.IsEnabled
        };
        clearItem.Click += ClearRecentFilesButton_Click;
        openRecentMenuItem.Items.Add(clearItem);
    }

    private static IEnumerable<string> NormalizeRecentFilePaths(IEnumerable<string> paths)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (string path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            string normalizedPath = path.Trim();
            if (!seen.Add(normalizedPath))
                continue;

            yield return normalizedPath;
            if (seen.Count >= MaxRecentFileCount)
                yield break;
        }
    }

    private static RecentFileItem BuildRecentFileItem(string path)
    {
        string title = string.IsNullOrWhiteSpace(Path.GetFileName(path))
            ? path
            : Path.GetFileName(path);
        string status = File.Exists(path) ? "Available" : "Missing";
        return new RecentFileItem(title, path, status);
    }

    private void RefreshScriptsMenu()
    {
        ScriptsMenuItem.Items.Clear();

        MenuFlyoutItem openFolderItem = new()
        {
            Text = "Open scripts folder"
        };
        openFolderItem.Click += OpenScriptsFolderMenuItem_Click;
        ScriptsMenuItem.Items.Add(openFolderItem);

        MenuFlyoutItem openOtherItem = new()
        {
            Text = "Open other script..."
        };
        openOtherItem.Click += OpenOtherScriptMenuItem_Click;
        ScriptsMenuItem.Items.Add(openOtherItem);
        ScriptsMenuItem.Items.Add(new MenuFlyoutSeparator());

        string? scriptsRoot = GetScriptsRoot();
        if (scriptsRoot is null)
        {
            ScriptsMenuItem.Items.Add(new MenuFlyoutItem
            {
                Text = "No built-in scripts found",
                IsEnabled = false
            });
            return;
        }

        try
        {
            int count = AddScriptMenuItems(ScriptsMenuItem.Items, new DirectoryInfo(scriptsRoot));
            if (count == 0)
            {
                ScriptsMenuItem.Items.Add(new MenuFlyoutItem
                {
                    Text = "No built-in scripts found",
                    IsEnabled = false
                });
            }
        }
        catch (Exception ex)
        {
            ScriptsMenuItem.Items.Add(new MenuFlyoutItem
            {
                Text = $"Could not read scripts: {ex.Message}",
                IsEnabled = false
            });
        }
    }

    private int AddScriptMenuItems(IList<MenuFlyoutItemBase> targetItems, DirectoryInfo directory)
    {
        if (!directory.Exists)
            return 0;

        int count = 0;
        foreach (FileInfo file in directory.EnumerateFiles("*.csx").OrderBy(file => file.Name))
        {
            MenuFlyoutItem scriptItem = new()
            {
                Text = Path.GetFileNameWithoutExtension(file.Name),
                Tag = file.FullName
            };
            scriptItem.Click += BuiltInScriptMenuItem_Click;
            targetItems.Add(scriptItem);
            count++;
        }

        foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories().OrderBy(dir => dir.Name))
        {
            MenuFlyoutSubItem subItem = new()
            {
                Text = subDirectory.Name
            };
            int scriptCount = AddScriptMenuItems(subItem.Items, subDirectory);
            if (scriptCount == 0)
                continue;

            targetItems.Add(subItem);
            count += scriptCount;
        }

        return count;
    }

    private static string? GetScriptsRoot()
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "Scripts"),
            Path.Combine(Directory.GetCurrentDirectory(), "Scripts"),
            Path.Combine(Directory.GetCurrentDirectory(), "UndertaleModTool", "Scripts"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "UndertaleModTool", "Scripts"))
        ];

        return candidates.FirstOrDefault(path => Directory.Exists(path));
    }

    private void OpenShellTarget(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Could not open {path}:{Environment.NewLine}{ex.Message}";
        }
    }

    public UndertaleData Data => _data ?? throw new ScriptException("No data file is currently loaded.");

    public ProjectContext? Project => _project;

    public string FilePath => _currentFilePath ?? string.Empty;

    public string ScriptPath => _currentScriptPath ?? string.Empty;

    public object? Highlighted => _selectedResource?.Value;

    public object? Selected => _selectedResource?.Value;

    public bool CanSave => _data is not null && !_data.UnsupportedBytecodeVersion;

    public bool ScriptExecutionSuccess => _scriptExecutionSuccess;

    public string ScriptErrorMessage => _scriptErrorMessage;

    public string ExePath => AppContext.BaseDirectory;

    public string ScriptErrorType => _scriptErrorType;

    public bool IsAppClosed => false;

    public Action<Action> MainThreadAction => RunOnUiThreadBlocking;

    public bool MakeNewDataFile()
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            CreateNewDataFileCore(UndertaleData.CreateNew());
            return true;
        }

        return RunOnUiThreadBlocking(CreateNewDataFileAsync);
    }

    public void ScriptMessage(string message) => SetUMTConsoleText(message);

    public void ScriptWarning(string message) => SetUMTConsoleText($"Warning: {message}");

    public void SetUMTConsoleText(string message)
    {
        DispatcherQueue.TryEnqueue(() => StatusBox.Text = message);
    }

    public bool ScriptQuestion(string message)
    {
        return RunOnUiThreadBlocking(async () =>
        {
            ContentDialog dialog = new()
            {
                Title = "Script question",
                Content = message,
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        });
    }

    public void ScriptError(string error, string title = "Error", bool SetConsoleText = true)
    {
        if (SetConsoleText)
            SetUMTConsoleText($"{title}: {error}");
    }

    public void ScriptOpenURL(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            OpenShellTarget(uri.ToString());
    }

    public bool RunUMTScript(string path)
    {
        string? resolvedPath = ResolveScriptPath(path);
        if (resolvedPath is null)
        {
            _scriptExecutionSuccess = false;
            _scriptErrorMessage = $"Script file was not found: {path}";
            _scriptErrorType = "FileNotFound";
            SetUMTConsoleText(_scriptErrorMessage);
            return false;
        }

        try
        {
            EvaluateScriptFileAsync(resolvedPath).GetAwaiter().GetResult();
            return true;
        }
        catch (CompilationErrorException ex)
        {
            _scriptExecutionSuccess = false;
            _scriptErrorMessage = ex.Message;
            _scriptErrorType = nameof(CompilationErrorException);
            SetUMTConsoleText(ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _scriptExecutionSuccess = false;
            _scriptErrorMessage = ex.Message;
            _scriptErrorType = ex.GetType().Name;
            SetUMTConsoleText(ScriptingUtil.PrettifyException(in ex));
            return false;
        }
    }

    public bool LintUMTScript(string path)
    {
        try
        {
            string? resolvedPath = ResolveScriptPath(path);
            if (resolvedPath is null)
                return false;

            string scriptText = $"#line 1 \"{resolvedPath}\"{Environment.NewLine}" + File.ReadAllText(resolvedPath, Encoding.UTF8);
            CSharpScript.Create(scriptText, _scriptOptions.WithFilePath(resolvedPath).WithFileEncoding(Encoding.UTF8), typeof(IScriptInterface))
                        .Compile();
            return true;
        }
        catch (CompilationErrorException ex)
        {
            _scriptExecutionSuccess = false;
            _scriptErrorMessage = ex.Message;
            _scriptErrorType = nameof(CompilationErrorException);
            SetUMTConsoleText(ex.Message);
            return false;
        }
    }

    private string? ResolveScriptPath(string path)
    {
        if (File.Exists(path))
            return Path.GetFullPath(path);

        string[] candidates =
        [
            string.IsNullOrWhiteSpace(_currentScriptPath)
                ? path
                : Path.Combine(Path.GetDirectoryName(_currentScriptPath) ?? string.Empty, path),
            Path.Combine(GetScriptsRoot() ?? string.Empty, path),
            Path.Combine(AppContext.BaseDirectory, path)
        ];

        return candidates.FirstOrDefault(File.Exists) is { } resolved
            ? Path.GetFullPath(resolved)
            : null;
    }

    public void InitializeScriptDialog()
    {
    }

    public string GetDecompiledText(string codeName, GlobalDecompileContext? context = null, IDecompileSettings? settings = null)
    {
        return GetDecompiledText(Data.Code.ByName(codeName), context, settings);
    }

    public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext? context = null, IDecompileSettings? settings = null)
    {
        if (code.ParentEntry is not null)
            return $"// This code entry is a reference to an anonymous function within \"{code.ParentEntry.Name?.Content}\", decompile that instead.";

        GlobalDecompileContext globalContext = context ?? new GlobalDecompileContext(Data);
        return new DecompileContext(globalContext, code, settings ?? Data.ToolInfo.DecompilerSettings).DecompileToString();
    }

    public string GetDisassemblyText(string codeName) => GetDisassemblyText(Data.Code.ByName(codeName));

    public string GetDisassemblyText(UndertaleCode code) => BuildCodeDisassembly(Data, code);

    public string ScriptInputDialog(string title, string label, string defaultInput, string cancelText, string submitText, bool isMultiline, bool preventClose)
    {
        return ShowScriptTextInput(title, label, defaultInput, isMultiline, cancelText, submitText, preventClose);
    }

    public string SimpleTextInput(string title, string label, string defaultValue, bool allowMultiline, bool showDialog = true)
    {
        return showDialog
            ? ShowScriptTextInput(title, label, defaultValue, allowMultiline, "Cancel", "OK", preventClose: false)
            : defaultValue;
    }

    public void SimpleTextOutput(string title, string label, string message, bool allowMultiline)
    {
        SetUMTConsoleText($"{title}{Environment.NewLine}{label}{Environment.NewLine}{message}");
    }

    public System.Threading.Tasks.Task ClickableSearchOutput(
        string title,
        string query,
        int resultsCount,
        IOrderedEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> resultsDict,
        bool showInDecompiledView,
        IOrderedEnumerable<string>? failedList = null)
    {
        return ShowScriptClickableSearchOutput(title, query, resultsCount, resultsDict, showInDecompiledView, failedList);
    }

    public System.Threading.Tasks.Task ClickableSearchOutput(
        string title,
        string query,
        int resultsCount,
        IDictionary<string, List<(int lineNum, string codeLine)>> resultsDict,
        bool showInDecompiledView,
        IEnumerable<string>? failedList = null)
    {
        IEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> ordered = resultsDict.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase);
        return ShowScriptClickableSearchOutput(title, query, resultsCount, ordered, showInDecompiledView, failedList);
    }

    private System.Threading.Tasks.Task ShowScriptClickableSearchOutput(
        string title,
        string query,
        int resultsCount,
        IEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> results,
        bool showInDecompiledView,
        IEnumerable<string>? failedList)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ScriptCodeSearchResult[] items = BuildScriptCodeSearchResults(results, query, showInDecompiledView).ToArray();
            if (items.Length == 0)
            {
                SetUMTConsoleText($"{title}: no clickable result(s) for \"{query}\".");
                return;
            }

            NavigateToCategory("Code");
            ShowGlobalTools(GlobalToolsMode.Code);
            GlobalCodeSearchModeBox.SelectedIndex = showInDecompiledView ? 1 : 0;
            GlobalCodeSearchBox.Text = query;
            GlobalCodeSearchResultsList.ItemsSource = items;

            string failedText = failedList is null ? string.Empty : $" Failed: {failedList.Count()}.";
            StatusBox.Text = $"{title}: showing {items.Length} of {resultsCount} script search result(s) for \"{query}\".{failedText}";
        });

        return System.Threading.Tasks.Task.CompletedTask;
    }

    private IEnumerable<ScriptCodeSearchResult> BuildScriptCodeSearchResults(
        IEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> results,
        string query,
        bool decompiled)
    {
        if (_data is null)
            yield break;

        foreach (KeyValuePair<string, List<(int lineNum, string codeLine)>> result in results)
        {
            UndertaleCode? code = _data.Code.ByName(result.Key);
            if (code is null)
                continue;

            int codeIndex = _data.Code.IndexOf(code);
            if (codeIndex < 0)
                continue;

            foreach ((int lineNum, string codeLine) in result.Value)
            {
                yield return new ScriptCodeSearchResult(codeIndex, result.Key, lineNum, codeLine.Trim(), decompiled, query);
            }
        }
    }

    public void SetFinishedMessage(bool isFinishedMessageEnabled)
    {
        _finishedMessageEnabled = isFinishedMessageEnabled;
    }

    public void UpdateProgressBar(string message, string status, double progressValue, double maxValue) => SetProgressBar(message, status, progressValue, maxValue);

    public void SetProgressBar(string message, string status, double progressValue, double maxValue)
    {
        lock (_scriptProgressGate)
        {
            _scriptProgressMessage = string.IsNullOrWhiteSpace(message) ? "Script progress" : message;
            _scriptProgressStatus = status ?? string.Empty;
            _scriptProgressMaximum = Math.Max(1, maxValue);
            _scriptProgressValue = (int)Math.Clamp(progressValue, 0, _scriptProgressMaximum);
        }

        UpdateScriptProgressUi(show: true);
    }

    public void SetProgressBar()
    {
        lock (_scriptProgressGate)
        {
            _scriptProgressMessage = "Script progress";
            _scriptProgressStatus = string.Empty;
            _scriptProgressMaximum = Math.Max(1, _scriptProgressMaximum);
        }

        UpdateScriptProgressUi(show: true);
    }

    public void UpdateProgressValue(double progressValue)
    {
        lock (_scriptProgressGate)
        {
            _scriptProgressValue = (int)Math.Clamp(progressValue, 0, _scriptProgressMaximum);
        }

        UpdateScriptProgressUi(show: true);
    }

    public void UpdateProgressStatus(string status)
    {
        lock (_scriptProgressGate)
        {
            _scriptProgressStatus = status ?? string.Empty;
        }

        UpdateScriptProgressUi(show: true);
    }

    public void AddProgress(int amount)
    {
        lock (_scriptProgressGate)
        {
            _scriptProgressValue = (int)Math.Clamp(_scriptProgressValue + amount, 0, _scriptProgressMaximum);
        }

        UpdateScriptProgressUi(show: true);
    }

    public void IncrementProgress()
    {
        AddProgress(1);
    }

    public void AddProgressParallel(int amount)
    {
        AddProgress(amount);
    }

    public void IncrementProgressParallel()
    {
        IncrementProgress();
    }

    public int GetProgress()
    {
        lock (_scriptProgressGate)
        {
            return _scriptProgressValue;
        }
    }

    public void SetProgress(int value)
    {
        lock (_scriptProgressGate)
        {
            _scriptProgressValue = (int)Math.Clamp(value, 0, _scriptProgressMaximum);
        }

        UpdateScriptProgressUi(show: true);
    }

    public void HideProgressBar()
    {
        UpdateScriptProgressUi(show: false);
    }

    public void EnableUI()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            OpenButton.IsEnabled = true;
            UpdateCommandStates();
        });
    }

    public void StartProgressBarUpdater()
    {
        SetProgressBar();
    }

    public System.Threading.Tasks.Task StopProgressBarUpdater() => System.Threading.Tasks.Task.CompletedTask;

    private void UpdateScriptProgressUi(bool show)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ScriptProgressPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (!show)
                return;

            int value;
            double maximum;
            string message;
            string status;

            lock (_scriptProgressGate)
            {
                value = _scriptProgressValue;
                maximum = Math.Max(1, _scriptProgressMaximum);
                message = _scriptProgressMessage;
                status = _scriptProgressStatus;
            }

            ScriptProgressMessageText.Text = message;
            ScriptProgressStatusText.Text = status;
            ScriptProgressBar.Maximum = maximum;
            ScriptProgressBar.Value = Math.Clamp(value, 0, maximum);
            ScriptProgressValueText.Text = $"{value}/{maximum:0}";
        });
    }

    public void ChangeSelection(object newSelection, bool inNewTab = false)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ResourceItem? item = FindResourceItem(newSelection, out ResourceCategory? category);
            if (item is null || category is null)
                return;

            _selectedCategory = category;
            CategoryList.SelectedItem = category;
            SetResourceFilterText(string.Empty);
            ApplyResourceFilter();
            ResourceList.SelectedItem = item;
            OpenResourceItem(item, inNewTab);
        });
    }

    public string PromptChooseDirectory()
    {
        return RunOnUiThreadBlocking(async () =>
        {
            FolderPicker picker = new()
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add("*");
            InitializePickerWithMainWindow(picker);

            StorageFolder? folder = await picker.PickSingleFolderAsync();
            return folder?.Path ?? throw new ScriptCancelledException("No directory was selected.");
        });
    }

    public string PromptLoadFile(string defaultExt, string filter)
    {
        return RunOnUiThreadBlocking(async () =>
        {
            FileOpenPicker picker = new()
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            AddPickerFileTypes(picker.FileTypeFilter, defaultExt, filter);
            InitializePickerWithMainWindow(picker);

            StorageFile? file = await picker.PickSingleFileAsync();
            return file?.Path ?? throw new ScriptCancelledException("No file was selected.");
        });
    }

    public string PromptSaveFile(string defaultExt, string filter)
    {
        return RunOnUiThreadBlocking(async () =>
        {
            FileSavePicker picker = new()
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = $"untitled.{defaultExt.TrimStart('.')}"
            };
            AddPickerFileTypeChoices(picker.FileTypeChoices, defaultExt, filter);
            InitializePickerWithMainWindow(picker);

            StorageFile? file = await picker.PickSaveFileAsync();
            return file?.Path ?? throw new ScriptCancelledException("No save path was selected.");
        });
    }

    private string ShowScriptTextInput(
        string title,
        string label,
        string defaultValue,
        bool allowMultiline,
        string cancelText,
        string submitText,
        bool preventClose)
    {
        return RunOnUiThreadBlocking(async () =>
        {
            TextBox input = new()
            {
                Text = defaultValue,
                AcceptsReturn = allowMultiline,
                TextWrapping = allowMultiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
                MinWidth = 360,
                MinHeight = allowMultiline ? 140 : 0
            };
            StackPanel content = new()
            {
                Spacing = 8
            };
            content.Children.Add(new TextBlock
            {
                Text = label,
                TextWrapping = TextWrapping.Wrap
            });
            content.Children.Add(input);

            ContentDialog dialog = new()
            {
                Title = title,
                Content = content,
                PrimaryButtonText = string.IsNullOrWhiteSpace(submitText) ? "OK" : submitText,
                CloseButtonText = preventClose ? string.Empty : (string.IsNullOrWhiteSpace(cancelText) ? "Cancel" : cancelText),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary
                ? input.Text
                : throw new ScriptCancelledException("Script input was cancelled.");
        });
    }

    private T RunOnUiThreadBlocking<T>(Func<System.Threading.Tasks.Task<T>> operation)
    {
        if (DispatcherQueue.HasThreadAccess)
            throw new ScriptException("Synchronous script UI prompts cannot be shown from the UI thread.");

        using ManualResetEventSlim completed = new(false);
        T? result = default;
        Exception? error = null;
        if (!DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    result = await operation();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    completed.Set();
                }
            }))
        {
            throw new ScriptException("Could not schedule script UI prompt.");
        }

        completed.Wait();
        if (error is not null)
            throw error;

        return result!;
    }

    private void RunOnUiThreadBlocking(Action operation)
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            operation();
            return;
        }

        using ManualResetEventSlim completed = new(false);
        Exception? error = null;
        if (!DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    operation();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    completed.Set();
                }
            }))
        {
            throw new ScriptException("Could not schedule main-thread operation.");
        }

        completed.Wait();
        if (error is not null)
            throw error;
    }

    private static void AddPickerFileTypes(IList<string> fileTypeFilter, string defaultExt, string filter)
    {
        string[] extensions = ExtractPickerExtensions(defaultExt, filter);
        foreach (string extension in extensions)
            fileTypeFilter.Add(extension);
    }

    private static void AddPickerFileTypeChoices(IDictionary<string, IList<string>> fileTypeChoices, string defaultExt, string filter)
    {
        string[] extensions = ExtractPickerExtensions(defaultExt, filter);
        fileTypeChoices.Add("Script selected file type", extensions);
    }

    private static string[] ExtractPickerExtensions(string defaultExt, string filter)
    {
        string normalizedDefault = NormalizePickerExtension(defaultExt);
        string[] extensions = filter.Split('|', StringSplitOptions.RemoveEmptyEntries)
                                    .Where(part => part.Contains("*."))
                                    .SelectMany(part => part.Split(';', StringSplitOptions.RemoveEmptyEntries))
                                    .Select(part => NormalizePickerExtension(part.Replace("*", string.Empty).Trim()))
                                    .Where(part => part != ".*")
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .ToArray();

        if (extensions.Length > 0)
            return extensions;

        return [normalizedDefault == ".*" ? "*" : normalizedDefault];
    }

    private static string NormalizePickerExtension(string extension)
    {
        string trimmed = extension.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return "*";

        if (trimmed == "*" || trimmed == "*.*")
            return "*";

        if (trimmed.StartsWith("*.", StringComparison.Ordinal))
            trimmed = trimmed[1..];

        if (!trimmed.StartsWith(".", StringComparison.Ordinal))
            trimmed = "." + trimmed;

        return trimmed;
    }

    private static void InitializePickerWithMainWindow(object picker)
    {
        if (App.MainWindow is null)
            return;

        nint hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        switch (picker)
        {
            case FileOpenPicker openPicker:
                InitializeWithWindow.Initialize(openPicker, hwnd);
                break;
            case FileSavePicker savePicker:
                InitializeWithWindow.Initialize(savePicker, hwnd);
                break;
            case FolderPicker folderPicker:
                InitializeWithWindow.Initialize(folderPicker, hwnd);
                break;
        }
    }

    private ResourceItem? FindResourceItem(object value, out ResourceCategory? category)
    {
        foreach (ResourceCategory candidateCategory in _categories)
        {
            ResourceItem? item = candidateCategory.Items.FirstOrDefault(candidate => ReferenceEquals(candidate.Value, value));
            if (item is not null)
            {
                category = candidateCategory;
                return item;
            }
        }

        category = null;
        return null;
    }

    private void UpdateGlobalToolsVisibility()
    {
        bool isCodeCategory = _selectedCategory?.Label == "Code";
        bool isStringCategory = _selectedCategory?.Label == "Strings";
        bool hasGlobalTools = isCodeCategory || isStringCategory;

        if (!hasGlobalTools)
        {
            _selectedGlobalToolsMode = GlobalToolsMode.None;
            _keepGlobalToolsExpanded = false;
        }
        else if ((_selectedGlobalToolsMode == GlobalToolsMode.Code && !isCodeCategory) ||
                 (_selectedGlobalToolsMode == GlobalToolsMode.Strings && !isStringCategory))
        {
            _selectedGlobalToolsMode = GlobalToolsMode.None;
            _keepGlobalToolsExpanded = false;
        }

        bool showCodeTools = isCodeCategory && _selectedGlobalToolsMode == GlobalToolsMode.Code;
        bool showStringTools = isStringCategory && _selectedGlobalToolsMode == GlobalToolsMode.Strings;
        bool hasVisiblePanel = showCodeTools || showStringTools;

        _isUpdatingGlobalTools = true;
        try
        {
            GlobalToolsExpander.Visibility = hasGlobalTools ? Visibility.Visible : Visibility.Collapsed;
            GlobalToolsExpander.IsExpanded = hasVisiblePanel && _keepGlobalToolsExpanded;
            GlobalCodeToolsPanel.Visibility = showCodeTools ? Visibility.Visible : Visibility.Collapsed;
            GlobalStringToolsPanel.Visibility = showStringTools ? Visibility.Visible : Visibility.Collapsed;
        }
        finally
        {
            _isUpdatingGlobalTools = false;
        }
    }

    private void ShowGlobalTools(GlobalToolsMode mode)
    {
        _selectedGlobalToolsMode = mode;
        _keepGlobalToolsExpanded = true;
        UpdateGlobalToolsVisibility();
    }

    private bool NavigateToCategory(string categoryLabel)
    {
        ResourceCategory? category = _categories.FirstOrDefault(candidate => candidate.Label == categoryLabel);
        if (category is null)
            return false;

        ResourceList.SelectedIndex = -1;
        _selectedCategory = category;
        _selectedResource = null;
        CategoryList.SelectedItem = category;
        UpdateGlobalToolsVisibility();
        SetResourceFilterText(string.Empty);
        ApplyResourceFilter(force: true);
        UpdateResourceCommandButtons();
        UpdateCommandStates();
        OpenFirstFilteredResourceOrShowCategory(addTab: false);
        return true;
    }

    private void OpenFirstFilteredResourceOrShowCategory(bool addTab)
    {
        IReadOnlyList<ResourceItem>? items = _lastFilteredResourceItems;
        if (items is { Count: > 0 })
        {
            ResourceItem item = items[0];
            ResourceList.SelectedItem = item;
            OpenResourceItem(item, addTab);
            return;
        }

        ShowCategorySummaryWithoutResourceSelection();
    }

    private void ShowSelectedCategorySummary()
    {
        if (_selectedCategory is null)
        {
            DetailsTitleText.Text = "Details";
            DetailsList.ItemsSource = null;
            UpdateProjectExportToggle();
            return;
        }

        DetailsTitleText.Text = _selectedCategory.Label;
        DetailsExpander.Visibility = Visibility.Visible;
        DetailsList.ItemsSource = new[]
        {
            new DetailRow("Items", _selectedCategory.Count.ToString(CultureInfo.InvariantCulture))
        };
        UpdateProjectExportToggle();
    }

    private void ShowCategorySummaryWithoutResourceSelection()
    {
        _selectedResource = null;
        HideEditors();
        ShowSelectedCategorySummary();
        ClearSelectedResourceTab();
        UpdateResourceListFooter(_lastFilteredResourceItems, _selectedCategory?.Count ?? 0);
        UpdateCommandStates();
    }

    private void OpenResourceItem(ResourceItem item, bool addTab, bool revealInList = true, bool syncTabSelection = true)
    {
        _selectedResource = item;
        DetailsTitleText.Text = item.Title;
        DetailsList.ItemsSource = BuildDetails(item).ToArray();
        ShowEditorFor(item);
        UpdateProjectExportToggle();
        UpdateResourceCommandButtons();
        UpdateResourceListFooter(_lastFilteredResourceItems, _selectedCategory?.Count ?? 0);
        if (revealInList)
            RevealResourceListItem(item);

        if (addTab && _selectedCategory is not null)
            AddOrSelectResourceTab(_selectedCategory.Label, item);
        else if (syncTabSelection && _selectedCategory is not null)
        {
            SelectResourceTab(_selectedCategory.Label, item.Index);
        }
        else if (!syncTabSelection)
        {
            ClearSelectedResourceTab();
        }

        if (_selectedCategory is not null)
            RecordResourceNavigation(_selectedCategory.Label, item.Index);

        UpdateCommandStates();
    }

    private void AddOrSelectResourceTab(string categoryLabel, ResourceItem item)
    {
        ResourceTab? existing = _openResourceTabs.FirstOrDefault(tab =>
            tab.CategoryLabel == categoryLabel && tab.ItemIndex == item.Index);

        if (existing is null)
        {
            existing = new ResourceTab(categoryLabel, item.Index, item.Title, item.IconSymbol);
            _openResourceTabs.Add(existing);
        }
        else
        {
            existing.Title = item.Title;
            existing.IconSymbol = item.IconSymbol;
        }

        OpenResourceTabsList.Visibility = Visibility.Visible;
        SelectOpenResourceTab(existing);
        UpdateCommandStates();
    }

    private void SelectResourceTab(string categoryLabel, int itemIndex)
    {
        ResourceTab? tab = _openResourceTabs.FirstOrDefault(candidate =>
            candidate.CategoryLabel == categoryLabel && candidate.ItemIndex == itemIndex);
        if (tab is not null)
            SelectOpenResourceTab(tab);
    }

    private void SelectOpenResourceTab(ResourceTab tab)
    {
        _isUpdatingOpenResourceTabs = true;
        OpenResourceTabsList.SelectedItem = tab;
        _isUpdatingOpenResourceTabs = false;
        UpdateCommandStates();
    }

    private void ClearSelectedResourceTab()
    {
        if (OpenResourceTabsList.SelectedItem is null)
            return;

        _isUpdatingOpenResourceTabs = true;
        try
        {
            OpenResourceTabsList.SelectedItem = null;
        }
        finally
        {
            _isUpdatingOpenResourceTabs = false;
        }
    }

    private void RefreshCategoriesPreservingSelection()
    {
        if (_data is null)
            return;

        string? categoryLabel = _selectedCategory?.Label ?? (CategoryList.SelectedItem as ResourceCategory)?.Label;
        int itemIndex = _selectedResource?.Index ?? -1;
        IReadOnlyList<ResourceCategory> categories = BuildCategories(_data);
        _categories = categories;
        CategoryList.ItemsSource = _categories;

        ResourceCategory? selectedCategory = categories.FirstOrDefault(category => category.Label == categoryLabel) ??
                                             categories.FirstOrDefault();
        CategoryList.SelectedItem = selectedCategory;
        _selectedCategory = selectedCategory;
        UpdateGlobalToolsVisibility();
        ApplyResourceFilter();

        if (selectedCategory is null || itemIndex < 0)
            return;

        ResourceItem? selectedItem = selectedCategory.Items.FirstOrDefault(item => item.Index == itemIndex);
        if (selectedItem is null)
            return;

        ResourceList.SelectedItem = selectedItem;
        _selectedResource = selectedItem;
        DetailsTitleText.Text = selectedItem.Title;
        DetailsList.ItemsSource = BuildDetails(selectedItem).ToArray();
        ShowEditorFor(selectedItem);
    }

    private void RefreshSelectedResourceTitle()
    {
        if (_selectedCategory is null || _selectedResource is null)
            return;

        ResourceItem updatedItem = BuildResourceItem(_selectedResource.Value, _selectedResource.Index);
        _selectedResource.Title = updatedItem.Title;
        _selectedResource.Subtitle = updatedItem.Subtitle;
        _selectedResource.IconSymbol = updatedItem.IconSymbol;

        DetailsTitleText.Text = _selectedResource.Title;
        UpdateGlobalToolsVisibility();
        ApplyResourceFilter(force: true);
        ResourceList.SelectedItem = _selectedResource;
        UpdateOpenResourceTabTitle(_selectedCategory.Label, _selectedResource);
        UpdateProjectExportToggle();
    }

    private void RefreshSelectedResourceDependentPanels()
    {
        if (_selectedResource is null)
            return;

        if (_selectedResource.Value is UndertaleTexturePageItem)
            ShowTexturePageItemReferencesFor(_selectedResource);

        if (TryGetPreviewableTextureValue(_selectedResource.Value, out object? textureValue) && textureValue is not null)
            TexturePreviewInfoText.Text = BuildTexturePreviewInfo(textureValue);

        if (_selectedResource.Value is UndertaleBackground background)
        {
            BackgroundSummaryText.Text = BuildBackgroundSummary(background);
            BackgroundOpenTextureButton.IsEnabled = background.Texture is not null;
            BackgroundOpenExportedSpriteButton.IsEnabled = background.GMS2ExportedSprite is not null;
            RefreshBackgroundTileIds(background, (BackgroundTileIdsList.SelectedItem as BackgroundTileIdSummary)?.TileId);
        }

        if (_selectedResource.Value is UndertaleCodeLocals codeLocals)
            CodeLocalsSummaryText.Text = BuildCodeLocalsSummary(codeLocals);

        if (_selectedResource.Value is UndertaleAudioGroup audioGroup)
            AudioGroupSummaryText.Text = BuildAudioGroupSummary(audioGroup);

        if (_selectedResource.Value is UndertaleGeneralInfo generalInfo)
            RefreshGeneralInfoEditor(generalInfo);

        if (_selectedResource.Value is UndertaleGameObject)
            ShowObjectSummaryFor(_selectedResource);

        if (_selectedResource.Value is UndertaleTextureGroupInfo textureGroup)
            TextureGroupSummaryText.Text = BuildTextureGroupSummary(textureGroup);

        if (_selectedResource.Value is UndertaleFont font)
        {
            FontSummaryText.Text = BuildFontSummary(font);
            IReadOnlyList<FontGlyphSummary> glyphs = BuildFontGlyphSummaries(font);
            FontGlyphsList.ItemsSource = glyphs;
            FontGlyphSummary? selectedGlyph = glyphs.FirstOrDefault();
            FontGlyphsList.SelectedItem = selectedGlyph;
            UpdateFontGlyphEditor(selectedGlyph);
            RefreshFontKerningForGlyph(selectedGlyph);
            UpdateFontActionButtons();
        }
    }

    private void UpdateOpenResourceTabTitle(string categoryLabel, ResourceItem item)
    {
        ResourceTab? tab = _openResourceTabs.FirstOrDefault(candidate =>
            candidate.CategoryLabel == categoryLabel && candidate.ItemIndex == item.Index);
        if (tab is not null)
            tab.Title = item.Title;
    }

    private void UpdateProjectExportToggle()
    {
        _isUpdatingProjectExportToggle = true;
        try
        {
            if (_project is ProjectContext project &&
                _selectedResource?.Value is IProjectAsset { ProjectExportable: true } projectAsset)
            {
                ProjectExportPanel.Visibility = Visibility.Visible;
                ProjectExportDescriptionText.Text = $"{_selectedResource.Title} will be included the next time the project is saved.";
                ProjectExportCheckBox.IsEnabled = !_isProjectOperation;
                ProjectExportCheckBox.IsChecked = project.IsAssetMarkedForExport(projectAsset);
                return;
            }

            ProjectExportCheckBox.IsChecked = false;
            ProjectExportPanel.Visibility = Visibility.Collapsed;
            ProjectExportDescriptionText.Text = string.Empty;
        }
        finally
        {
            _isUpdatingProjectExportToggle = false;
        }
    }

    private void HideProjectExportToggle()
    {
        _isUpdatingProjectExportToggle = true;
        try
        {
            ProjectExportCheckBox.IsChecked = false;
            ProjectExportPanel.Visibility = Visibility.Collapsed;
            ProjectExportDescriptionText.Text = string.Empty;
        }
        finally
        {
            _isUpdatingProjectExportToggle = false;
        }
    }

    private void ProjectExportCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingProjectExportToggle ||
            _project is not ProjectContext project ||
            _selectedResource?.Value is not IProjectAsset { ProjectExportable: true } projectAsset ||
            ProjectExportCheckBox.IsChecked is not bool isChecked)
        {
            return;
        }

        bool changed = isChecked
            ? project.MarkAssetForExport(projectAsset)
            : project.UnmarkAssetForExport(projectAsset);

        if (!changed)
            return;

        StatusBox.Text = isChecked
            ? $"Marked {_selectedResource.Title} for project export."
            : $"Unmarked {_selectedResource.Title} for project export.";
        UpdateCommandStates();
        UpdateWindowTitle();
    }

    private void ScheduleResourceFilter()
    {
        if (_resourceFilterTimer is null)
        {
            ApplyResourceFilter();
            return;
        }

        _resourceFilterTimer.Stop();
        _resourceFilterTimer.Start();
    }

    private void ApplyResourceFilter(bool force = false)
    {
        if (_selectedCategory is null)
        {
            ResourceList.ItemsSource = null;
            _lastFilteredCategory = null;
            _lastAppliedResourceFilter = null;
            _lastFilteredResourceItems = null;
            UpdateResourceListFooter(null, 0);
            return;
        }

        string filter = ResourceFilterBox.Text.Trim();
        IReadOnlyList<ResourceItem> items;
        if (!force &&
            ReferenceEquals(_selectedCategory, _lastFilteredCategory) &&
            string.Equals(filter, _lastAppliedResourceFilter, StringComparison.Ordinal) &&
            _lastFilteredResourceItems is not null)
        {
            items = _lastFilteredResourceItems;
        }
        else
        {
            items = string.IsNullOrEmpty(filter)
                ? _selectedCategory.Items
                : _selectedCategory.Items
                                   .Where(item => ResourceMatchesFilter(item, filter))
                                   .ToArray();
            _lastFilteredCategory = _selectedCategory;
            _lastAppliedResourceFilter = filter;
            _lastFilteredResourceItems = items;
            ResourceList.ItemsSource = items;
        }

        ResourceItem? selectedItem = FindFilteredResourceItem(items, _selectedResource);
        if (selectedItem is not null)
            ResourceList.SelectedItem = selectedItem;
        else
            ResourceList.SelectedIndex = -1;

        StatusBox.Text = string.IsNullOrEmpty(filter)
            ? $"Showing {_selectedCategory.Count} {_selectedCategory.Label} item(s)."
            : $"Showing {items.Count} of {_selectedCategory.Count} {_selectedCategory.Label} item(s) matching \"{filter}\".";
        UpdateResourceListFooter(items, _selectedCategory.Count);
    }

    private void UpdateResourceListFooter(IReadOnlyList<ResourceItem>? items, int totalCount)
    {
        if (_selectedCategory is null || items is null)
        {
            ResourceListFooterText.Text = "No items";
            return;
        }

        int selectedPosition = -1;
        if (_selectedResource is not null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Index == _selectedResource.Index)
                {
                    selectedPosition = i + 1;
                    break;
                }
            }
        }

        string category = _selectedCategory.Label;
        if (items.Count == 0)
        {
            ResourceListFooterText.Text = totalCount == 0
                ? $"No {category} items"
                : $"No matches ({totalCount:N0} total)";
            return;
        }

        if (selectedPosition > 0)
        {
            ResourceListFooterText.Text = items.Count == totalCount
                ? $"{selectedPosition:N0} of {totalCount:N0} selected"
                : $"{selectedPosition:N0} of {items.Count:N0} filtered results selected ({totalCount:N0} total)";
            return;
        }

        ResourceListFooterText.Text = items.Count == totalCount
            ? $"Showing {items.Count:N0}"
            : $"Showing {items.Count:N0} of {totalCount:N0}";
    }

    private static ResourceItem? FindFilteredResourceItem(IReadOnlyList<ResourceItem> items, ResourceItem? selectedResource)
    {
        if (selectedResource is null)
            return null;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Index == selectedResource.Index)
                return items[i];
        }

        return null;
    }

    private void SetResourceFilterText(string text)
    {
        _isUpdatingResourceFilter = true;
        ResourceFilterBox.Text = text;
        _isUpdatingResourceFilter = false;
    }

    private static bool ResourceMatchesFilter(ResourceItem item, string filter)
    {
        return item.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
               item.Subtitle.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportedDataFile(StorageFile file)
    {
        return IsSupportedDataFilePath(file.FileType);
    }

    internal static bool IsSupportedDataFilePath(string path)
    {
        string extension = Path.GetExtension(path);
        if (string.IsNullOrEmpty(extension))
            extension = path;

        return string.Equals(extension, ".win", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".ios", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".unx", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".droid", StringComparison.OrdinalIgnoreCase);
    }

    private bool NavigateToResource(string categoryLabel, int itemIndex, bool addTab = false)
    {
        ResourceCategory? category = _categories.FirstOrDefault(candidate => candidate.Label == categoryLabel);
        if (category is null || itemIndex < 0 || itemIndex >= category.Items.Count)
        {
            StatusBox.Text = $"Could not open {categoryLabel} #{itemIndex}.";
            return false;
        }

        ResourceItem item = category.Items[itemIndex];
        _selectedCategory = category;
        _selectedResource = item;
        CategoryList.SelectedItem = category;
        UpdateGlobalToolsVisibility();
        SetResourceFilterText(string.Empty);
        ApplyResourceFilter();
        ResourceList.SelectedItem = item;
        OpenResourceItem(item, addTab);
        return true;
    }

    private void RevealResourceListItem(ResourceItem item)
    {
        if (!ReferenceEquals(ResourceList.SelectedItem, item))
            ResourceList.SelectedItem = item;

        DispatcherQueue.TryEnqueue(() =>
        {
            if (ResourceList.ItemsSource is null)
                return;

            if (!ReferenceEquals(ResourceList.SelectedItem, item))
                ResourceList.SelectedItem = item;

            ResourceList.ScrollIntoView(item);
        });
    }

    private static IReadOnlyList<ResourceCategory> BuildCategories(UndertaleData data)
    {
        return
        [
            BuildCategory("Sprites", data.Sprites),
            BuildCategory("Sounds", data.Sounds),
            BuildCategory("Rooms", data.Rooms),
            BuildCategory("Objects", data.GameObjects),
            BuildCategory("Code", data.Code),
            BuildSingleItemCategory("General info", data.GeneralInfo),
            BuildCategory("Global init", data.GlobalInitScripts),
            BuildCategory("Game End scripts", data.GameEndScripts),
            BuildCategory("Audio groups", data.AudioGroups),
            BuildCategory("Strings", data.Strings),
            BuildCategory("Texture page items", data.TexturePageItems),
            BuildCategory("Embedded textures", data.EmbeddedTextures),
            BuildCategory("Embedded audio", data.EmbeddedAudio),
            BuildCategory("Texture group information", data.TextureGroupInfo),
            BuildCategory("Embedded images", data.EmbeddedImages),
            BuildCategory("Fonts", data.Fonts),
            BuildCategory("Scripts", data.Scripts),
            BuildCategory("Extensions", data.Extensions),
            BuildCategory("Backgrounds", data.Backgrounds),
            BuildCategory("Paths", data.Paths),
            BuildCategory("Shaders", data.Shaders),
            BuildCategory("Timelines", data.Timelines),
            BuildCategory("Variables", data.Variables),
            BuildCategory("Functions", data.Functions),
            BuildCategory("Code locals", data.CodeLocals),
            BuildCategory("Sequences", data.Sequences),
            BuildCategory("Animation curves", data.AnimationCurves),
            BuildCategory("Particle systems", data.ParticleSystems),
            BuildCategory("Particle system emitters", data.ParticleSystemEmitters)
        ];
    }

    private static IReadOnlyList<ResourceCategory> BuildNoDataCategoriesCore()
    {
        string[] labels =
        [
            "Sprites",
            "Sounds",
            "Rooms",
            "Objects",
            "Code",
            "General info",
            "Global init",
            "Game End scripts",
            "Audio groups",
            "Strings",
            "Texture page items",
            "Embedded textures",
            "Embedded audio",
            "Texture group information",
            "Embedded images",
            "Fonts",
            "Scripts",
            "Extensions",
            "Backgrounds",
            "Paths",
            "Shaders",
            "Timelines",
            "Variables",
            "Functions",
            "Code locals",
            "Sequences",
            "Animation curves",
            "Particle systems",
            "Particle system emitters"
        ];

        return labels.Select(label => new ResourceCategory(label, GetCategorySymbol(label), -1, [], null))
                     .ToArray();
    }

    private static string BuildCodeDisassembly(UndertaleData data, UndertaleCode code)
    {
        try
        {
            if (code.ParentEntry is not null)
                return $"; This code entry is a reference to an anonymous function within \"{code.ParentEntry.Name.Content}\".";

            return code.Disassemble(data.Variables, data.CodeLocals?.For(code), data.CodeLocals is null);
        }
        catch (Exception ex)
        {
            return $"; EXCEPTION while disassembling{Environment.NewLine}; {ex}";
        }
    }

    private static string BuildCodeDecompiledText(UndertaleData data, UndertaleCode code)
    {
        try
        {
            if (code.ParentEntry is not null)
                return $"// This code entry is a reference to an anonymous function within \"{code.ParentEntry.Name.Content}\".";

            GlobalDecompileContext context = new(data);
            return new DecompileContext(context, code, data.ToolInfo.DecompilerSettings).DecompileToString();
        }
        catch (Exception ex)
        {
            return $"/* EXCEPTION while decompiling{Environment.NewLine}{ex}{Environment.NewLine}*/";
        }
    }

    private static string? ApplyDisassembly(UndertaleData data, UndertaleCode code, string disassembly)
    {
        List<UndertaleInstruction> instructions = Assembler.Assemble(disassembly, data);
        code.Replace(instructions);
        return null;
    }

    private static string? ApplyDecompiledGml(UndertaleData data, UndertaleCode code, string sourceCode)
    {
        CompileGroup group = new(data);
        group.QueueCodeReplace(code, sourceCode);
        CompileResult result = group.Compile();
        return result.Successful ? null : result.PrintAllErrors(codeEntryNames: false);
    }

    private static CodeSearchResult[] SearchAllCode(UndertaleData data, string query, bool decompiled)
    {
        List<CodeSearchResult> results = new();
        for (int codeIndex = 0; codeIndex < data.Code.Count; codeIndex++)
        {
            UndertaleCode code = data.Code[codeIndex];
            string text = decompiled ? BuildCodeDecompiledText(data, code) : BuildCodeDisassembly(data, code);
            int lineNumber = 0;

            foreach (string line in NewLineRegex.Split(text))
            {
                lineNumber++;
                if (!line.Contains(query, StringComparison.OrdinalIgnoreCase))
                    continue;

                string codeName = FormatTitle(code.Name?.Content);
                string preview = FormatTitle(line.Trim());
                results.Add(new CodeSearchResult(codeIndex, codeName, lineNumber, preview, decompiled));
                if (results.Count >= 500)
                    return results.ToArray();
            }
        }

        return results.ToArray();
    }

    private static int ExportAllCode(UndertaleData data, string directoryPath, bool decompiled)
    {
        Directory.CreateDirectory(directoryPath);
        string extension = decompiled ? ".gml" : ".asm";
        int exportedCount = 0;

        for (int codeIndex = 0; codeIndex < data.Code.Count; codeIndex++)
        {
            UndertaleCode code = data.Code[codeIndex];
            if (code.ParentEntry is not null)
                continue;

            string codeName = string.IsNullOrWhiteSpace(code.Name?.Content) ? $"code_{codeIndex}" : code.Name.Content;
            string fileName = $"{codeIndex:D5}_{SafeFileName(codeName, $"code_{codeIndex}")}{extension}";
            string text = decompiled ? BuildCodeDecompiledText(data, code) : BuildCodeDisassembly(data, code);
            File.WriteAllText(Path.Combine(directoryPath, fileName), text);
            exportedCount++;
        }

        return exportedCount;
    }

    private static StringSearchResult[] SearchAllStrings(UndertaleData data, string query)
    {
        List<StringSearchResult> results = new();
        for (int stringIndex = 0; stringIndex < data.Strings.Count; stringIndex++)
        {
            UndertaleString str = data.Strings[stringIndex];
            string content = str.Content ?? string.Empty;
            if (!content.Contains(query, StringComparison.OrdinalIgnoreCase))
                continue;

            results.Add(new StringSearchResult(stringIndex, FormatTitle(content)));
            if (results.Count >= 500)
                return results.ToArray();
        }

        return results.ToArray();
    }

    private static string ExportStringsJson(UndertaleData data)
    {
        StringExportEntry[] entries = data.Strings
                                          .Select((str, index) => new StringExportEntry(index, str.Content ?? string.Empty))
                                          .ToArray();
        return JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
    }

    private static int ImportStringsJson(UndertaleData data, string json)
    {
        StringExportEntry[]? entries = JsonSerializer.Deserialize<StringExportEntry[]>(json);
        if (entries is null)
            throw new InvalidDataException("String import file did not contain a JSON array.");

        int updatedCount = 0;
        foreach (StringExportEntry entry in entries)
        {
            if (entry.Index < 0 || entry.Index >= data.Strings.Count)
                throw new InvalidDataException($"String index {entry.Index} is outside the loaded data file string range.");

            UndertaleString str = data.Strings[entry.Index];
            string content = entry.Content ?? string.Empty;
            if (str.Content == content)
                continue;

            str.Content = content;
            updatedCount++;
        }

        return updatedCount;
    }

    private static byte[] BuildTexturePreviewPng(object textureValue)
    {
        return textureValue switch
        {
            UndertaleTexturePageItem item => BuildTexturePageItemPreviewPng(item),
            UndertaleEmbeddedTexture texture => BuildEmbeddedTexturePreviewPng(texture),
            _ => throw new InvalidOperationException($"Unsupported texture preview type {textureValue.GetType().Name}")
        };
    }

    private static bool ShouldDeferTexturePreview(object textureValue, out long pixelCount)
    {
        pixelCount = GetTexturePreviewPixelCount(textureValue);
        return pixelCount > TexturePreviewAutoRenderPixelLimit;
    }

    private static long GetTexturePreviewPixelCount(object textureValue)
    {
        return textureValue switch
        {
            UndertaleTexturePageItem item => Math.Max(1L, Convert.ToInt64(item.SourceWidth)) *
                                             Math.Max(1L, Convert.ToInt64(item.SourceHeight)),
            UndertaleEmbeddedTexture texture when texture.TextureData?.Image is GMImage image =>
                Math.Max(1L, Convert.ToInt64(image.Width)) * Math.Max(1L, Convert.ToInt64(image.Height)),
            _ => 0
        };
    }

    private static string FormatPixelCount(long pixelCount)
    {
        if (pixelCount >= 1_000_000)
            return $"{(pixelCount / 1_000_000d).ToString("0.#", CultureInfo.InvariantCulture)} MP";

        return $"{pixelCount.ToString("N0", CultureInfo.InvariantCulture)} pixels";
    }

    private static bool TryGetPreviewableTextureValue(object value, out object? textureValue)
    {
        textureValue = value switch
        {
            UndertaleTexturePageItem item => item,
            UndertaleEmbeddedTexture texture => texture,
            UndertaleBackground background => background.Texture,
            UndertaleFont font => font.Texture,
            _ => null
        };

        return textureValue is UndertaleTexturePageItem or UndertaleEmbeddedTexture;
    }

    private static byte[] BuildTexturePageItemPreviewPng(UndertaleTexturePageItem item)
    {
        using TextureWorker worker = new();
        return BuildTexturePageItemPreviewPng(item, worker);
    }

    private static byte[] BuildTexturePageItemPreviewPng(UndertaleTexturePageItem item, TextureWorker worker)
    {
        using IMagickImage<byte> image = worker.GetTextureFor(item, item.Name?.Content ?? "texture", includePadding: true);
        using MemoryStream pngStream = new();
        image.Write(pngStream, MagickFormat.Png32);
        return pngStream.ToArray();
    }

    private byte[] GetCachedTexturePreviewPng(object textureValue)
    {
        return textureValue switch
        {
            UndertaleTexturePageItem item => GetCachedTexturePageItemPreviewPng(item),
            UndertaleEmbeddedTexture texture => GetCachedEmbeddedTexturePreviewPng(texture),
            _ => BuildTexturePreviewPng(textureValue)
        };
    }

    private byte[] GetCachedTexturePageItemPreviewPng(UndertaleTexturePageItem item)
    {
        lock (_previewCacheGate)
        {
            if (_texturePageItemPreviewCache.TryGetValue(item, out byte[]? cached))
                return cached;

            byte[] bytes = BuildTexturePageItemPreviewPng(item, GetPreviewTextureWorkerLocked(item.TexturePage));
            AddPreviewBytesToCacheLocked(_texturePageItemPreviewCache, item, bytes);
            return bytes;
        }
    }

    private byte[] GetCachedEmbeddedTexturePreviewPng(UndertaleEmbeddedTexture texture)
    {
        lock (_previewCacheGate)
        {
            if (_embeddedTexturePreviewCache.TryGetValue(texture, out byte[]? cached))
                return cached;

            byte[] bytes = BuildEmbeddedTexturePreviewPng(texture);
            AddPreviewBytesToCacheLocked(_embeddedTexturePreviewCache, texture, bytes);
            return bytes;
        }
    }

    private TextureWorker GetPreviewTextureWorkerLocked(UndertaleEmbeddedTexture? texturePage)
    {
        if (texturePage is not null &&
            !_previewTextureWorkerPages.Contains(texturePage) &&
            _previewTextureWorkerPages.Count >= PreviewTextureWorkerPageLimit)
        {
            DisposePreviewTextureWorkerLocked();
        }

        TextureWorker worker = _previewTextureWorker ??= new TextureWorker();
        if (texturePage is not null)
            _previewTextureWorkerPages.Add(texturePage);

        return worker;
    }

    private byte[] GetCachedRoomTilePreviewPng(RoomPreviewTileKey key)
    {
        lock (_previewCacheGate)
        {
            if (_roomTilePreviewCache.TryGetValue(key, out byte[]? cached))
                return cached;

            byte[] bytes = BuildRoomTilePreviewPng(key, GetPreviewTextureWorkerLocked(key.Texture.TexturePage));
            AddPreviewBytesToCacheLocked(_roomTilePreviewCache, key, bytes);
            return bytes;
        }
    }

    private void ClearPreviewCaches()
    {
        lock (_previewCacheGate)
        {
            DisposePreviewTextureWorkerLocked();
            ClearPreviewByteCachesLocked();
        }
    }

    private void DisposePreviewTextureWorkerLocked()
    {
        _previewTextureWorker?.Dispose();
        _previewTextureWorker = null;
        _previewTextureWorkerPages.Clear();
    }

    private void AddPreviewBytesToCacheLocked<TKey>(Dictionary<TKey, byte[]> cache, TKey key, byte[] bytes)
        where TKey : notnull
    {
        if (bytes.LongLength > PreviewPngCacheByteLimit)
            return;

        if (cache.TryGetValue(key, out byte[]? existing))
            _previewPngCacheBytes -= existing.LongLength;

        if (_previewPngCacheBytes + bytes.LongLength > PreviewPngCacheByteLimit ||
            GetPreviewCacheEntryCountLocked() >= PreviewPngCacheEntryLimit)
        {
            ClearPreviewByteCachesLocked();
        }

        cache[key] = bytes;
        _previewPngCacheBytes += bytes.LongLength;
    }

    private int GetPreviewCacheEntryCountLocked() =>
        _texturePageItemPreviewCache.Count +
        _embeddedTexturePreviewCache.Count +
        _roomTilePreviewCache.Count;

    private void ClearPreviewByteCachesLocked()
    {
        _texturePageItemPreviewCache.Clear();
        _embeddedTexturePreviewCache.Clear();
        _roomTilePreviewCache.Clear();
        _previewPngCacheBytes = 0;
    }

    private static byte[] BuildRoomTilePreviewPng(RoomPreviewTileKey key)
    {
        using TextureWorker worker = new();
        return BuildRoomTilePreviewPng(key, worker);
    }

    private static byte[] BuildRoomTilePreviewPng(RoomPreviewTileKey key, TextureWorker worker)
    {
        using IMagickImage<byte> source = worker.GetTextureFor(key.Texture, key.Texture.Name?.Content ?? "tile", includePadding: false);
        int x = Math.Clamp(key.SourceX, 0, Math.Max(0, (int)source.Width - 1));
        int y = Math.Clamp(key.SourceY, 0, Math.Max(0, (int)source.Height - 1));
        uint width = (uint)Math.Clamp(key.Width, 1, Math.Max(1, (int)source.Width - x));
        uint height = (uint)Math.Clamp(key.Height, 1, Math.Max(1, (int)source.Height - y));
        using IMagickImage<byte> tile = source.CloneArea(x, y, width, height);
        ApplyRoomTileTransform(tile, key.Transform);
        using MemoryStream pngStream = new();
        tile.Write(pngStream, MagickFormat.Png32);
        return pngStream.ToArray();
    }

    private static void ApplyRoomTileTransform(IMagickImage<byte> tile, uint transform)
    {
        switch (transform)
        {
            case 0:
                return;
            case 1:
                tile.Flop();
                return;
            case 2:
                tile.Flip();
                return;
            case 3:
                tile.Flop();
                tile.Flip();
                return;
            case 4:
                tile.Rotate(90);
                return;
            case 5:
                tile.Flop();
                tile.Rotate(90);
                return;
            case 6:
                tile.Flip();
                tile.Rotate(90);
                return;
            case 7:
                tile.Flop();
                tile.Flip();
                tile.Rotate(90);
                return;
            default:
                throw new InvalidDataException($"{transform} is not a valid room tile transform.");
        }
    }

    private static byte[] BuildEmbeddedTexturePreviewPng(UndertaleEmbeddedTexture texture)
    {
        if (texture.TextureData?.Image is null)
            throw new InvalidOperationException("Embedded texture has no image data.");

        using MemoryStream pngStream = new();
        texture.TextureData.Image.SavePng(pngStream);
        return pngStream.ToArray();
    }

    private static BitmapImage LoadBitmapImage(byte[] pngBytes)
    {
        InMemoryRandomAccessStream randomAccessStream = new();
        using (DataWriter writer = new(randomAccessStream))
        {
            writer.WriteBytes(pngBytes);
            writer.StoreAsync().AsTask().GetAwaiter().GetResult();
            writer.FlushAsync().AsTask().GetAwaiter().GetResult();
            writer.DetachStream();
        }

        randomAccessStream.Seek(0);
        BitmapImage bitmap = new();
        bitmap.SetSource(randomAccessStream);
        return bitmap;
    }

    private static void ExportTexture(object textureValue, string path)
    {
        switch (textureValue)
        {
            case UndertaleTexturePageItem item:
                using (TextureWorker worker = new())
                {
                    worker.ExportAsPNG(item, path);
                }
                break;
            case UndertaleEmbeddedTexture texture:
                if (texture.TextureData?.Image is null)
                    throw new InvalidOperationException("Embedded texture has no image data.");
                using (FileStream stream = new(path, FileMode.Create, FileAccess.Write))
                {
                    texture.TextureData.Image.SavePng(stream);
                }
                break;
            default:
                throw new InvalidOperationException($"Unsupported texture export type {textureValue.GetType().Name}");
        }
    }

    private static IReadOnlyList<SpriteFrameItem> BuildSpriteFrameItems(UndertaleSprite sprite)
    {
        return sprite.Textures
                     .Select((entry, index) =>
                     {
                         string textureName = FormatTitle(entry?.Texture?.Name?.Content);
                         return new SpriteFrameItem(index, $"Frame {index}", textureName);
                     })
                     .ToArray();
    }

    private static IReadOnlyList<SpriteTextureSummary> BuildSpriteTextureSummaries(UndertaleSprite sprite)
    {
        if (sprite.Textures.Count == 0)
            return [new SpriteTextureSummary(-1, "(none)", "No texture frames")];

        return sprite.Textures
                     .Select((entry, index) =>
                     {
                         UndertaleTexturePageItem? texture = entry?.Texture;
                         if (texture is null)
                             return new SpriteTextureSummary(index, $"Frame {index}", "(null texture)");

                         string name = FormatTitle(texture.Name?.Content);
                         string details = $"{texture.SourceWidth}x{texture.SourceHeight} source, " +
                                          $"{texture.BoundingWidth}x{texture.BoundingHeight} bounds, " +
                                          $"page {FormatTitle(texture.TexturePage?.Name?.Content)}";
                         return new SpriteTextureSummary(index, $"Frame {index} - {name}", details);
                     })
                     .ToArray();
    }

    private static IReadOnlyList<SpriteMaskSummary> BuildSpriteMaskSummaries(UndertaleSprite sprite)
    {
        if (sprite.CollisionMasks.Count == 0)
            return [new SpriteMaskSummary(-1, "(none)", "No collision masks")];

        return sprite.CollisionMasks
                     .Select((mask, index) =>
                     {
                         if (mask is null)
                             return new SpriteMaskSummary(index, $"Mask {index}", "(null mask)");

                         string bytes = mask.Data is null ? "no data" : $"{mask.Data.Length} bytes";
                         return new SpriteMaskSummary(index, $"Mask {index}", $"{mask.Width}x{mask.Height}, {bytes}");
                     })
                     .ToArray();
    }

    private static bool TryGetSpriteFrameTexture(
        UndertaleSprite sprite,
        int frameIndex,
        out UndertaleTexturePageItem? texture)
    {
        texture = null;
        if (frameIndex < 0 || frameIndex >= sprite.Textures.Count)
            return false;

        texture = sprite.Textures[frameIndex]?.Texture;
        return texture is not null;
    }

    private static string BuildSpritePreviewInfo(UndertaleSprite sprite)
    {
        string spriteName = FormatTitle(sprite.Name?.Content);
        if (sprite.Textures.Count == 0)
            return $"{spriteName} has no texture frames.";

        return $"{spriteName} - {sprite.Width}x{sprite.Height}, {sprite.Textures.Count} frame(s), origin {sprite.OriginXWrapper},{sprite.OriginYWrapper}.";
    }

    private static string BuildSpritePreviewInfo(
        UndertaleSprite sprite,
        int frameIndex,
        UndertaleTexturePageItem texture)
    {
        string spriteName = FormatTitle(sprite.Name?.Content);
        string textureName = FormatTitle(texture.Name?.Content);
        return $"{spriteName} - frame {frameIndex + 1}/{sprite.Textures.Count}, {sprite.Width}x{sprite.Height}, origin {sprite.OriginXWrapper},{sprite.OriginYWrapper}; texture {textureName}.";
    }

    private static void ExportSpriteFrame(UndertaleTexturePageItem texture, string path)
    {
        using TextureWorker worker = new();
        worker.ExportAsPNG(texture, path, null, includePadding: true);
    }

    private static int ExportAllSpriteFrames(UndertaleSprite sprite, string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);
        string spriteName = SafeFileName(sprite.Name?.Content, "sprite");
        int exportedCount = 0;

        using TextureWorker worker = new();
        for (int frameIndex = 0; frameIndex < sprite.Textures.Count; frameIndex++)
        {
            UndertaleTexturePageItem? texture = sprite.Textures[frameIndex]?.Texture;
            if (texture is null)
                continue;

            string path = Path.Combine(directoryPath, $"{spriteName}_{frameIndex}.png");
            worker.ExportAsPNG(texture, path, null, includePadding: true);
            exportedCount++;
        }

        return exportedCount;
    }

    private static IReadOnlyList<ObjectEventSummary> BuildObjectEventSummaries(UndertaleGameObject gameObject)
    {
        List<ObjectEventSummary> summaries = new();
        for (int eventTypeIndex = 0; eventTypeIndex < gameObject.Events.Count; eventTypeIndex++)
        {
            UndertalePointerList<UndertaleGameObject.Event>? events = gameObject.Events[eventTypeIndex];
            if (events is null)
                continue;

            EventType eventType = (EventType)eventTypeIndex;
            int eventIndex = 0;
            foreach (UndertaleGameObject.Event ev in events)
            {
                int actionCount = ev.Actions?.Count ?? 0;
                UndertaleCode? firstCode = ev.Actions?.Select(action => action.CodeId).FirstOrDefault(code => code is not null);
                string title = $"{FormatEventType(eventType)} / {FormatEventSubtype(eventType, ev.EventSubtype)}";
                string codeName = FormatTitle(firstCode?.Name?.Content);
                string subtitle = firstCode is null
                    ? $"{actionCount} action(s)"
                    : $"{actionCount} action(s), first code: {codeName}";
                summaries.Add(new ObjectEventSummary(title, subtitle, eventType, eventTypeIndex, eventIndex, ev, firstCode));
                eventIndex++;
            }
        }

        return summaries;
    }

    private static IReadOnlyList<ObjectEventActionSummary> BuildObjectEventActionSummaries(UndertaleGameObject.Event ev)
    {
        return ev.Actions?
                 .Select((action, index) =>
                 {
                     string codeName = FormatTitle(action.CodeId?.Name?.Content);
                     string title = $"#{index} - {codeName}";
                     string subtitle = action.CodeId is null ? "No code entry selected" : "Runs code entry";
                     return new ObjectEventActionSummary(index, title, subtitle, action);
                 })
                 .ToArray() ?? [];
    }

    private static UndertalePointerList<UndertaleGameObject.Event> EnsureObjectEventBucket(
        UndertaleGameObject gameObject,
        EventType eventType)
    {
        int index = (int)eventType;
        while (gameObject.Events.Count <= index)
            gameObject.Events.Add(new UndertalePointerList<UndertaleGameObject.Event>());

        if (gameObject.Events[index] is null)
            gameObject.Events[index] = new UndertalePointerList<UndertaleGameObject.Event>();

        return gameObject.Events[index];
    }

    private static string BuildObjectPhysicsSummary(UndertaleGameObject gameObject)
    {
        int vertexCount = gameObject.PhysicsVertices?.Count ?? 0;
        return $"{vertexCount} vertex/vertices; collision shape {gameObject.CollisionShape}; uses physics {gameObject.UsesPhysics}.";
    }

    private static IReadOnlyList<ObjectPhysicsVertexSummary> BuildObjectPhysicsVertexSummaries(UndertaleGameObject gameObject)
    {
        return gameObject.PhysicsVertices?
                         .Select((vertex, index) =>
                         {
                             string x = vertex.X.ToString(CultureInfo.InvariantCulture);
                             string y = vertex.Y.ToString(CultureInfo.InvariantCulture);
                             return new ObjectPhysicsVertexSummary(index, $"#{index} - {x}, {y}", $"X {x}; Y {y}", vertex);
                         })
                         .ToArray() ?? [];
    }

    private static string FormatEventType(EventType eventType)
    {
        return Enum.IsDefined(typeof(EventType), eventType) ? eventType.ToString() : $"Event {(uint)eventType}";
    }

    private static string FormatEventSubtype(EventType eventType, uint subtype)
    {
        try
        {
            return eventType switch
            {
                EventType.Step => Enum.IsDefined(typeof(EventSubtypeStep), subtype)
                    ? ((EventSubtypeStep)subtype).ToString()
                    : subtype.ToString(CultureInfo.InvariantCulture),
                EventType.Mouse => Enum.IsDefined(typeof(EventSubtypeMouse), subtype)
                    ? ((EventSubtypeMouse)subtype).ToString()
                    : subtype.ToString(CultureInfo.InvariantCulture),
                EventType.Other => Enum.IsDefined(typeof(EventSubtypeOther), subtype)
                    ? ((EventSubtypeOther)subtype).ToString()
                    : subtype.ToString(CultureInfo.InvariantCulture),
                EventType.Draw => Enum.IsDefined(typeof(EventSubtypeDraw), subtype)
                    ? ((EventSubtypeDraw)subtype).ToString()
                    : subtype.ToString(CultureInfo.InvariantCulture),
                EventType.Gesture => Enum.IsDefined(typeof(EventSubtypeGesture), subtype)
                    ? ((EventSubtypeGesture)subtype).ToString()
                    : subtype.ToString(CultureInfo.InvariantCulture),
                EventType.Keyboard or EventType.KeyPress or EventType.KeyRelease => Enum.IsDefined(typeof(EventSubtypeKey), subtype)
                    ? ((EventSubtypeKey)subtype).ToString()
                    : subtype.ToString(CultureInfo.InvariantCulture),
                _ => subtype.ToString(CultureInfo.InvariantCulture)
            };
        }
        catch
        {
            return subtype.ToString(CultureInfo.InvariantCulture);
        }
    }

    private static string BuildRoomOverview(UndertaleRoom room)
    {
        string creationCode = FormatTitle(room.CreationCodeId?.Name?.Content);
        int enabledViews = room.Views?.Count(view => view.Enabled) ?? 0;
        int layerCount = room.Layers?.Count ?? 0;
        int objectCount = room.GameObjects?.Count ?? 0;
        int tileCount = room.Tiles?.Count ?? 0;
        int backgroundCount = room.Backgrounds?.Count(background => background.Enabled) ?? 0;
        return $"{FormatTitle(room.Name?.Content)} - {room.Width}x{room.Height}, speed {room.Speed}, flags {room.Flags}. " +
               $"{objectCount} object instance(s), {tileCount} tile(s), {layerCount} layer(s), {enabledViews} enabled view(s), {backgroundCount} enabled background(s). " +
               $"Creation code: {creationCode}.";
    }

    private static IReadOnlyList<RoomInstanceSummary> BuildRoomInstanceSummaries(UndertaleRoom room)
    {
        return room.GameObjects?
                   .Select(instance =>
                   {
                       string objectName = FormatTitle(instance.ObjectDefinition?.Name?.Content);
                       string title = $"#{instance.InstanceID} - {objectName}";
                       string creationCode = FormatTitle(instance.CreationCode?.Name?.Content);
                       string subtitle = $"Position {instance.X},{instance.Y}; scale {FormatFloat(instance.ScaleX)}x{FormatFloat(instance.ScaleY)}; image {instance.ImageIndex}; creation code {creationCode}";
                       return new RoomInstanceSummary(title, subtitle, objectName, instance, instance.ObjectDefinition, instance.CreationCode);
                   })
                   .ToArray() ?? [];
    }

    private static IReadOnlyList<RoomBackgroundSummary> BuildRoomBackgroundSummaries(UndertaleRoom room)
    {
        return room.Backgrounds?
                   .Select((background, index) =>
                   {
                       string backgroundName = FormatTitle(background.BackgroundDefinition?.Name?.Content);
                       string state = background.Enabled ? "enabled" : "disabled";
                       string layerKind = background.Foreground ? "foreground" : "background";
                       string title = $"Slot #{index} - {backgroundName}";
                       string subtitle = $"{state}, {layerKind}; position {background.X},{background.Y}; speed {background.SpeedX},{background.SpeedY}; tiled {background.TiledHorizontally}/{background.TiledVertically}; stretch {background.Stretch}";
                       return new RoomBackgroundSummary(index, title, subtitle, background);
                   })
                   .ToArray() ?? [];
    }

    private static IReadOnlyList<RoomTileSummary> BuildRoomTileSummaries(UndertaleRoom room)
    {
        List<RoomTileSummary> summaries = new();
        int index = 0;

        foreach (UndertaleRoom.Tile tile in room.Tiles ?? [])
        {
            summaries.Add(BuildRoomTileSummary(index++, "Room tile", tile, null));
        }

        foreach (UndertaleRoom.Layer layer in room.Layers ?? [])
        {
            if (layer.LayerType != UndertaleRoom.LayerType.Assets || layer.AssetsData?.LegacyTiles is null)
                continue;

            string layerName = FormatTitle(layer.LayerName?.Content);
            foreach (UndertaleRoom.Tile tile in layer.AssetsData.LegacyTiles)
            {
                summaries.Add(BuildRoomTileSummary(index++, $"Layer {layerName}", tile, layer));
            }
        }

        return summaries;
    }

    private static RoomTileSummary BuildRoomTileSummary(
        int index,
        string scope,
        UndertaleRoom.Tile tile,
        UndertaleRoom.Layer? layer)
    {
        string definitionName = FormatRoomTileDefinition(tile);
        string title = $"Tile #{tile.InstanceID} - {definitionName}";
        string subtitle = $"{scope}; position {tile.X},{tile.Y}; source {tile.SourceX},{tile.SourceY} {tile.Width}x{tile.Height}; " +
                          $"scale {FormatFloat(tile.ScaleX)}x{FormatFloat(tile.ScaleY)}; depth {tile.TileDepth}; color {FormatRoomTileColor(tile.Color)}";
        return new RoomTileSummary(index, title, subtitle, tile, layer);
    }

    private static string FormatRoomTileDefinition(UndertaleRoom.Tile tile)
    {
        return FormatTitle(tile.spriteMode
            ? tile.SpriteDefinition?.Name?.Content
            : tile.BackgroundDefinition?.Name?.Content);
    }

    private static IReadOnlyList<RoomViewSummary> BuildRoomViewSummaries(UndertaleRoom room)
    {
        return room.Views?
                   .Select((view, index) =>
                   {
                       string state = view.Enabled ? "enabled" : "disabled";
                       string objectName = FormatTitle(view.ObjectId?.Name?.Content);
                       string title = $"View #{index} - {state}";
                       string subtitle = $"view {view.ViewX},{view.ViewY} {view.ViewWidth}x{view.ViewHeight}; port {view.PortX},{view.PortY} {view.PortWidth}x{view.PortHeight}; border {view.BorderX},{view.BorderY}; speed {view.SpeedX},{view.SpeedY}; follows {objectName}";
                       return new RoomViewSummary(index, title, subtitle, view);
                   })
                   .ToArray() ?? [];
    }

    private static double GetRoomInstancePreviewWidth(UndertaleRoom.GameObject instance)
    {
        UndertaleSprite? sprite = instance.ObjectDefinition?.Sprite;
        double width = sprite?.Width ?? 24;
        return Math.Max(8, width * Math.Abs(instance.ScaleX));
    }

    private static double GetRoomInstancePreviewHeight(UndertaleRoom.GameObject instance)
    {
        UndertaleSprite? sprite = instance.ObjectDefinition?.Sprite;
        double height = sprite?.Height ?? 24;
        return Math.Max(8, height * Math.Abs(instance.ScaleY));
    }

    private static bool TryGetRoomInstancePreviewTexture(
        UndertaleRoom.GameObject instance,
        out UndertaleTexturePageItem? texture)
    {
        texture = null;
        UndertaleSprite? sprite = instance.ObjectDefinition?.Sprite;
        if (sprite is null || sprite.Textures.Count == 0)
            return false;

        int frameIndex = Math.Clamp(instance.WrappedImageIndex, 0, sprite.Textures.Count - 1);
        texture = sprite.Textures[frameIndex]?.Texture;
        return texture is not null;
    }

    private static IReadOnlyList<RoomPreviewAssetSpriteSummary> BuildRoomPreviewAssetSpriteSummaries(UndertaleRoom room)
    {
        List<RoomPreviewAssetSpriteSummary> summaries = new();
        foreach (UndertaleRoom.Layer layer in room.Layers ?? [])
        {
            if (!layer.IsVisible ||
                layer.LayerType != UndertaleRoom.LayerType.Assets ||
                layer.AssetsData?.Sprites is null)
            {
                continue;
            }

            foreach (UndertaleRoom.SpriteInstance instance in layer.AssetsData.Sprites)
            {
                if (summaries.Count >= RoomPreviewAssetSpriteLimit)
                    return summaries;

                if (!TryBuildRoomPreviewAssetSpriteSummary(layer, instance, out RoomPreviewAssetSpriteSummary? summary))
                    continue;

                summaries.Add(summary);
            }
        }

        return summaries;
    }

    private static bool TryBuildRoomPreviewAssetSpriteSummary(
        UndertaleRoom.Layer layer,
        UndertaleRoom.SpriteInstance instance,
        out RoomPreviewAssetSpriteSummary summary)
    {
        summary = default!;
        UndertaleSprite? sprite = instance.Sprite;
        if (sprite is null || sprite.Textures.Count == 0)
            return false;

        int frameIndex = Math.Clamp(instance.WrappedFrameIndex, 0, sprite.Textures.Count - 1);
        UndertaleTexturePageItem? texture = sprite.Textures[frameIndex]?.Texture;
        if (texture is null)
            return false;

        double width = Math.Max(1d, texture.TargetWidth);
        double height = Math.Max(1d, texture.TargetHeight);
        summary = new RoomPreviewAssetSpriteSummary(
            layer,
            instance,
            instance.XOffset,
            instance.YOffset,
            width,
            height,
            instance.ScaleX,
            instance.ScaleY,
            instance.OppositeRotation,
            -instance.SpriteXOffset,
            -instance.SpriteYOffset,
            GetRoomPreviewColorOpacity(instance.Color),
            texture);
        return true;
    }

    private static IReadOnlyList<RoomPreviewSequenceSummary> BuildRoomPreviewSequenceSummaries(UndertaleRoom room)
    {
        List<RoomPreviewSequenceSummary> summaries = new();
        foreach (UndertaleRoom.Layer layer in room.Layers ?? [])
        {
            if (!layer.IsVisible ||
                layer.LayerType != UndertaleRoom.LayerType.Assets ||
                layer.AssetsData?.Sequences is null)
            {
                continue;
            }

            foreach (UndertaleRoom.SequenceInstance instance in layer.AssetsData.Sequences)
            {
                if (summaries.Count >= RoomPreviewSequenceLimit)
                    return summaries;

                UndertaleSequence? sequence = instance.Sequence;
                double width = Math.Max(32d, sequence?.Width > 0 ? sequence.Width : 32d);
                double height = Math.Max(24d, sequence?.Height > 0 ? sequence.Height : 24d);
                double originX = sequence?.OriginX ?? 0;
                double originY = sequence?.OriginY ?? 0;
                summaries.Add(new RoomPreviewSequenceSummary(
                    layer,
                    instance,
                    instance.X,
                    instance.Y,
                    width,
                    height,
                    originX,
                    originY,
                    instance.ScaleX,
                    instance.ScaleY,
                    360d - instance.Rotation,
                    GetRoomPreviewColorOpacity(instance.Color)));
            }
        }

        return summaries;
    }

    private static IReadOnlyList<RoomPreviewParticleSummary> BuildRoomPreviewParticleSummaries(UndertaleRoom room)
    {
        List<RoomPreviewParticleSummary> summaries = new();
        foreach (UndertaleRoom.Layer layer in room.Layers ?? [])
        {
            if (!layer.IsVisible ||
                layer.LayerType != UndertaleRoom.LayerType.Assets ||
                layer.AssetsData?.ParticleSystems is null)
            {
                continue;
            }

            foreach (UndertaleRoom.ParticleSystemInstance instance in layer.AssetsData.ParticleSystems)
            {
                if (summaries.Count >= RoomPreviewParticleLimit)
                    return summaries;

                RoomPreviewParticleBounds bounds = BuildRoomPreviewParticleBounds(instance.ParticleSystem);
                summaries.Add(new RoomPreviewParticleSummary(
                    layer,
                    instance,
                    instance.X,
                    instance.Y,
                    instance.ScaleX,
                    instance.ScaleY,
                    instance.OppositeRotation,
                    GetRoomPreviewColorOpacity(instance.Color),
                    bounds.X,
                    bounds.Y,
                    bounds.Width,
                    bounds.Height));
            }
        }

        return summaries;
    }

    private static RoomPreviewParticleBounds BuildRoomPreviewParticleBounds(UndertaleParticleSystem? particleSystem)
    {
        UndertaleParticleSystemEmitter[] emitters = particleSystem?.Emitters?
            .Select(reference => reference?.Resource)
            .Where(emitter => emitter is not null)
            .Cast<UndertaleParticleSystemEmitter>()
            .ToArray() ?? [];
        if (emitters.Length == 0)
            return default;

        float minX = emitters.Min(emitter => emitter.RegionX);
        float maxX = emitters.Max(emitter => emitter.RegionX + emitter.RegionWidth);
        float minY = emitters.Min(emitter => emitter.RegionY);
        float maxY = emitters.Max(emitter => emitter.RegionY + emitter.RegionHeight);
        float boundsX = emitters.Min(emitter => emitter.RegionX - emitter.RegionWidth * 0.5f) + 8;
        float boundsY = emitters.Min(emitter => emitter.RegionY - emitter.RegionHeight * 0.5f) + 8;
        return new RoomPreviewParticleBounds(
            boundsX,
            boundsY,
            Math.Abs(minX - maxX),
            Math.Abs(minY - maxY));
    }

    private static double GetRoomPreviewColorOpacity(uint color)
    {
        double opacity = ((color >> 24) & 0xFF) / 255d;
        return Math.Clamp(opacity, 0.08, 1d);
    }

    private static IReadOnlyList<RoomPreviewBackgroundSummary> BuildRoomPreviewBackgroundSummaries(UndertaleRoom room, bool foreground)
    {
        List<RoomPreviewBackgroundSummary> summaries = new();

        foreach (UndertaleRoom.Background background in room.Backgrounds ?? [])
        {
            if (!background.Enabled || background.Foreground != foreground || background.BackgroundDefinition?.Texture is null)
                continue;

            UndertaleTexturePageItem texture = background.BackgroundDefinition.Texture;
            double width = background.Stretch ? Math.Max(1, room.Width) : Math.Max(1, texture.TargetWidth * Math.Abs(background.CalcScaleX));
            double height = background.Stretch ? Math.Max(1, room.Height) : Math.Max(1, texture.TargetHeight * Math.Abs(background.CalcScaleY));
            summaries.Add(new RoomPreviewBackgroundSummary(
                background.XOffset,
                background.YOffset,
                width,
                height,
                background.TiledHorizontally && !background.Stretch,
                background.TiledVertically && !background.Stretch,
                foreground,
                texture));
        }

        foreach (UndertaleRoom.Layer layer in room.Layers ?? [])
        {
            UndertaleRoom.Layer.LayerBackgroundData? data = layer.BackgroundData;
            if (layer.LayerType != UndertaleRoom.LayerType.Background ||
                !layer.IsVisible ||
                data is null ||
                !data.Visible ||
                data.Foreground != foreground ||
                !TryGetSpriteFrameTexture(data.Sprite, 0, out UndertaleTexturePageItem? texture) ||
                texture is null)
            {
                continue;
            }

            double width = data.Stretch ? Math.Max(1, room.Width) : Math.Max(1, data.Sprite.Width * Math.Abs(data.CalcScaleX));
            double height = data.Stretch ? Math.Max(1, room.Height) : Math.Max(1, data.Sprite.Height * Math.Abs(data.CalcScaleY));
            summaries.Add(new RoomPreviewBackgroundSummary(
                data.XOffset,
                data.YOffset,
                width,
                height,
                data.TiledHorizontally && !data.Stretch,
                data.TiledVertically && !data.Stretch,
                foreground,
                texture));
        }

        return summaries;
    }

    private static IReadOnlyList<RoomPreviewTileSummary> BuildRoomPreviewTileSummaries(UndertaleRoom room, int maxTiles = RoomPreviewTileLimit)
    {
        List<RoomPreviewTileSummary> summaries = new();
        if (maxTiles <= 0)
            return summaries;

        foreach (UndertaleRoom.Tile tile in room.Tiles ?? [])
        {
            if (summaries.Count >= maxTiles)
                return summaries;

            if (TryBuildLegacyTilePreviewSummary(tile, out RoomPreviewTileSummary? summary))
                summaries.Add(summary);
        }

        foreach (UndertaleRoom.Layer layer in room.Layers ?? [])
        {
            if (!layer.IsVisible)
                continue;

            if (layer.LayerType == UndertaleRoom.LayerType.Assets && layer.AssetsData?.LegacyTiles is not null)
            {
                foreach (UndertaleRoom.Tile tile in layer.AssetsData.LegacyTiles)
                {
                    if (summaries.Count >= maxTiles)
                        return summaries;

                    if (TryBuildLegacyTilePreviewSummary(tile, out RoomPreviewTileSummary? summary))
                        summaries.Add(summary);
                }
            }
            else if (layer.LayerType == UndertaleRoom.LayerType.Tiles && layer.TilesData is not null)
            {
                AddTileLayerPreviewSummaries(layer, summaries, maxTiles);
                if (summaries.Count >= maxTiles)
                    return summaries;
            }
        }

        return summaries;
    }

    private static bool TryBuildLegacyTilePreviewSummary(UndertaleRoom.Tile tile, out RoomPreviewTileSummary summary)
    {
        summary = default!;
        UndertaleTexturePageItem? texture = tile.Tpag;
        if (texture is null || tile.Width == 0 || tile.Height == 0)
            return false;

        summary = new RoomPreviewTileSummary(
            tile.X,
            tile.Y,
            Math.Max(1, tile.Width * Math.Abs(tile.ScaleX)),
            Math.Max(1, tile.Height * Math.Abs(tile.ScaleY)),
            new RoomPreviewTileKey(texture, tile.SourceX, tile.SourceY, (int)tile.Width, (int)tile.Height, 0));
        return true;
    }

    private static void AddTileLayerPreviewSummaries(
        UndertaleRoom.Layer layer,
        List<RoomPreviewTileSummary> summaries,
        int maxTiles)
    {
        UndertaleRoom.Layer.LayerTilesData tilesData = layer.TilesData;
        UndertaleBackground? background = tilesData.Background;
        if (background?.Texture is null || tilesData.TileData is null)
            return;

        int tileWidth = (int)Math.Max(1, background.GMS2TileWidth);
        int tileHeight = (int)Math.Max(1, background.GMS2TileHeight);
        int tilesY = Math.Min((int)tilesData.TilesY, tilesData.TileData.Length);
        for (int y = 0; y < tilesY; y++)
        {
            uint[] row = tilesData.TileData[y];
            int tilesX = Math.Min((int)tilesData.TilesX, row.Length);
            for (int x = 0; x < tilesX; x++)
            {
                if (summaries.Count >= maxTiles)
                    return;

                uint tile = row[x];
                uint tileId = tile & RoomTileIndexMask;
                uint transform = (tile & RoomTileFlagsMask) >> 28;
                if (tileId == 0)
                    continue;

                if (!TryGetGms2TileSource(background, tileId, out int sourceX, out int sourceY))
                    continue;

                summaries.Add(new RoomPreviewTileSummary(
                    layer.XOffset + x * tileWidth,
                    layer.YOffset + y * tileHeight,
                    tileWidth,
                    tileHeight,
                    new RoomPreviewTileKey(background.Texture, sourceX, sourceY, tileWidth, tileHeight, transform)));
            }
        }
    }

    private static bool TryGetGms2TileSource(UndertaleBackground background, uint tileId, out int sourceX, out int sourceY)
    {
        sourceX = 0;
        sourceY = 0;
        if (background.GMS2TileColumns == 0)
            return false;

        uint column = tileId % background.GMS2TileColumns;
        uint row = tileId / background.GMS2TileColumns;
        sourceX = (int)(((column + 1) * background.GMS2OutputBorderX) + (column * (background.GMS2TileWidth + background.GMS2OutputBorderX)));
        sourceY = (int)(((row + 1) * background.GMS2OutputBorderY) + (row * (background.GMS2TileHeight + background.GMS2OutputBorderY)));
        return sourceX >= 0 && sourceY >= 0;
    }

    private static IReadOnlyList<RoomLayerSummary> BuildRoomLayerSummaries(UndertaleRoom room)
    {
        return room.Layers?
                   .Select((layer, index) =>
                   {
                        string title = $"{FormatTitle(layer.LayerName?.Content)} - {layer.LayerType}";
                        string dataSummary = layer.LayerType switch
                        {
                           UndertaleRoom.LayerType.Instances => $"{layer.InstancesData?.Instances?.Count ?? 0} instance id(s)",
                           UndertaleRoom.LayerType.Assets => BuildAssetLayerSummary(layer.AssetsData),
                           UndertaleRoom.LayerType.Tiles => $"{layer.TilesData?.TilesX ?? 0}x{layer.TilesData?.TilesY ?? 0} tiles",
                           UndertaleRoom.LayerType.Background => $"sprite {FormatTitle(layer.BackgroundData?.Sprite?.Name?.Content)}",
                           UndertaleRoom.LayerType.Effect => $"effect {FormatTitle(layer.EffectType?.Content)}",
                            _ => string.Empty
                        };
                        string subtitle = $"Depth {layer.LayerDepth}, visible {layer.IsVisible}, offset {FormatFloat(layer.XOffset)},{FormatFloat(layer.YOffset)}. {dataSummary}";
                        return new RoomLayerSummary(index, title, subtitle, layer);
                   })
                   .ToArray() ?? [];
    }

    private static IReadOnlyList<RoomInstanceLayerItem> BuildRoomInstanceLayerItems(UndertaleRoom room)
    {
        return room.Layers?
                   .Where(layer => layer.LayerType == UndertaleRoom.LayerType.Instances &&
                                   layer.InstancesData is not null)
                   .Select((layer, index) =>
                   {
                       string layerName = FormatTitle(layer.LayerName?.Content);
                       string title = $"{layerName} - depth {layer.LayerDepth} (#{index})";
                       return new RoomInstanceLayerItem(title, layer);
                   })
                   .ToArray() ?? [];
    }

    private static string BuildAssetLayerSummary(UndertaleRoom.Layer.LayerAssetsData? assetsData)
    {
        if (assetsData is null)
            return "empty assets layer";

        return $"{assetsData.Sprites?.Count ?? 0} sprite(s), {assetsData.LegacyTiles?.Count ?? 0} legacy tile(s), " +
               $"{assetsData.Sequences?.Count ?? 0} sequence(s), {assetsData.ParticleSystems?.Count ?? 0} particle system(s), " +
               $"{assetsData.TextItems?.Count ?? 0} text item(s)";
    }

    private static string BuildRoomLayerAssetDataSummary(UndertaleRoom.Layer.LayerAssetsData assetsData)
    {
        return $"{assetsData.Sprites?.Count ?? 0} sprite instance(s), " +
               $"{assetsData.Sequences?.Count ?? 0} sequence instance(s), " +
               $"{assetsData.ParticleSystems?.Count ?? 0} particle system instance(s), " +
               $"{assetsData.TextItems?.Count ?? 0} text item(s), " +
               $"{assetsData.LegacyTiles?.Count ?? 0} legacy tile(s).";
    }

    private static IReadOnlyList<RoomLayerAssetSpriteInstanceSummary> BuildRoomLayerAssetSpriteSummaries(
        UndertaleRoom.Layer.LayerAssetsData assetsData)
    {
        return assetsData.Sprites?
                         .Select((instance, index) =>
                         {
                             string name = FormatTitle(instance.Name?.Content);
                             string sprite = FormatTitle(instance.Sprite?.Name?.Content);
                             string title = $"Sprite #{index}: {name}";
                             string subtitle = $"Definition {sprite}; position {instance.X},{instance.Y}; scale {FormatFloat(instance.ScaleX)}x{FormatFloat(instance.ScaleY)}; frame {FormatFloat(instance.FrameIndex)}; rotation {FormatFloat(instance.Rotation)}.";
                             return new RoomLayerAssetSpriteInstanceSummary(index, title, subtitle, instance);
                         })
                         .ToArray() ?? [];
    }

    private static IReadOnlyList<RoomLayerAssetSequenceInstanceSummary> BuildRoomLayerAssetSequenceSummaries(
        UndertaleRoom.Layer.LayerAssetsData assetsData)
    {
        return assetsData.Sequences?
                         .Select((instance, index) =>
                         {
                             string name = FormatTitle(instance.Name?.Content);
                             string sequence = FormatTitle(instance.Sequence?.Name?.Content);
                             string title = $"Sequence #{index}: {name}";
                             string subtitle = $"Definition {sequence}; position {instance.X},{instance.Y}; scale {FormatFloat(instance.ScaleX)}x{FormatFloat(instance.ScaleY)}; frame {FormatFloat(instance.FrameIndex)}; rotation {FormatFloat(instance.Rotation)}.";
                             return new RoomLayerAssetSequenceInstanceSummary(index, title, subtitle, instance);
                         })
                         .ToArray() ?? [];
    }

    private static IReadOnlyList<RoomLayerAssetParticleInstanceSummary> BuildRoomLayerAssetParticleSummaries(
        UndertaleRoom.Layer.LayerAssetsData assetsData)
    {
        return assetsData.ParticleSystems?
                         .Select((instance, index) =>
                         {
                             string name = FormatTitle(instance.Name?.Content);
                             string particleSystem = FormatTitle(instance.ParticleSystem?.Name?.Content);
                             string title = $"Particle #{index}: {name}";
                             string subtitle = $"Definition {particleSystem}; position {instance.X},{instance.Y}; scale {FormatFloat(instance.ScaleX)}x{FormatFloat(instance.ScaleY)}; rotation {FormatFloat(instance.Rotation)}; id {instance.InstanceID}.";
                             return new RoomLayerAssetParticleInstanceSummary(index, title, subtitle, instance);
                         })
                         .ToArray() ?? [];
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static bool TryParseFloat(string value, out float result)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ||
               float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result);
    }

    private static string? ImportTexture(object textureValue, string path)
    {
        return textureValue switch
        {
            UndertaleTexturePageItem item => ImportTexturePageItem(item, path),
            UndertaleEmbeddedTexture texture => ImportEmbeddedTexture(texture, path),
            _ => throw new InvalidOperationException($"Unsupported texture import type {textureValue.GetType().Name}")
        };
    }

    private static string? ImportTexturePageItem(UndertaleTexturePageItem item, string path)
    {
        GMImage.ImageFormat? previousFormat = item.TexturePage?.TextureData?.Image?.Format;
        using MagickImage image = TextureWorker.ReadBGRAImageFromFile(path);
        item.ReplaceTexture(image);

        GMImage.ImageFormat? currentFormat = item.TexturePage?.TextureData?.Image?.Format;
        if (previousFormat == GMImage.ImageFormat.Dds && currentFormat == GMImage.ImageFormat.Png)
        {
            return $"{FormatTitle(item.TexturePage?.Name?.Content)} was converted to PNG because importing into DDS is not supported.";
        }

        return null;
    }

    private static string? ImportEmbeddedTexture(UndertaleEmbeddedTexture texture, string path)
    {
        UndertaleEmbeddedTexture.TexData textureData = texture.TextureData;
        GMImage.ImageFormat? previousFormat = textureData.Image?.Format;
        GMImage image = GMImage.FromPng(File.ReadAllBytes(path), verifyHeader: true)
                               .ConvertToFormat(previousFormat ?? GMImage.ImageFormat.Png);

        string? warning = null;
        uint width = (uint)image.Width;
        uint height = (uint)image.Height;
        if ((width & (width - 1)) != 0 || (height & (height - 1)) != 0)
        {
            warning = "WARNING: texture page dimensions are not powers of 2. Sprite blurring is very likely in-game.";
        }

        textureData.Image = image;
        GMImage.ImageFormat? currentFormat = textureData.Image.Format;
        if (previousFormat == GMImage.ImageFormat.Dds && currentFormat == GMImage.ImageFormat.Png)
        {
            string ddsWarning = $"{FormatTitle(texture.Name?.Content)} was converted to PNG because importing into DDS is not supported.";
            warning = warning is null ? ddsWarning : $"{warning}{Environment.NewLine}{ddsWarning}";
        }

        return warning;
    }

    private static string BuildTexturePreviewInfo(object textureValue)
    {
        return textureValue switch
        {
            UndertaleTexturePageItem item => BuildTexturePageItemPreviewInfo(item),
            UndertaleEmbeddedTexture texture => BuildEmbeddedTexturePreviewInfo(texture),
            _ => string.Empty
        };
    }

    private static string BuildBackgroundSummary(UndertaleBackground background)
    {
        string name = FormatTitle(background.Name?.Content);
        string textureName = FormatTitle(background.Texture?.Name?.Content);
        string textureDetails = background.Texture is null
            ? "no texture"
            : $"{textureName}, {background.Texture.BoundingWidth}x{background.Texture.BoundingHeight}";
        string exportedSprite = FormatTitle(background.GMS2ExportedSprite?.Name?.Content);
        return $"{name} uses {textureDetails}. Tileset version {background.GMS2TilesetVersion}; " +
               $"{background.GMS2TileWidth}x{background.GMS2TileHeight} tile size; " +
               $"{background.GMS2TileColumns} column(s), {background.GMS2TileCount} tile(s), " +
               $"{background.GMS2ItemsPerTileCount} frame(s) per tile; " +
               $"border {background.GMS2OutputBorderX},{background.GMS2OutputBorderY}; " +
               $"separation {background.GMS2TileSeparationX},{background.GMS2TileSeparationY}; " +
               $"frame length {background.GMS2FrameLength} us; exported sprite {exportedSprite}; " +
               $"{background.GMS2TileIds?.Count ?? 0} tile ID(s).";
    }

    private static IReadOnlyList<BackgroundTileIdSummary> BuildBackgroundTileIdSummaries(UndertaleBackground background)
    {
        return background.GMS2TileIds?
                   .Select((tileId, index) => new BackgroundTileIdSummary(
                       index,
                       $"Tile ID #{index}",
                       $"ID {tileId.ID}",
                       tileId))
                   .ToArray() ?? [];
    }

    private void RenderPathPreview(UndertalePath path, UndertalePath.PathPoint? selectedPoint)
    {
        PathPreviewCanvas.Children.Clear();

        if (path.Points.Count == 0)
            return;

        const double canvasWidth = 342;
        const double canvasHeight = 202;
        const double padding = 14;
        PathPreviewCanvas.Width = canvasWidth;
        PathPreviewCanvas.Height = canvasHeight;

        float minX = path.Points.Min(point => point.X);
        float maxX = path.Points.Max(point => point.X);
        float minY = path.Points.Min(point => point.Y);
        float maxY = path.Points.Max(point => point.Y);
        double rangeX = Math.Max(1, maxX - minX);
        double rangeY = Math.Max(1, maxY - minY);
        double scale = Math.Min((canvasWidth - padding * 2) / rangeX, (canvasHeight - padding * 2) / rangeY);

        (double X, double Y) Map(UndertalePath.PathPoint point)
        {
            double x = padding + (point.X - minX) * scale;
            double y = padding + (point.Y - minY) * scale;
            return (x, y);
        }

        for (int i = 0; i < path.Points.Count - 1; i++)
            AddPathPreviewLine(Map(path.Points[i]), Map(path.Points[i + 1]));

        if (path.IsClosed && path.Points.Count > 1)
            AddPathPreviewLine(Map(path.Points[^1]), Map(path.Points[0]));

        for (int i = 0; i < path.Points.Count; i++)
        {
            UndertalePath.PathPoint point = path.Points[i];
            (double x, double y) = Map(point);
            bool selected = ReferenceEquals(point, selectedPoint);
            double size = selected ? 8 : 6;
            Microsoft.UI.Xaml.Shapes.Ellipse marker = new()
            {
                Width = size,
                Height = size,
                Fill = selected ? CreateAccentBrush() : new SolidColorBrush(Microsoft.UI.Colors.White),
                Stroke = CreateAccentBrush(),
                StrokeThickness = selected ? 2 : 1,
                Tag = new PathPreviewPointMarker(point, minX, minY, scale, padding)
            };
            marker.PointerPressed += PathPreviewPoint_PointerPressed;
            marker.PointerMoved += PathPreviewPoint_PointerMoved;
            marker.PointerReleased += PathPreviewPoint_PointerReleased;
            marker.PointerCanceled += PathPreviewPoint_PointerCanceled;
            marker.PointerCaptureLost += PathPreviewPoint_PointerCanceled;
            Canvas.SetLeft(marker, x - size / 2);
            Canvas.SetTop(marker, y - size / 2);
            PathPreviewCanvas.Children.Add(marker);
        }

        void AddPathPreviewLine((double X, double Y) from, (double X, double Y) to)
        {
            Microsoft.UI.Xaml.Shapes.Line line = new()
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = CreateAccentBrush(),
                StrokeThickness = path.IsSmooth ? 3 : 2,
                Opacity = path.IsSmooth ? 0.75 : 0.95
            };
            PathPreviewCanvas.Children.Add(line);
        }
    }

    private static string BuildPathSummary(UndertalePath path)
    {
        string name = FormatTitle(path.Name?.Content);
        if (path.Points.Count == 0)
            return $"{name} has no points. Smooth {path.IsSmooth}, closed {path.IsClosed}, precision {path.Precision}.";

        float minX = path.Points.Min(point => point.X);
        float maxX = path.Points.Max(point => point.X);
        float minY = path.Points.Min(point => point.Y);
        float maxY = path.Points.Max(point => point.Y);
        return $"{name} - {path.Points.Count} point(s), smooth {path.IsSmooth}, closed {path.IsClosed}, precision {path.Precision}. " +
               $"Bounds X {FormatFloat(minX)}..{FormatFloat(maxX)}, Y {FormatFloat(minY)}..{FormatFloat(maxY)}.";
    }

    private static IReadOnlyList<PathPointSummary> BuildPathPointSummaries(UndertalePath path)
    {
        return path.Points
                   .Select((point, index) =>
                   {
                       string title = $"#{index} - {FormatFloat(point.X)}, {FormatFloat(point.Y)}";
                       string subtitle = $"Speed {FormatFloat(point.Speed)}";
                       return new PathPointSummary(index, title, subtitle, point);
                   })
                   .ToArray();
    }

    private static string BuildCodeLocalsSummary(UndertaleCodeLocals codeLocals)
    {
        string name = FormatTitle(codeLocals.Name?.Content);
        return $"{name} - {codeLocals.Locals.Count} local variable(s).";
    }

    private static IReadOnlyList<CodeLocalSummary> BuildCodeLocalSummaries(UndertaleCodeLocals codeLocals)
    {
        return codeLocals.Locals
                         .Select((local, index) =>
                         {
                             string title = $"#{index} - {FormatTitle(local.Name?.Content)}";
                             string subtitle = $"Index {local.Index.ToString(CultureInfo.InvariantCulture)}";
                             return new CodeLocalSummary(index, title, subtitle, local);
                         })
                         .ToArray();
    }

    private static string BuildAudioGroupSummary(UndertaleAudioGroup audioGroup)
    {
        string name = FormatTitle(audioGroup.Name?.Content);
        string path = FormatTitle(audioGroup.Path?.Content);
        return $"{name} - path {path}.";
    }

    private bool TryCreateResourceReferenceState(ResourceItem item, out ResourceReferenceState? state)
    {
        state = null;
        if (_data is null)
            return false;

        switch (item.Value)
        {
            case UndertaleScript script:
                state = CreateCodeReferenceState(
                    "Script code",
                    "Code",
                    script.Code,
                    canClear: false,
                    _data);
                return true;

            case UndertaleGlobalInit globalInit:
                string title = string.Equals(_selectedCategory?.Label, "Game End scripts", StringComparison.Ordinal)
                    ? "Game End code"
                    : "Global initialization code";
                bool canClear = string.Equals(_selectedCategory?.Label, "Global init", StringComparison.Ordinal);
                state = CreateCodeReferenceState(
                    title,
                    "Code",
                    globalInit.Code,
                    canClear,
                    _data);
                return true;

            case UndertaleEmbeddedImage embeddedImage:
                state = CreateTextureReferenceState(
                    "Embedded image texture",
                    "Texture page items",
                    embeddedImage.TextureEntry,
                    canClear: false,
                    _data);
                return true;

            default:
                return false;
        }
    }

    private static ResourceReferenceState CreateCodeReferenceState(
        string title,
        string categoryLabel,
        UndertaleCode? current,
        bool canClear,
        UndertaleData data)
    {
        ResourceReferenceOption[] options = data.Code
                                                .Select((code, index) =>
                                                    new ResourceReferenceOption(index, $"#{index} - {FormatTitle(code.Name?.Content)}", code))
                                                .ToArray();
        int currentIndex = current is null ? -1 : data.Code.IndexOf(current);
        string summary = currentIndex >= 0
            ? $"Current: Code #{currentIndex} - {FormatTitle(current?.Name?.Content)}."
            : "Current: no code reference.";
        return new ResourceReferenceState(title, summary, categoryLabel, currentIndex, current, canClear, options);
    }

    private static ResourceReferenceState CreateTextureReferenceState(
        string title,
        string categoryLabel,
        UndertaleTexturePageItem? current,
        bool canClear,
        UndertaleData data)
    {
        ResourceReferenceOption[] options = data.TexturePageItems
                                                .Select((texture, index) =>
                                                    new ResourceReferenceOption(index, $"#{index} - {FormatTitle(texture.Name?.Content)}", texture))
                                                .ToArray();
        int currentIndex = current is null ? -1 : data.TexturePageItems.IndexOf(current);
        string summary = currentIndex >= 0
            ? $"Current: Texture page items #{currentIndex} - {FormatTitle(current?.Name?.Content)}."
            : "Current: no texture reference.";
        return new ResourceReferenceState(title, summary, categoryLabel, currentIndex, current, canClear, options);
    }

    private static bool SetResourceReference(ResourceItem item, object resource)
    {
        switch (item.Value)
        {
            case UndertaleScript script when resource is UndertaleCode code:
                if (ReferenceEquals(script.Code, code))
                    return false;
                script.Code = code;
                return true;

            case UndertaleGlobalInit globalInit when resource is UndertaleCode code:
                if (ReferenceEquals(globalInit.Code, code))
                    return false;
                globalInit.Code = code;
                return true;

            case UndertaleEmbeddedImage embeddedImage when resource is UndertaleTexturePageItem texture:
                if (ReferenceEquals(embeddedImage.TextureEntry, texture))
                    return false;
                embeddedImage.TextureEntry = texture;
                return true;

            default:
                return false;
        }
    }

    private bool ClearResourceReference(ResourceItem item)
    {
        if (item.Value is not UndertaleGlobalInit globalInit ||
            !string.Equals(_selectedCategory?.Label, "Global init", StringComparison.Ordinal) ||
            globalInit.Code is null)
        {
            return false;
        }

        globalInit.Code = null!;
        return true;
    }

    private static string BuildGeneralInfoRoomOrderSummary(UndertaleGeneralInfo generalInfo, UndertaleData? data)
    {
        int roomCount = data?.Rooms?.Count ?? 0;
        return $"{generalInfo.RoomOrder?.Count ?? 0} room order entr{((generalInfo.RoomOrder?.Count ?? 0) == 1 ? "y" : "ies")}; {roomCount} room resource(s).";
    }

    private static string BuildGeneralInfoOptionsSummary(UndertaleOptions options)
    {
        int constantCount = options.Constants?.Count ?? 0;
        int textureCount = 0;
        if (options.BackImage?.Texture is not null)
            textureCount++;
        if (options.FrontImage?.Texture is not null)
            textureCount++;
        if (options.LoadImage?.Texture is not null)
            textureCount++;

        return $"{constantCount} constant(s); {textureCount}/3 loading texture reference(s).";
    }

    private static string BuildGeneralInfoLanguageSummary(UndertaleLanguage language)
    {
        int entryIdCount = language.EntryIDs?.Count ?? 0;
        int languageCount = language.Languages?.Count ?? 0;
        int entryCount = language.Languages?.Sum(current => current.Entries?.Count ?? 0) ?? 0;
        return $"{languageCount} language(s); {entryIdCount} entry id(s); {entryCount} localized value(s).";
    }

    private static IReadOnlyList<GeneralInfoLanguageSummary> BuildGeneralInfoLanguageSummaries(UndertaleLanguage language)
    {
        if (language.Languages is null)
            return [];

        return language.Languages
                       .Select((current, index) =>
                       {
                           string name = FormatTitle(current.Name?.Content);
                           string region = FormatTitle(current.Region?.Content);
                           int entryCount = current.Entries?.Count ?? 0;
                           string title = $"#{index} - {name}";
                           string subtitle = $"Region {region}; {entryCount} entr{(entryCount == 1 ? "y" : "ies")}.";
                           return new GeneralInfoLanguageSummary(index, title, subtitle, current);
                       })
                       .ToArray();
    }

    private static IReadOnlyList<GeneralInfoTextureSlotOption> BuildGeneralInfoTextureSlotOptions(
        UndertaleOptions options,
        UndertaleData? data)
    {
        return
        [
            BuildGeneralInfoTextureSlotOption(GeneralInfoOptionTextureSlot.BackImage, "Back image", options.BackImage, data),
            BuildGeneralInfoTextureSlotOption(GeneralInfoOptionTextureSlot.FrontImage, "Front image", options.FrontImage, data),
            BuildGeneralInfoTextureSlotOption(GeneralInfoOptionTextureSlot.LoadImage, "Load image", options.LoadImage, data)
        ];
    }

    private static GeneralInfoTextureSlotOption BuildGeneralInfoTextureSlotOption(
        GeneralInfoOptionTextureSlot slot,
        string label,
        UndertaleSprite.TextureEntry? entry,
        UndertaleData? data)
    {
        UndertaleTexturePageItem? texture = entry?.Texture;
        int textureIndex = texture is null || data is null ? -1 : data.TexturePageItems.IndexOf(texture);
        string subtitle = texture is null
            ? "(none)"
            : textureIndex >= 0
                ? $"Texture page items #{textureIndex} - {FormatTitle(texture.Name?.Content)}"
                : $"Unlisted texture - {FormatTitle(texture.Name?.Content)}";
        return new GeneralInfoTextureSlotOption(slot, label, subtitle, texture);
    }

    private static IReadOnlyList<TexturePageItemOption> BuildTexturePageItemOptions(UndertaleData data)
    {
        return data.TexturePageItems
                   .Select((texture, index) =>
                       new TexturePageItemOption(index, $"#{index} - {FormatTitle(texture.Name?.Content)}", texture))
                   .ToArray();
    }

    private static IReadOnlyList<GeneralInfoConstantSummary> BuildGeneralInfoConstantSummaries(UndertaleOptions options)
    {
        if (options.Constants is null)
            return [];

        return options.Constants
                      .Select((constant, index) =>
                      {
                          string name = FormatTitle(constant.Name?.Content);
                          string value = FormatTitle(constant.Value?.Content);
                          return new GeneralInfoConstantSummary(index, $"#{index} - {name}", value, constant);
                      })
                      .ToArray();
    }

    private int GetSelectedGeneralInfoConstantIndex()
    {
        return GeneralInfoConstantsList.SelectedItem is GeneralInfoConstantSummary summary ? summary.Index : 0;
    }

    private static UndertaleSprite.TextureEntry GetGeneralInfoTextureEntry(
        UndertaleOptions options,
        GeneralInfoOptionTextureSlot slot)
    {
        return slot switch
        {
            GeneralInfoOptionTextureSlot.BackImage => options.BackImage ??= new UndertaleSprite.TextureEntry(),
            GeneralInfoOptionTextureSlot.FrontImage => options.FrontImage ??= new UndertaleSprite.TextureEntry(),
            GeneralInfoOptionTextureSlot.LoadImage => options.LoadImage ??= new UndertaleSprite.TextureEntry(),
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        };
    }

    private static IReadOnlyList<RoomReferenceOption> BuildRoomReferenceOptions(UndertaleData data)
    {
        return data.Rooms
                   .Select((room, index) => new RoomReferenceOption(index, $"#{index} - {FormatTitle(room.Name?.Content)}", room))
                   .ToArray();
    }

    private static IReadOnlyList<GeneralInfoRoomOrderSummary> BuildGeneralInfoRoomOrderSummaries(
        UndertaleGeneralInfo generalInfo,
        UndertaleData data)
    {
        return generalInfo.RoomOrder
                          .Select((roomRef, orderIndex) =>
                          {
                              UndertaleRoom? room = roomRef.Resource;
                              int roomIndex = room is null ? roomRef.CachedId : data.Rooms.IndexOf(room);
                              string roomName = room is null ? "(unresolved)" : FormatTitle(room.Name?.Content);
                              string title = $"#{orderIndex} - {roomName}";
                              string subtitle = roomIndex >= 0
                                  ? $"Rooms #{roomIndex}"
                                  : $"Cached id {roomRef.CachedId.ToString(CultureInfo.InvariantCulture)}";
                              return new GeneralInfoRoomOrderSummary(orderIndex, title, subtitle, roomIndex, room);
                          })
                          .ToArray();
    }

    private static TextureGroupSectionOption[] BuildTextureGroupSectionOptions()
    {
        return
        [
            new(TextureGroupSectionKind.TexturePages, "Texture pages", "Embedded textures"),
            new(TextureGroupSectionKind.Sprites, "Sprites", "Sprites"),
            new(TextureGroupSectionKind.SpineSprites, "Spine sprites", "Sprites"),
            new(TextureGroupSectionKind.Fonts, "Fonts", "Fonts"),
            new(TextureGroupSectionKind.Tilesets, "Tilesets", "Backgrounds")
        ];
    }

    private static IReadOnlyList<TextureGroupResourceOption> BuildTextureGroupResourceOptions(
        TextureGroupSectionKind sectionKind,
        UndertaleData data)
    {
        return sectionKind switch
        {
            TextureGroupSectionKind.TexturePages => BuildTextureGroupResourceOptions(sectionKind, data.EmbeddedTextures),
            TextureGroupSectionKind.Sprites => BuildTextureGroupResourceOptions(sectionKind, data.Sprites),
            TextureGroupSectionKind.SpineSprites => BuildTextureGroupResourceOptions(sectionKind, data.Sprites),
            TextureGroupSectionKind.Fonts => BuildTextureGroupResourceOptions(sectionKind, data.Fonts),
            TextureGroupSectionKind.Tilesets => BuildTextureGroupResourceOptions(sectionKind, data.Backgrounds),
            _ => []
        };
    }

    private static IReadOnlyList<TextureGroupResourceOption> BuildTextureGroupResourceOptions<T>(
        TextureGroupSectionKind sectionKind,
        IList<T>? resources)
        where T : UndertaleResource
    {
        if (resources is null)
            return [];

        return resources
               .Select((resource, index) => new TextureGroupResourceOption(
                   sectionKind,
                   index,
                   $"#{index} - {FormatObjectTitle(resource)}",
                   resource))
               .ToArray();
    }

    private static string BuildTextureGroupSummary(UndertaleTextureGroupInfo textureGroup)
    {
        string name = FormatTitle(textureGroup.Name?.Content);
        return $"{name} - {textureGroup.TexturePages?.Count ?? 0} texture page(s), " +
               $"{textureGroup.Sprites?.Count ?? 0} sprite(s), {textureGroup.SpineSprites?.Count ?? 0} spine sprite(s), " +
               $"{textureGroup.Fonts?.Count ?? 0} font(s), {textureGroup.Tilesets?.Count ?? 0} tileset(s).";
    }

    private static IReadOnlyList<TextureGroupEntrySummary> BuildTextureGroupEntrySummaries(
        UndertaleTextureGroupInfo textureGroup,
        UndertaleData data)
    {
        List<TextureGroupEntrySummary> entries = new();
        AddTextureGroupEntries(entries, TextureGroupSectionKind.TexturePages, "Texture pages", "Embedded textures", textureGroup.TexturePages, data.EmbeddedTextures);
        AddTextureGroupEntries(entries, TextureGroupSectionKind.Sprites, "Sprites", "Sprites", textureGroup.Sprites, data.Sprites);
        AddTextureGroupEntries(entries, TextureGroupSectionKind.SpineSprites, "Spine sprites", "Sprites", textureGroup.SpineSprites, data.Sprites);
        AddTextureGroupEntries(entries, TextureGroupSectionKind.Fonts, "Fonts", "Fonts", textureGroup.Fonts, data.Fonts);
        AddTextureGroupEntries(entries, TextureGroupSectionKind.Tilesets, "Tilesets", "Backgrounds", textureGroup.Tilesets, data.Backgrounds);
        return entries;
    }

    private static void AddTextureGroupEntries<T, ChunkT>(
        List<TextureGroupEntrySummary> entries,
        TextureGroupSectionKind sectionKind,
        string section,
        string categoryLabel,
        IEnumerable<UndertaleResourceById<T, ChunkT>>? resourceRefs,
        IList<T>? resources)
        where T : UndertaleResource, new()
        where ChunkT : UndertaleListChunk<T>
    {
        if (resourceRefs is null)
            return;

        int sectionIndex = 0;
        foreach (UndertaleResourceById<T, ChunkT> resourceRef in resourceRefs)
        {
            T? resource = resourceRef.Resource;
            int resourceIndex = resource is not null && resources is not null
                ? resources.IndexOf(resource)
                : resourceRef.CachedId;
            string name = resource is null
                ? "(unresolved)"
                : FormatObjectTitle(resource);
            string title = $"{section} #{sectionIndex} - {name}";
            string subtitle = resourceIndex >= 0
                ? $"{categoryLabel} #{resourceIndex}"
                : $"{categoryLabel}; cached id {resourceRef.CachedId.ToString(CultureInfo.InvariantCulture)}";
            entries.Add(new TextureGroupEntrySummary(sectionKind, section, sectionIndex, title, subtitle, categoryLabel, resourceIndex, resource));
            sectionIndex++;
        }
    }

    private static int GetTextureGroupSectionCount(UndertaleTextureGroupInfo textureGroup, TextureGroupSectionKind sectionKind)
    {
        return sectionKind switch
        {
            TextureGroupSectionKind.TexturePages => textureGroup.TexturePages?.Count ?? 0,
            TextureGroupSectionKind.Sprites => textureGroup.Sprites?.Count ?? 0,
            TextureGroupSectionKind.SpineSprites => textureGroup.SpineSprites?.Count ?? 0,
            TextureGroupSectionKind.Fonts => textureGroup.Fonts?.Count ?? 0,
            TextureGroupSectionKind.Tilesets => textureGroup.Tilesets?.Count ?? 0,
            _ => 0
        };
    }

    private static bool AddTextureGroupResource(
        UndertaleTextureGroupInfo textureGroup,
        TextureGroupSectionKind sectionKind,
        UndertaleResource resource)
    {
        switch (sectionKind)
        {
            case TextureGroupSectionKind.TexturePages when resource is UndertaleEmbeddedTexture texture:
                textureGroup.TexturePages.Add(new UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR>(texture));
                return true;
            case TextureGroupSectionKind.Sprites when resource is UndertaleSprite sprite:
                textureGroup.Sprites.Add(new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>(sprite));
                return true;
            case TextureGroupSectionKind.SpineSprites when resource is UndertaleSprite spineSprite:
                textureGroup.SpineSprites.Add(new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>(spineSprite));
                return true;
            case TextureGroupSectionKind.Fonts when resource is UndertaleFont font:
                textureGroup.Fonts.Add(new UndertaleResourceById<UndertaleFont, UndertaleChunkFONT>(font));
                return true;
            case TextureGroupSectionKind.Tilesets when resource is UndertaleBackground tileset:
                textureGroup.Tilesets.Add(new UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND>(tileset));
                return true;
            default:
                return false;
        }
    }

    private static bool ReplaceTextureGroupResource(
        UndertaleTextureGroupInfo textureGroup,
        TextureGroupSectionKind sectionKind,
        int sectionIndex,
        UndertaleResource resource)
    {
        switch (sectionKind)
        {
            case TextureGroupSectionKind.TexturePages when resource is UndertaleEmbeddedTexture texture &&
                                                          sectionIndex >= 0 &&
                                                          sectionIndex < textureGroup.TexturePages.Count:
                textureGroup.TexturePages[sectionIndex] = new UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR>(texture);
                return true;
            case TextureGroupSectionKind.Sprites when resource is UndertaleSprite sprite &&
                                                    sectionIndex >= 0 &&
                                                    sectionIndex < textureGroup.Sprites.Count:
                textureGroup.Sprites[sectionIndex] = new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>(sprite);
                return true;
            case TextureGroupSectionKind.SpineSprites when resource is UndertaleSprite spineSprite &&
                                                         sectionIndex >= 0 &&
                                                         sectionIndex < textureGroup.SpineSprites.Count:
                textureGroup.SpineSprites[sectionIndex] = new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>(spineSprite);
                return true;
            case TextureGroupSectionKind.Fonts when resource is UndertaleFont font &&
                                                  sectionIndex >= 0 &&
                                                  sectionIndex < textureGroup.Fonts.Count:
                textureGroup.Fonts[sectionIndex] = new UndertaleResourceById<UndertaleFont, UndertaleChunkFONT>(font);
                return true;
            case TextureGroupSectionKind.Tilesets when resource is UndertaleBackground tileset &&
                                                     sectionIndex >= 0 &&
                                                     sectionIndex < textureGroup.Tilesets.Count:
                textureGroup.Tilesets[sectionIndex] = new UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND>(tileset);
                return true;
            default:
                return false;
        }
    }

    private static bool RemoveTextureGroupResource(
        UndertaleTextureGroupInfo textureGroup,
        TextureGroupSectionKind sectionKind,
        int sectionIndex)
    {
        switch (sectionKind)
        {
            case TextureGroupSectionKind.TexturePages when sectionIndex >= 0 && sectionIndex < textureGroup.TexturePages.Count:
                textureGroup.TexturePages.RemoveAt(sectionIndex);
                return true;
            case TextureGroupSectionKind.Sprites when sectionIndex >= 0 && sectionIndex < textureGroup.Sprites.Count:
                textureGroup.Sprites.RemoveAt(sectionIndex);
                return true;
            case TextureGroupSectionKind.SpineSprites when sectionIndex >= 0 && sectionIndex < textureGroup.SpineSprites.Count:
                textureGroup.SpineSprites.RemoveAt(sectionIndex);
                return true;
            case TextureGroupSectionKind.Fonts when sectionIndex >= 0 && sectionIndex < textureGroup.Fonts.Count:
                textureGroup.Fonts.RemoveAt(sectionIndex);
                return true;
            case TextureGroupSectionKind.Tilesets when sectionIndex >= 0 && sectionIndex < textureGroup.Tilesets.Count:
                textureGroup.Tilesets.RemoveAt(sectionIndex);
                return true;
            default:
                return false;
        }
    }

    private static string BuildFontSummary(UndertaleFont font)
    {
        string name = FormatTitle(font.Name?.Content);
        string displayName = FormatTitle(font.DisplayName?.Content);
        string textureName = FormatTitle(font.Texture?.Name?.Content);
        string textureDetails = font.Texture is null
            ? "no atlas texture"
            : $"{textureName}, {font.Texture.BoundingWidth}x{font.Texture.BoundingHeight}";

        return $"{name} ({displayName}) - {FormatFloat(font.EmSize)} em, bold {font.Bold}, italic {font.Italic}, " +
               $"range {font.RangeStart}-{font.RangeEnd}, {font.Glyphs.Count} glyph(s), scale {FormatFloat(font.ScaleX)}x{FormatFloat(font.ScaleY)}. " +
               $"Atlas: {textureDetails}.";
    }

    private static IReadOnlyList<FontGlyphSummary> BuildFontGlyphSummaries(UndertaleFont font)
    {
        return font.Glyphs
                   .Select((glyph, index) =>
                   {
                       string title = $"#{index} - {FormatGlyphCharacter(glyph.Character)}";
                       string subtitle = $"Code {glyph.Character}; source {glyph.SourceX},{glyph.SourceY} {glyph.SourceWidth}x{glyph.SourceHeight}; shift {glyph.Shift}, offset {glyph.Offset}, {glyph.Kerning.Count} kerning pair(s).";
                       return new FontGlyphSummary(index, title, subtitle, font.Texture, glyph);
                   })
                   .ToArray();
    }

    private static IReadOnlyList<FontKerningSummary> BuildFontKerningSummaries(UndertaleFont.Glyph glyph)
    {
        return glyph.Kerning
                    .Select((kerning, index) =>
                    {
                        string title = $"#{index} - {FormatKerningCharacter(kerning.Character)}";
                        string subtitle = $"Additional shift {kerning.ShiftModifier.ToString(CultureInfo.InvariantCulture)}";
                        return new FontKerningSummary(index, title, subtitle, kerning);
                    })
                    .ToArray();
    }

    private static string FormatGlyphCharacter(ushort character)
    {
        if (character == 0)
            return "(null)";

        char ch = Convert.ToChar(character);
        return char.IsControl(ch)
            ? $"U+{character:X4}"
            : $"'{ch}' (U+{character:X4})";
    }

    private static string FormatKerningCharacter(short character)
    {
        if (character == 0)
            return "(none)";

        if (character < 0)
            return character.ToString(CultureInfo.InvariantCulture);

        char ch = Convert.ToChar(character);
        return char.IsControl(ch)
            ? $"U+{character:X4}"
            : $"'{ch}' (U+{character:X4})";
    }

    private static bool TryParseKerningCharacter(string value, out short character)
    {
        value = value.Trim();
        if (short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out character))
            return true;

        if (value.Length == 1)
        {
            character = (short)value[0];
            return true;
        }

        character = 0;
        return false;
    }

    private static bool TryParseGlyphCharacter(string value, out ushort character)
    {
        value = value.Trim();
        if (ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out character))
            return true;

        if (value.Length == 1)
        {
            character = value[0];
            return true;
        }

        character = 0;
        return false;
    }

    private static string BuildShaderSummary(UndertaleShader shader)
    {
        return $"{FormatTitle(shader.Name?.Content)} - type {shader.Type}, version {shader.Version}, " +
               $"{shader.VertexShaderAttributes.Count} vertex attribute(s).";
    }

    private static IReadOnlyList<ShaderSourceItem> BuildShaderSourceItems(UndertaleShader shader)
    {
        return
        [
            new("GLSL ES vertex source", shader.GLSL_ES_Vertex ??= new UndertaleString(string.Empty)),
            new("GLSL ES fragment source", shader.GLSL_ES_Fragment ??= new UndertaleString(string.Empty)),
            new("GLSL vertex source", shader.GLSL_Vertex ??= new UndertaleString(string.Empty)),
            new("GLSL fragment source", shader.GLSL_Fragment ??= new UndertaleString(string.Empty)),
            new("HLSL9 vertex source", shader.HLSL9_Vertex ??= new UndertaleString(string.Empty)),
            new("HLSL9 fragment source", shader.HLSL9_Fragment ??= new UndertaleString(string.Empty))
        ];
    }

    private static IReadOnlyList<ShaderAttributeSummary> BuildShaderAttributeSummaries(UndertaleShader shader)
    {
        return shader.VertexShaderAttributes
                     .Select((attribute, index) =>
                     {
                         string name = FormatTitle(attribute.Name?.Content);
                         return new ShaderAttributeSummary(index, $"#{index} - {name}", "Vertex shader attribute", attribute);
                     })
                     .ToArray();
    }

    private static string BuildTimelineSummary(UndertaleTimeline timeline)
    {
        int actionCount = timeline.Moments.Sum(moment => moment.Event?.Count ?? 0);
        if (timeline.Moments.Count == 0)
            return $"{FormatTitle(timeline.Name?.Content)} has no moments.";

        uint firstStep = timeline.Moments.Min(moment => moment.Step);
        uint lastStep = timeline.Moments.Max(moment => moment.Step);
        return $"{FormatTitle(timeline.Name?.Content)} - {timeline.Moments.Count} moment(s), {actionCount} action(s), steps {firstStep}..{lastStep}.";
    }

    private static IReadOnlyList<TimelineMomentSummary> BuildTimelineMomentSummaries(UndertaleTimeline timeline)
    {
        return timeline.Moments
                       .Select((moment, index) =>
                       {
                           int actionCount = moment.Event?.Count ?? 0;
                           string title = $"#{index} - step {moment.Step}";
                           string subtitle = $"{actionCount} action(s)";
                           return new TimelineMomentSummary(index, title, subtitle, moment);
                       })
                       .ToArray();
    }

    private static IReadOnlyList<TimelineActionSummary> BuildTimelineActionSummaries(UndertaleTimeline.UndertaleTimelineMoment moment)
    {
        return moment.Event?
                     .Select((action, index) =>
                     {
                         string codeName = FormatTitle(action.CodeId?.Name?.Content);
                         string title = $"#{index} - {codeName}";
                         string subtitle = action.CodeId is null ? "No code entry selected" : "Runs code entry";
                         return new TimelineActionSummary(index, title, subtitle, action);
                     })
                     .ToArray() ?? [];
    }

    private static IReadOnlyList<CodeReferenceItem> BuildCodeReferenceItems(UndertaleData data, bool includeNull = true)
    {
        IEnumerable<CodeReferenceItem> items = data.Code
                                                   .Select((code, index) => new CodeReferenceItem(index, FormatTitle(code.Name?.Content), code));
        return includeNull
            ? [new CodeReferenceItem(-1, "(null)", null), .. items]
            : items.ToArray();
    }

    private static IReadOnlyList<ObjectReferenceItem> BuildObjectReferenceItems(UndertaleData data, bool includeNull = false)
    {
        IEnumerable<ObjectReferenceItem> items = data.GameObjects
                                                     .Select((gameObject, index) => new ObjectReferenceItem(index, FormatTitle(gameObject.Name?.Content), gameObject));
        return includeNull
            ? [new ObjectReferenceItem(-1, "(null)", null), .. items]
            : items.ToArray();
    }

    private static IReadOnlyList<BackgroundReferenceItem> BuildBackgroundReferenceItems(UndertaleData data, bool includeNull = false)
    {
        IEnumerable<BackgroundReferenceItem> items = data.Backgrounds
                                                       .Select((background, index) => new BackgroundReferenceItem(index, FormatTitle(background.Name?.Content), background));
        return includeNull
            ? [new BackgroundReferenceItem(-1, "(null)", null), .. items]
            : items.ToArray();
    }

    private static UndertaleTimeline.UndertaleTimelineMoment CreateTimelineMoment(uint step, bool addEmptyAction)
    {
        UndertalePointerList<UndertaleGameObject.EventAction> actions = new();
        if (addEmptyAction)
            actions.Add(new UndertaleGameObject.EventAction());

        return new UndertaleTimeline.UndertaleTimelineMoment(step, actions);
    }

    private static string BuildExtensionSummary(UndertaleExtension extension)
    {
        int functionCount = extension.Files.Sum(file => file.Functions?.Count ?? 0);
        int argumentCount = extension.Files.Sum(file => file.Functions?.Sum(function => function.Arguments?.Count ?? 0) ?? 0);
        return $"{FormatTitle(extension.Name?.Content)} - class {FormatTitle(extension.ClassName?.Content)}, " +
               $"{extension.Files.Count} file(s), {functionCount} function(s), {argumentCount} argument(s), {extension.Options.Count} option(s).";
    }

    private static IReadOnlyList<ExtensionFileSummary> BuildExtensionFileSummaries(UndertaleExtension extension)
    {
        return extension.Files
                        .Select((file, index) =>
                        {
                            string title = $"#{index} - {FormatTitle(file.Filename?.Content)}";
                            string subtitle = $"{file.Kind}; init {FormatTitle(file.InitScript?.Content)}, cleanup {FormatTitle(file.CleanupScript?.Content)}, {file.Functions.Count} function(s)";
                            return new ExtensionFileSummary(index, title, subtitle, file);
                        })
                        .ToArray();
    }

    private static IReadOnlyList<ExtensionFunctionSummary> BuildExtensionFunctionSummaries(UndertaleExtensionFile file)
    {
        return file.Functions
                   .Select((function, index) =>
                   {
                       string title = $"#{index} - {FormatTitle(function.Name?.Content)}";
                       string subtitle = $"External {FormatTitle(function.ExtName?.Content)}, ID {function.ID}, kind {function.Kind}, returns {function.RetType}, {function.Arguments.Count} argument(s)";
                       return new ExtensionFunctionSummary(index, title, subtitle, function);
                   })
                   .ToArray();
    }

    private static IReadOnlyList<ExtensionArgumentSummary> BuildExtensionArgumentSummaries(UndertaleExtensionFunction function)
    {
        return function.Arguments
                       .Select((argument, index) => new ExtensionArgumentSummary(index, $"#{index} - {argument.Type}", argument))
                       .ToArray();
    }

    private static IReadOnlyList<ExtensionOptionSummary> BuildExtensionOptionSummaries(UndertaleExtension extension)
    {
        return extension.Options
                        .Select((option, index) =>
                        {
                            string title = $"#{index} - {FormatTitle(option.Name?.Content)}";
                            string subtitle = $"{option.Kind}: {FormatTitle(option.Value?.Content)}";
                            return new ExtensionOptionSummary(index, title, subtitle, option);
                        })
                        .ToArray();
    }

    private static UndertaleExtensionFile CloneExtensionFile(UndertaleExtensionFile source, UndertaleData data)
    {
        UndertaleExtensionFile clone = new()
        {
            Filename = data.Strings.MakeString(source.Filename?.Content ?? string.Empty),
            InitScript = data.Strings.MakeString(source.InitScript?.Content ?? string.Empty),
            CleanupScript = data.Strings.MakeString(source.CleanupScript?.Content ?? string.Empty),
            Kind = source.Kind,
            Functions = new UndertalePointerList<UndertaleExtensionFunction>()
        };

        foreach (UndertaleExtensionFunction function in source.Functions)
            clone.Functions.Add(CloneExtensionFunction(function, data));

        return clone;
    }

    private static UndertaleExtensionFunction CreateExtensionFunction(UndertaleData data, int index)
    {
        return new UndertaleExtensionFunction
        {
            Name = data.Strings.MakeString($"new_extension_function_{index}"),
            ExtName = data.Strings.MakeString($"new_extension_function_{index}_ext"),
            RetType = UndertaleExtensionVarType.Double,
            Arguments = new UndertaleSimpleList<UndertaleExtensionFunctionArg>(),
            Kind = 11,
            ID = data.ExtensionFindLastId()
        };
    }

    private static UndertaleExtensionFunction CloneExtensionFunction(UndertaleExtensionFunction source, UndertaleData data)
    {
        UndertaleExtensionFunction clone = new()
        {
            Name = data.Strings.MakeString(source.Name?.Content ?? string.Empty),
            ExtName = data.Strings.MakeString(source.ExtName?.Content ?? string.Empty),
            ID = source.ID,
            Kind = source.Kind,
            RetType = source.RetType,
            Arguments = new UndertaleSimpleList<UndertaleExtensionFunctionArg>()
        };

        foreach (UndertaleExtensionFunctionArg argument in source.Arguments)
            clone.Arguments.Add(new UndertaleExtensionFunctionArg(argument.Type));

        return clone;
    }

    private static UndertaleExtensionOption CloneExtensionOption(UndertaleExtensionOption source, UndertaleData data)
    {
        return new UndertaleExtensionOption
        {
            Name = data.Strings.MakeString(source.Name?.Content ?? string.Empty),
            Value = data.Strings.MakeString(source.Value?.Content ?? string.Empty, createNew: true),
            Kind = source.Kind
        };
    }

    private static string NormalizeExtensionOptionValue(string value, UndertaleExtensionOption.OptionKind kind)
    {
        return kind switch
        {
            UndertaleExtensionOption.OptionKind.Boolean => value.Equals("true", StringComparison.OrdinalIgnoreCase) ? "True" : "False",
            UndertaleExtensionOption.OptionKind.Number => double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                ? value
                : "0",
            _ => value
        };
    }

    private static bool UpdateUndertaleString(
        UndertaleString? current,
        string content,
        UndertaleData data,
        out UndertaleString value)
    {
        if (current is null)
        {
            value = data.Strings.MakeString(content);
            return true;
        }

        value = current;
        if (current.Content == content)
            return false;

        current.Content = content;
        return true;
    }

    private static string BuildParticleSystemSummary(UndertaleParticleSystem particleSystem)
    {
        return $"{FormatTitle(particleSystem.Name?.Content)} - origin {particleSystem.OriginX},{particleSystem.OriginY}, " +
               $"draw order {particleSystem.DrawOrder}, global space {particleSystem.GlobalSpaceParticles}, {particleSystem.Emitters.Count} emitter link(s).";
    }

    private static string BuildParticleEmitterSummary(UndertaleParticleSystemEmitter emitter, UndertaleData data)
    {
        int emitterIndex = data.ParticleSystemEmitters.IndexOf(emitter);
        string spriteName = FormatTitle(emitter.Sprite?.Name?.Content);
        string deathName = FormatTitle(emitter.SpawnOnDeath?.Name?.Content);
        string updateName = FormatTitle(emitter.SpawnOnUpdate?.Name?.Content);
        return $"Global #{emitterIndex} - {emitter.Mode}, {emitter.Shape}, {emitter.Texture}; sprite {spriteName}; " +
               $"spawn on death {deathName} x{emitter.SpawnOnDeathCount}; spawn on update {updateName} x{emitter.SpawnOnUpdateCount}.";
    }

    private static IReadOnlyList<ParticleEmitterSummary> BuildParticleEmitterSummaries(
        UndertaleParticleSystem particleSystem,
        UndertaleData data)
    {
        return particleSystem.Emitters
                             .Select((reference, index) =>
                             {
                                 UndertaleParticleSystemEmitter? emitter = reference?.Resource;
                                 int globalIndex = emitter is null ? -1 : data.ParticleSystemEmitters.IndexOf(emitter);
                                 string title = $"#{index} - {FormatTitle(emitter?.Name?.Content)}";
                                 string subtitle = emitter is null
                                     ? "Missing emitter reference"
                                     : $"Global #{globalIndex}, {emitter.Mode}, {emitter.Shape}, {emitter.Texture}, emit count {emitter.EmitCount}";
                                 return new ParticleEmitterSummary(index, title, subtitle, emitter);
                             })
                             .ToArray();
    }

    private static IReadOnlyList<SpriteReferenceItem> BuildSpriteReferenceItems(UndertaleData data, bool includeNull = false)
    {
        IEnumerable<SpriteReferenceItem> items = data.Sprites
                                                     .Select((sprite, index) => new SpriteReferenceItem(index, FormatTitle(sprite.Name?.Content), sprite));
        return includeNull
            ? [new SpriteReferenceItem(-1, "(none)", null), .. items]
            : items.ToArray();
    }

    private static IReadOnlyList<SequenceReferenceItem> BuildSequenceReferenceItems(UndertaleData data, bool includeNull = false)
    {
        IEnumerable<SequenceReferenceItem> items = data.Sequences
                                                       .Select((sequence, index) => new SequenceReferenceItem(index, FormatTitle(sequence.Name?.Content), sequence));
        return includeNull
            ? [new SequenceReferenceItem(-1, "(none)", null), .. items]
            : items.ToArray();
    }

    private static IReadOnlyList<ParticleSystemReferenceItem> BuildParticleSystemReferenceItems(
        UndertaleData data,
        bool includeNull = false)
    {
        IEnumerable<ParticleSystemReferenceItem> items = data.ParticleSystems
                                                             .Select((particleSystem, index) => new ParticleSystemReferenceItem(index, FormatTitle(particleSystem.Name?.Content), particleSystem));
        return includeNull
            ? [new ParticleSystemReferenceItem(-1, "(none)", null), .. items]
            : items.ToArray();
    }

    private static IReadOnlyList<ParticleEmitterReferenceItem> BuildParticleEmitterReferenceItems(UndertaleData data, bool includeNull = false)
    {
        IEnumerable<ParticleEmitterReferenceItem> items = data.ParticleSystemEmitters
                                                              .Select((emitter, index) => new ParticleEmitterReferenceItem(index, FormatTitle(emitter.Name?.Content), emitter));
        return includeNull
            ? [new ParticleEmitterReferenceItem(-1, "(none)", null), .. items]
            : items.ToArray();
    }

    private static UndertaleParticleSystemEmitter CreateParticleSystemEmitter(UndertaleData data, int index)
    {
        return new UndertaleParticleSystemEmitter
        {
            Name = data.Strings.MakeString($"particleEmitter{index}"),
            Enabled = true,
            Mode = UndertaleParticleSystemEmitter.EmitMode.Stream,
            EmitCount = 1,
            Distribution = UndertaleParticleSystemEmitter.DistributionEnum.Linear,
            Shape = UndertaleParticleSystemEmitter.EmitterShape.Rectangle,
            RegionWidth = 64,
            RegionHeight = 64,
            Texture = UndertaleParticleSystemEmitter.TextureEnum.Pixel,
            LifetimeMin = 80,
            LifetimeMax = 80,
            ScaleX = 1,
            ScaleY = 1,
            SizeMinX = 1,
            SizeMaxX = 1,
            SizeMinY = 1,
            SizeMaxY = 1,
            SpeedMin = 5,
            SpeedMax = 5,
            DirectionMin = 80,
            DirectionMax = 100,
            GravityDirection = 270,
            StartColor = 0xFFFFFFFF,
            MidColor = 0xFFFFFFFF,
            EndColor = 0xFFFFFFFF
        };
    }

    private static string BuildTexturePageItemPreviewInfo(UndertaleTexturePageItem item)
    {
        string textureName = FormatTitle(item.Name?.Content);
        string pageName = FormatTitle(item.TexturePage?.Name?.Content);
        return $"{textureName} on {pageName} - source {item.SourceX},{item.SourceY} {item.SourceWidth}x{item.SourceHeight}; target {item.TargetX},{item.TargetY} {item.TargetWidth}x{item.TargetHeight}; bounds {item.BoundingWidth}x{item.BoundingHeight}.";
    }

    private static string BuildEmbeddedTexturePreviewInfo(UndertaleEmbeddedTexture texture)
    {
        string textureName = FormatTitle(texture.Name?.Content);
        GMImage? image = texture.TextureData?.Image;
        return image is null
            ? $"{textureName} - no image data."
            : $"{textureName} - {image.Width}x{image.Height}, {image.Format}.";
    }

    private static string? GetTextureName(object textureValue)
    {
        return textureValue switch
        {
            UndertaleTexturePageItem item => item.Name?.Content,
            UndertaleEmbeddedTexture texture => texture.Name?.Content,
            _ => null
        };
    }

    private string BuildEmbeddedAudioInfo(UndertaleEmbeddedAudio audio)
    {
        string audioName = FormatTitle(audio.Name?.Content);
        string kind = GetEmbeddedAudioKind(audio.Data);
        string details = GetEmbeddedAudioDetails(audio.Data);
        int audioIndex = _data?.EmbeddedAudio.IndexOf(audio) ?? -1;
        string idText = audioIndex >= 0 ? $"embedded audio #{audioIndex}" : "embedded audio";
        return string.Join(
            Environment.NewLine,
            $"{audioName} - {idText}, {kind} data, {FormatByteCount(audio.Data.Length)}.{details}",
            BuildEmbeddedAudioPreviewSummary(audio.Data),
            BuildEmbeddedAudioReferenceSummary(audio, audioIndex));
    }

    private string BuildEmbeddedAudioReferenceSummary(UndertaleEmbeddedAudio audio, int audioIndex)
    {
        if (_data?.Sounds is null)
            return "Linked sounds: unavailable.";

        int builtinGroupId = _data.GetBuiltinSoundGroupID();
        string[] references = _data.Sounds
                                   .Select((sound, index) => new
                                   {
                                       Index = index,
                                       Sound = sound
                                   })
                                   .Where(item => ReferenceEquals(item.Sound.AudioFile, audio) ||
                                                  (audioIndex >= 0 && item.Sound.GroupID == builtinGroupId && item.Sound.AudioID == audioIndex))
                                   .Select(item =>
                                       $"#{item.Index} {FormatTitle(item.Sound.Name?.Content)} " +
                                       $"(group {item.Sound.GroupID}, audio {item.Sound.AudioID}, volume {item.Sound.Volume.ToString(CultureInfo.InvariantCulture)}, " +
                                       $"pitch {item.Sound.Pitch.ToString(CultureInfo.InvariantCulture)}, preload {item.Sound.Preload})")
                                   .Take(6)
                                   .ToArray();

        if (references.Length == 0)
            return "Linked sounds: none.";

        int total = _data.Sounds.Count(sound => ReferenceEquals(sound.AudioFile, audio) ||
                                                (audioIndex >= 0 && sound.GroupID == builtinGroupId && sound.AudioID == audioIndex));
        string suffix = total > references.Length ? $" (+{total - references.Length} more)" : string.Empty;
        return $"Linked sounds: {string.Join("; ", references)}{suffix}.";
    }

    private UndertaleEmbeddedAudio? GetBuiltinEmbeddedAudioForSound(UndertaleSound sound)
    {
        if (_data is null || sound.GroupID != _data.GetBuiltinSoundGroupID())
            return null;

        if (sound.AudioFile is not null)
            return sound.AudioFile;

        return sound.AudioID >= 0 && sound.AudioID < _data.EmbeddedAudio.Count
            ? _data.EmbeddedAudio[sound.AudioID]
            : null;
    }

    private string BuildSoundAudioInfo(UndertaleSound sound, SoundAudioSource source)
    {
        string soundName = FormatTitle(sound.Name?.Content);
        int builtinGroupId = _data?.GetBuiltinSoundGroupID() ?? 0;

        if (source.Error is not null)
        {
            return string.Join(
                Environment.NewLine,
                $"{soundName} audio is not available: {source.Error}",
                $"IDs: built-in group {builtinGroupId}, group {sound.GroupID}, audio {sound.AudioID}.",
                BuildSoundPlaybackSummary(sound));
        }

        if (source.FilePath is not null && source.EmbeddedAudio is null)
        {
            FileInfo fileInfo = new(source.FilePath);
            string fileKind = Path.GetExtension(source.FilePath).TrimStart('.').ToUpperInvariant();
            if (fileKind.Length == 0)
                fileKind = "Unknown";

            return string.Join(
                Environment.NewLine,
                $"{soundName} uses {source.SourceLabel}: {fileKind}, {FormatByteCount(fileInfo.Length)}.",
                CanPreviewAudioFile(source.FilePath) ? "Preview: playable in-app." : "Preview: unsupported format for in-app playback.",
                $"IDs: built-in group {builtinGroupId}, group {sound.GroupID}, audio {sound.AudioID}.",
                BuildSoundPlaybackSummary(sound));
        }

        UndertaleEmbeddedAudio? audio = source.EmbeddedAudio;
        if (audio is null)
            return $"{soundName} does not have a valid audio source.";

        int audioIndex = source.AudioIndex;
        string audioName = FormatTitle(audio.Name?.Content);
        string audioKind = GetEmbeddedAudioKind(audio.Data);
        string details = GetEmbeddedAudioDetails(audio.Data);
        return string.Join(
            Environment.NewLine,
            $"{soundName} uses {source.SourceLabel} ({audioName}): {audioKind}, {FormatByteCount(audio.Data.Length)}.{details}",
            BuildEmbeddedAudioPreviewSummary(audio.Data),
            $"IDs: built-in group {builtinGroupId}, group {sound.GroupID}, audio {sound.AudioID}, resolved group {source.GroupId}, audio #{audioIndex}.",
            BuildSoundPlaybackSummary(sound));
    }

    private static bool CanExportSoundAudio(SoundAudioSource source)
    {
        return source.EmbeddedAudio is { Data.Length: > 0 } ||
               (source.FilePath is not null && File.Exists(source.FilePath));
    }

    private static string GetSoundAudioSourceExtension(SoundAudioSource source)
    {
        if (source.EmbeddedAudio is not null)
            return GetEmbeddedAudioExtension(source.EmbeddedAudio.Data);

        if (source.FilePath is not null)
        {
            string extension = Path.GetExtension(source.FilePath);
            if (!string.IsNullOrWhiteSpace(extension))
                return extension;
        }

        return ".bin";
    }

    private static string GetSoundAudioSuggestedFileName(UndertaleSound sound, SoundAudioSource source)
    {
        if (source.FilePath is not null)
            return SafeFileName(Path.GetFileName(source.FilePath), "sound");

        return SafeFileName(sound.File?.Content ?? sound.Name?.Content, "sound");
    }

    private static string BuildSoundPlaybackSummary(UndertaleSound sound)
    {
        return $"File {FormatTitle(sound.File?.Content)}, type {FormatTitle(sound.Type.ToString())}, flags {FormatTitle(sound.Flags.ToString())}, effects {sound.Effects.ToString(CultureInfo.InvariantCulture)}, volume {sound.Volume.ToString(CultureInfo.InvariantCulture)}, pitch {sound.Pitch.ToString(CultureInfo.InvariantCulture)}, preload {sound.Preload}, audio length {FormatFloat(sound.AudioLength)}.";
    }

    private static string FormatByteCount(long byteCount)
    {
        if (byteCount < 1024)
            return $"{byteCount.ToString(CultureInfo.InvariantCulture)} bytes";

        double kib = byteCount / 1024d;
        if (kib < 1024)
            return $"{kib.ToString("0.#", CultureInfo.InvariantCulture)} KiB ({byteCount.ToString(CultureInfo.InvariantCulture)} bytes)";

        double mib = kib / 1024d;
        return $"{mib.ToString("0.##", CultureInfo.InvariantCulture)} MiB ({byteCount.ToString(CultureInfo.InvariantCulture)} bytes)";
    }

    private static string GetEmbeddedAudioDetails(byte[] data)
    {
        if (IsWav(data))
        {
            string? wavDetails = TryReadWavDetails(data);
            return wavDetails is null ? string.Empty : $" {wavDetails}";
        }

        if (IsOgg(data))
            return " OGG stream.";

        return string.Empty;
    }

    private static bool CanPreviewEmbeddedAudio(byte[] data)
    {
        return data.Length > 0 && (IsWav(data) || IsOgg(data));
    }

    private static string BuildEmbeddedAudioPreviewSummary(byte[] data)
    {
        if (data.Length == 0)
            return "Preview: no audio data.";

        return CanPreviewEmbeddedAudio(data)
            ? "Preview: playable in-app."
            : "Preview: unsupported format for in-app playback.";
    }

    private static string? TryReadWavDetails(byte[] data)
    {
        if (data.Length < 44)
            return null;

        int formatIndex = -1;
        for (int i = 12; i <= data.Length - 24; i++)
        {
            if (data[i] == (byte)'f' && data[i + 1] == (byte)'m' && data[i + 2] == (byte)'t' && data[i + 3] == (byte)' ')
            {
                formatIndex = i;
                break;
            }
        }

        if (formatIndex < 0 || formatIndex + 24 > data.Length)
            return null;

        ushort channels = BitConverter.ToUInt16(data, formatIndex + 10);
        uint sampleRate = BitConverter.ToUInt32(data, formatIndex + 12);
        ushort bitsPerSample = BitConverter.ToUInt16(data, formatIndex + 22);
        return $"WAV PCM summary: {channels} channel(s), {sampleRate.ToString(CultureInfo.InvariantCulture)} Hz, {bitsPerSample} bit.";
    }

    private static string? ValidateImportedAudio(byte[] currentData, byte[] importedData)
    {
        bool currentWav = IsWav(currentData);
        bool currentOgg = IsOgg(currentData);
        bool importedWav = IsWav(importedData);
        bool importedOgg = IsOgg(importedData);

        if (!importedWav && !importedOgg)
            return "Warning: imported file is not WAV or OGG. This may corrupt the sound.";

        if ((currentWav && importedOgg) || (currentOgg && importedWav))
            return "Warning: imported file type does not match the existing file type. Sound asset compression settings may also need adjustment.";

        return null;
    }

    private static string GetEmbeddedAudioKind(byte[] data)
    {
        if (IsWav(data))
            return "WAV";
        if (IsOgg(data))
            return "OGG";
        return "Unknown";
    }

    private static string GetEmbeddedAudioExtension(byte[] data)
    {
        if (IsWav(data))
            return ".wav";
        if (IsOgg(data))
            return ".ogg";
        return ".bin";
    }

    private static bool IsWav(byte[] data)
    {
        return data.Length >= 4 && data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F';
    }

    private static bool IsOgg(byte[] data)
    {
        return data.Length >= 4 && data[0] == 'O' && data[1] == 'g' && data[2] == 'g' && data[3] == 'S';
    }

    private static string SafeFileName(string? value, string fallback)
    {
        string name = string.IsNullOrWhiteSpace(value) ? fallback : value;
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        return name.Length == 0 ? fallback : name;
    }

    private async void PreviewImage_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (_isImagePreviewDialogOpen)
        {
            e.Handled = true;
            return;
        }

        if (ReferenceEquals(sender, TexturePreviewImage) && _suppressNextTexturePreviewTap)
        {
            _suppressNextTexturePreviewTap = false;
            e.Handled = true;
            return;
        }

        if (sender is not Image { Source: ImageSource source })
            return;

        await ShowImagePreviewDialogAsync(source);
    }

    private async System.Threading.Tasks.Task ShowImagePreviewDialogAsync(ImageSource source)
    {
        if (_isImagePreviewDialogOpen)
            return;

        _isImagePreviewDialogOpen = true;
        UpdateImagePreviewOpenStates();
        BitmapImage? bitmap = source as BitmapImage;

        double rootWidth = XamlRoot?.Size.Width > 0 ? XamlRoot.Size.Width : Math.Max(ActualWidth, 960);
        double rootHeight = XamlRoot?.Size.Height > 0 ? XamlRoot.Size.Height : Math.Max(ActualHeight, 720);
        double maxDialogWidth = Math.Min(1040, Math.Max(420, rootWidth - 96));
        double maxDialogHeight = Math.Min(820, Math.Max(360, rootHeight - 120));
        double dialogWidth = Math.Min(maxDialogWidth, Math.Max(520, rootWidth * 0.72));
        double dialogHeight = Math.Min(maxDialogHeight, Math.Max(420, rootHeight * 0.68));
        double previewViewportWidth = Math.Max(320, dialogWidth);
        double previewViewportHeight = Math.Max(260, dialogHeight - 58);
        const double PreviewPadding = 48;

        Image image = new()
        {
            Source = source,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Stretch = Stretch.Uniform
        };

        TextBlock zoomValueText = new()
        {
            MinWidth = 48,
            VerticalAlignment = VerticalAlignment.Center,
            Text = "100%"
        };

        Slider zoomSlider = new()
        {
            Width = Math.Clamp(dialogWidth * 0.18, 120, 220),
            Minimum = 0.25,
            Maximum = 16,
            Value = 1,
            StepFrequency = 0.25
        };

        Brush previewBackground = Application.Current.Resources.TryGetValue("WinUiPanelBackgroundBrush", out object backgroundResource) &&
                                  backgroundResource is Brush backgroundBrush
            ? backgroundBrush
            : new SolidColorBrush(Microsoft.UI.Colors.Transparent);

        Grid imageHost = new()
        {
            Width = previewViewportWidth,
            Height = previewViewportHeight,
            MinWidth = 320,
            MinHeight = 260,
            Background = previewBackground
        };
        imageHost.Children.Add(image);

        ScrollViewer scrollViewer = new()
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            ZoomMode = ZoomMode.Disabled,
            Content = imageHost
        };

        Brush previewBorderBrush = Application.Current.Resources.TryGetValue("WinUiPanelStrokeBrush", out object borderResource) &&
                                   borderResource is Brush borderBrush
            ? borderBrush
            : new SolidColorBrush(Microsoft.UI.Colors.Transparent);

        Border scrollBorder = new()
        {
            Background = previewBackground,
            BorderBrush = previewBorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = scrollViewer
        };

        Button fitButton = new()
        {
            Content = "Fit"
        };

        Button actualSizeButton = new()
        {
            Content = "100%"
        };

        bool updatingZoom = false;
        double GetPreviewViewportWidth() => Math.Max(1, scrollViewer.ViewportWidth > 0 ? scrollViewer.ViewportWidth : previewViewportWidth);

        double GetPreviewViewportHeight() => Math.Max(1, scrollViewer.ViewportHeight > 0 ? scrollViewer.ViewportHeight : previewViewportHeight);

        void SetDialogZoom(double zoom, bool resetOffset = false)
        {
            zoom = Math.Clamp(zoom, zoomSlider.Minimum, zoomSlider.Maximum);
            double sourceWidth = bitmap?.PixelWidth > 0 ? bitmap.PixelWidth : 1;
            double sourceHeight = bitmap?.PixelHeight > 0 ? bitmap.PixelHeight : 1;
            double scaledWidth = Math.Max(1, sourceWidth * zoom);
            double scaledHeight = Math.Max(1, sourceHeight * zoom);
            double viewportWidth = GetPreviewViewportWidth();
            double viewportHeight = GetPreviewViewportHeight();

            image.Width = scaledWidth;
            image.Height = scaledHeight;
            imageHost.Width = Math.Max(viewportWidth, scaledWidth + PreviewPadding);
            imageHost.Height = Math.Max(viewportHeight, scaledHeight + PreviewPadding);

            updatingZoom = true;
            zoomSlider.Value = zoom;
            zoomValueText.Text = $"{zoom * 100:0}%";
            updatingZoom = false;
            if (resetOffset)
            {
                scrollViewer.UpdateLayout();
                double horizontalOffset = Math.Max(0, (imageHost.Width - GetPreviewViewportWidth()) / 2);
                double verticalOffset = Math.Max(0, (imageHost.Height - GetPreviewViewportHeight()) / 2);
                scrollViewer.ChangeView(horizontalOffset, verticalOffset, null);
            }
        }

        double GetFitZoom()
        {
            if (bitmap is null || bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
                return 1;

            double availableWidth = Math.Max(1, GetPreviewViewportWidth() - PreviewPadding);
            double availableHeight = Math.Max(1, GetPreviewViewportHeight() - PreviewPadding);
            return Math.Clamp(
                Math.Min(availableWidth / bitmap.PixelWidth, availableHeight / bitmap.PixelHeight),
                zoomSlider.Minimum,
                zoomSlider.Maximum);
        }

        zoomSlider.ValueChanged += (_, args) =>
        {
            if (!updatingZoom)
                SetDialogZoom(args.NewValue);
        };

        scrollViewer.PointerWheelChanged += (_, args) =>
        {
            int wheelDelta = args.GetCurrentPoint(scrollViewer).Properties.MouseWheelDelta;
            if (wheelDelta == 0)
                return;

            double factor = wheelDelta > 0 ? 1.15 : 1 / 1.15;
            SetDialogZoom(zoomSlider.Value * factor, resetOffset: false);
            args.Handled = true;
        };

        fitButton.Click += (_, _) => SetDialogZoom(GetFitZoom(), true);
        actualSizeButton.Click += (_, _) => SetDialogZoom(1, true);

        Grid toolbar = new()
        {
            ColumnSpacing = 8
        };
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        TextBlock imageInfoText = new()
        {
            Text = bitmap is not null && bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0
                ? $"{bitmap.PixelWidth} x {bitmap.PixelHeight}"
                : "Preview image",
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = Application.Current.Resources.TryGetValue("WinUiQuietTextBrush", out object quietTextResource) &&
                         quietTextResource is Brush quietTextBrush
                ? quietTextBrush
                : null
        };
        toolbar.Children.Add(imageInfoText);
        Grid.SetColumn(fitButton, 1);
        toolbar.Children.Add(fitButton);
        Grid.SetColumn(actualSizeButton, 2);
        toolbar.Children.Add(actualSizeButton);
        Grid.SetColumn(zoomValueText, 3);
        toolbar.Children.Add(zoomValueText);
        Grid.SetColumn(zoomSlider, 4);
        toolbar.Children.Add(zoomSlider);

        Grid content = new()
        {
            RowSpacing = 10,
            Width = dialogWidth,
            Height = dialogHeight,
            MinWidth = Math.Min(420, dialogWidth)
        };
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        content.Children.Add(toolbar);
        Grid.SetRow(scrollBorder, 1);
        content.Children.Add(scrollBorder);
        void ApplyInitialFit()
        {
            double initialZoom = GetFitZoom();
            SetDialogZoom(initialZoom, resetOffset: true);
        }

        content.Loaded += (_, _) =>
        {
            if (!DispatcherQueue.TryEnqueue(ApplyInitialFit))
                ApplyInitialFit();
        };

        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = "Preview",
            Content = content,
            CloseButtonText = "Close"
        };
        dialog.MinWidth = Math.Min(dialogWidth + 72, maxDialogWidth + 72);
        dialog.MaxWidth = maxDialogWidth + 96;
        dialog.MaxHeight = maxDialogHeight + 96;
        dialog.Resources["ContentDialogMaxWidth"] = maxDialogWidth + 96;
        dialog.Resources["ContentDialogMaxHeight"] = maxDialogHeight + 96;

        try
        {
            await dialog.ShowAsync();
        }
        finally
        {
            _isImagePreviewDialogOpen = false;
            UpdateImagePreviewOpenStates();
        }
    }

    private void UpdateImagePreviewOpenStates()
    {
        bool canOpen = !_isImagePreviewDialogOpen;
        SpritePreviewImage.IsHitTestVisible = canOpen && SpritePreviewImage.Source is not null;
        SpriteLargePreviewImage.IsHitTestVisible = canOpen && SpriteLargePreviewImage.Source is not null;
        TexturePreviewImage.IsHitTestVisible = canOpen && TexturePreviewImage.Source is not null;
        TextureOpenPreviewButton.IsEnabled =
            canOpen &&
            TexturePreviewPanel.Visibility == Visibility.Visible &&
            TexturePreviewImage.Source is not null;
    }

    private static IEnumerable<EditablePropertyRow> BuildEditableProperties(object value)
    {
        foreach (PropertyInfo property in GetEditableProperties(value.GetType()))
        {
            object? currentValue;
            try
            {
                currentValue = property.GetValue(value);
            }
            catch
            {
                continue;
            }

            yield return new EditablePropertyRow(value, property, property.Name, FormatEditableValue(currentValue));
        }
    }

    private static PropertyInfo[] GetEditableProperties(Type type)
    {
        return EditablePropertiesByType.GetOrAdd(type, static currentType =>
            currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                       .Where(IsEditableProperty)
                       .ToArray());
    }

    private static bool IsEditableProperty(PropertyInfo property)
    {
        if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length != 0)
            return false;

        if (property.Name is "Name" or "Content")
            return false;

        Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        return IsEditableScalarType(type);
    }

    private static bool IsEditableScalarType(Type type)
    {
        return type.IsEnum ||
               type == typeof(string) ||
               type == typeof(bool) ||
               type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(short) ||
               type == typeof(ushort) ||
               type == typeof(int) ||
               type == typeof(uint) ||
               type == typeof(long) ||
               type == typeof(ulong) ||
               type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(decimal);
    }

    private static string FormatEditableValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static object? ParseEditableValue(string value, Type targetType)
    {
        Type type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (Nullable.GetUnderlyingType(targetType) is not null && string.IsNullOrWhiteSpace(value))
            return null;

        if (type == typeof(string))
            return value;

        if (type == typeof(bool))
            return bool.Parse(value);

        if (type.IsEnum)
            return Enum.Parse(type, value, ignoreCase: true);

        return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
    }

    private sealed record LoadedGame(
        UndertaleData Data,
        string GameName,
        IReadOnlyList<ResourceCategory> Categories,
        string Status);

    private sealed record TexturePreviewPerfMetrics(
        int SampleCount,
        int TexturePageItemCount,
        int EmbeddedTextureCount,
        double FirstPassMs,
        double SecondPassMs,
        long Bytes);

    private sealed record RoomTilePalettePreviewPerfMetrics(
        int SampleCount,
        int BackgroundCount,
        double FirstPassMs,
        double SecondPassMs,
        long Bytes);

    private sealed record RoomTilePreviewPerfMetrics(
        int SampleCount,
        int RoomCount,
        double FirstPassMs,
        double SecondPassMs,
        long Bytes);

    private sealed record RoomTileCellEditorItem(int X, int Y, uint Value)
    {
        public string Coordinates => $"{X},{Y}";

        public string ValueText => Value.ToString(CultureInfo.InvariantCulture);

        public string Detail
        {
            get
            {
                if (Value == 0)
                    return "empty";

                uint index = Value & RoomTileIndexMask;
                uint flags = (Value & RoomTileFlagsMask) >> 28;
                return flags == 0 ? $"index {index}" : $"index {index}, flags {flags}";
            }
        }
    }

    private sealed class RoomTilePaletteEditorItem : INotifyPropertyChanged
    {
        private bool _previewRequested;
        private ImageSource? _previewSource;
        private string _previewStatus = "preview pending";

        public RoomTilePaletteEditorItem(int index, uint value)
        {
            Index = index;
            Value = value;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Index { get; }

        public uint Value { get; }

        public ImageSource? PreviewSource
        {
            get => _previewSource;
            private set
            {
                if (ReferenceEquals(_previewSource, value))
                    return;

                _previewSource = value;
                OnPropertyChanged(nameof(PreviewSource));
            }
        }

        public string PreviewStatus
        {
            get => _previewStatus;
            private set
            {
                if (string.Equals(_previewStatus, value, StringComparison.Ordinal))
                    return;

                _previewStatus = value;
                OnPropertyChanged(nameof(PreviewStatus));
            }
        }

        public string Title => $"Tile {Index}";

        public string ValueText => Value.ToString(CultureInfo.InvariantCulture);

        public string Detail
        {
            get
            {
                uint tileIndex = Value & RoomTileIndexMask;
                uint flags = (Value & RoomTileFlagsMask) >> 28;
                return flags == 0 ? $"index {tileIndex}" : $"index {tileIndex}, flags {flags}";
            }
        }

        public bool TryBeginPreviewLoad()
        {
            if (_previewRequested)
                return false;

            _previewRequested = true;
            PreviewStatus = "loading preview";
            return true;
        }

        public void SetPreviewDeferred()
        {
            if (!_previewRequested)
                PreviewStatus = "select to preview";
        }

        public void SetPreview(ImageSource? source, string status)
        {
            PreviewSource = source;
            PreviewStatus = status;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    private sealed record ResourceCategory(string Label, Symbol IconSymbol, int Count, IReadOnlyList<ResourceItem> Items, IList? SourceList)
    {
        public string CountText => Count < 0 ? "-" : Count.ToString(CultureInfo.InvariantCulture);
    }

    private sealed class NullResourcePlaceholder
    {
        public static NullResourcePlaceholder Instance { get; } = new();

        private NullResourcePlaceholder()
        {
        }
    }

    private sealed class ResourceItem(
        int index,
        string title,
        string subtitle,
        Symbol iconSymbol,
        object value) : INotifyPropertyChanged
    {
        private string _title = title;
        private string _subtitle = subtitle;
        private Symbol _iconSymbol = iconSymbol;

        public int Index { get; } = index;

        public string Title
        {
            get => _title;
            set
            {
                if (_title == value)
                    return;

                _title = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }

        public string Subtitle
        {
            get => _subtitle;
            set
            {
                if (_subtitle == value)
                    return;

                _subtitle = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Subtitle)));
            }
        }

        public Symbol IconSymbol
        {
            get => _iconSymbol;
            set
            {
                if (_iconSymbol == value)
                    return;

                _iconSymbol = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconSymbol)));
            }
        }

        public object Value { get; } = value;

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed record RecentFileItem(string Title, string Path, string Status);

    private sealed record ClosedResourceTab(string CategoryLabel, int ItemIndex, string Title, Symbol IconSymbol);

    private sealed record ResourceNavigationEntry(string CategoryLabel, int ItemIndex);

    private sealed class ResourceTab(string categoryLabel, int itemIndex, string title, Symbol iconSymbol) : INotifyPropertyChanged
    {
        private int _itemIndex = itemIndex;
        private string _title = title;
        private Symbol _iconSymbol = iconSymbol;

        public string CategoryLabel { get; } = categoryLabel;

        public int ItemIndex
        {
            get => _itemIndex;
            set
            {
                if (_itemIndex == value)
                    return;

                _itemIndex = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ItemIndex)));
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                if (_title == value)
                    return;

                _title = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }

        public Symbol IconSymbol
        {
            get => _iconSymbol;
            set
            {
                if (_iconSymbol == value)
                    return;

                _iconSymbol = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconSymbol)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed record DetailRow(string Label, string Value);

    private interface ICodeSearchResult
    {
        int CodeIndex { get; }

        string CodeName { get; }

        int LineNumber { get; }

        bool Decompiled { get; }

        string SearchText { get; }

        string Title { get; }

        string Preview { get; }
    }

    private sealed record CodeSearchResult(
        int CodeIndex,
        string CodeName,
        int LineNumber,
        string LinePreview,
        bool Decompiled) : ICodeSearchResult
    {
        public string Title => $"{CodeName} - line {LineNumber}";
        public string Preview => LinePreview;
        public string SearchText => string.Empty;
    }

    private sealed record ScriptCodeSearchResult(
        int CodeIndex,
        string CodeName,
        int LineNumber,
        string LinePreview,
        bool Decompiled,
        string Query) : ICodeSearchResult
    {
        public string Title => $"{CodeName} - line {LineNumber}";
        public string Preview => LinePreview;
        public string SearchText => Query.Length == 0 ? LinePreview : Query;
    }

    private sealed record StringSearchResult(int StringIndex, string Value)
    {
        public string Title => $"String #{StringIndex}";
        public string Preview => Value;
    }

    private sealed record ProjectAssetSummary(string Name, string AssetType, IProjectAsset Asset)
    {
        public string DisplayText => $"{AssetType}: {Name}";
    }

    private sealed record ReferenceTypeOption(Type Type, string Label);

    private sealed record ReferenceSearchResult(string Group, string Title, object Value)
    {
        public string DisplayText => $"{Group}: {Title}";
    }

    private sealed record ExternalAudioGroupCacheEntry(string Path, UndertaleData Data);

    private sealed record SoundAudioSource(
        UndertaleEmbeddedAudio? EmbeddedAudio,
        string? FilePath,
        int GroupId,
        int AudioIndex,
        string SourceLabel,
        string? Error)
    {
        public static SoundAudioSource FromError(string message) => new(null, null, -1, -1, string.Empty, message);
    }

    private sealed record RuntimeSettings(string GameMakerStudioPath, string GameMakerStudio2RuntimesPath);

    private sealed record RuntimeCandidate(string Version, string Path, string? DebuggerPath)
    {
        public string DisplayText => DebuggerPath is null
            ? $"{Version} - {Path}"
            : $"{Version} - {Path} (debugger available)";
    }

    private sealed record SpriteFrameItem(int Index, string Title, string TextureName)
    {
        public override string ToString() => $"{Title} - {TextureName}";
    }

    private sealed record SpriteTextureSummary(int Index, string Title, string Subtitle);

    private sealed record SpriteMaskSummary(int Index, string Title, string Subtitle);

    private sealed record ObjectEventSummary(
        string Title,
        string Subtitle,
        EventType EventType,
        int EventTypeIndex,
        int EventIndex,
        UndertaleGameObject.Event Event,
        UndertaleCode? FirstCode);

    private sealed record ObjectEventActionSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleGameObject.EventAction Action);

    private sealed record RoomInstanceSummary(
        string Title,
        string Subtitle,
        string PreviewLabel,
        UndertaleRoom.GameObject Instance,
        UndertaleGameObject? ObjectDefinition,
        UndertaleCode? CreationCode);

    private sealed record RoomBackgroundSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleRoom.Background Background);

    private sealed record RoomTileSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleRoom.Tile Tile,
        UndertaleRoom.Layer? Layer);

    private sealed record RoomViewSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleRoom.View View);

    private sealed record RoomPreviewUndoState(object Instance, int X, int Y, UndertaleRoom.Layer? Layer);

    private sealed record RoomPreviewInstanceMarker(RoomInstanceSummary Summary, double Scale);

    private sealed class RoomPreviewDragState(
        FrameworkElement marker,
        RoomInstanceSummary summary,
        uint pointerId,
        double scale,
        double previewWidth,
        double previewHeight,
        Point startPointer,
        int startX,
        int startY)
    {
        public FrameworkElement Marker { get; } = marker;
        public RoomInstanceSummary Summary { get; } = summary;
        public uint PointerId { get; } = pointerId;
        public double Scale { get; } = scale;
        public double PreviewWidth { get; } = previewWidth;
        public double PreviewHeight { get; } = previewHeight;
        public Point StartPointer { get; } = startPointer;
        public int StartX { get; } = startX;
        public int StartY { get; } = startY;
        public bool HasMoved { get; set; }
    }

    private sealed record RoomPreviewAssetMarker(
        UndertaleRoom.Layer Layer,
        object Instance,
        double Scale);

    private sealed record RoomPreviewViewMarker(RoomViewSummary Summary, double Scale);

    private sealed class RoomPreviewViewDragState(
        FrameworkElement marker,
        RoomViewSummary summary,
        uint pointerId,
        double scale,
        Point startPointer,
        int startX,
        int startY,
        double startCanvasLeft,
        double startCanvasTop)
    {
        public FrameworkElement Marker { get; } = marker;
        public RoomViewSummary Summary { get; } = summary;
        public uint PointerId { get; } = pointerId;
        public double Scale { get; } = scale;
        public Point StartPointer { get; } = startPointer;
        public int StartX { get; } = startX;
        public int StartY { get; } = startY;
        public double StartCanvasLeft { get; } = startCanvasLeft;
        public double StartCanvasTop { get; } = startCanvasTop;
        public bool HasMoved { get; set; }
    }

    private sealed class RoomPreviewAssetDragState(
        FrameworkElement marker,
        UndertaleRoom.Layer layer,
        object instance,
        uint pointerId,
        double scale,
        double previewWidth,
        double previewHeight,
        Point startPointer,
        int startX,
        int startY,
        double startCanvasLeft,
        double startCanvasTop,
        bool wasCopied)
    {
        public FrameworkElement Marker { get; } = marker;
        public UndertaleRoom.Layer Layer { get; } = layer;
        public object Instance { get; } = instance;
        public uint PointerId { get; } = pointerId;
        public double Scale { get; } = scale;
        public double PreviewWidth { get; } = previewWidth;
        public double PreviewHeight { get; } = previewHeight;
        public Point StartPointer { get; } = startPointer;
        public int StartX { get; } = startX;
        public int StartY { get; } = startY;
        public double StartCanvasLeft { get; } = startCanvasLeft;
        public double StartCanvasTop { get; } = startCanvasTop;
        public bool WasCopied { get; } = wasCopied;
        public bool HasMoved { get; set; }
    }

    private sealed record RoomPreviewSpriteRequest(
        Image Image,
        UndertaleTexturePageItem Texture);

    private sealed record RoomPreviewAssetSpriteSummary(
        UndertaleRoom.Layer Layer,
        UndertaleRoom.SpriteInstance Instance,
        double X,
        double Y,
        double Width,
        double Height,
        double ScaleX,
        double ScaleY,
        double OppositeRotation,
        double TransformCenterX,
        double TransformCenterY,
        double Opacity,
        UndertaleTexturePageItem Texture);

    private readonly record struct RoomPreviewParticleBounds(
        double X,
        double Y,
        double Width,
        double Height);

    private sealed record RoomPreviewSequenceSummary(
        UndertaleRoom.Layer Layer,
        UndertaleRoom.SequenceInstance Instance,
        double X,
        double Y,
        double Width,
        double Height,
        double OriginX,
        double OriginY,
        double ScaleX,
        double ScaleY,
        double OppositeRotation,
        double Opacity);

    private sealed record RoomPreviewParticleSummary(
        UndertaleRoom.Layer Layer,
        UndertaleRoom.ParticleSystemInstance Instance,
        double X,
        double Y,
        double ScaleX,
        double ScaleY,
        double OppositeRotation,
        double Opacity,
        double BoundsX,
        double BoundsY,
        double BoundsWidth,
        double BoundsHeight);

    private sealed record RoomPreviewImageRequest(
        Image Image,
        UndertaleTexturePageItem Texture);

    private sealed record RoomPreviewTileRequest(
        Image Image,
        RoomPreviewTileKey TileKey);

    private sealed record RoomPreviewTileKey(
        UndertaleTexturePageItem Texture,
        int SourceX,
        int SourceY,
        int Width,
        int Height,
        uint Transform);

    private sealed record RoomPreviewTileSummary(
        double X,
        double Y,
        double Width,
        double Height,
        RoomPreviewTileKey TileKey);

    private sealed record RoomPreviewBackgroundSummary(
        double X,
        double Y,
        double Width,
        double Height,
        bool TiledHorizontally,
        bool TiledVertically,
        bool Foreground,
        UndertaleTexturePageItem Texture);

    private sealed record BackgroundTileIdSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleBackground.TileID TileId);

    private sealed record RoomLayerSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleRoom.Layer Layer);

    private sealed record RoomInstanceLayerItem(string Title, UndertaleRoom.Layer Layer);

    private sealed record RoomLayerAssetSpriteInstanceSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleRoom.SpriteInstance Instance);

    private sealed record RoomLayerAssetSequenceInstanceSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleRoom.SequenceInstance Instance);

    private sealed record RoomLayerAssetParticleInstanceSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleRoom.ParticleSystemInstance Instance);

    private sealed record PathPointSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertalePath.PathPoint Point);

    private sealed record PathPreviewPointMarker(
        UndertalePath.PathPoint Point,
        float MinX,
        float MinY,
        double Scale,
        double Padding)
    {
        public double MapX(UndertalePath.PathPoint point) => Padding + (point.X - MinX) * Scale;

        public double MapY(UndertalePath.PathPoint point) => Padding + (point.Y - MinY) * Scale;
    }

    private sealed class PathPreviewDragState(
        FrameworkElement marker,
        PathPreviewPointMarker markerData,
        uint pointerId,
        Point startPointer,
        float startX,
        float startY)
    {
        public FrameworkElement Marker { get; } = marker;
        public PathPreviewPointMarker MarkerData { get; } = markerData;
        public uint PointerId { get; } = pointerId;
        public Point StartPointer { get; } = startPointer;
        public float StartX { get; } = startX;
        public float StartY { get; } = startY;
        public bool HasMoved { get; set; }
    }

    private sealed record CodeLocalSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleCodeLocals.LocalVar Local);

    private sealed record ResourceReferenceState(
        string Title,
        string Summary,
        string CategoryLabel,
        int CurrentIndex,
        object? CurrentResource,
        bool CanClear,
        IReadOnlyList<ResourceReferenceOption> Options);

    private sealed record ResourceReferenceOption(
        int Index,
        string Title,
        object Resource)
    {
        public override string ToString() => Title;
    }

    private sealed record RoomReferenceOption(
        int Index,
        string Title,
        UndertaleRoom Room)
    {
        public override string ToString() => Title;
    }

    private enum GeneralInfoOptionTextureSlot
    {
        BackImage,
        FrontImage,
        LoadImage
    }

    private sealed record GeneralInfoRoomOrderSummary(
        int OrderIndex,
        string Title,
        string Subtitle,
        int RoomIndex,
        UndertaleRoom? Room);

    private sealed record GeneralInfoTextureSlotOption(
        GeneralInfoOptionTextureSlot Slot,
        string Label,
        string Subtitle,
        UndertaleTexturePageItem? Texture)
    {
        public string Title => $"{Label}: {Subtitle}";

        public override string ToString() => Title;
    }

    private sealed record TexturePageItemOption(
        int Index,
        string Title,
        UndertaleTexturePageItem Texture)
    {
        public override string ToString() => Title;
    }

    private sealed record GeneralInfoConstantSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleOptions.Constant Constant);

    private sealed record GeneralInfoLanguageSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleLanguage.LanguageData Language);

    private enum TextureGroupSectionKind
    {
        TexturePages,
        Sprites,
        SpineSprites,
        Fonts,
        Tilesets
    }

    private sealed record TextureGroupSectionOption(
        TextureGroupSectionKind Kind,
        string Label,
        string CategoryLabel)
    {
        public override string ToString() => Label;
    }

    private sealed record TextureGroupResourceOption(
        TextureGroupSectionKind SectionKind,
        int Index,
        string Title,
        UndertaleResource Resource)
    {
        public override string ToString() => Title;
    }

    private sealed record TextureGroupEntrySummary(
        TextureGroupSectionKind SectionKind,
        string Section,
        int SectionIndex,
        string Title,
        string Subtitle,
        string CategoryLabel,
        int ResourceIndex,
        UndertaleResource? Resource);

    private sealed record FontGlyphSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleTexturePageItem? Texture,
        UndertaleFont.Glyph Glyph);

    private sealed record FontKerningSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleFont.Glyph.GlyphKerning Kerning);

    private sealed record TexturePageItemReferenceItem(
        string Title,
        string Subtitle,
        string CategoryLabel,
        int ItemIndex,
        int SpriteFrameIndex);

    private sealed record ShaderSourceItem(string Title, UndertaleString Source)
    {
        public override string ToString() => Title;
    }

    private sealed record ShaderAttributeSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleShader.VertexShaderAttribute Attribute);

    private sealed record TimelineMomentSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleTimeline.UndertaleTimelineMoment Moment);

    private sealed record TimelineActionSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleGameObject.EventAction Action);

    private sealed record ObjectPhysicsVertexSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleGameObject.UndertalePhysicsVertex Vertex);

    private sealed record CodeReferenceItem(int Index, string Title, UndertaleCode? Code)
    {
        public override string ToString() => Title;
    }

    private sealed record ObjectReferenceItem(int Index, string Title, UndertaleGameObject? Object)
    {
        public override string ToString() => Title;
    }

    private sealed record BackgroundReferenceItem(int Index, string Title, UndertaleBackground? Background)
    {
        public override string ToString() => Title;
    }

    private sealed record ExtensionFileSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleExtensionFile File);

    private sealed record ExtensionFunctionSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleExtensionFunction Function);

    private sealed record ExtensionArgumentSummary(
        int Index,
        string Title,
        UndertaleExtensionFunctionArg Argument);

    private sealed record ExtensionOptionSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleExtensionOption Option);

    private sealed record ParticleEmitterSummary(
        int Index,
        string Title,
        string Subtitle,
        UndertaleParticleSystemEmitter? Emitter);

    private sealed record SpriteReferenceItem(
        int Index,
        string Title,
        UndertaleSprite? Sprite)
    {
        public override string ToString() => Title;
    }

    private sealed record SequenceReferenceItem(
        int Index,
        string Title,
        UndertaleSequence? Sequence)
    {
        public override string ToString() => Title;
    }

    private sealed record ParticleSystemReferenceItem(
        int Index,
        string Title,
        UndertaleParticleSystem? ParticleSystem)
    {
        public override string ToString() => Title;
    }

    private sealed record ParticleEmitterReferenceItem(
        int Index,
        string Title,
        UndertaleParticleSystemEmitter? Emitter)
    {
        public override string ToString() => Title;
    }

    private sealed record StringExportEntry(int Index, string Content);

    private sealed class EditablePropertyRow(object owner, PropertyInfo property, string label, string value)
    {
        public object Owner { get; } = owner;
        public PropertyInfo Property { get; } = property;
        public string Label { get; } = label;
        public string Value { get; set; } = value;
    }

    private void ShowCustomSpriteEditorFor(ResourceItem item)
    {
        if (item.Value is not UndertaleSprite sprite)
        {
            CustomSpriteEditorPanel.Visibility = Visibility.Collapsed;
            return;
        }

        bool sameSprite = ReferenceEquals(_activeSprite, sprite);
        int selectedFrameIndex = sameSprite && SpriteFrameComboBox.SelectedItem is SpriteFrameItem selectedFrame
            ? selectedFrame.Index
            : 0;
        int selectedMaskIndex = sameSprite && SpriteMasksList.SelectedItem is SpriteMaskSummary selectedMask
            ? selectedMask.Index
            : 0;

        _activeSprite = sprite;
        CustomSpriteEditorPanel.Visibility = Visibility.Visible;
        UpdateSpriteEditorEditability();

        SpriteEditorTitleText.Text = FormatNamedResourceTitle(sprite.Name?.Content, "Sprite", item.Index);
        SpriteEditorSubtitleText.Text = $"UndertaleSprite - ID {item.Index}";

        SelectorBarItem? initialMode = sameSprite
            ? _lastSpriteEditorMode ?? SpriteEditorSelectorBar.SelectedItem ?? MetadataTab
            : GetDefaultSpriteEditorMode(_lastSpriteEditorMode ?? SpriteEditorSelectorBar.SelectedItem);
        SelectSpriteEditorMode(initialMode);

        SpriteTexturesList.ItemsSource = BuildSpriteTextureSummaries(sprite);
        SpriteMasksList.ItemsSource = BuildSpriteMaskSummaries(sprite);
        SpriteFrameComboBox.ItemsSource = BuildSpriteFrameItems(sprite);
        SpriteMasksList.SelectedIndex = sprite.CollisionMasks.Count > 0
            ? Math.Clamp(selectedMaskIndex, 0, sprite.CollisionMasks.Count - 1)
            : -1;
        UpdateSpriteWorkspaceSummaries(sprite);

        _isUpdatingSpriteFrame = true;
        SpriteFrameComboBox.SelectedIndex = sprite.Textures.Count > 0
            ? Math.Clamp(selectedFrameIndex, 0, sprite.Textures.Count - 1)
            : -1;
        _isUpdatingSpriteFrame = false;
        UpdateSpriteFrameNavigationState();

        LoadSpriteProperties(sprite);
        RenderOrResetSpritePreview(sprite);
    }

    private void HideCustomSpriteEditor()
    {
        _activeSprite = null;
        _isSpritePreviewRendered = false;
        CustomSpriteEditorPanel.Visibility = Visibility.Collapsed;
        SpriteTexturesList.ItemsSource = null;
        SpriteMasksList.ItemsSource = null;
        SpriteFrameComboBox.ItemsSource = null;
        SpriteFrameComboBox.SelectedIndex = -1;
        SpriteNameBox.Text = string.Empty;
        SpriteFramesSummaryText.Text = string.Empty;
        SpriteCollisionSummaryText.Text = string.Empty;
        SpriteExportSummaryText.Text = string.Empty;
        SpriteExportCurrentFrameText.Text = string.Empty;
        ClearSpritePreviewDetails();
    }

    private void UpdateSpriteEditorEditability()
    {
        bool canEdit = _data is not null && !_data.UnsupportedBytecodeVersion;

        TextBox[] editableTextBoxes =
        [
            SpriteNameBox,
            SpriteWidthBox,
            SpriteHeightBox,
            SpriteMarginLeftBox,
            SpriteMarginRightBox,
            SpriteMarginBottomBox,
            SpriteMarginTopBox,
            SpriteOriginXBox,
            SpriteOriginYBox,
            SpriteOriginXWrapperBox,
            SpriteOriginYWrapperBox,
            SpriteBBoxModeBox,
            SpriteGMS2PlaybackSpeedBox,
            SpriteSpineVersionBox,
            SpriteSpineCacheVersionBox,
            SpriteSpineJSONBox,
            SpriteSpineAtlasBox,
            SpriteSWFVersionBox,
            SpriteVectorVersionBox,
            SpriteVectorCollisionMaskWidthBox,
            SpriteVectorCollisionMaskHeightBox,
            SpriteSVersionBox
        ];

        foreach (TextBox textBox in editableTextBoxes)
            textBox.IsReadOnly = !canEdit;

        ComboBox[] editableComboBoxes =
        [
            SpriteTransparentBox,
            SpriteSmoothBox,
            SpriteSepMasksBox,
            SpriteSSpriteTypeBox,
            SpriteGMS2PlaybackSpeedTypeBox,
            SpriteIsSpecialTypeBox,
            SpriteSpineHasTextureDataBox
        ];

        foreach (ComboBox comboBox in editableComboBoxes)
            comboBox.IsEnabled = canEdit;

        SpritePreloadBox.IsEnabled = canEdit;
        SpriteBlendModeBox.IsEnabled = false;
    }

    private void LoadSpriteProperties(UndertaleSprite sprite)
    {
        _isUpdatingSpriteProperties = true;

        try
        {
            SpriteNameBox.Text = sprite.Name?.Content ?? string.Empty;
            SpriteWidthBox.Text = sprite.Width.ToString();
            SpriteHeightBox.Text = sprite.Height.ToString();
            SpriteMarginLeftBox.Text = sprite.MarginLeft.ToString();
            SpriteMarginRightBox.Text = sprite.MarginRight.ToString();
            SpriteMarginBottomBox.Text = sprite.MarginBottom.ToString();
            SpriteMarginTopBox.Text = sprite.MarginTop.ToString();

            SpriteTransparentBox.SelectedIndex = sprite.Transparent ? 1 : 0;
            SpriteSmoothBox.SelectedIndex = sprite.Smooth ? 1 : 0;
            SpritePreloadBox.IsChecked = sprite.Preload;

            SpriteOriginXBox.Text = sprite.OriginX.ToString();
            SpriteOriginYBox.Text = sprite.OriginY.ToString();
            SpriteOriginXWrapperBox.Text = sprite.OriginXWrapper.ToString();
            SpriteOriginYWrapperBox.Text = sprite.OriginYWrapper.ToString();

            SpriteSepMasksBox.SelectedIndex = Math.Clamp((int)sprite.SepMasks, 0, 2);
            SpriteBBoxModeBox.Text = sprite.BBoxMode.ToString();

            SpriteSVersionBox.Text = sprite.SVersion.ToString();
            SpriteSSpriteTypeBox.SelectedIndex = Math.Clamp((int)sprite.SSpriteType, 0, 3);
            SpriteGMS2PlaybackSpeedBox.Text = sprite.GMS2PlaybackSpeed.ToString();
            SpriteGMS2PlaybackSpeedTypeBox.SelectedIndex = Math.Clamp((int)sprite.GMS2PlaybackSpeedType, 0, 1);
            SpriteIsSpecialTypeBox.SelectedIndex = sprite.IsSpecialType ? 1 : 0;

            SpriteSpineVersionBox.Text = sprite.SpineVersion.ToString();
            SpriteSpineCacheVersionBox.Text = sprite.SpineCacheVersion.ToString();
            SpriteSpineJSONBox.Text = sprite.SpineJSON ?? string.Empty;
            SpriteSpineAtlasBox.Text = sprite.SpineAtlas ?? string.Empty;
            SpriteSpineHasTextureDataBox.SelectedIndex = sprite.SpineHasTextureData ? 1 : 0;

            SpriteSWFVersionBox.Text = sprite.SWFVersion.ToString();
            SpriteVectorVersionBox.Text = sprite.VectorVersion.ToString();
            SpriteVectorCollisionMaskWidthBox.Text = sprite.VectorCollisionMaskWidth.ToString();
            SpriteVectorCollisionMaskHeightBox.Text = sprite.VectorCollisionMaskHeight.ToString();

            // Populate Overview Stats
            OverviewSpriteNameText.Text = FormatNamedResourceTitle(sprite.Name?.Content, "Sprite", _activeSprite is null || _data is null ? 0 : _data.Sprites.IndexOf(_activeSprite));
            OverviewDimensionsText.Text = $"{sprite.Width} x {sprite.Height} px";
            OverviewFramesCountText.Text = $"{sprite.Textures.Count} frame(s)";
            OverviewCollisionCountText.Text = $"{sprite.CollisionMasks.Count} mask(s)";
            OverviewOriginText.Text = $"({sprite.OriginX}, {sprite.OriginY})";
            OverviewTypeText.Text = sprite.SSpriteType.ToString();
            UpdateSpriteWorkspaceSummaries(sprite);
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Error loading sprite properties: {ex.Message}";
        }

        _isUpdatingSpriteProperties = false;
    }

    private void SpritePropertyBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingSpriteProperties || _activeSprite is null || sender is not TextBox textBox || textBox.Tag is not string propertyName)
            return;

        try
        {
            switch (propertyName)
            {
                case "Width":
                    if (uint.TryParse(textBox.Text, out uint width)) _activeSprite.Width = width;
                    break;
                case "Height":
                    if (uint.TryParse(textBox.Text, out uint height)) _activeSprite.Height = height;
                    break;
                case "MarginLeft":
                    if (int.TryParse(textBox.Text, out int ml)) _activeSprite.MarginLeft = ml;
                    break;
                case "MarginRight":
                    if (int.TryParse(textBox.Text, out int mr)) _activeSprite.MarginRight = mr;
                    break;
                case "MarginBottom":
                    if (int.TryParse(textBox.Text, out int mb)) _activeSprite.MarginBottom = mb;
                    break;
                case "MarginTop":
                    if (int.TryParse(textBox.Text, out int mt)) _activeSprite.MarginTop = mt;
                    break;
                case "OriginX":
                    if (int.TryParse(textBox.Text, out int ox)) _activeSprite.OriginX = ox;
                    break;
                case "OriginY":
                    if (int.TryParse(textBox.Text, out int oy)) _activeSprite.OriginY = oy;
                    break;
                case "OriginXWrapper":
                    if (int.TryParse(textBox.Text, out int oxw)) _activeSprite.OriginXWrapper = oxw;
                    break;
                case "OriginYWrapper":
                    if (int.TryParse(textBox.Text, out int oyw)) _activeSprite.OriginYWrapper = oyw;
                    break;
                case "BBoxMode":
                    if (uint.TryParse(textBox.Text, out uint bbox)) _activeSprite.BBoxMode = bbox;
                    break;
                case "SVersion":
                    if (uint.TryParse(textBox.Text, out uint sversion)) _activeSprite.SVersion = sversion;
                    break;
                case "GMS2PlaybackSpeed":
                    if (float.TryParse(textBox.Text, out float speed)) _activeSprite.GMS2PlaybackSpeed = speed;
                    break;
                case "SpineVersion":
                    if (int.TryParse(textBox.Text, out int sv)) _activeSprite.SpineVersion = sv;
                    break;
                case "SpineCacheVersion":
                    if (int.TryParse(textBox.Text, out int scv)) _activeSprite.SpineCacheVersion = scv;
                    break;
                case "SpineJSON":
                    _activeSprite.SpineJSON = textBox.Text;
                    break;
                case "SpineAtlas":
                    _activeSprite.SpineAtlas = textBox.Text;
                    break;
                case "SWFVersion":
                    if (int.TryParse(textBox.Text, out int swfv)) _activeSprite.SWFVersion = swfv;
                    break;
                case "VectorVersion":
                    if (int.TryParse(textBox.Text, out int vv)) _activeSprite.VectorVersion = vv;
                    break;
                case "VectorCollisionMaskWidth":
                    if (int.TryParse(textBox.Text, out int vcmw)) _activeSprite.VectorCollisionMaskWidth = vcmw;
                    break;
                case "VectorCollisionMaskHeight":
                    if (int.TryParse(textBox.Text, out int vcmh)) _activeSprite.VectorCollisionMaskHeight = vcmh;
                    break;
            }

            MarkDirty();
            RefreshSpriteEditorAfterEdit();
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to update {propertyName}: {ex.Message}";
        }
    }

    private void SpriteNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        CommitSpriteNameEdit();
    }

    private void SpriteNameBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_activeSprite is null)
            return;

        if (e.Key == VirtualKey.Enter)
        {
            CommitSpriteNameEdit();
            e.Handled = true;
            return;
        }

        if (e.Key == VirtualKey.Escape)
        {
            _isUpdatingSpriteProperties = true;
            SpriteNameBox.Text = _activeSprite.Name?.Content ?? string.Empty;
            _isUpdatingSpriteProperties = false;
            e.Handled = true;
        }
    }

    private void CommitSpriteNameEdit()
    {
        if (_isUpdatingSpriteProperties || _activeSprite is null || _activeSprite.Name is null)
            return;

        string newName = SpriteNameBox.Text;
        if (newName == (_activeSprite.Name.Content ?? string.Empty))
            return;

        _activeSprite.Name.Content = newName;
        int spriteIndex = _data?.Sprites.IndexOf(_activeSprite) ?? 0;
        SpriteEditorTitleText.Text = FormatNamedResourceTitle(newName, "Sprite", spriteIndex);
        OverviewSpriteNameText.Text = SpriteEditorTitleText.Text;
        DetailsTitleText.Text = FormatTitle(newName);
        DetailsList.ItemsSource = _selectedResource is null ? null : BuildDetails(_selectedResource).ToArray();
        UpdateSpriteWorkspaceSummaries(_activeSprite);
        MarkDirty();
        RefreshSelectedResourceTitle();
        RenderOrResetSpritePreview(_activeSprite);
    }

    private void SpriteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingSpriteProperties || _activeSprite is null || sender is not ComboBox comboBox || comboBox.Tag is not string propertyName)
            return;

        int selectedIndex = comboBox.SelectedIndex;
        if (selectedIndex < 0)
            return;

        try
        {
            switch (propertyName)
            {
                case "Transparent":
                    _activeSprite.Transparent = selectedIndex == 1;
                    break;
                case "Smooth":
                    _activeSprite.Smooth = selectedIndex == 1;
                    break;
                case "SepMasks":
                    _activeSprite.SepMasks = (UndertaleSprite.SepMaskType)selectedIndex;
                    break;
                case "SSpriteType":
                    _activeSprite.SSpriteType = (UndertaleSprite.SpriteType)selectedIndex;
                    break;
                case "GMS2PlaybackSpeedType":
                    _activeSprite.GMS2PlaybackSpeedType = (AnimSpeedType)selectedIndex;
                    break;
                case "IsSpecialType":
                    _activeSprite.IsSpecialType = selectedIndex == 1;
                    break;
                case "SpineHasTextureData":
                    _activeSprite.SpineHasTextureData = selectedIndex == 1;
                    break;
            }

            MarkDirty();
            RefreshSpriteEditorAfterEdit();
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to update {propertyName}: {ex.Message}";
        }
    }

    private void SpriteCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingSpriteProperties || _activeSprite is null || sender is not CheckBox checkBox || checkBox.Tag is not string propertyName)
            return;

        try
        {
            switch (propertyName)
            {
                case "Preload":
                    _activeSprite.Preload = checkBox.IsChecked == true;
                    break;
            }

            MarkDirty();
            RefreshSpriteEditorAfterEdit();
        }
        catch (Exception ex)
        {
            StatusBox.Text = $"Failed to update {propertyName}: {ex.Message}";
        }
    }

    private void RefreshSpriteEditorAfterEdit()
    {
        if (_activeSprite is null)
            return;

        LoadSpriteProperties(_activeSprite);
        RefreshCurrentDetails();
        RenderOrResetSpritePreview(_activeSprite);
    }

    private void SpriteFramePrevButton_Click(object sender, RoutedEventArgs e)
    {
        if (SpriteFrameComboBox.Items.Count > 0)
        {
            int index = SpriteFrameComboBox.SelectedIndex;
            if (index > 0)
                SpriteFrameComboBox.SelectedIndex = index - 1;
            else
                SpriteFrameComboBox.SelectedIndex = SpriteFrameComboBox.Items.Count - 1;
        }
    }

    private void SpriteFrameNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (SpriteFrameComboBox.Items.Count > 0)
        {
            int index = SpriteFrameComboBox.SelectedIndex;
            if (index < SpriteFrameComboBox.Items.Count - 1)
                SpriteFrameComboBox.SelectedIndex = index + 1;
            else
                SpriteFrameComboBox.SelectedIndex = 0;
        }
    }

    private void SpriteOpenTextureButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data is null || _selectedResource?.Value is not UndertaleSprite sprite)
            return;

        int frameIndex = SpriteFrameComboBox.SelectedItem is SpriteFrameItem frame ? frame.Index : 0;
        if (!TryGetSpriteFrameTexture(sprite, frameIndex, out UndertaleTexturePageItem? texture) || texture is null)
            return;

        int textureIndex = _data.TexturePageItems.IndexOf(texture);
        if (textureIndex >= 0)
            NavigateToResource("Texture page items", textureIndex);
    }

    private void SpriteTexturesList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not SpriteTextureSummary frame || frame.Index < 0 || SpriteFrameComboBox.Items.Count == 0)
            return;

        SpriteFrameComboBox.SelectedIndex = Math.Clamp(frame.Index, 0, SpriteFrameComboBox.Items.Count - 1);
    }

    private void SpriteMasksList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not SpriteMaskSummary mask || mask.Index < 0)
            return;

        if (SpriteFrameComboBox.Items.Count > 0)
            SpriteFrameComboBox.SelectedIndex = Math.Clamp(mask.Index, 0, SpriteFrameComboBox.Items.Count - 1);

        StatusBox.Text = $"Selected sprite collision mask {mask.Index}.";
    }

    private void SpriteEditorSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (_isUpdatingSpriteViewMode)
            return;

        SelectSpriteEditorMode(sender.SelectedItem);
    }

    private void SelectSpriteEditorMode(SelectorBarItem? selectedItem)
    {
        if (selectedItem is null)
            return;

        _isUpdatingSpriteViewMode = true;
        _lastSpriteEditorMode = selectedItem;
        SpriteEditorSelectorBar.SelectedItem = selectedItem;

        SpriteOverviewTabPanel.Visibility = Visibility.Collapsed;
        SpriteMetadataTabPanel.Visibility = Visibility.Collapsed;
        SpritePreviewTabPanel.Visibility = Visibility.Collapsed;
        SpriteFramesTabPanel.Visibility = Visibility.Collapsed;
        SpriteCollisionTabPanel.Visibility = Visibility.Collapsed;
        SpriteExportTabPanel.Visibility = Visibility.Collapsed;

        if (selectedItem == OverviewTab)
            SpriteOverviewTabPanel.Visibility = Visibility.Visible;
        else if (selectedItem == MetadataTab)
            SpriteMetadataTabPanel.Visibility = Visibility.Visible;
        else if (selectedItem == PreviewTab)
            SpritePreviewTabPanel.Visibility = Visibility.Visible;
        else if (selectedItem == FramesTab)
            SpriteFramesTabPanel.Visibility = Visibility.Visible;
        else if (selectedItem == CollisionTab)
            SpriteCollisionTabPanel.Visibility = Visibility.Visible;
        else if (selectedItem == ExportTab)
            SpriteExportTabPanel.Visibility = Visibility.Visible;

        _isUpdatingSpriteViewMode = false;
    }

    private SelectorBarItem GetDefaultSpriteEditorMode(SelectorBarItem? requestedMode)
    {
        return requestedMode == ExportTab ? MetadataTab : requestedMode ?? MetadataTab;
    }

    private void UpdateSpritePreviewDetails(UndertaleSprite sprite, int frameIndex, UndertaleTexturePageItem texture)
    {
        int spriteIndex = _data?.Sprites.IndexOf(sprite) ?? 0;
        string spriteTitle = FormatNamedResourceTitle(sprite.Name?.Content, "Sprite", spriteIndex);
        SpritePreviewTitleText.Text = $"{spriteTitle} - Frame {frameIndex} / {sprite.Textures.Count}";
        SpritePreviewSourceSizeText.Text = $"{sprite.Width} x {sprite.Height}";
        SpritePreviewBoundsText.Text = $"{texture.BoundingWidth} x {texture.BoundingHeight}";
        SpritePreviewOriginText.Text = $"{sprite.OriginX}, {sprite.OriginY}";
        SpritePreviewTextureText.Text = texture.Name?.Content ?? $"PageItem {texture.TexturePage?.Name?.Content ?? ""}";
        SpritePreviewTextureSizeText.Text = $"{texture.SourceWidth} x {texture.SourceHeight}";

        int texturePageIndex = _data?.EmbeddedTextures.IndexOf(texture.TexturePage) ?? -1;
        SpritePreviewTextureIndexText.Text = texturePageIndex >= 0 ? texturePageIndex.ToString() : "-";
        SpriteTexturePageItemBox.Text = texture.Name?.Content ?? $"PageItem {texture.TexturePage?.Name?.Content ?? ""}";
        SpriteTextureInfoText.Text = $"PageItem {texture.TexturePage?.Name?.Content ?? ""}, {texture.BoundingWidth}x{texture.BoundingHeight}";
        SpriteExportCurrentFrameText.Text = $"{texture.Name?.Content ?? $"Frame {frameIndex}"}; {texture.SourceWidth}x{texture.SourceHeight} source, {texture.BoundingWidth}x{texture.BoundingHeight} bounds.";
        if (SpriteTexturesList.Items.Count > 0)
            SpriteTexturesList.SelectedIndex = Math.Clamp(frameIndex, 0, SpriteTexturesList.Items.Count - 1);
    }

    private void ClearSpritePreviewDetails()
    {
        SpritePreviewTitleText.Text = "No frame selected";
        SpritePreviewSourceSizeText.Text = "-";
        SpritePreviewBoundsText.Text = "-";
        SpritePreviewOriginText.Text = "-";
        SpritePreviewTextureText.Text = "-";
        SpritePreviewTextureSizeText.Text = "-";
        SpritePreviewTextureIndexText.Text = "-";
        SpriteTexturePageItemBox.Text = string.Empty;
        SpriteTextureInfoText.Text = string.Empty;
        SpriteExportCurrentFrameText.Text = "No frame selected.";
    }

    private void UpdateSpriteWorkspaceSummaries(UndertaleSprite sprite)
    {
        string frameText = sprite.Textures.Count == 1 ? "1 frame" : $"{sprite.Textures.Count} frames";
        string maskText = sprite.CollisionMasks.Count == 1 ? "1 mask" : $"{sprite.CollisionMasks.Count} masks";
        SpriteFramesSummaryText.Text = $"{frameText}, {sprite.Width}x{sprite.Height}, origin {sprite.OriginXWrapper},{sprite.OriginYWrapper}.";
        SpriteCollisionSummaryText.Text = $"{maskText}, bounds {sprite.MarginLeft},{sprite.MarginTop} to {sprite.MarginRight},{sprite.MarginBottom}.";
        int spriteIndex = _data?.Sprites.IndexOf(sprite) ?? 0;
        string spriteTitle = FormatNamedResourceTitle(sprite.Name?.Content, "Sprite", spriteIndex);
        SpriteExportSummaryText.Text = $"Export {frameText} from {spriteTitle} as PNG.";
        SpriteCollisionModeText.Text = sprite.SepMasks.ToString();
        SpriteCollisionBoundsText.Text = $"{sprite.MarginLeft},{sprite.MarginTop} - {sprite.MarginRight},{sprite.MarginBottom}";
        SpriteCollisionCountText.Text = sprite.CollisionMasks.Count.ToString();
    }
}
