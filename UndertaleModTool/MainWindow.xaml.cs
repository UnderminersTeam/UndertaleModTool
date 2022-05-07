using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.ModelsDebug;
using UndertaleModLib.Scripting;
using UndertaleModLib.Util;
using UndertaleModTool.Windows;
using System.IO.Pipes;
using Ookii.Dialogs.Wpf;

using ColorConvert = System.Windows.Media.ColorConverter;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Reflection.Metadata.Ecma335;
using System.Windows.Media.Imaging;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Runtime;
using SystemJson = System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Globalization;
using System.Windows.Controls.Primitives;

namespace UndertaleModTool
{
    public class Tab : INotifyPropertyChanged
    {
        public static readonly BitmapImage ClosedIcon = new(new Uri(@"/Resources/X.png", UriKind.RelativeOrAbsolute));
        public static readonly BitmapImage ClosedHoverIcon = new(new Uri(@"/Resources/X_Down.png", UriKind.RelativeOrAbsolute));

        // it's actually used, but when it will be compiled (see the note in "MainWindow" class)
        #pragma warning disable CS0067
        public event PropertyChangedEventHandler PropertyChanged;
        #pragma warning restore CS0067

        public object OpenedObject { get; set; }
        public string TabTitle { get; set; } = "Untitled";
        public int TabIndex { get; set; }
        public bool AutoClose { get; set; } = false;

        public Tab(object obj, int tabIndex, string tabTitle = null)
        {
            OpenedObject = obj;
            TabIndex = tabIndex;
            TabTitle = tabTitle ?? GetTitleForObject(obj);
            AutoClose = obj is DescriptionView;
        }

        public static string GetTitleForObject(object obj)
        {
            if (obj is null)
                return null;

            string title = null;

            if (obj is DescriptionView view)
            {
                if (view.Heading.Contains("Welcome"))
                {
                    title = "Welcome!";
                }
                else
                {
                    title = view.Heading;
                }
            }
            else if (obj is UndertaleNamedResource namedRes)
            {
                string content = namedRes.Name?.Content;

                string header = obj switch
                {
                    UndertaleAudioGroup => "Audio Group",
                    UndertaleSound => "Sound",
                    UndertaleSprite => "Sprite",
                    UndertaleBackground => "Background",
                    UndertalePath => "Path",
                    UndertaleScript => "Script",
                    UndertaleShader => "Shader",
                    UndertaleFont => "Font",
                    UndertaleTimeline => "Timeline",
                    UndertaleGameObject => "Game Object",
                    UndertaleRoom => "Room",
                    UndertaleExtension => "Extension",
                    UndertaleTexturePageItem => "Texture Page Item",
                    UndertaleCode => "Code",
                    UndertaleVariable => "Variable",
                    UndertaleFunction => "Function",
                    UndertaleCodeLocals => "Code Locals",
                    UndertaleEmbeddedTexture => "Embedded Texture",
                    UndertaleEmbeddedAudio => "Embedded Audio",
                    UndertaleTextureGroupInfo => "Texture Group Info",
                    UndertaleEmbeddedImage => "Embedded Image",
                    UndertaleSequence => "Sequence",
                    UndertaleAnimationCurve => "Animation Curve",
                    _ => null
                };

                if (header is not null)
                    title = header + " - " + content;
                else
                    Debug.WriteLine($"Could not handle type {obj.GetType()}");
            }
            else if (obj is UndertaleString)
            {
                title = "String - " + ((UndertaleString)obj).Content;
            }
            else if (obj is UndertaleChunkVARI)
            {
                title = "Variables Overview";
            }
            else if (obj is GeneralInfoEditor)
            {
                title = "General Info";
            }
            else if (obj is GlobalInitEditor)
            {
                title = "Global Init";
            }
            else if (obj is GameEndEditor)
            {
                title = "Game End";
            }
            else
            {
                Debug.WriteLine($"Could not handle type {obj.GetType()}");
            }

            return title;
        }

        public override string ToString()
        {
            // for ease of debugging
            return GetType().FullName + " - {" + OpenedObject?.ToString() + '}';
        }
    }
    public class TabTitleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return Tab.GetTitleForObject(values[0]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        /// Note for those who don't know what is "PropertyChanged.Fody" -
        /// it automatically adds "OnPropertyChanged()" to every property (or modify existing) of the class that implements INotifyPropertyChanged.
        /// It does that on code compilation.

        public UndertaleData Data { get; set; }
        public string FilePath { get; set; }
        public string ScriptPath { get; set; } // For the scripting interface specifically

        public string TitleMain { get; set; }

        public static RoutedUICommand CloseTabCommand = new RoutedUICommand("Close current tab", "CloseTab", typeof(MainWindow));
        public static RoutedUICommand CloseAllTabsCommand = new RoutedUICommand("Close all tabs", "CloseAllTabs", typeof(MainWindow));
        public static RoutedUICommand RestoreClosedTabCommand = new RoutedUICommand("Restore last closed tab", "RestoreClosedTab", typeof(MainWindow));
        public static RoutedUICommand SwitchToNextTabCommand = new RoutedUICommand("Switch to the next tab", "SwitchToNextTab", typeof(MainWindow));
        public static RoutedUICommand SwitchToPrevTabCommand = new RoutedUICommand("Switch to the previous tab", "SwitchToPrevTab", typeof(MainWindow));
        public ObservableCollection<Tab> Tabs { get; set; } = new();
        public Tab CurrentTab { get; set; }
        public int CurrentTabIndex { get; set; } = 0;

        public object Highlighted { get; set; }
        public object Selected { get; set; }

        public Visibility IsGMS2 => (Data?.GeneralInfo?.Major ?? 0) >= 2 ? Visibility.Visible : Visibility.Collapsed;
        // God this is so ugly, if there's a better way, please, put in a pull request
        public Visibility IsExtProductIDEligible => (((Data?.GeneralInfo?.Major ?? 0) >= 2) || (((Data?.GeneralInfo?.Major ?? 0) == 1) && (((Data?.GeneralInfo?.Build ?? 0) >= 1773) || ((Data?.GeneralInfo?.Build ?? 0) == 1539)))) ? Visibility.Visible : Visibility.Collapsed;

        public ObservableCollection<Tab> SelectionHistory { get; } = new();
        public List<Tab> ClosedTabsHistory { get; } = new();

        public bool CanSave { get; set; }
        public bool CanSafelySave = false;
        public bool WasWarnedAboutTempRun = false;
        public bool FinishedMessageEnabled = true;
        public bool ScriptExecutionSuccess { get; set; } = true;
        public bool IsSaving { get; set; }
        public string ScriptErrorMessage { get; set; } = "";
        public string ExePath { get; private set; } = Program.GetExecutableDirectory();
        public string ScriptErrorType { get; set; } = "";

        public enum CodeEditorMode
        {
            Unstated,
            DontDecompile,
            Decompile
        }
        public enum SaveResult
        {
            NotSaved,
            Saved,
            Error
        }
        public enum ScrollDirection
        {
            Left,
            Right
        }
        public static CodeEditorMode CodeEditorDecompile { get; set; } = CodeEditorMode.Unstated;

        private int progressValue;
        private Task updater;
        private CancellationTokenSource cts;
        private CancellationToken cToken;
        private readonly object bindingLock = new();
        private HashSet<string> syncBindings = new();
        private bool _roomRendererEnabled;

        public bool GMLCacheEnabled => SettingsWindow.UseGMLCache;

        public bool RoomRendererEnabled
        {
            get => _roomRendererEnabled;
            set
            {
                if (UndertaleRoomRenderer.RoomRendererTemplate is null)
                    UndertaleRoomRenderer.RoomRendererTemplate = (DataTemplate)DataEditor.FindResource("roomRendererTemplate");

                if (value)
                {
                    DataEditor.ContentTemplate = UndertaleRoomRenderer.RoomRendererTemplate;
                    UndertaleCachedImageLoader.ReuseTileBuffer = true;
                }
                else
                {
                    DataEditor.ContentTemplate = null;
                    Selected = LastOpenedObject;
                    LastOpenedObject = null;
                    UndertaleCachedImageLoader.Reset();
                    CachedTileDataLoader.Reset();
                }

                _roomRendererEnabled = value;
            }
        }
        public object LastOpenedObject { get; set; } // for restoring the object that was opened before room rendering

        public bool IsAppClosed { get; set; }

        private HttpClient httpClient;

        public event PropertyChangedEventHandler PropertyChanged;

        // For delivering messages to LoaderDialogs
        public delegate void FileMessageEventHandler(string message);
        public event FileMessageEventHandler FileMessageEvent;

        private LoaderDialog scriptDialog;

        // Related to profile system and appdata
        public byte[] MD5PreviouslyLoaded = new byte[13];
        public byte[] MD5CurrentlyLoaded = new byte[15];
        public static string AppDataFolder => Settings.AppDataFolder;
        public static string ProfilesFolder = Path.Combine(Settings.AppDataFolder, "Profiles");
        public static string CorrectionsFolder = Path.Combine(Program.GetExecutableDirectory(), "Corrections");
        public string ProfileHash = "Unknown";
        public bool CrashedWhileEditing = false;

        // Scripting interface-related
        private ScriptOptions scriptOptions;
        private Task scriptSetupTask;

        // Version info
        public static string Edition = "";

        // On debug, build with git versions. Otherwise, use the provided release version.
#if DEBUG
        public static string Version = GitVersion.GetGitVersion();
#else
        public static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString() + (Edition != "" ? "-" + Edition : "");
#endif

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            Highlighted = new DescriptionView("Welcome to UndertaleModTool!", "Open a data.win file to get started, then double click on the items on the left to view them.");
            OpenInTab(Highlighted);
            SelectionHistory.Clear();
            ClosedTabsHistory.Clear();

            TitleMain = "UndertaleModTool by krzys_h v:" + Version;

            CanSave = false;
            CanSafelySave = false;

            scriptSetupTask = Task.Run(() =>
            {
                scriptOptions = ScriptOptions.Default
                                .AddImports("UndertaleModLib", "UndertaleModLib.Models", "UndertaleModLib.Decompiler",
                                            "UndertaleModLib.Scripting", "UndertaleModLib.Compiler",
                                            "UndertaleModTool", "System", "System.IO", "System.Collections.Generic",
                                            "System.Text.RegularExpressions")
                                .AddReferences(typeof(UndertaleObject).GetTypeInfo().Assembly,
                                                GetType().GetTypeInfo().Assembly,
                                                typeof(JsonConvert).GetTypeInfo().Assembly,
                                                typeof(System.Text.RegularExpressions.Regex).GetTypeInfo().Assembly)
                                .WithEmitDebugInformation(true); //when script throws an exception, add a exception location (line number)
            });
        }

        private void SetIDString(string str)
        {
            ((Label)this.FindName("ObjectLabel")).Content = str;
        }

        [DllImport("shell32.dll")]
        static extern void SHChangeNotify(long wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
        const long SHCNE_ASSOCCHANGED = 0x08000000;

        public static readonly string[] IFF_EXTENSIONS = new string[] { ".win", ".unx", ".ios", ".droid", ".3ds", ".symbian" };

        private void UpdateTree()
        {
            foreach (var child in (MainTree.Items[0] as TreeViewItem).Items)
                ((child as TreeViewItem).ItemsSource as ICollectionView)?.Refresh();
        }
        /*
        private static bool IsLikelyRunFromZipFolder()
        {
            var path = System.Environment.CurrentDirectory;
            var fileInfo = new FileInfo(path);
            return fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly);
        }

        private static bool IsRunFromTempFolder()
        {
            var path = System.Environment.CurrentDirectory;
            var temp = Path.GetTempPath();
            return path.IndexOf(temp, StringComparison.OrdinalIgnoreCase) == 0;
        }
        */
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Settings.Load();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    string procFileName = Process.GetCurrentProcess().MainModule.FileName;
                    var HKCU_Classes = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true);
                    var UndertaleModTool_app = HKCU_Classes.CreateSubKey(@"UndertaleModTool");

                    UndertaleModTool_app.SetValue("", "UndertaleModTool");
                    UndertaleModTool_app.CreateSubKey(@"shell\open\command").SetValue("", "\"" + procFileName + "\" \"%1\"", RegistryValueKind.String);
                    UndertaleModTool_app.CreateSubKey(@"shell\launch\command").SetValue("", "\"" + procFileName + "\" \"%1\" launch", RegistryValueKind.String);
                    UndertaleModTool_app.CreateSubKey(@"shell\launch").SetValue("", "Run game normally", RegistryValueKind.String);
                    UndertaleModTool_app.CreateSubKey(@"shell\special_launch\command").SetValue("", "\"" + procFileName + "\" \"%1\" special_launch", RegistryValueKind.String);
                    UndertaleModTool_app.CreateSubKey(@"shell\special_launch").SetValue("", "Run extended options", RegistryValueKind.String);

                    if (File.Exists("dna.txt"))
                    {
                        ScriptMessage("Opt out detected.");
                        SettingsWindow.AutomaticFileAssociation = false;
                        Settings.Save();
                    }
                    if (SettingsWindow.AutomaticFileAssociation)
                    {
                        foreach (var extStr in IFF_EXTENSIONS)
                        {
                            var ext = HKCU_Classes.CreateSubKey(extStr);
                            ext.SetValue("", "UndertaleModTool", RegistryValueKind.String);
                        }
                        SHChangeNotify(SHCNE_ASSOCCHANGED, 0, IntPtr.Zero, IntPtr.Zero);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }

            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                string arg = args[1];
                if (File.Exists(arg))
                {
                    await LoadFile(arg, true);
                }
                else if (arg == "deleteTempFolder") // if was launched from UndertaleModToolUpdater
                {
                    _ = Task.Run(() =>
                    {
                        Process[] updaterInstances = Process.GetProcessesByName("UndertaleModToolUpdater");
                        bool updaterClosed = false;

                        if (updaterInstances.Length > 0)
                        {
                            foreach (Process instance in updaterInstances)
                            {
                                if (!instance.WaitForExit(5000))
                                    ShowWarning("UndertaleModToolUpdater app didn't exit.\nCan't delete its temp folder.");
                                else
                                    updaterClosed = true;
                            }
                        }
                        else
                            updaterClosed = true;

                        if (updaterClosed)
                        {
                            bool deleted = false;
                            string exMessage = "(error message is missing)";
                            string tempFolder = Path.Combine(Path.GetTempPath(), "UndertaleModTool");

                            for (int i = 0; i <= 5; i++)
                            {
                                try
                                {
                                    Directory.Delete(tempFolder, true);

                                    deleted = true;
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    exMessage = ex.Message;
                                }

                                Thread.Sleep(1000);
                            }

                            if (!deleted)
                                ShowWarning($"The updater temp folder can't be deleted.\nError - {exMessage}.");
                        }
                    });
                }
            }
            if (args.Length > 2)
            {
                if (args[2] == "special_launch")
                {
                    RuntimePicker picker = new RuntimePicker();
                    picker.Owner = this;
                    var runtime = picker.Pick(FilePath, Data);
                    if (runtime == null)
                        return;
                    Process.Start(runtime.Path, "-game \"" + FilePath + "\"");
                    Environment.Exit(0);
                }
                else if (args[2] == "launch")
                {
                    string gameExeName = Data?.GeneralInfo?.Filename?.Content;
                    if (gameExeName == null || FilePath == null)
                    {
                        ScriptError("Null game executable name or location");
                        Environment.Exit(0);
                    }
                    string gameExePath = Path.Combine(Path.GetDirectoryName(FilePath), gameExeName + ".exe");
                    if (!File.Exists(gameExePath))
                    {
                        ScriptError("Cannot find game executable path, expected: " + gameExePath);
                        Environment.Exit(0);
                    }
                    if (!File.Exists(FilePath))
                    {
                        ScriptError("Cannot find data file path, expected: " + FilePath);
                        Environment.Exit(0);
                    }
                    if (gameExeName != null)
                        Process.Start(gameExePath, "-game \"" + FilePath + "\" -debugoutput \"" + Path.ChangeExtension(FilePath, ".gamelog.txt") + "\"");
                    Environment.Exit(0);
                }
                else
                {
                    _ = ListenChildConnection(args[2]);
                }
            }

            // Copy the known code corrections into the profile, if they don't already exist.
            ApplyCorrections();
            CrashCheck();
        }

        public Dictionary<string, NamedPipeServerStream> childFiles = new Dictionary<string, NamedPipeServerStream>();

        public void OpenChildFile(string filename, string chunkName, int itemIndex)
        {
            if (childFiles.ContainsKey(filename))
            {
                try
                {
                    StreamWriter existingwriter = new StreamWriter(childFiles[filename]);
                    existingwriter.WriteLine(chunkName + ":" + itemIndex);
                    existingwriter.Flush();
                    return;
                }
                catch (IOException e)
                {
                    Debug.WriteLine(e);
                    childFiles.Remove(filename);
                }
            }

            string key = Guid.NewGuid().ToString();

            string dir = Path.GetDirectoryName(FilePath);
            Process.Start(Process.GetCurrentProcess().MainModule.FileName, "\"" + Path.Combine(dir, filename) + "\" " + key);

            var server = new NamedPipeServerStream(key);
            server.WaitForConnection();
            childFiles.Add(filename, server);

            StreamWriter writer = new StreamWriter(childFiles[filename]);
            writer.WriteLine(chunkName + ":" + itemIndex);
            writer.Flush();
        }

        public void CloseChildFiles()
        {
            foreach (var pair in childFiles)
            {
                pair.Value.Close();
            }
            childFiles.Clear();
        }

        public async Task ListenChildConnection(string key)
        {
            var client = new NamedPipeClientStream(key);
            client.Connect();
            StreamReader reader = new StreamReader(client);

            while (true)
            {
                string[] thingToOpen = (await reader.ReadLineAsync()).Split(':');
                if (thingToOpen.Length != 2)
                    throw new Exception("ummmmm");
                if (thingToOpen[0] != "AUDO") // Just pretend I'm not hacking it together that poorly
                    throw new Exception("errrrr");
                OpenInTab(Data.EmbeddedAudio[Int32.Parse(thingToOpen[1])], false, "Embedded Audio");
                Activate();
            }
        }

        private async void Command_New(object sender, ExecutedRoutedEventArgs e)
        {
            await MakeNewDataFile();
        }
        public async Task<bool> MakeNewDataFile()
        {
            if (Data != null)
            {
                if (MessageBox.Show("Warning: you currently have a project open.\nAre you sure you want to make a new project?", "UndertaleModTool", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    return false;
            }
            this.Dispatcher.Invoke(() =>
            {
                CommandBox.Text = "";
            });

            await SaveGMLCache(FilePath, false);
            FilePath = null;
            Data = UndertaleData.CreateNew();
            Data.ToolInfo.AppDataProfiles = ProfilesFolder;
            CloseChildFiles();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Data)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilePath)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsGMS2)));

            BackgroundsItemsList.Header = IsGMS2 == Visibility.Visible
                                          ? "Tile sets"
                                          : "Backgrounds & Tile sets";

            CurrentTab = null;
            Tabs.Clear();
            Highlighted = new DescriptionView("Welcome to UndertaleModTool!", "New file created, have fun making a game out of nothing\nI TOLD YOU to open a data.win, not create a new file! :P");
            OpenInTab(Highlighted);
            SelectionHistory.Clear();
            ClosedTabsHistory.Clear();

            CanSave = true;
            CanSafelySave = true;

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce; //clean "GC holes" left in the memory by previous game data
            GC.Collect();                                                                           //https://docs.microsoft.com/en-us/dotnet/api/system.runtime.gcsettings.largeobjectheapcompactionmode?view=net-5.0

            return true;
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            // ignore drop events inside the main window (e.g. resource tree)
            if (sender is MainWindow)
            {
                // try to detect stuff, autoConvert is false because we don't want any conversion.
                if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
                {
                    string filepath = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                    string fileext = Path.GetExtension(filepath);

                    if (fileext == ".csx")
                    {
                        if (MessageBox.Show($"Run {filepath} as a script?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                            await RunScript(filepath);
                    }
                    else if (IFF_EXTENSIONS.Contains(fileext) || fileext == ".dat" /* audiogroup */)
                    {
                        if (MessageBox.Show($"Open {filepath} as a data file?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                            await LoadFile(filepath, true);
                    }
                    // else, do something?
                }
            }
        }

        public async Task<bool> DoOpenDialog()
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.DefaultExt = "win";
            dlg.Filter = "Game Maker Studio data files (.win, .unx, .ios, .droid, audiogroup*.dat)|*.win;*.unx;*.ios;*.droid;audiogroup*.dat|All files|*";

            if (dlg.ShowDialog(this) == true)
            {
                await LoadFile(dlg.FileName, true);
                return true;
            }
            return false;
        }
        public async Task<bool> DoSaveDialog(bool suppressDebug = false)
        {
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.DefaultExt = "win";
            dlg.Filter = "Game Maker Studio data files (.win, .unx, .ios, .droid, audiogroup*.dat)|*.win;*.unx;*.ios;*.droid;audiogroup*.dat|All files|*";
            dlg.FileName = FilePath;

            if (dlg.ShowDialog(this) == true)
            {
                await SaveFile(dlg.FileName, suppressDebug);
                return true;
            }
            return false;
        }

        public async Task<SaveResult> SaveCodeChanges()
        {
            SaveResult result = SaveResult.NotSaved;

            DependencyObject child = VisualTreeHelper.GetChild(DataEditor, 0);
            if (child is not null && VisualTreeHelper.GetChild(child, 0) is UndertaleCodeEditor codeEditor)
            {
                #pragma warning disable CA1416
                if (codeEditor.DecompiledChanged || codeEditor.DisassemblyChanged)
                {
                    IsSaving = true;

                    await codeEditor.SaveChanges();
                    //"IsSaving" should became false on success

                    result = IsSaving ? SaveResult.Error : SaveResult.Saved;
                    IsSaving = false;
                }
                #pragma warning restore CA1416
            }

            return result;
        }

        private void Command_Open(object sender, ExecutedRoutedEventArgs e)
        {
            _ = DoOpenDialog();
        }

        private async void Command_Save(object sender, ExecutedRoutedEventArgs e)
        {
            if (CanSave)
            {
                if (!CanSafelySave)
                    ShowWarning("Errors occurred during loading. High chance of data loss! Proceed at your own risk.");

                if (await SaveCodeChanges() == SaveResult.NotSaved)
                    _ = DoSaveDialog();
            }
        }
        private async void DataWindow_Closing(object sender, CancelEventArgs e)
        {
            if (Data != null)
            {
                e.Cancel = true;

                bool save = false;

                if (SettingsWindow.WarnOnClose)
                {
                    if (ShowQuestion("Are you sure you want to quit?") == MessageBoxResult.Yes)
                    {
                        if (ShowQuestion("Save changes first?") == MessageBoxResult.Yes)
                        {
                            if (scriptDialog is not null)
                            {
                                if (ShowQuestion("Script still runs. Save anyway?\nIt can corrupt the data file that you'll save.") == MessageBoxResult.Yes)
                                    save = true;
                            }
                            else
                                save = true;

                            if (save)
                            {
                                SaveResult saveRes = await SaveCodeChanges();

                                if (saveRes == SaveResult.NotSaved)
                                    _ = DoSaveDialog();
                                else if (saveRes == SaveResult.Error)
                                    return;
                            }
                        }
                        else
                            RevertProfile();

                        DestroyUMTLastEdited();
                    }
                    else
                        return;
                }
                else
                {
                    RevertProfile();
                    DestroyUMTLastEdited();
                }

                if (SettingsWindow.UseGMLCache && Data?.GMLCache?.Count > 0 && !Data.GMLCacheWasSaved && Data.GMLCacheIsReady)
                    if (ShowQuestion("Save unedited code cache?") == MessageBoxResult.Yes)
                        await SaveGMLCache(FilePath, save);

                CloseOtherWindows();

                IsAppClosed = true;

                Closing -= DataWindow_Closing; //disable "on window closed" event handler (prevent recursion)
                _ = Task.Run(() => Dispatcher.Invoke(Close));
            }
        }
        private void Command_Close(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }
        private void CloseOtherWindows() //close "standalone" windows (e.g. "ClickableTextOutput")
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w is not MainWindow && w.Owner is null) //&& is not a modal window
                    w.Close();
            }
        }

        private void Command_CloseTab(object sender, ExecutedRoutedEventArgs e)
        {
            CloseTab();
        }
        private void Command_CloseAllTabs(object sender, ExecutedRoutedEventArgs e)
        {
            if (Tabs.Count == 1 && CurrentTab.TabTitle == "Welcome!")
                return;

            SelectionHistory.Clear();
            ClosedTabsHistory.Clear();
            Tabs.Clear();
            CurrentTab = null;

            Highlighted = new DescriptionView("Welcome to UndertaleModTool!", "Open data.win file to get started, then double click on the items on the left to view them");
            OpenInTab(Highlighted);
            CurrentTab = Tabs[CurrentTabIndex];

            Selected = CurrentTab.OpenedObject;
            UpdateObjectLabel(Selected);
        }
        private void Command_RestoreClosedTab(object sender, ExecutedRoutedEventArgs e)
        {
            if (ClosedTabsHistory.Count > 0)
            {
                Tab lastTab = ClosedTabsHistory.Last();
                ClosedTabsHistory.RemoveAt(ClosedTabsHistory.Count - 1);

                if (CurrentTab.AutoClose)
                    CloseTab(false);

                Tabs.Insert(lastTab.TabIndex, lastTab);
                CurrentTabIndex = lastTab.TabIndex;

                for (int i = CurrentTabIndex + 1; i < Tabs.Count; i++)
                    Tabs[i].TabIndex = i;

                ScrollToTab(CurrentTabIndex);

                Selected = lastTab.OpenedObject;
                UpdateObjectLabel(Selected);
            }
        }
        private void Command_SwitchToNextTab(object sender, ExecutedRoutedEventArgs e)
        {
            if (CurrentTabIndex < Tabs.Count - 1)
                CurrentTabIndex++;
        }
        private void Command_SwitchToPrevTab(object sender, ExecutedRoutedEventArgs e)
        {
            if (CurrentTabIndex > 0)
                CurrentTabIndex--;
        }

        private async Task LoadFile(string filename, bool preventClose = false)
        {
            LoaderDialog dialog = new LoaderDialog("Loading", "Loading, please wait...");
            dialog.PreventClose = preventClose;
            this.Dispatcher.Invoke(() =>
            {
                CommandBox.Text = "";
            });
            dialog.Owner = this;

            // temporary save the current tabs in case of loading error
            Tab[] tempTabs = Tabs.ToArray();
            int lastTabIndex = CurrentTabIndex;

            CurrentTab = null;
            Tabs.Clear(); // clear the current tabs to destroy game object references

            Task t = Task.Run(() =>
            {
                bool hadWarnings = false;
                UndertaleData data = null;
                try
                {
                    using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        data = UndertaleIO.Read(stream, warning =>
                        {
                            MessageBox.Show(warning, "Loading warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            hadWarnings = true;
                        }, message =>
                        {
                            FileMessageEvent?.Invoke(message);
                        });
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("An error occured while trying to load:\n" + e.Message, "Load error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                Dispatcher.Invoke(async () =>
                {
                    if (data != null)
                    {
                        if (data.UnsupportedBytecodeVersion)
                        {
                            MessageBox.Show("Only bytecode versions 13 to 17 are supported for now, you are trying to load " + data.GeneralInfo.BytecodeVersion + ". A lot of code is disabled and will likely break something. Saving/exporting is disabled.", "Unsupported bytecode version", MessageBoxButton.OK, MessageBoxImage.Warning);
                            CanSave = false;
                            CanSafelySave = false;
                        }
                        else if (hadWarnings)
                        {
                            MessageBox.Show("Warnings occurred during loading. Data loss will likely occur when trying to save!", "Loading problems", MessageBoxButton.OK, MessageBoxImage.Warning);
                            CanSave = true;
                            CanSafelySave = false;
                        }
                        else
                        {
                            CanSave = true;
                            CanSafelySave = true;
                            UpdateProfile(data, filename);
                            if (data != null)
                            {
                                data.ToolInfo.ProfileMode = SettingsWindow.ProfileModeEnabled;
                                data.ToolInfo.CurrentMD5 = BitConverter.ToString(MD5CurrentlyLoaded).Replace("-", "").ToLowerInvariant();
                            }
                        }
                        if (data.GMS2_3 && SettingsWindow.Warn_About_GMS23)
                        {
                            MessageBox.Show("This game was built using GameMaker Studio 2.3 (or above). Support for this version is a work in progress, and you will likely run into issues decompiling code or in other places.", "GMS 2.3", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        if (data.IsYYC())
                        {
                            MessageBox.Show("This game uses YYC (YoYo Compiler), which means the code is embedded into the game executable. This configuration is currently not fully supported; continue at your own risk.", "YYC", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        if (data.GeneralInfo != null)
                        {
                            if (!data.GeneralInfo.DisableDebugger)
                            {
                                MessageBox.Show("This game is set to run with the GameMaker Studio debugger and the normal runtime will simply hang after loading if the debugger is not running. You can turn this off in General Info by checking the \"Disable Debugger\" box and saving.", "GMS Debugger", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                        if (Path.GetDirectoryName(FilePath) != Path.GetDirectoryName(filename))
                            CloseChildFiles();

                        if (FilePath != filename)
                            await SaveGMLCache(FilePath, false, dialog);

                        Data = data;

                        await LoadGMLCache(filename, dialog);
                        UndertaleCachedImageLoader.Reset();
                        CachedTileDataLoader.Reset();

                        Data.ToolInfo.AppDataProfiles = ProfilesFolder;
                        FilePath = filename;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Data)));
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilePath)));
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsGMS2)));

                        BackgroundsItemsList.Header = IsGMS2 == Visibility.Visible
                                                      ? "Tile sets"
                                                      : "Backgrounds & Tile sets";

                        #pragma warning disable CA1416
                        UndertaleCodeEditor.gettext = null;
                        UndertaleCodeEditor.gettextJSON = null;
                        #pragma warning restore CA1416

                        Highlighted = new DescriptionView("Welcome to UndertaleModTool!", "Double click on the items on the left to view them!");
                        OpenInTab(Highlighted);
                        SelectionHistory.Clear();
                        ClosedTabsHistory.Clear();
                    }
                    else
                    {
                        // restore the tabs because new data hasn't been loaded
                        Tabs = new(tempTabs);
                        CurrentTab = Tabs[lastTabIndex];
                        CurrentTabIndex = lastTabIndex;
                    }

                    dialog.Hide();
                });
            });
            dialog.ShowDialog();
            await t;

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce; //clean "GC holes" left in the memory by previous game data
            GC.Collect();                                                                           //https://docs.microsoft.com/en-us/dotnet/api/system.runtime.gcsettings.largeobjectheapcompactionmode?view=net-5.0
        }

        private async Task SaveFile(string filename, bool suppressDebug = false)
        {
            if (Data == null || Data.UnsupportedBytecodeVersion)
                return;

            bool isDifferentPath = FilePath != filename;

            LoaderDialog dialog = new LoaderDialog("Saving", "Saving, please wait...");
            dialog.PreventClose = true;
            IProgress<Tuple<int, string>> progress = new Progress<Tuple<int, string>>(i => { dialog.ReportProgress(i.Item2, i.Item1); });
            IProgress<double?> setMax = new Progress<double?>(i => { dialog.Maximum = i; });
            dialog.Owner = this;
            FilePath = filename;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilePath)));
            if (Path.GetDirectoryName(FilePath) != Path.GetDirectoryName(filename))
                CloseChildFiles();

            DebugDataDialog.DebugDataMode debugMode = DebugDataDialog.DebugDataMode.NoDebug;
            if (!suppressDebug && Data.GeneralInfo != null && !Data.GeneralInfo.DisableDebugger)
                MessageBox.Show("You are saving the game in GameMaker Studio debug mode. Unless the debugger is running, the normal runtime will simply hang after loading. You can turn this off in General Info by checking the \"Disable Debugger\" box and saving.", "GMS Debugger", MessageBoxButton.OK, MessageBoxImage.Warning);
            Task t = Task.Run(async () =>
            {
                bool SaveSucceeded = true;

                try
                {
                    using (var stream = new FileStream(filename + "temp", FileMode.Create, FileAccess.Write))
                    {
                        UndertaleIO.Write(stream, Data, message =>
                        {
                            FileMessageEvent?.Invoke(message);
                        });
                    }

                    if (debugMode != DebugDataDialog.DebugDataMode.NoDebug)
                    {
                        FileMessageEvent?.Invoke("Generating debugger data...");

                        UndertaleDebugData debugData = UndertaleDebugData.CreateNew();

                        setMax.Report(Data.Code.Count);
                        int count = 0;
                        object countLock = new object();
                        string[] outputs = new string[Data.Code.Count];
                        UndertaleDebugInfo[] outputsOffsets = new UndertaleDebugInfo[Data.Code.Count];
                        GlobalDecompileContext context = new GlobalDecompileContext(Data, false);
                        Parallel.For(0, Data.Code.Count, (i) =>
                        {
                            var code = Data.Code[i];

                            if (debugMode == DebugDataDialog.DebugDataMode.Decompiled)
                            {
                                //Debug.WriteLine("Decompiling " + code.Name.Content);
                                string output;
                                try
                                {
                                    output = Decompiler.Decompile(code, context);
                                }
                                catch (Exception e)
                                {
                                    Debug.WriteLine(e.Message);
                                    output = "/*\nEXCEPTION!\n" + e.ToString() + "\n*/";
                                }
                                outputs[i] = output;

                                UndertaleDebugInfo debugInfo = new UndertaleDebugInfo();
                                debugInfo.Add(new UndertaleDebugInfo.DebugInfoPair() { SourceCodeOffset = 0, BytecodeOffset = 0 }); // TODO: generate this too! :D
                                outputsOffsets[i] = debugInfo;
                            }
                            else
                            {
                                StringBuilder sb = new StringBuilder();
                                UndertaleDebugInfo debugInfo = new UndertaleDebugInfo();

                                foreach (var instr in code.Instructions)
                                {
                                    if (debugMode == DebugDataDialog.DebugDataMode.FullAssembler || instr.Kind == UndertaleInstruction.Opcode.Pop || instr.Kind == UndertaleInstruction.Opcode.Popz || instr.Kind == UndertaleInstruction.Opcode.B || instr.Kind == UndertaleInstruction.Opcode.Bt || instr.Kind == UndertaleInstruction.Opcode.Bf || instr.Kind == UndertaleInstruction.Opcode.Ret || instr.Kind == UndertaleInstruction.Opcode.Exit)
                                        debugInfo.Add(new UndertaleDebugInfo.DebugInfoPair() { SourceCodeOffset = (uint)sb.Length, BytecodeOffset = instr.Address * 4 });
                                    sb.Append(instr.ToString(code));
                                    sb.Append('\n');
                                }
                                outputs[i] = sb.ToString();
                                outputsOffsets[i] = debugInfo;
                            }

                            lock (countLock)
                            {
                                progress.Report(new Tuple<int, string>(++count, code.Name.Content));
                            }
                        });
                        setMax.Report(null);

                        for (int i = 0; i < Data.Code.Count; i++)
                        {
                            debugData.SourceCode.Add(new UndertaleScriptSource() { SourceCode = debugData.Strings.MakeString(outputs[i]) });
                            debugData.DebugInfo.Add(outputsOffsets[i]);
                            debugData.LocalVars.Add(Data.CodeLocals[i]);
                            if (debugData.Strings.IndexOf(Data.CodeLocals[i].Name) < 0)
                                debugData.Strings.Add(Data.CodeLocals[i].Name);
                            foreach (var local in Data.CodeLocals[i].Locals)
                                if (debugData.Strings.IndexOf(local.Name) < 0)
                                    debugData.Strings.Add(local.Name);
                        }

                        using (UndertaleWriter writer = new UndertaleWriter(new FileStream(Path.ChangeExtension(FilePath, ".yydebug"), FileMode.Create, FileAccess.Write)))
                        {
                            debugData.FORM.Serialize(writer);
                            writer.ThrowIfUnwrittenObjects();
                            writer.Flush();
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("An error occured while trying to save:\n" + e.Message, "Save error", MessageBoxButton.OK, MessageBoxImage.Error);
                    SaveSucceeded = false;
                }
                // Don't make any changes unless the save succeeds.
                try
                {
                    if (SaveSucceeded)
                    {
                        // It saved successfully!
                        // If we're overwriting a previously existing data file, we're going to delete it now.
                        // Then, we're renaming it back to the proper (non-temp) file name.
                        if (File.Exists(filename))
                            File.Delete(filename);
                        File.Move(filename + "temp", filename);

                        await SaveGMLCache(filename, true, dialog, isDifferentPath);

                        // Also make the changes to the profile system.
                        ProfileSaveEvent(Data, filename);
                        SaveTempToMainProfile();
                    }
                    else
                    {
                        // It failed, but since we made a temp file for saving, no data was overwritten or destroyed (hopefully)
                        // We need to delete the temp file though (if it exists).
                        if (File.Exists(filename + "temp"))
                            File.Delete(filename + "temp");
                        // No profile system changes, since the save failed, like a save was never attempted.
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show("An error occured while trying to save:\n" + exc.Message, "Save error", MessageBoxButton.OK, MessageBoxImage.Error);
                    SaveSucceeded = false;
                }
                if (Data != null)
                {
                    Data.ToolInfo.ProfileMode = SettingsWindow.ProfileModeEnabled;
                    Data.ToolInfo.CurrentMD5 = BitConverter.ToString(MD5CurrentlyLoaded).Replace("-", "").ToLowerInvariant();
                }

                #pragma warning disable CA1416
                UndertaleCodeEditor.gettextJSON = null;
                #pragma warning restore CA1416

                Dispatcher.Invoke(() =>
                {
                    dialog.Hide();
                });
            });
            dialog.ShowDialog();
            await t;
        }

        public string GenerateMD5(string filename)
        {
            using (MD5 md5 = MD5.Create())
            {
                using (FileStream fs = File.OpenRead(filename))
                {
                    byte[] hash = md5.ComputeHash(fs);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        private async Task LoadGMLCache(string filename, LoaderDialog dialog = null)
        {
            await Task.Run(() => {
                if (SettingsWindow.UseGMLCache && File.Exists(Path.Join("GMLCache", "index")))
                {
                    dialog?.Dispatcher.Invoke(() => dialog.ReportProgress("Loading decompiled code cache..."));

                    string[] indexLines = File.ReadAllLines(Path.Join("GMLCache", "index"));

                    int num = -1;
                    for (int i = 0; i < indexLines.Length; i++)
                        if (indexLines[i] == filename)
                        {
                            num = i;
                            break;
                        }

                    if (num == -1)
                        return;

                    if (!File.Exists(Path.Join("GMLCache", num.ToString())))
                    {
                        ShowWarning("Decompiled code cache file for open data is missing, but its name present in the index.");

                        return;
                    }

                    string hash = GenerateMD5(filename);

                    using (StreamReader fs = new(Path.Join("GMLCache", num.ToString())))
                    {
                        string prevHash = fs.ReadLine();

                        if (!Regex.IsMatch(prevHash, "^[0-9a-fA-F]{32}$")) //if first 32 bytes of cache file are not a valid MD5
                            ShowWarning("Decompiled code cache for open file is broken.\nThe cache will be generated again.");
                        else
                        {
                            if (hash == prevHash)
                            {
                                string cacheStr = fs.ReadLine();
                                string failedStr = fs.ReadLine();

                                try
                                {
                                    Data.GMLCache = SystemJson.JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(cacheStr);

                                    if (failedStr is not null)
                                        Data.GMLCacheFailed = SystemJson.JsonSerializer.Deserialize<List<string>>(failedStr);
                                    else
                                        Data.GMLCacheFailed = new();
                                }
                                catch
                                {
                                    ShowWarning("Decompiled code cache for open file is broken.\nThe cache will be generated again.");

                                    Data.GMLCache = null;
                                    Data.GMLCacheFailed = null;

                                    return;
                                }

                                string[] codeNames = Data.Code.Where(x => x.ParentEntry is null).Select(x => x.Name.Content).ToArray();
                                string[] invalidNames = Data.GMLCache.Keys.Except(codeNames).ToArray();
                                if (invalidNames.Length > 0)
                                {
                                    ShowWarning($"Decompiled code cache for open file contains one or more non-existent code names (first - \"{invalidNames[0]}\").\nThe cache will be generated again.");

                                    Data.GMLCache = null;

                                    return;
                                }

                                Data.GMLCacheChanged = new();
                                Data.GMLEditedBefore = new();
                                Data.GMLCacheWasSaved = true;
                            }
                            else
                                ShowWarning("Open file differs from the one the cache was generated for.\nThat decompiled code cache will be generated again.");
                        }
                    }
                }
            });
        }
        private async Task SaveGMLCache(string filename, bool updateCache = true, LoaderDialog dialog = null, bool isDifferentPath = false)
        {
            await Task.Run(async () => {
                if (SettingsWindow.UseGMLCache && Data?.GMLCache?.Count > 0 && Data.GMLCacheIsReady && (isDifferentPath || !Data.GMLCacheWasSaved || !Data.GMLCacheChanged.IsEmpty))
                {
                    dialog?.Dispatcher.Invoke(() => dialog.ReportProgress("Saving decompiled code cache..."));

                    if (!File.Exists(Path.Join("GMLCache", "index")))
                    {
                        Directory.CreateDirectory("GMLCache");

                        File.WriteAllText(Path.Join("GMLCache", "index"), filename);
                    }

                    List<string> indexLines = File.ReadAllLines(Path.Join("GMLCache", "index")).ToList();

                    int num = -1;
                    for (int i = 0; i < indexLines.Count; i++)
                        if (indexLines[i] == filename)
                        {
                            num = i;
                            break;
                        }

                    if (num == -1) //if it's new cache file
                    {
                        num = indexLines.Count;

                        indexLines.Add(filename);
                    }

                    if (updateCache)
                    {
                        await GenerateGMLCache(null, dialog, true);
                        await StopProgressBarUpdater();
                    }

                    string[] codeNames = Data.Code.Where(x => x.ParentEntry is null).Select(x => x.Name.Content).ToArray();
                    Dictionary<string, string> sortedCache = new(Data.GMLCache.OrderBy(x => Array.IndexOf(codeNames, x.Key)));
                    Data.GMLCacheFailed = Data.GMLCacheFailed.OrderBy(x => Array.IndexOf(codeNames, x)).ToList();

                    if (!updateCache && Data.GMLEditedBefore.Count > 0) //if saving the original cache
                        foreach (string name in Data.GMLEditedBefore)
                            sortedCache.Remove(name);                   //exclude the code that was edited from the save list

                    dialog?.Dispatcher.Invoke(() => dialog.ReportProgress("Saving decompiled code cache..."));

                    string hash = GenerateMD5(filename);

                    using (FileStream fs = File.Create(Path.Join("GMLCache", num.ToString())))
                    {
                        fs.Write(Encoding.UTF8.GetBytes(hash + '\n'));
                        fs.Write(SystemJson.JsonSerializer.SerializeToUtf8Bytes(sortedCache));

                        if (Data.GMLCacheFailed.Count > 0)
                        {
                            fs.WriteByte((byte)'\n');
                            fs.Write(SystemJson.JsonSerializer.SerializeToUtf8Bytes(Data.GMLCacheFailed));
                        }
                    }

                    File.WriteAllLines(Path.Join("GMLCache", "index"), indexLines);

                    Data.GMLCacheWasSaved = true;
                }
            });
        }

        public async Task<bool> GenerateGMLCache(ThreadLocal<GlobalDecompileContext> decompileContext = null, object dialog = null, bool clearGMLEditedBefore = false)
        {
            if (!SettingsWindow.UseGMLCache)
                return false;

            bool createdDialog = false;
            bool existedDialog = false;
            Data.GMLCacheIsReady = false;

            if (Data.GMLCache is null)
                Data.GMLCache = new();

            ConcurrentBag<string> failedBag = new();

            if (scriptDialog is null)
            {
                if (dialog is null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        scriptDialog = new LoaderDialog("Script in progress...", "Please wait...")
                        {
                            Owner = this,
                            PreventClose = true
                        };
                    });

                    createdDialog = true;
                }
                else
                    scriptDialog = dialog as LoaderDialog;
            }
            else
                existedDialog = true;

            if (decompileContext is null)
                decompileContext = new(() => new GlobalDecompileContext(Data, false));

            if (Data.KnownSubFunctions is null) //if we run script before opening any code
                Decompiler.BuildSubFunctionCache(Data);

            if (Data.GMLCache.IsEmpty)
            {
                SetProgressBar(null, "Generating decompiled code cache...", 0, Data.Code.Count);
                StartProgressBarUpdater();

                await Task.Run(() => Parallel.ForEach(Data.Code, (code) =>
                {
                    if (code is not null && code.ParentEntry is null)
                    {
                        try
                        {
                            Data.GMLCache[code.Name.Content] = Decompiler.Decompile(code, decompileContext.Value);
                        }
                        catch
                        {
                            failedBag.Add(code.Name.Content);
                        }
                    }

                    IncrementProgressParallel();
                }));

                Data.GMLEditedBefore = new(Data.GMLCacheChanged);
                Data.GMLCacheChanged.Clear();
                Data.GMLCacheFailed = failedBag.ToList();
            }
            else
            {
                List<string> codeToUpdate;
                bool cacheIsFull = !(Data.GMLCache.Count < Data.Code.Where(x => x.ParentEntry is null).Count() - Data.GMLCacheFailed.Count);

                if (cacheIsFull)
                {
                    Data.GMLCacheChanged = new(Data.GMLCacheChanged.Distinct()); //remove duplicates

                    codeToUpdate = Data.GMLCacheChanged.ToList();
                }
                else
                {
                    //add missing and modified code cache names to the update list (and remove duplicates)
                    codeToUpdate = Data.GMLCacheChanged.Union(
                        Data.Code.Where(x => x.ParentEntry is null)
                                 .Select(x => x.Name.Content)
                                 .Except(Data.GMLCache.Keys)
                                 .Except(Data.GMLCacheFailed))
                        .ToList();
                }

                if (codeToUpdate.Count > 0)
                {
                    SetProgressBar(null, "Updating decompiled code cache...", 0, codeToUpdate.Count);
                    StartProgressBarUpdater();

                    await Task.Run(() => Parallel.ForEach(codeToUpdate.Select(x => Data.Code.ByName(x)), (code) =>
                    {
                        if (code is not null && code.ParentEntry is null)
                        {
                            try
                            {
                                Data.GMLCache[code.Name.Content] = Decompiler.Decompile(code, decompileContext.Value);

                                Data.GMLCacheFailed.Remove(code.Name.Content); //that code compiles now
                            }
                            catch
                            {
                                failedBag.Add(code.Name.Content);
                            }
                        }

                        IncrementProgressParallel();
                    }));

                    if (clearGMLEditedBefore)
                        Data.GMLEditedBefore.Clear();
                    else
                        Data.GMLEditedBefore = Data.GMLEditedBefore.Union(Data.GMLCacheChanged).ToList();

                    Data.GMLCacheChanged.Clear();
                    Data.GMLCacheFailed = Data.GMLCacheFailed.Union(failedBag).ToList();
                    Data.GMLCacheWasSaved = false;
                }
                else if (clearGMLEditedBefore)
                    Data.GMLEditedBefore.Clear();

                if (!existedDialog)
                    scriptDialog = null;

                if (createdDialog)
                {
                    await StopProgressBarUpdater();
                    HideProgressBar();
                }
            }

            Data.GMLCacheIsReady = true;

            return true;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem)
            {
                string item = (e.NewValue as TreeViewItem)?.Header?.ToString();

                if (item == "Data")
                {
                    Highlighted = new DescriptionView("Welcome to UndertaleModTool!", Data != null ? "Double click on the items on the left to view them" : "Open data.win file to get started");
                    return;
                }

                if (Data == null)
                {
                    Highlighted = new DescriptionView(item, "Load data.win file first");
                    return;
                }

                Highlighted = item switch
                {
                    "General info" => new GeneralInfoEditor(Data?.GeneralInfo, Data?.Options, Data?.Language),
                    "Global init" => new GlobalInitEditor(Data?.GlobalInitScripts),
                    "Game End scripts" => new GameEndEditor(Data?.GameEndScripts),
                    "Variables" => (object)Data.FORM.Chunks["VARI"],
                    _ => new DescriptionView(item, "Expand the list on the left to edit items"),
                };
            }
            else
            {
                Highlighted = e.NewValue;
            }
        }

        private void MainTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenInTab(Highlighted);
        }

        private void MainTree_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                OpenInTab(Highlighted);
            }
        }

        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragDropEffects effects = DragDropEffects.Move | DragDropEffects.Link;

                UndertaleObject draggedItem = Highlighted as UndertaleObject;
                if (draggedItem != null)
                {
                    DataObject data = new DataObject(draggedItem);
                    //data.SetText(draggedItem.ToString());
                    /*if (draggedItem is UndertaleEmbeddedTexture)
                    {
                        UndertaleEmbeddedTexture tex = draggedItem as UndertaleEmbeddedTexture;
                        MemoryStream ms = new MemoryStream(tex.TextureData.TextureBlob);
                        PngBitmapDecoder decoder = new PngBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                        data.SetImage(decoder.Frames[0]);
                        Debug.WriteLine("PNG data attached");
                        effects |= DragDropEffects.Copy;
                    }*/

                    DragDrop.DoDragDrop(MainTree, data, effects);
                }
            }
        }
        private void TreeView_DragOver(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[e.Data.GetFormats().Length - 1]) as UndertaleObject; // TODO: make this more reliable

            TreeViewItem targetTreeItem = VisualUpwardSearch<TreeViewItem>(e.OriginalSource as UIElement);
            UndertaleObject targetItem = targetTreeItem.DataContext as UndertaleObject;

            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Move) && sourceItem != null && targetItem != null && sourceItem != targetItem && sourceItem.GetType() == targetItem.GetType() ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }
        private void TreeView_Drop(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[e.Data.GetFormats().Length - 1]) as UndertaleObject;

#if DEBUG
            Debug.WriteLine("Format(s) of dropped TreeViewItem - " + String.Join(", ", e.Data.GetFormats()));
#endif

            TreeViewItem targetTreeItem = VisualUpwardSearch<TreeViewItem>(e.OriginalSource as UIElement);
            UndertaleObject targetItem = targetTreeItem.DataContext as UndertaleObject;

            e.Effects = (e.AllowedEffects.HasFlag(DragDropEffects.Move) && sourceItem != null && targetItem != null && sourceItem != targetItem &&
                         sourceItem.GetType() == targetItem.GetType() && SettingsWindow.AssetOrderSwappingEnabled)
                            ? DragDropEffects.Move : DragDropEffects.None;
            if (e.Effects == DragDropEffects.Move)
            {
                object source = GetNearestParent<TreeViewItem>(targetTreeItem).ItemsSource;
                IList list = ((source as ICollectionView)?.SourceCollection as IList) ?? (source as IList);
                int sourceIndex = list.IndexOf(sourceItem);
                int targetIndex = list.IndexOf(targetItem);
                Debug.Assert(sourceIndex >= 0 && targetIndex >= 0);
                list[sourceIndex] = targetItem;
                list[targetIndex] = sourceItem;
            }
            e.Handled = true;
        }

        private static T VisualUpwardSearch<T>(DependencyObject element) where T : class
        {
            T container = element as T;
            while (container == null && element != null)
            {
                element = VisualTreeHelper.GetParent(element);
                container = element as T;
            }
            return container;
        }

        private static T GetNearestParent<T>(DependencyObject item) where T : class
        {
            DependencyObject parent = VisualTreeHelper.GetParent(item);
            while (parent is not T)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as T;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T t)
                    {
                        yield return t;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            foreach (childItem child in FindVisualChildren<childItem>(obj))
            {
                return child;
            }

            return null;
        }

        private TreeViewItem GetTreeViewItemFor(UndertaleObject obj)
        {
            foreach (var child in (MainTree.Items[0] as TreeViewItem).Items)
            {
                var twi = (child as TreeViewItem).ItemContainerGenerator.ContainerFromItem(obj) as TreeViewItem;
                if (twi != null)
                    return twi;
            }
            return null;
        }

        private void DeleteItem(UndertaleObject obj)
        {
            TreeViewItem container = GetNearestParent<TreeViewItem>(GetTreeViewItemFor(obj));
            object source = container.ItemsSource;
            IList list = ((source as ICollectionView)?.SourceCollection as IList) ?? (source as IList);
            bool isLast = list.IndexOf(obj) == list.Count - 1;
            if (MessageBox.Show("Delete " + obj.ToString() + "?" + (!isLast ? "\n\nNote that the code often references objects by ID, so this operation is likely to break stuff because other items will shift up!" : ""), "Confirmation", MessageBoxButton.YesNo, isLast ? MessageBoxImage.Question : MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                list.Remove(obj);
                if (obj is UndertaleCode codeObj)
                {
                    string codeName = codeObj.Name.Content;
                    Data.GMLCache?.TryRemove(codeName, out _);
                    Data.GMLCacheChanged = new ConcurrentBag<string>(Data.GMLCacheChanged.Except(new[] { codeName }));
                    Data.GMLCacheFailed?.Remove(codeName);
                    Data.GMLEditedBefore?.Remove(codeName);
                }

                CloseTab(obj);
                UpdateTree();

                // remove all tabs with deleted object occurrences from the closed tab history
                for (int i = 0; i < ClosedTabsHistory.Count; i++)
                {
                    if (ClosedTabsHistory[i].OpenedObject == obj)
                        ClosedTabsHistory.RemoveAt(i);
                }

                // remove consecutive duplicates ( { 1, 1, 2 } -> { 1, 2 } )
                for (int i = 0; i < ClosedTabsHistory.Count - 1; i++)
                {
                    if (ClosedTabsHistory[i] == ClosedTabsHistory[i + 1])
                        ClosedTabsHistory.RemoveAt(i);
                }
            }
        }
        private void CopyItemName(UndertaleNamedResource namedRes)
        {
            if (namedRes.Name?.Content is not null)
                Clipboard.SetText(namedRes.Name.Content);
            else
                ShowWarning("Item name is null.");
        }

        private void MainTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (Highlighted is UndertaleObject obj)
                    DeleteItem(obj);
            }
        }

        private async void CommandBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                Debug.WriteLine(CommandBox.Text);
                e.Handled = true;
                CommandBox.IsEnabled = false;
                object result;
                try
                {
                    if (!scriptSetupTask.IsCompleted)
                        await scriptSetupTask;

                    ScriptPath = null;

                    result = await CSharpScript.EvaluateAsync(CommandBox.Text, scriptOptions, this, typeof(IScriptInterface));
                }
                catch (CompilationErrorException exc)
                {
                    result = exc.Message;
                    Debug.WriteLine(exc);
                }
                catch (Exception exc)
                {
                    result = exc;
                }
                if (FinishedMessageEnabled)
                {
                    Dispatcher.Invoke(() => CommandBox.Text = result != null ? result.ToString() : "");
                }
                else
                {
                    FinishedMessageEnabled = true;
                }
                CommandBox.IsEnabled = true;
            }
        }

        private void Command_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            // TODO: ugly, but I can't get focus to work properly
            /*var command = FindVisualChild<UndertaleRoomEditor>(DataEditor)?.CommandBindings.OfType<CommandBinding>()
                .FirstOrDefault(cmd => cmd.Command == e.Command);

            if (command != null && command.Command.CanExecute(e.Parameter))
                command.Command.Execute(e.Parameter);*/
            FindVisualChild<UndertaleRoomEditor>(DataEditor)?.Command_Copy(sender, e);
        }

        private void Command_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            FindVisualChild<UndertaleRoomEditor>(DataEditor)?.Command_Paste(sender, e);
        }

        private void MainTree_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch<TreeViewItem>(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                e.Handled = true;
            }
        }

        private void MenuItem_OpenInNewTab_Click(object sender, RoutedEventArgs e)
        {
            OpenInTab(Highlighted, true);
        }
        private void MenuItem_CopyName_Click(object sender, RoutedEventArgs e)
        {
            if (Highlighted is UndertaleNamedResource namedRes)
                CopyItemName(namedRes);
        }
        private void MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (Highlighted is UndertaleObject obj)
                DeleteItem(obj);
        }

        private void MenuItem_Add_Click(object sender, RoutedEventArgs e)
        {
            object source = null;
            try
            {
                source = (MainTree.SelectedItem as TreeViewItem).ItemsSource;
            }
            catch (Exception ex)
            {
                ScriptError("An error occurred while trying to add the menu item. No action has been taken.\r\n\r\nError:\r\n\r\n" + ex.ToString());
                return;
            }
            IList list = ((source as ICollectionView)?.SourceCollection as IList) ?? (source as IList);
            Type t = list.GetType().GetGenericArguments()[0];
            Debug.Assert(typeof(UndertaleResource).IsAssignableFrom(t));
            UndertaleResource obj = Activator.CreateInstance(t) as UndertaleResource;
            if (obj is UndertaleNamedResource)
            {
                bool doMakeString = obj is not (UndertaleTexturePageItem or UndertaleEmbeddedAudio or UndertaleEmbeddedTexture);
                string notDataNewName = null;
                if (obj is UndertaleTexturePageItem)
                {
                    notDataNewName = "PageItem " + list.Count;
                }
                if ((obj is UndertaleExtension) && (IsExtProductIDEligible == Visibility.Visible))
                {
                    var newProductID = new byte[] { 0xBA, 0x5E, 0xBA, 0x11, 0xBA, 0xDD, 0x06, 0x60, 0xBE, 0xEF, 0xED, 0xBA, 0x0B, 0xAB, 0xBA, 0xBE };
                    Data.FORM.EXTN.productIdData.Add(newProductID);
                }
                if (obj is UndertaleEmbeddedAudio)
                {
                    notDataNewName = "EmbeddedSound " + list.Count;
                }
                if (obj is UndertaleEmbeddedTexture)
                {
                    notDataNewName = "Texture " + list.Count;
                }

                if (doMakeString)
                {
                    string newname = obj.GetType().Name.Replace("Undertale", "").Replace("GameObject", "Object").ToLower() + list.Count;
                    (obj as UndertaleNamedResource).Name = Data.Strings.MakeString(newname);
                    if (obj is UndertaleRoom)
                    {
                        (obj as UndertaleRoom).Caption = Data.Strings.MakeString("");

                        if (IsGMS2 == Visibility.Visible)
                            (obj as UndertaleRoom).Flags |= UndertaleRoom.RoomEntryFlags.IsGMS2;
                    }

                    if (obj is UndertaleScript)
                    {
                        UndertaleCode code = new UndertaleCode();
                        code.Name = Data.Strings.MakeString("gml_Script_" + newname);
                        Data.Code.Add(code);
                        if (Data?.GeneralInfo.BytecodeVersion > 14)
                        {
                            UndertaleCodeLocals locals = new UndertaleCodeLocals();
                            locals.Name = code.Name;
                            UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
                            argsLocal.Name = Data.Strings.MakeString("arguments");
                            argsLocal.Index = 0;
                            locals.Locals.Add(argsLocal);
                            code.LocalsCount = 1;
                            code.GenerateLocalVarDefinitions(code.FindReferencedLocalVars(), locals);
                            Data.CodeLocals.Add(locals);
                        }
                        (obj as UndertaleScript).Code = code;
                    }
                    if ((obj is UndertaleCode) && (Data?.GeneralInfo.BytecodeVersion > 14))
                    {
                        UndertaleCodeLocals locals = new UndertaleCodeLocals();
                        locals.Name = (obj as UndertaleCode).Name;
                        UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
                        argsLocal.Name = Data.Strings.MakeString("arguments");
                        argsLocal.Index = 0;
                        locals.Locals.Add(argsLocal);
                        (obj as UndertaleCode).LocalsCount = 1;
                        (obj as UndertaleCode).GenerateLocalVarDefinitions((obj as UndertaleCode).FindReferencedLocalVars(), locals);
                        Data.CodeLocals.Add(locals);
                    }
                }
                else
                {
                    (obj as UndertaleNamedResource).Name = new UndertaleString(notDataNewName); // not Data.MakeString!
                }
            }
            list.Add(obj);
            UpdateTree();
            HighlightObject(obj);
            OpenInTab(obj, true);
        }

        private void MenuItem_RunBuiltinScript_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            MenuItem_RunScript_SubmenuOpened(sender, e, "SampleScripts");
        }
        private void MenuItem_RunCommunityScript_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            MenuItem_RunScript_SubmenuOpened(sender, e, "CommunityScripts");
        }
        private void MenuItem_RunTechnicalScript_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            MenuItem_RunScript_SubmenuOpened(sender, e, "TechnicalScripts");
        }
        private void MenuItem_RunUnpackScript_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            MenuItem_RunScript_SubmenuOpened(sender, e, "Unpackers");
        }
        private void MenuItem_RunRepackScript_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            MenuItem_RunScript_SubmenuOpened(sender, e, "Repackers");
        }
        private void MenuItem_RunDemoScript_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            MenuItem_RunScript_SubmenuOpened(sender, e, "DemoScripts");
        }
        private void MenuItem_RunScript_SubmenuOpened(object sender, RoutedEventArgs e, string folderName)
        {
            MenuItem item = sender as MenuItem;
            item.Items.Clear();
            try
            {
                var appDir = Program.GetExecutableDirectory();
                foreach (var path in Directory.EnumerateFiles(Path.Combine(appDir, folderName)))
                {
                    var filename = Path.GetFileName(path);
                    if (!filename.EndsWith(".csx"))
                        continue;
                    MenuItem subitem = new MenuItem() { Header = filename.Replace("_", "__") };
                    subitem.Click += MenuItem_RunBuiltinScript_Item_Click;
                    subitem.CommandParameter = path;
                    item.Items.Add(subitem);
                }
                if (item.Items.Count == 0)
                    item.Items.Add(new MenuItem() { Header = "(whoops, no scripts found?)", IsEnabled = false });
            }
            catch (Exception err)
            {
                item.Items.Add(new MenuItem() { Header = err.ToString(), IsEnabled = false });
            }
        }

        public void UpdateProgressBar(string message, string status, double progressValue, double maxValue)
        {
            if (scriptDialog != null)
            {
                scriptDialog.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => {
                    scriptDialog.Update(message, status, progressValue, maxValue);
                }));
            }
        }

        public void SetProgressBar(string message, string status, double progressValue, double maxValue)
        {
            if (scriptDialog != null)
            {
                this.progressValue = (int)progressValue;
                scriptDialog.SavedStatusText = status;

                UpdateProgressBar(message, status, progressValue, maxValue);
            }
        }
        public void SetProgressBar()
        {
            if (scriptDialog != null && !scriptDialog.IsVisible)
                scriptDialog.Dispatcher.Invoke(scriptDialog.Show);
        }

        public void UpdateProgressValue(double progressValue)
        {
            if (scriptDialog != null)
            {
                scriptDialog.Dispatcher.Invoke(DispatcherPriority.Normal, (Action) (() => {
                    scriptDialog.ReportProgress(progressValue);
                }));
            }
        }

        public void UpdateProgressStatus(string status)
        {
            if (scriptDialog != null)
            {
                scriptDialog.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => {
                    scriptDialog.ReportProgress(status);
                }));
            }
        }

        public void HideProgressBar()
        {
            if (scriptDialog != null)
                scriptDialog.TryHide();
        }

        public void AddProgress(int amount)
        {
            progressValue += amount;
        }
        public void IncrementProgress()
        {
            progressValue++;
        }
        public void AddProgressParallel(int amount) //P - Parallel (multithreaded)
        {
            Interlocked.Add(ref progressValue, amount); //thread-safe add operation (not the same as "lock ()")
        }
        public void IncrementProgressParallel()
        {
            Interlocked.Increment(ref progressValue); //thread-safe increment
        }
        public int GetProgress()
        {
            return progressValue;
        }
        public void SetProgress(int value)
        {
            progressValue = value;
        }

        public void EnableUI()
        {
            if (!this.IsEnabled)
                this.IsEnabled = true;
        }

        public void SyncBinding(string resourceType, bool enable)
        {
            if (resourceType.Contains(',')) //if several types are listed
            {
                string[] resTypes = resourceType.Replace(" ", "").Split(',');

                if (enable)
                {
                    foreach (string resType in resTypes)
                    {
                        IEnumerable resListCollection = Data[resType] as IEnumerable;
                        if (resListCollection is not null)
                        {
                            BindingOperations.EnableCollectionSynchronization(resListCollection, bindingLock);

                            syncBindings.Add(resType);
                        }
                    }
                }
                else
                {
                    foreach (string resType in resTypes)
                    {
                        if (syncBindings.Contains(resType))
                        {
                            BindingOperations.DisableCollectionSynchronization(Data[resType] as IEnumerable);

                            syncBindings.Remove(resType);
                        }
                    }
                }
            }
            else
            {
                if (enable)
                {
                    IEnumerable resListCollection = Data[resourceType] as IEnumerable;
                    if (resListCollection is not null)
                    {
                        BindingOperations.EnableCollectionSynchronization(resListCollection, bindingLock);

                        syncBindings.Add(resourceType);
                    }
                }
                else if (syncBindings.Contains(resourceType))
                {
                    BindingOperations.DisableCollectionSynchronization(Data[resourceType] as IEnumerable);

                    syncBindings.Remove(resourceType);
                }
            }
        }
        public void DisableAllSyncBindings() //disable all sync. bindings
        {
            if (syncBindings.Count <= 0) return;

            foreach (string resType in syncBindings)
                BindingOperations.DisableCollectionSynchronization(Data[resType] as IEnumerable);

            syncBindings.Clear();
        }

        private void ProgressUpdater()
        {
            DateTime prevTime = default;
            int prevValue = 0;

            while (true)
            {
                if (cToken.IsCancellationRequested)
                {
                    if (prevValue >= progressValue) //if reached maximum
                        return;
                    else
                    {
                        if (prevTime == default)
                            prevTime = DateTime.UtcNow;                                       //begin measuring
                        else if (DateTime.UtcNow.Subtract(prevTime).TotalMilliseconds >= 500) //timeout - 0.5 seconds
                            return;
                    }
                }

                UpdateProgressValue(progressValue);

                prevValue = progressValue;

                Thread.Sleep(100); //10 times per second
            }
        }
        public void StartProgressBarUpdater()
        {
            if (cts is not null)
                ScriptWarning("Warning - there is another progress bar updater task running (hangs) in the background.\nRestart the application to prevent some unexpected behavior.");

            cts = new CancellationTokenSource();
            cToken = cts.Token;

            updater = Task.Run(ProgressUpdater);
        }
        public async Task StopProgressBarUpdater() //async because "Wait()" blocks UI thread
        {
            if (cts is null) return;

            cts.Cancel();

            if (await Task.Run(() => !updater.Wait(2000))) //if ProgressUpdater isn't responding
                ScriptError("Stopping the progress bar updater task is failed.\nIt's highly recommended to restart the application.",
                    "Script error", false);
            else
            {
                cts.Dispose();
                cts = null;
            }

            updater.Dispose();
        }

        public void OpenCodeFile(string name, CodeEditorMode editorDecompile)
        {
            UndertaleCode code = Data.Code.ByName(name);

            if (code is not null)
            {
                Focus();

                CodeEditorDecompile = editorDecompile;

                HighlightObject(code);
                ChangeSelection(code);
            }
            else
            {
                ShowError($"Can't find code \"{name}\".\n(probably, different game data was loaded)");
            }
        }

        public string ProcessException(in Exception exc, in string scriptText)
        {
            List<int> excLineNums = new();
            string excText = string.Empty;
            List<string> traceLines = new();
            Dictionary<string, int> exTypesDict = null;

            if (exc is AggregateException)
            {
                List<string> exTypes = new();

                foreach (Exception ex in (exc as AggregateException).InnerExceptions)
                {
                    traceLines.AddRange(ex.StackTrace.Split(Environment.NewLine));
                    exTypes.Add(ex.GetType().FullName);
                }

                if (exTypes.Count > 1)
                {
                    exTypesDict = exTypes.GroupBy(x => x)
                                         .Select(x => new { Name = x.Key, Count = x.Count() })
                                         .OrderByDescending(x => x.Count)
                                         .ToDictionary(x => x.Name, x => x.Count);
                }
            }
            else if (exc.InnerException is not null)
            {
                traceLines.AddRange(exc.InnerException.StackTrace.Split(Environment.NewLine));
            }

            traceLines.AddRange(exc.StackTrace.Split(Environment.NewLine));

            try
            {
                foreach (string traceLine in traceLines)
                {
                    if (traceLine.TrimStart()[..13] == "at Submission") // only stack trace lines from the script
                    {
                        int linePos = traceLine.IndexOf(":line ") + 6;  // ":line ".Length = 6
                        if (linePos != (-1 + 6))
                        {
                            int lineNum = Convert.ToInt32(traceLine[linePos..]);
                            if (!excLineNums.Contains(lineNum))
                                excLineNums.Add(lineNum);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                string excString = exc.ToString();

                int endOfPrevStack = excString.IndexOf("--- End of stack trace from previous location ---");
                if (endOfPrevStack != -1)
                    excString = excString[..endOfPrevStack]; //keep only stack trace of the script

                return $"An error occurred while processing the exception text.\nError message - \"{e.Message}\"\nThe unprocessed text is below.\n\n" + excString;
            }

            if (excLineNums.Count > 0) //if line number(s) is found
            {
                string[] scriptLines = scriptText.Split('\n');
                string excLines = string.Join('\n', excLineNums.Select(n => $"Line {n}: {scriptLines[n - 1].TrimStart(new char[] { '\t', ' ' })}"));
                if (exTypesDict is not null)
                {
                    string exTypesStr = string.Join(",\n", exTypesDict.Select(x => $"{x.Key}{((x.Value > 1) ? " (x" + x.Value + ")" : string.Empty)}"));
                    excText = $"{exc.GetType().FullName}: One on more errors occured:\n{exTypesStr}\n\nThe current stacktrace:\n{excLines}";
                }
                else
                    excText = $"{exc.GetType().FullName}: {exc.Message}\n\nThe current stacktrace:\n{excLines}";
            }
            else
            {
                string excString = exc.ToString();

                int endOfPrevStack = excString.IndexOf("--- End of stack trace from previous location ---");
                if (endOfPrevStack != -1)
                    excString = excString[..endOfPrevStack]; //keep only stack trace of the script

                excText = excString;
            }

            return excText;
        }

        public async Task RunScript(string path)
        {
            ScriptExecutionSuccess = true;
            ScriptErrorMessage = "";
            ScriptErrorType = "";
            scriptDialog = new LoaderDialog("Script in progress...", "Please wait...");
            scriptDialog.Owner = this;
            scriptDialog.PreventClose = true;
            this.IsEnabled = false; // Prevent interaction while the script is running.

            await RunScriptNow(path); // Runs the script now.
            HideProgressBar(); // Hide the progress bar.
            scriptDialog = null;
            this.IsEnabled = true; // Allow interaction again.
        }

        private async Task RunScriptNow(string path)
        {
            string scriptText = File.ReadAllText(path);
            Debug.WriteLine(path);

            Dispatcher.Invoke(() => CommandBox.Text = "Running " + Path.GetFileName(path) + " ...");
            try
            {
                if (!scriptSetupTask.IsCompleted)
                    await scriptSetupTask;

                ScriptPath = path;

                string compatScriptText = Regex.Replace(scriptText, @"\bDecompileContext\b", "GlobalDecompileContext", RegexOptions.None);
                object result = await CSharpScript.EvaluateAsync(compatScriptText, scriptOptions, this, typeof(IScriptInterface));

                if (FinishedMessageEnabled)
                {
                    Dispatcher.Invoke(() => CommandBox.Text = result != null ? result.ToString() : Path.GetFileName(path) + " finished!");
                }
                else
                {
                    FinishedMessageEnabled = true;
                }
            }
            catch (CompilationErrorException exc)
            {
                Console.WriteLine(exc.ToString());
                Dispatcher.Invoke(() => CommandBox.Text = exc.Message);
                MessageBox.Show(exc.Message, "Script compile error", MessageBoxButton.OK, MessageBoxImage.Error);
                ScriptExecutionSuccess = false;
                ScriptErrorMessage = exc.Message;
                ScriptErrorType = "CompilationErrorException";
            }
            catch (Exception exc)
            {
                bool isScriptException = exc.GetType().Name == "ScriptException";
                string excString = string.Empty;

                if (!isScriptException)
                    excString = ProcessException(in exc, in scriptText);

                await StopProgressBarUpdater();

                Console.WriteLine(exc.ToString());
                Dispatcher.Invoke(() => CommandBox.Text = exc.Message);
                MessageBox.Show(isScriptException ? exc.Message : excString, "Script error", MessageBoxButton.OK, MessageBoxImage.Error);
                ScriptExecutionSuccess = false;
                ScriptErrorMessage = exc.Message;
                ScriptErrorType = "Exception";
            }
            scriptText = null;
        }

        public string PromptLoadFile(string defaultExt, string filter)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = defaultExt ?? "win";
            dlg.Filter = filter ?? "Game Maker Studio data files (.win, .unx, .ios, .droid, audiogroup*.dat)|*.win;*.unx;*.ios;*.droid;audiogroup*.dat|All files|*";
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        #pragma warning disable CA1416
        public string PromptChooseDirectory()
        {
            VistaFolderBrowserDialog folderBrowser = new VistaFolderBrowserDialog();
            // vista dialog doesn't suffix the folder name with "/", so we're fixing it here.
            return folderBrowser.ShowDialog() == true ? folderBrowser.SelectedPath + "/" : null;
        }

        #pragma warning disable CA1416
        public void PlayInformationSound()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                System.Media.SystemSounds.Asterisk.Play();
        }
        #pragma warning restore CA1416

        public void ScriptMessage(string message)
        {
            MessageBox.Show(message, "Script message", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        public bool ScriptQuestion(string message)
        {
            PlayInformationSound();
            return MessageBox.Show(message, "Script message", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
        public void ScriptWarning(string message)
        {
            MessageBox.Show(message, "Script warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        public void ScriptError(string error, string title = "Error", bool SetConsoleText = true)
        {
            MessageBox.Show(error, title, MessageBoxButton.OK, MessageBoxImage.Error);
            if (SetConsoleText)
            {
                SetUMTConsoleText(error);
                SetFinishedMessage(false);
            }
        }

        public static void ShowMessage(string message, bool wait = true)
        {
            if (wait)
                MessageBox.Show(message, "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                _ = Task.Run(() => MessageBox.Show(message, "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Information));
        }
        public static MessageBoxResult ShowQuestion(string message, MessageBoxImage icon = MessageBoxImage.Question)
        {
            return MessageBox.Show(message, "UndertaleModTool", MessageBoxButton.YesNo, icon);
        }
        public static void ShowWarning(string message, bool wait = true)
        {
            if (wait)
                MessageBox.Show(message, "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Warning);
            else
                _ = Task.Run(() => MessageBox.Show(message, "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Warning));
        }
        public static void ShowError(string message, bool wait = true)
        {
            if (wait)
                MessageBox.Show(message, "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Error);
            else
                _ = Task.Run(() => MessageBox.Show(message, "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Error));
        }

        public void SetUMTConsoleText(string message)
        {
            this.Dispatcher.Invoke(() =>
            {
                CommandBox.Text = message;
            });
        }
        public void SetFinishedMessage(bool isFinishedMessageEnabled)
        {
            this.Dispatcher.Invoke(() =>
            {
                FinishedMessageEnabled = isFinishedMessageEnabled;
            });
        }

        public string SimpleTextInput(string titleText, string labelText, string defaultInputBoxText, bool isMultiline, bool showDialog = true)
        {
            TextInput input = new TextInput(labelText, titleText, defaultInputBoxText, isMultiline);

            System.Windows.Forms.DialogResult result = System.Windows.Forms.DialogResult.None;
            if (showDialog)
            {
                result = input.ShowDialog();
                input.Dispose();

                if (result == System.Windows.Forms.DialogResult.OK)
                    return input.ReturnString.Replace('\v', '\n'); //values preserved after close; Shift+Enter -> '\v'
                else
                    return null;
            }
            else //if we don't need to wait for result
            {
                input.Show();
                return null;
                //no need to call input.Dispose(), because if form wasn't shown modally, Form.Close() (or closing it with "X") also calls Dispose()
            }
        }

        public void SimpleTextOutput(string titleText, string labelText, string message, bool isMultiline)
        {
            TextInput textOutput = new TextInput(labelText, titleText, message, isMultiline, true); //read-only mode
            textOutput.Show();
        }
        public async Task ClickableSearchOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<string>>> resultsDict, bool showInDecompiledView, IOrderedEnumerable<string> failedList = null)
        {
            await Task.Delay(150); //wait until progress bar status is displayed

            ClickableTextOutput textOutput = new(title, query, resultsCount, resultsDict, showInDecompiledView, failedList);

            await textOutput.Dispatcher.InvokeAsync(textOutput.GenerateResults);
            _ = Task.Factory.StartNew(textOutput.FillingNotifier, TaskCreationOptions.LongRunning); //"LongRunning" = prefer creating a new thread

            textOutput.Show();

            PlayInformationSound();
        }
        public async Task ClickableSearchOutput(string title, string query, int resultsCount, IDictionary<string, List<string>> resultsDict, bool showInDecompiledView, IEnumerable<string> failedList = null)
        {
            await Task.Delay(150);

            ClickableTextOutput textOutput = new(title, query, resultsCount, resultsDict, showInDecompiledView, failedList);

            await textOutput.Dispatcher.InvokeAsync(textOutput.GenerateResults);
            _ = Task.Factory.StartNew(textOutput.FillingNotifier, TaskCreationOptions.LongRunning);

            textOutput.Show();

            PlayInformationSound();
        }

        public void ScriptOpenURL(string url)
        {
            OpenBrowser(url);
        }

        public string ScriptInputDialog(string title, string label, string defaultInput, string cancelText, string submitText, bool isMultiline, bool preventClose)
        {
            TextInputDialog dlg = new TextInputDialog(title, label, defaultInput, cancelText, submitText, isMultiline, preventClose);
            bool? dlgResult = dlg.ShowDialog();

            if (!dlgResult.HasValue || dlgResult == false)
            {
                // returns null (not an empty!!!) string if the dialog has been closed, or an error has occured.
                return null;
            }

            // otherwise just return the input (it may be empty aka .Length == 0).
            return dlg.InputText;
        }

        private async void MenuItem_RunBuiltinScript_Item_Click(object sender, RoutedEventArgs e)
        {
            string path = (string)(sender as MenuItem).CommandParameter;
            await RunScript(path);
        }

        private async void MenuItem_RunOtherScript_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.DefaultExt = "csx";
            dlg.Filter = "Scripts (.csx)|*.csx|All files|*";

            if (dlg.ShowDialog() == true)
            {
                await RunScript(dlg.FileName);
            }
        }

        private void MenuItem_GitHub_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser("https://github.com/krzys-h/UndertaleModTool");
        }

        private void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("UndertaleModTool by krzys_h\nVersion " + Version, "About", MessageBoxButton.OK);
        }

        /// From https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.Dialogs/AboutAvaloniaDialog.xaml.cs
        public static void OpenBrowser(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    using (var process = Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = "/bin/sh",
                            Arguments = $"-c \"{$"xdg-open {url}".Replace("\"", "\\\"")}\"",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        }
                    )) { }
                }
                else
                {
                    using (var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? url : "open",
                        Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? $"{url}" : "",
                        CreateNoWindow = true,
                        UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    })) { }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to open browser!\n" + e.ToString());
            }
        }

        public static void OpenFolder(string folder)
        {
            if (!folder.EndsWith(Path.DirectorySeparatorChar))
                folder += Path.DirectorySeparatorChar;

            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = folder,
                    UseShellExecute = true,
                    Verb = "Open"
                });
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to open folder!\n" + e.ToString());
            }
        }


        private async Task<HttpResponseMessage> HttpGetAsync(string uri)
        {
            try
            {
                return await httpClient.GetAsync(uri);
            }
            catch (Exception exp) when (exp is not NullReferenceException)
            {
                return null;
            }
        }
        public async void UpdateApp(SettingsWindow window)
        {
            //TODO: rewrite this slightly + comment this out so this is clearer on what this does.

            window.UpdateButtonEnabled = false;

            httpClient = new();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            // remove the invalid characters (everything within square brackets) from the version string.
            // Probably needs to be expanded later, these are just the ones I know of.
            Regex invalidChars = new Regex(@"[  ()]");
            string version = invalidChars.Replace(Version, "");
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("UndertaleModTool", version));

            double bytesToMB = 1024 * 1024;

            if (!Environment.Is64BitOperatingSystem)
            {
                if (ShowQuestion("Your operating system is 32-bit.\n" +
                                 "32-bit (x86) version of UndertaleModTool is obsolete.\n" +
                                 "Do you want to continue anyway?", MessageBoxImage.Error) != MessageBoxResult.Yes)
                {
                    window.UpdateButtonEnabled = true;
                    return;
                }
            }

            string sysDriveLetter = Path.GetTempPath()[0].ToString();
            if ((new DriveInfo(sysDriveLetter).AvailableFreeSpace / bytesToMB) < 500)
            {
                ShowError($"Not enough space on the system drive {sysDriveLetter} - at least 500 MB is required.");
                window.UpdateButtonEnabled = true;
                return;
            }

            bool isSingleFile = !File.Exists(Path.Combine(ExePath, "UndertaleModTool.dll"));
            string assemblyLocation = AppDomain.CurrentDomain.GetAssemblies()
                                      .First(x => x.GetName().Name.StartsWith("System.Collections")).Location; // any of currently used assemblies
            bool isBundled = !Regex.Match(assemblyLocation, @"C:\\Program Files( \(x86\))*\\dotnet\\shared\\").Success;

            string baseUrl = "https://api.github.com/repos/krzys-h/UndertaleModTool/actions/";
            string detectedActionName = $"Publish GUI{(!Environment.Is64BitOperatingSystem ? " 32Bit" : "")}";

            // Fetch the latest workflow run
            var result = await HttpGetAsync(baseUrl + "runs?branch=master&status=success&per_page=20");
            if (result?.IsSuccessStatusCode != true)
            {
                string errText = $"{(result is null ? "Check your internet connection." : $"HTTP error - {result.ReasonPhrase}.")}";
                ShowError($"Failed to fetch latest build!\n{errText}");
                window.UpdateButtonEnabled = true;
                return;
            }
            // Parse it as JSON
            var actionInfo = JObject.Parse(await result.Content.ReadAsStringAsync());
            var actionList = (JArray)actionInfo["workflow_runs"];
            JObject action = null;

            for (int index = 0; index < actionList.Count; index++)
            {
                var currentAction = (JObject)actionList[index];
                if (currentAction["name"].ToString() == detectedActionName)
                {
                    action = currentAction;
                    break;
                }
            }
            if (action == null)
            {
                ShowError($"Failed to find latest build!\nDetected action name - {detectedActionName}");
                window.UpdateButtonEnabled = true;
                return;
            }

            DateTime currDate = File.GetLastWriteTime(Path.Combine(ExePath, "UndertaleModTool.exe"));
            DateTime lastDate = (DateTime)action["updated_at"];
            if (lastDate.Subtract(currDate).Minutes <= 10)
                if (ShowQuestion("UndertaleModTool is already up to date.\nUpdate anyway?") != MessageBoxResult.Yes)
                {
                    window.UpdateButtonEnabled = true;
                    return;
                }

            var result2 = await HttpGetAsync($"{baseUrl}runs/{action["id"]}/artifacts"); // Grab information about the artifacts
            if (result2?.IsSuccessStatusCode != true)
            {
                string errText = $"{(result2 is null ? "Check your internet connection." : $"HTTP error - {result2.ReasonPhrase}.")}";
                ShowError($"Failed to fetch latest build!\n{errText}");
                window.UpdateButtonEnabled = true;
                return;
            }

            var artifactInfo = JObject.Parse(await result2.Content.ReadAsStringAsync()); // And now parse them as JSON
            var artifactList = (JArray) artifactInfo["artifacts"];                       // Grab the array of artifacts

            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                if (ShowQuestion("Detected 32-bit (x86) version of UndertaleModTool on the 64-bit operating system.\n" +
                                 "It's highly recommended to use the 64-bit version instead.\n" +
                                 "Download that?") != MessageBoxResult.Yes)
                {
                    window.UpdateButtonEnabled = true;
                    return;
                }
            }

            JObject artifact = null;
            for (int index = 0; index < artifactList.Count; index++)
            {
                var currentArtifact = (JObject) artifactList[index];
                string artifactName = (string)currentArtifact["name"];

                if (Environment.Is64BitOperatingSystem)
                {
                    if (artifactName.Contains($"isBundled-{isBundled.ToString().ToLower()}-isSingleFile-{isSingleFile.ToString().ToLower()}"))
                        artifact = currentArtifact;
                }
                else
                {
                    if (artifactName.Contains($"isSingleFile-{isSingleFile.ToString().ToLower()}"))
                        artifact = currentArtifact;
                }
            }
            if (artifact is null)
            {
                ShowError("Failed to find the artifact!");
                window.UpdateButtonEnabled = true;
                return;
            }

            // Github doesn't let anonymous users download artifacts, so let's use nightly.link

            string baseDownloadUrl = artifact["archive_download_url"].ToString();
            string downloadUrl = baseDownloadUrl.Replace("api.github.com/repos", "nightly.link").Replace("/zip", ".zip");

            string tempFolder = Path.Combine(Path.GetTempPath(), "UndertaleModTool");
            Directory.CreateDirectory(tempFolder); // We're about to download, so make sure the download dir actually exists
            File.WriteAllText(Path.Combine(tempFolder, "detectedActionName.txt"), detectedActionName); // for debugging purposes (will be removed later)

            // It's time to download; let's use a cool progress bar
            scriptDialog = new("Downloading", "Downloading new version...")
            {
                PreventClose = true,
                Owner = this,
                StatusText = "Downloaded MB: 0.00"
            };
            SetProgressBar();

            using (WebClient webClient = new())
            {
                bool end = false;
                bool ended = false;
                string downloaded = "0.00";

                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler((sender, e) =>
                {
                    if (!end)
                        downloaded = (e.BytesReceived / bytesToMB).ToString("F2", CultureInfo.InvariantCulture);
                });
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler((sender, e) =>
                {
                    end = true;

                    HideProgressBar();
                    _ = Task.Run(() =>
                    {
                        // wait until progress bar updater loop is finished
                        while (!ended)
                            Thread.Sleep(100);

                        scriptDialog = null;
                    });

                    if (e.Error is not null)
                    {
                        string errMsg;

                        if (e.Error.InnerException?.InnerException is Exception ex)
                        {
                            if (ex.Message.StartsWith("Unable to read data")
                                && e.Error.InnerException.Message.StartsWith("The SSL connection could not be established"))
                            {
                                errMsg = "Failed to download new version of UndertaleModTool.\n" +
                                         "Error - The SSL connection could not be established.";

                                bool isWin7 = Environment.OSVersion.Version.Major == 6;
                                string win7upd = "\nProbably, you need to install Windows update KB2992611.\n" +
                                                 "Open the update download page?";

                                if (isWin7)
                                {
                                    if (ShowQuestion(errMsg + win7upd, MessageBoxImage.Error) == MessageBoxResult.Yes)
                                        OpenBrowser("https://www.microsoft.com/en-us/download/details.aspx?id=44622");

                                    window.UpdateButtonEnabled = true;
                                    return;
                                }
                            }
                            else
                                errMsg = ex.Message;
                        }
                        else if (e.Error.InnerException is Exception ex1)
                            errMsg = ex1.Message;
                        else
                            errMsg = e.Error.Message;

                        ShowError($"Failed to download new version of UndertaleModTool.\nError - {errMsg}.");
                        window.UpdateButtonEnabled = true;
                        return;
                    }

                    string updaterFolder = Path.Combine(ExePath, "Updater");
                    if (!File.Exists(Path.Combine(updaterFolder, "UndertaleModToolUpdater.exe")))
                    {
                        ShowError("Updater not found! Aborting update, report this to the devs!\nLocation checked: " + updaterFolder);
                        window.UpdateButtonEnabled = true;
                        return;
                    }

                    string updaterFolderTemp = Path.Combine(tempFolder, "Updater");
                    try
                    {
                        if (Directory.Exists(updaterFolderTemp))
                            Directory.Delete(updaterFolderTemp, true);

                        Directory.CreateDirectory(updaterFolderTemp);
                        foreach (string file in Directory.GetFiles(updaterFolder))
                        {
                            File.Copy(file, Path.Combine(updaterFolderTemp, Path.GetFileName(file)));
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowError($"Can't copy the updater app to the temporary folder.\n{ex}");
                        window.UpdateButtonEnabled = true;
                        return;
                    }
                    File.WriteAllText(Path.Combine(updaterFolderTemp, "actualAppFolder"), ExePath);

                    window.UpdateButtonEnabled = true;

                    ShowMessage("UndertaleModTool will now close to finish the update.");

                    Process.Start(new ProcessStartInfo(Path.Combine(updaterFolderTemp, "UndertaleModToolUpdater.exe"))
                    {
                        WorkingDirectory = updaterFolderTemp
                    });

                    CloseOtherWindows();

                    Closing -= DataWindow_Closing; // disable "on window closed" event handler
                    Close();
                });

                _ = Task.Run(() =>
                {
                    while (!end)
                    {
                        try
                        {
                            UpdateProgressStatus($"Downloaded MB: {downloaded}");
                        }
                        catch {}

                        Thread.Sleep(100);
                    }

                    ended = true;
                });

                webClient.DownloadFileAsync(new Uri(downloadUrl), Path.GetTempPath() + "UndertaleModTool\\Update.zip");
            }
        }

        private async void Command_Run(object sender, ExecutedRoutedEventArgs e)
        {
            if (Data == null)
            {
                ScriptError("Nothing to run!");
                return;
            }
            if ((!WasWarnedAboutTempRun) && SettingsWindow.TempRunMessageShow)
            {
                ScriptMessage(@"WARNING:
Temp running the game does not permanently 
save your changes. Please ""Save"" the game
to save your changes. Closing UndertaleModTool
without using the ""Save"" option can
result in loss of work.");
                WasWarnedAboutTempRun = true;
            }
            bool saveOk = true;
            string oldFilePath = FilePath;
            bool oldDisableDebuggerState = true;
            int oldSteamValue = 0;
            oldDisableDebuggerState = Data.GeneralInfo.DisableDebugger;
            oldSteamValue = Data.GeneralInfo.SteamAppID;
            Data.GeneralInfo.SteamAppID = 0;
            Data.GeneralInfo.DisableDebugger = true;
            string TempFilesFolder = (oldFilePath != null ? Path.Combine(Path.GetDirectoryName(oldFilePath), "MyMod.temp") : "");
            await SaveFile(TempFilesFolder, false);
            Data.GeneralInfo.SteamAppID = oldSteamValue;
            FilePath = oldFilePath;
            Data.GeneralInfo.DisableDebugger = oldDisableDebuggerState;
            if (TempFilesFolder == null)
            {
                ShowWarning("Temp folder is null.");
                return;
            }
            else if (saveOk)
            {
                string gameExeName = Data?.GeneralInfo?.Filename?.Content;
                if (gameExeName == null || FilePath == null)
                {
                    ScriptError("Null game executable name or location");
                    return;
                }
                string gameExePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(FilePath), gameExeName + ".exe");
                if (!File.Exists(gameExePath))
                {
                    ScriptError("Cannot find game executable path, expected: " + gameExePath);
                    return;
                }
                if (!File.Exists(TempFilesFolder))
                {
                    ScriptError("Cannot find game path, expected: " + TempFilesFolder);
                    return;
                }
                if (gameExeName != null)
                    Process.Start(gameExePath, "-game \"" + TempFilesFolder + "\" -debugoutput \"" + Path.ChangeExtension(TempFilesFolder, ".gamelog.txt") + "\"");
            }
            else if (!saveOk)
            {
                ShowWarning("Temp save failed, cannot run.");
                return;
            }
            if (File.Exists(TempFilesFolder))
            {
                await Task.Delay(3000);
                //File.Delete(TempFilesFolder);
            }
        }
        private async void Command_RunSpecial(object sender, ExecutedRoutedEventArgs e)
        {
            if (Data == null)
                return;

            bool saveOk = true;
            if (!Data.GeneralInfo.DisableDebugger)
            {
                if (ShowQuestion("The game has the debugger enabled. Would you like to disable it so the game will run?") == MessageBoxResult.Yes)
                {
                    Data.GeneralInfo.DisableDebugger = true;
                    if (!await DoSaveDialog())
                    {
                        ShowError("You must save your changes to run.");
                        Data.GeneralInfo.DisableDebugger = false;
                        return;
                    }
                }
                else
                {
                    ShowError("Use the \"Run game using debugger\" option to run this game.");
                    return;
                }
            }
            else
            {
                Data.GeneralInfo.DisableDebugger = true;
                if (ShowQuestion("Save changes first?") == MessageBoxResult.Yes)
                    saveOk = await DoSaveDialog();
            }

            if (FilePath == null)
            {
                ShowWarning("The file must be saved in order to be run.");
            }
            else if (saveOk)
            {
                RuntimePicker picker = new RuntimePicker();
                picker.Owner = this;
                var runtime = picker.Pick(FilePath, Data);
                if (runtime != null)
                    Process.Start(runtime.Path, "-game \"" + FilePath + "\" -debugoutput \"" + Path.ChangeExtension(FilePath, ".gamelog.txt") + "\"");
            }
        }

        private async void Command_RunDebug(object sender, ExecutedRoutedEventArgs e)
        {
            if (Data == null)
                return;

            bool origDbg = Data.GeneralInfo.DisableDebugger;
            Data.GeneralInfo.DisableDebugger = false;

            bool saveOk = await DoSaveDialog(true);
            if (FilePath == null)
            {
                ShowWarning("The file must be saved in order to be run.");
            }
            else if (saveOk)
            {
                RuntimePicker picker = new RuntimePicker();
                picker.Owner = this;
                var runtime = picker.Pick(FilePath, Data);
                if (runtime == null)
                    return;
                if (runtime.DebuggerPath == null)
                {
                    MessageBox.Show("The selected runtime does not support debugging.", "Run error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }


                string tempProject = Path.GetTempFileName().Replace(".tmp", ".gmx");
                File.WriteAllText(tempProject, @"<!-- Without this file the debugger crashes, but it doesn't actually need to contain anything! -->
<assets>
  <Configs name=""configs"">
    <Config>Configs\Default</Config>
  </Configs>
  <NewExtensions/>
  <sounds name=""sound""/>
  <sprites name=""sprites""/>
  <backgrounds name=""background""/>
  <paths name=""paths""/>
  <objects name=""objects""/>
  <rooms name=""rooms""/>
  <help/>
  <TutorialState>
    <IsTutorial>0</IsTutorial>
    <TutorialName></TutorialName>
    <TutorialPage>0</TutorialPage>
  </TutorialState>
</assets>");

                Process.Start(runtime.Path, "-game \"" + FilePath + "\" -debugoutput \"" + Path.ChangeExtension(FilePath, ".gamelog.txt") + "\"");
                Process.Start(runtime.DebuggerPath, "-d=\"" + Path.ChangeExtension(FilePath, ".yydebug") + "\" -t=\"127.0.0.1\" -tp=" + Data.GeneralInfo.DebuggerPort + " -p=\"" + tempProject + "\"");
            }
            Data.GeneralInfo.DisableDebugger = origDbg;
        }

        private void Command_Settings(object sender, ExecutedRoutedEventArgs e)
        {
            SettingsWindow settings = new SettingsWindow();
            settings.Owner = this;
            settings.ShowDialog();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTree();
        }

        public void UpdateObjectLabel(object obj)
        {
            int foundIndex = obj is UndertaleNamedResource ? Data.IndexOf(obj as UndertaleNamedResource, false) : -1;
            SetIDString(foundIndex == -1 ? "None" : (foundIndex == -2 ? "N/A" : Convert.ToString(foundIndex)));
        }

        public void HighlightObject(object obj, bool silent = true)
        {
            UndertaleResource res = obj as UndertaleResource;
            if (res is null)
            {
                if (!silent)
                    ShowWarning($"Can't highlight the object - it's null or isn't a UndertaleResource.");

                return;
            }

            string objName = null;
            if (obj is not UndertaleNamedResource)
            {
                if (obj is UndertaleVariable var)
                    objName = var.Name?.Content;
            }
            else
                objName = (res as UndertaleNamedResource).Name?.Content;

            ScrollViewer mainTreeViewer = FindVisualChild<ScrollViewer>(MainTree);
            Type objType = res.GetType();

            TreeViewItem resListView = (MainTree.Items[0] as TreeViewItem).Items.Cast<TreeViewItem>()
                                                                                .FirstOrDefault(x => (x.ItemTemplate?.DataType as Type) == objType);
            IList resList;
            try
            {
                resList = Data[res.GetType()] as IList;
            }
            catch (Exception ex)
            {
                if (!silent)
                    ShowWarning($"Can't highlight the object \"{objName}\".\nError - {ex.Message}");

                return;
            }

            if (resListView is null)
            {
                if (!silent)
                    ShowWarning($"Can't highlight the object \"{objName}\" - element with object list not found.");

                return;
            }

            double initOffsetV = mainTreeViewer.VerticalOffset;
            double initOffsetH = mainTreeViewer.HorizontalOffset;
            bool initExpanded = resListView.IsExpanded;

            resListView.IsExpanded = true;
            resListView.BringIntoView();
            resListView.UpdateLayout();

            VirtualizingStackPanel resPanel = FindVisualChild<VirtualizingStackPanel>(resListView);
            if (resPanel.Children.Count > 0)
            {
                (resPanel.Children[0] as TreeViewItem).BringIntoView();
                mainTreeViewer.UpdateLayout();

                double firstElemOffset = mainTreeViewer.VerticalOffset + (resPanel.Children[0] as TreeViewItem).TransformToAncestor(mainTreeViewer).Transform(new Point(0, 0)).Y;
                mainTreeViewer.ScrollToVerticalOffset(firstElemOffset + ((resList.IndexOf(obj) + 1) * 16) - (mainTreeViewer.ViewportHeight / 2));
            }
            mainTreeViewer.UpdateLayout();

            if (resListView.ItemContainerGenerator.ContainerFromItem(obj) is TreeViewItem resItem)
            {
                Highlighted = resItem.DataContext;
                resItem.IsSelected = true;

                mainTreeViewer.UpdateLayout();
                mainTreeViewer.ScrollToHorizontalOffset(0);
            }
            else
            {
                // revert visual changes
                resListView.IsExpanded = initExpanded;
                resListView.UpdateLayout();
                mainTreeViewer.ScrollToVerticalOffset(initOffsetV);
                mainTreeViewer.ScrollToHorizontalOffset(initOffsetH);
                resListView.UpdateLayout();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            TabController.SelectionChanged -= TabController_SelectionChanged;

            Tab lastTab = SelectionHistory.Last();
            SelectionHistory.RemoveAt(SelectionHistory.Count - 1);

            CurrentTab = lastTab;
            CurrentTabIndex = lastTab.TabIndex;

            ScrollToTab(CurrentTabIndex);

            Selected = lastTab.OpenedObject;
            UpdateObjectLabel(Selected);

            TabController.SelectionChanged += TabController_SelectionChanged;
        }

        public void EnsureDataLoaded()
        {
            if (Data == null)
            {
                throw new ScriptException("Please load data.win first!");
            }
        }

        private async void MenuItem_OffsetMap_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.DefaultExt = "win";
            dlg.Filter = "Game Maker Studio data files (.win, .unx, .ios, .droid)|*.win;*.unx;*.ios;*.droid|All files|*";

            if (dlg.ShowDialog() == true)
            {
                SaveFileDialog dlgout = new SaveFileDialog();

                dlgout.DefaultExt = "txt";
                dlgout.Filter = "Text files (.txt)|*.txt|All files|*";
                dlgout.FileName = dlg.FileName + ".offsetmap.txt";

                if (dlgout.ShowDialog() == true)
                {
                    LoaderDialog dialog = new LoaderDialog("Generating", "Loading, please wait...");
                    dialog.Owner = this;
                    Task t = Task.Run(() =>
                    {
                        try
                        {
                            using (var stream = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read))
                            {
                                var offsets = UndertaleIO.GenerateOffsetMap(stream);
                                using (var writer = File.CreateText(dlgout.FileName))
                                {
                                    foreach (var off in offsets.OrderBy((x) => x.Key))
                                    {
                                        writer.WriteLine(off.Key.ToString("X8") + " " + off.Value.ToString().Replace("\n", "\\\n"));
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occured while trying to load:\n" + ex.Message, "Load error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        Dispatcher.Invoke(() =>
                        {
                            dialog.Hide();
                        });
                    });
                    dialog.ShowDialog();
                    await t;
                }
            }
        }

        private void OpenInTab(object obj, bool isNewTab = false, string tabTitle = null)
        {
            if (obj is null)
                return;

            if (obj is DescriptionView && CurrentTab is not null && !CurrentTab.AutoClose)
                return;

            bool tabSwitched = true;

            for (int i = 0; i < Tabs.Count; i++)
            {
                if (Tabs[i].OpenedObject == obj)
                {
                    if (i != CurrentTabIndex)
                    {
                        CurrentTabIndex = i;

                        return;
                    }
                    else
                        tabSwitched = false;

                    break;
                }
            }

            if (tabSwitched)
            {
                // close auto-closing tab
                if (Tabs.Count > 0 && CurrentTabIndex >= 0 && CurrentTab.AutoClose)
                    CloseTab(CurrentTab.TabIndex, false);
            }
            else
                return;

            int newIndex = isNewTab ? Tabs.Count : (CurrentTabIndex == -1 ? 0 : CurrentTabIndex);

            Tab newTab = new(obj, newIndex, tabTitle);

            if (isNewTab || Tabs.Count == 0)
            {
                Tabs.Add(newTab);
                CurrentTabIndex = newIndex;
            }
            else
            {
                Tabs[CurrentTabIndex] = newTab;
                CurrentTabIndex = newIndex;
            }

            if (!TabController.IsLoaded)
                CurrentTab = newTab;
        }

        public void CloseTab(bool addDefaultTab = true) // close the current tab
        {
            CloseTab(CurrentTabIndex, addDefaultTab);
        }
        public void CloseTab(int tabIndex, bool addDefaultTab = true)
        {
            if (tabIndex >= 0 && tabIndex < Tabs.Count)
            {
                // remove all closed tab occurrences from the selection history
                Tab closingTab = Tabs[tabIndex];
                for (int i = 0; i < SelectionHistory.Count; i++)
                {
                    if (SelectionHistory[i].OpenedObject == closingTab.OpenedObject)
                        SelectionHistory.RemoveAt(i);
                }

                // remove consecutive duplicates ( { 1, 1, 2 } -> { 1, 2 } )
                for (int i = 0; i < SelectionHistory.Count - 1; i++)
                {
                    if (SelectionHistory[i] == SelectionHistory[i + 1])
                        SelectionHistory.RemoveAt(i);
                }

                TabController.SelectionChanged -= TabController_SelectionChanged;

                int currIndex = CurrentTabIndex;

                // "CurrentTabIndex" changes here (bound to "TabController.SelectedIndex")
                Tabs.RemoveAt(tabIndex);

                if (!closingTab.AutoClose)
                    ClosedTabsHistory.Add(closingTab);

                if (Tabs.Count == 0)
                {
                    CurrentTabIndex = -1;
                    CurrentTab = null;

                    if (addDefaultTab)
                    {
                        Highlighted = new DescriptionView("Welcome to UndertaleModTool!", "Open a data.win file to get started, then double click on the items on the left to view them");
                        OpenInTab(Highlighted);
                        CurrentTab = Tabs[CurrentTabIndex];

                        Selected = CurrentTab.OpenedObject;
                        UpdateObjectLabel(Selected);
                    }

                    TabController.SelectionChanged += TabController_SelectionChanged;
                }
                else
                {
                    for (int i = tabIndex; i < Tabs.Count; i++)
                        Tabs[i].TabIndex = i;

                    // if closing the currently open tab
                    if (currIndex == tabIndex)
                    {
                        // and if that tab is not the last
                        if (Tabs.Count > 1 && tabIndex < Tabs.Count - 1)
                        {
                            // switch to the last tab
                            currIndex = Tabs.Count - 1;
                        }
                        else if (currIndex != 0)
                            currIndex -= 1;
                    }
                    else if (currIndex > tabIndex)
                    {
                        currIndex -= 1;
                    }

                    TabController.SelectionChanged += TabController_SelectionChanged;

                    CurrentTabIndex = currIndex;
                    CurrentTab = Tabs[CurrentTabIndex];

                    if (SelectionHistory.Count > 0 && CurrentTab == SelectionHistory.Last())
                        SelectionHistory.RemoveAt(SelectionHistory.Count - 1);

                    Selected = CurrentTab.OpenedObject;
                }
            }
        }
        public void CloseTab(object obj, bool addDefaultTab = true)
        {
            if (obj is not null)
            {
                int tabIndex = Tabs.FirstOrDefault(x => x.OpenedObject == obj)?.TabIndex ?? -1;
                if (tabIndex != -1)
                    CloseTab(tabIndex, addDefaultTab);
            }
            else
                Debug.WriteLine("Can't close the tab - object is null.");
        }

        public void ChangeSelection(object newsel)
        {
            OpenInTab(newsel, true);
        }

        private void TabController_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabController.SelectedIndex >= 0)
            {
                if (CurrentTab is not null
                    && Tabs.Contains(CurrentTab)
                    && SelectionHistory.LastOrDefault()?.OpenedObject != CurrentTab.OpenedObject
                    && CurrentTab.OpenedObject is not DescriptionView)
                {
                    SelectionHistory.Add(CurrentTab);
                }

                ScrollToTab(CurrentTabIndex);

                CurrentTab = Tabs[CurrentTabIndex];
                Selected = CurrentTab.OpenedObject;
                UpdateObjectLabel(Selected);
            }
        }

        private void ScrollTabs(ScrollDirection dir)
        {
            double offset = TabScrollViewer.HorizontalOffset;
            double clearOffset = 0;
            TabPanel tabPanel = FindVisualChild<TabPanel>(TabController);

            if (Tabs.Count > 1
                && ((dir == ScrollDirection.Left && offset > 0)
                || (dir == ScrollDirection.Right && offset < TabController.ActualWidth)))
            {
                int count = VisualTreeHelper.GetChildrenCount(tabPanel);
                List<TabItem> tabItems = new(count);
                for (int i1 = 0; i1 < count; i1++)
                    tabItems.Add(VisualTreeHelper.GetChild(tabPanel, i1) as TabItem);

                // selected TabItem is in the end of child list somehow, so it should be fixed
                if (CurrentTabIndex != count - 1)
                {
                    tabItems.Insert(CurrentTabIndex, tabItems[^1]);
                    tabItems.RemoveAt(tabItems.Count - 1);
                }

                // get index of first visible tab
                int i = 0;
                foreach (TabItem item in tabItems)
                {
                    double actualWidth = item.ActualWidth;
                    if (i == CurrentTabIndex)
                        actualWidth -= 4; // selected tab is wider

                    clearOffset += actualWidth;

                    if (clearOffset > offset)
                    {
                        if (dir == ScrollDirection.Left)
                            clearOffset -= actualWidth;

                        break;
                    }

                    i++;
                }

                if (dir == ScrollDirection.Left && TabScrollViewer.ScrollableWidth != offset && i != 0)
                    TabScrollViewer.ScrollToHorizontalOffset(clearOffset - tabItems[i - 1].ActualWidth);
                else
                    TabScrollViewer.ScrollToHorizontalOffset(clearOffset);
            }
        }
        private void ScrollToTab(int tabIndex)
        {
            TabScrollViewer.UpdateLayout();

            if (tabIndex == 0)
                TabScrollViewer.ScrollToLeftEnd();
            else if (tabIndex == Tabs.Count - 1)
                TabScrollViewer.ScrollToRightEnd();
            else
            {
                TabPanel tabPanel = FindVisualChild<TabPanel>(TabController);

                int count = VisualTreeHelper.GetChildrenCount(tabPanel);
                List<TabItem> tabItems = new(count);
                for (int i1 = 0; i1 < count; i1++)
                    tabItems.Add(VisualTreeHelper.GetChild(tabPanel, i1) as TabItem);

                // selected TabItem is in the end of child list somehow, so it should be fixed
                if (CurrentTabIndex != count - 1)
                {
                    tabItems.Insert(CurrentTabIndex, tabItems[^1]);
                    tabItems.RemoveAt(tabItems.Count - 1);
                }

                TabItem currTabItem = null;
                double offset = 0;
                int i = 0;
                foreach (TabItem item in tabItems)
                {
                    if (i == tabIndex)
                    {
                        currTabItem = item;
                        break;
                    }

                    offset += item.ActualWidth;
                    i++;
                }

                double endOffset = TabScrollViewer.HorizontalOffset + TabScrollViewer.ViewportWidth;
                if (offset < TabScrollViewer.HorizontalOffset || offset > endOffset)
                    TabScrollViewer.ScrollToHorizontalOffset(offset);
                else
                    currTabItem?.BringIntoView();
            }
        }
        private void TabScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollTabs(e.Delta < 0 ? ScrollDirection.Right : ScrollDirection.Left);
            e.Handled = true;
        }
        private void TabsScrollLeftButton_Click(object sender, RoutedEventArgs e)
        {
            ScrollTabs(ScrollDirection.Left);
        }
        private void TabsScrollRightButton_Click(object sender, RoutedEventArgs e)
        {
            ScrollTabs(ScrollDirection.Right);
        }

        private void TabCloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            int tabIndex = (button.DataContext as Tab).TabIndex;

            CloseTab(tabIndex);
        }
        private void TabCloseButton_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as Button).Content = new Image() { Source = Tab.ClosedHoverIcon };
        }
        private void TabCloseButton_MouseLeave(object sender, MouseEventArgs e)
        {
            (sender as Button).Content = new Image() { Source = Tab.ClosedIcon };
        }

        private void TabItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Middle)
            {
                TabItem tabItem = sender as TabItem;
                Tab tab = tabItem?.DataContext as Tab;
                if (tab is null)
                    return;

                if (tab.TabTitle != "Welcome!")
                    CloseTab(tab.OpenedObject);
            }
        }

        // source - https://stackoverflow.com/a/10738247/12136394
        private void TabItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Source is not TabItem tabItem || e.OriginalSource is Button)
                return;

            if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed)
            {
                CurrentTabIndex = tabItem.TabIndex;
                DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
            }
        }
        private void TabItem_Drop(object sender, DragEventArgs e)
        {
            if (e.Source is TabItem tabItemTarget &&
                e.Data.GetData(typeof(TabItem)) is TabItem tabItemSource &&
                !tabItemTarget.Equals(tabItemSource))
            {
                int sourceIndex = tabItemSource.TabIndex;
                int targetIndex = tabItemTarget.TabIndex;
                Tab sourceTab = tabItemSource.DataContext as Tab;
                if (sourceTab is null)
                    return;

                TabController.SelectionChanged -= TabController_SelectionChanged;

                Tabs.RemoveAt(sourceIndex);
                Tabs.Insert(targetIndex, sourceTab);

                for (int i = 0; i < Tabs.Count; i++)
                    Tabs[i].TabIndex = i;

                CurrentTabIndex = targetIndex;

                TabController.SelectionChanged += TabController_SelectionChanged;
            }
        }

        private void CloseTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Tab tab = (sender as MenuItem).DataContext as Tab;
            if (tab is null)
                return;

            CloseTab(tab.TabIndex);
        }
        private void CloseOtherTabsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Tab tab = (sender as MenuItem).DataContext as Tab;
            if (tab is null)
                return;

            foreach (Tab t in Tabs.Reverse())
            {
                if (t == tab)
                    continue;

                ClosedTabsHistory.Add(t);
            }

            SelectionHistory.Clear();
            Tabs = new() { tab };
            CurrentTabIndex = 0;
        }
    }

    public class GeneralInfoEditor
    {
        public UndertaleGeneralInfo GeneralInfo { get; private set; }
        public UndertaleOptions Options { get; private set; }
        public UndertaleLanguage Language { get; private set; }

        public GeneralInfoEditor(UndertaleGeneralInfo generalInfo, UndertaleOptions options, UndertaleLanguage language)
        {
            this.GeneralInfo = generalInfo;
            this.Options = options;
            this.Language = language;
        }
    }

    public class GlobalInitEditor
    {
        public IList<UndertaleGlobalInit> GlobalInits { get; private set; }

        public GlobalInitEditor(IList<UndertaleGlobalInit> globalInits)
        {
            this.GlobalInits = globalInits;
        }
    }

    public class GameEndEditor
    {
        public IList<UndertaleGlobalInit> GameEnds { get; private set; }

        public GameEndEditor(IList<UndertaleGlobalInit> GameEnds)
        {
            this.GameEnds = GameEnds;
        }
    }

    public class DescriptionView
    {
        public string Heading { get; private set; }
        public string Description { get; private set; }

        // Used only by the "TabTitleConverter" to prevent the following WPF binding warning:
        // "Name property not found on object of type DescriptionView".
        // (within "TabControl.ItemTemplate")
        public UndertaleString Name { get; } = new();

        public DescriptionView(string heading, string description)
        {
            Heading = heading;
            Description = description;
        }
    }
}
