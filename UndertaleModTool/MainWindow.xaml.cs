using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using UndertaleModLib;
using UndertaleModLib.DebugData;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public UndertaleData Data { get; set; }
        public string FilePath { get; set; }

        private object _Highlighted;
        public object Highlighted { get { return _Highlighted; } set { _Highlighted = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Highlighted")); } }
        private object _Selected;
        public object Selected { get { return _Selected; } private set { _Selected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Selected")); } }
        public Visibility IsGMS2 => (Data?.GeneralInfo?.Major ?? 0) >= 2 ? Visibility.Visible : Visibility.Collapsed;

        public ObservableCollection<object> SelectionHistory { get; } = new ObservableCollection<object>();

        // TODO: extract the scripting interface into a separate class

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            ChangeSelection(Highlighted = new DescriptionView("Welcome to UndertaleModTool!", "Open data.win file to get started, then double click on the items on the left to view them"));
            SelectionHistory.Clear();

            Title = "UndertaleModTool by krzys_h v" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Command_New(object sender, ExecutedRoutedEventArgs e)
        {
            Data = UndertaleData.CreateNew();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Data"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsGMS2"));
            ChangeSelection(Highlighted = new DescriptionView("Welcome to UndertaleModTool!", "New file created, have fun making a game out of nothing\nI TOLD YOU to open data.win, not create a new file! :P"));
            SelectionHistory.Clear();
        }

        private async Task<bool> DoOpenDialog()
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.DefaultExt = "win";
            dlg.Filter = "Game Maker Studio data files (.win, .unx, .ios)|*.win;*.unx;*.ios|All files|*";

            if (dlg.ShowDialog() == true)
            {
                await LoadFile(dlg.FileName);
                return true;
            }
            return false;
        }
        private async Task<bool> DoSaveDialog()
        {
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.DefaultExt = "win";
            dlg.Filter = "Game Maker Studio data files (.win, .unx, .ios)|*.win;*.unx;*.ios|All files|*";
            dlg.FileName = FilePath;

            if (dlg.ShowDialog() == true)
            {
                await SaveFile(dlg.FileName);
                return true;
            }
            return false;
        }

        private void Command_Open(object sender, ExecutedRoutedEventArgs e)
        {
            DoOpenDialog();
        }

        private void Command_Save(object sender, ExecutedRoutedEventArgs e)
        {
            DoSaveDialog();
        }

        private void Command_Close(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private async Task LoadFile(string filename)
        {
            LoaderDialog dialog = new LoaderDialog("Loading", "Loading, please wait...");
            dialog.Owner = this;
            Task t = Task.Run(() =>
            {
                UndertaleData data = null;
                try
                {
                    using (var stream = new FileStream(filename, FileMode.Open))
                    {
                        data = UndertaleIO.Read(stream);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("An error occured while trying to load:\n" + e.Message, "Load error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                Dispatcher.Invoke(() =>
                {
                    if (data != null)
                    {
                        if (data.GeneralInfo.Major >= 2)
                        {
                            MessageBox.Show("LOL it worked :O\n\nGame Maker: Studio 2 game loaded! I just hacked this together quickly for the Nintendo Switch release of Undertale, so some things may be broken (saving definitely is). Expect a release with fixes soon!", "GMS2 game loaded", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        this.Data = data;
                        this.FilePath = filename;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Data"));
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsGMS2"));
                        ChangeSelection(Highlighted = new DescriptionView("Welcome to UndertaleModTool!", "Double click on the items on the left to view them!"));
                        SelectionHistory.Clear();
                    }
                    dialog.Hide();
                });
            });
            dialog.ShowDialog();
            await t;
        }

        private async Task SaveFile(string filename)
        {
            if (Data == null)
                return; // TODO: lock save button

            if (IsGMS2 == Visibility.Visible)
            {
                if (MessageBox.Show("heya, I told you this is not going to work.\nAre you that *DETERMINED* to see this error message?", "nope", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    MessageBox.Show("HA! I knew it", "nope", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                else
                {
                    MessageBox.Show("okay, here it goes ;)", "nope", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            LoaderDialog dialog = new LoaderDialog("Saving", "Saving, please wait...");
            dialog.Owner = this;
            FilePath = filename;

            DebugDataMode debugMode = DebugDataMode.NoDebug;
            if (!Data.GeneralInfo.DisableDebugger) // TODO: I think the game itself can also use the .yydebug file on crash reports
            {
                DebugDataDialog debugDialog = new DebugDataDialog();
                debugDialog.Owner = this;
                debugDialog.ShowDialog();
                debugMode = debugDialog.Result;
                // TODO: Add an option to generate debug data for just selected scripts (to make running faster / make the full assembly not take forever to load)
            }
            Task t = Task.Run(() =>
            {
                try
                {
                    using (var stream = new FileStream(filename, FileMode.Create))
                    {
                        UndertaleIO.Write(stream, Data);
                    }

                    if (debugMode != DebugDataMode.NoDebug)
                    {
                        Debug.WriteLine("Generating debugger data...");
                        UndertaleDebugData debugData = DebugDataGenerator.GenerateDebugData(Data, debugMode);
                        using (FileStream stream = new FileStream(System.IO.Path.ChangeExtension(FilePath, ".yydebug"), FileMode.Create))
                        {
                            using (UndertaleWriter writer = new UndertaleWriter(stream))
                            {
                                debugData.FORM.Serialize(writer);
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    MessageBox.Show("An error occured while trying to save:\n" + e.Message, "Save error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (IsGMS2 == Visibility.Visible)
                    {
                        MessageBox.Show("Are you happy now?", "nope", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    dialog.Hide();
                });
            });
            dialog.ShowDialog();
            await t;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem)
            {
                string item = (e.NewValue as TreeViewItem)?.Header?.ToString();
                if (item == "General info" && Data != null)
                {
                    Highlighted = new GeneralInfoEditor(Data?.GeneralInfo, Data?.Options, Data?.Language);
                }
                else if (item == "Data")
                {
                    Highlighted = new DescriptionView("Welcome to UndertaleModTool!", Data != null ? "Double click on the items on the left to view them" : "Open data.win file to get started");
                }
                else if (item == "Code locals (unused?)")
                {
                    Highlighted = new DescriptionView(item, Data != null ? "This seems to be unused as far as I can tell - you can remove the whole list and nothing happens" : "Load data.win file first");
                }
                else if (item == "Variables")
                {
                    Highlighted = Data != null ? (object)Data.FORM.Chunks["VARI"] : new DescriptionView(item, "Load data.win file first");
                }
                else
                {
                    Highlighted = new DescriptionView(item, Data != null ? "Expand the list on the left to edit items" : "Load data.win file first");
                }
            }
            else
            {
                Highlighted = e.NewValue;
            }
        }

        private void MainTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ChangeSelection(Highlighted);
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
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[e.Data.GetFormats().Length-1]) as UndertaleObject; // TODO: make this more reliable

            foreach (var s in e.Data.GetFormats())
                Debug.WriteLine(s);

            TreeViewItem targetTreeItem = VisualUpwardSearch<TreeViewItem>(e.OriginalSource as UIElement);
            UndertaleObject targetItem = targetTreeItem.DataContext as UndertaleObject;

            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Move) && sourceItem != null && targetItem != null && sourceItem != targetItem && sourceItem.GetType() == targetItem.GetType() ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void TreeView_Drop(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[e.Data.GetFormats().Length - 1]) as UndertaleObject;

            TreeViewItem targetTreeItem = VisualUpwardSearch<TreeViewItem>(e.OriginalSource as UIElement);
            UndertaleObject targetItem = targetTreeItem.DataContext as UndertaleObject;

            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Move) && sourceItem != null && targetItem != null && sourceItem != targetItem && sourceItem.GetType() == targetItem.GetType() ? DragDropEffects.Move : DragDropEffects.None;
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
                element = VisualTreeHelper.GetParent(element) as DependencyObject;
                container = element as T;
            }
            return container;
        }

        private static T GetNearestParent<T>(DependencyObject item) where T : class
        {
            DependencyObject parent = VisualTreeHelper.GetParent(item);
            while (!(parent is T))
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
                    if (child != null && child is T)
                    {
                        yield return (T)child;
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
            bool isLast = list.IndexOf(obj) == list.Count-1;
            if (MessageBox.Show("Delete " + obj.ToString() + "?" + (!isLast ? "\n\nNote that the code often references objects by ID, so this operation is likely to break stuff because other items will shift up!" : ""), "Confirmation", MessageBoxButton.YesNo, isLast ? MessageBoxImage.Question : MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                list.Remove(obj);
                while (SelectionHistory.Remove(obj)) ;
                if (Selected == obj)
                    ChangeSelection(null);
                if (Highlighted == obj)
                    Highlighted = null;
            }
        }

        private void MainTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (Selected != null && Selected is UndertaleObject)
                {
                    UndertaleObject obj = Selected as UndertaleObject;
                    DeleteItem(obj);
                }
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
                    using (var loader = new InteractiveAssemblyLoader())
                    {
                        loader.RegisterDependency(typeof(UndertaleObject).GetTypeInfo().Assembly);

                        var script = CSharpScript.Create<object>(CommandBox.Text, ScriptOptions.Default
                            .WithImports("UndertaleModLib", "UndertaleModLib.Models", "UndertaleModLib.Decompiler", "System", "System.IO", "System.Collections.Generic")
                            .WithReferences(Program.GetAssemblyMetadata(typeof(UndertaleObject).GetTypeInfo().Assembly)),
                            this.GetType(), loader);

                        result = (await script.RunAsync(this)).ReturnValue;
                    }
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
                CommandBox.Text = result != null ? result.ToString() : "";
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

        private void MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (Highlighted != null && Highlighted is UndertaleObject)
                DeleteItem(Highlighted as UndertaleObject);
        }

        private void MenuItem_Add_Click(object sender, RoutedEventArgs e)
        {
            object source = (MainTree.SelectedItem as TreeViewItem).ItemsSource;
            IList list = ((source as ICollectionView)?.SourceCollection as IList) ?? (source as IList);
            Type t = list.GetType().GetGenericArguments()[0];
            Debug.Assert(typeof(UndertaleResource).IsAssignableFrom(t));
            UndertaleResource obj = Activator.CreateInstance(t) as UndertaleResource;
            if (obj is UndertaleNamedResource)
            {
                string newname = obj.GetType().Name.Replace("Undertale", "").Replace("GameObject", "Object").ToLower() + list.Count;
                (obj as UndertaleNamedResource).Name = Data.Strings.MakeString(newname);
                if (obj is UndertaleRoom)
                    (obj as UndertaleRoom).Caption = Data.Strings.MakeString("");
            }
            list.Add(obj);
            // TODO: change highlighted too
            ChangeSelection(obj);
        }

        private void MenuItem_RunBuiltinScript_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            item.Items.Clear();
            foreach(var path in Directory.EnumerateFiles("SampleScripts"))
            {
                var filename = System.IO.Path.GetFileName(path);
                MenuItem subitem = new MenuItem() { Header = filename };
                subitem.Click += MenuItem_RunBuiltinScript_Item_Click;
                subitem.CommandParameter = path;
                item.Items.Add(subitem);
            }
            if (item.Items.Count == 0)
                item.Items.Add(new MenuItem() { Header = "(whoops, no scripts found?)", IsEnabled = false });
        }

        private async Task RunScript(string path)
        {
            Debug.WriteLine(path);

            CommandBox.Text = "Running " + System.IO.Path.GetFileName(path) + " ...";
            try
            {
                using (var loader = new InteractiveAssemblyLoader())
                {
                    loader.RegisterDependency(typeof(UndertaleObject).GetTypeInfo().Assembly);

                    var script = CSharpScript.Create<object>(File.ReadAllText(path), ScriptOptions.Default
                        .WithImports("UndertaleModLib", "UndertaleModLib.Models", "UndertaleModLib.Decompiler", "System", "System.IO", "System.Collections.Generic")
                        .WithReferences(Program.GetAssemblyMetadata(typeof(UndertaleObject).GetTypeInfo().Assembly)),
                        this.GetType(), loader);

                    object result = (await script.RunAsync(this)).ReturnValue;
                    CommandBox.Text = result != null ? result.ToString() : System.IO.Path.GetFileName(path) + " finished!";
                }
            }
            catch (CompilationErrorException exc)
            {
                Debug.WriteLine(exc.ToString());
                CommandBox.Text = exc.Message;
                MessageBox.Show(exc.Message, "Script compile error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc.ToString());
                CommandBox.Text = exc.Message;
                MessageBox.Show(exc.Message, "Script error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ScriptMessage(string message)
        {
            MessageBox.Show(message, "Script mesage", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool ScriptQuestion(string message)
        {
            return MessageBox.Show(message, "Script message", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        public void ScriptOpenURL(string url)
        {
            Process.Start(url);
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
            Process.Start("https://github.com/krzys-h/UndertaleModTool");
        }

        private void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            string version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
            MessageBox.Show("UndertaleModTool by krzys_h\nVersion " + version, "About", MessageBoxButton.OK);
        }

        private string FindRunner()
        {
            string gameRunner = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(FilePath), Data.GeneralInfo.Filename.Content + ".exe");
            Debug.WriteLine(gameRunner);
            if (File.Exists(gameRunner))
                return gameRunner;
            string studioRunner = System.IO.Path.Combine(Environment.ExpandEnvironmentVariables(SettingsWindow.GameMakerStudioPath), "Runner.exe");
            Debug.WriteLine(studioRunner);
            if (File.Exists(studioRunner))
                return studioRunner;
            MessageBox.Show("Unable to find game runner at " + gameRunner + " nor GM:S installation directory. Please check the paths.", "Run error", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }

        private string FindDebugger()
        {
            string studioDebugger = System.IO.Path.Combine(Environment.ExpandEnvironmentVariables(SettingsWindow.GameMakerStudioPath), @"GMDebug\GMDebug.exe");
            Debug.WriteLine(studioDebugger);
            if (File.Exists(studioDebugger))
                return studioDebugger;
            MessageBox.Show("Unable to find Game Maker: Studio debugger at " + studioDebugger + ". Please install GM:S and check the paths.", "Run error", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }

        private async void Command_Run(object sender, ExecutedRoutedEventArgs e)
        {
            if (Data == null)
                return;

            string runnerPath = FindRunner();
            if (runnerPath == null)
                return;

            bool origDbg = Data.GeneralInfo.DisableDebugger;
            Data.GeneralInfo.DisableDebugger = true;
            if (await DoSaveDialog())
            {
                Process.Start(runnerPath, "-game \"" + FilePath + "\" -debugoutput \"" + System.IO.Path.ChangeExtension(FilePath, ".gamelog.txt") + "\"");
            }
            Data.GeneralInfo.DisableDebugger = origDbg;
        }
        
        private async void Command_RunDebug(object sender, ExecutedRoutedEventArgs e)
        {
            if (Data == null)
                return;
            
            string runnerPath = FindRunner();
            string debuggerPath = FindDebugger();
            if (runnerPath == null || debuggerPath == null)
                return;

            bool origDbg = Data.GeneralInfo.DisableDebugger;
            Data.GeneralInfo.DisableDebugger = false;
            if (await DoSaveDialog())
            {
                string tempProject = System.IO.Path.GetTempFileName().Replace(".tmp", ".gmx");
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

                Process.Start(runnerPath, "-game \"" + FilePath + "\" -debugoutput \"" + System.IO.Path.ChangeExtension(FilePath, ".gamelog.txt") + "\"");
                Process.Start(debuggerPath, "-d=\"" + System.IO.Path.ChangeExtension(FilePath, ".yydebug") + "\" -t=\"127.0.0.1\" -tp=" + Data.GeneralInfo.DebuggerPort + " -p=\"" + tempProject + "\"");
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
            foreach (var child in (MainTree.Items[0] as TreeViewItem).Items)
            {
                ((child as TreeViewItem).ItemsSource as ICollectionView)?.Refresh();
            }
        }

        public void ChangeSelection(object newsel)
        {
            SelectionHistory.Add(Selected);
            Selected = newsel;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Selected = SelectionHistory.Last();
            SelectionHistory.RemoveAt(SelectionHistory.Count - 1);
        }

        public void EnsureDataLoaded()
        {
            if (Data == null)
                throw new Exception("Please load data.win first!");
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

    public class DescriptionView
    {
        public string Heading { get; private set; }
        public string Description { get; private set; }

        public DescriptionView(string heading, string description)
        {
            Heading = heading;
            Description = description;
        }
    }
}
