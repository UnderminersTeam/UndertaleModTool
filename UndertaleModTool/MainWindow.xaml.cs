﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
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
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.ModelsDebug;
using UndertaleModLib.Scripting;
using UndertaleModTool.Windows;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        public UndertaleData Data { get; set; }
        public string FilePath { get; set; }
        public string ScriptPath { get; set; } // For the scripting interface specifically

        public string TitleMain { get; set; }

        private object _Highlighted;
        public object Highlighted { get { return _Highlighted; } set { _Highlighted = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Highlighted")); } }
        private object _Selected;
        public object Selected { get { return _Selected; } private set { _Selected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Selected")); } }
        public Visibility IsGMS2 => (Data?.GeneralInfo?.Major ?? 0) >= 2 ? Visibility.Visible : Visibility.Collapsed;

        public ObservableCollection<object> SelectionHistory { get; } = new ObservableCollection<object>();

        private bool _CanSave = false;
        public bool CanSave { get { return _CanSave; } private set { _CanSave = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CanSave")); } }
        public bool CanSafelySave = false;

        public event PropertyChangedEventHandler PropertyChanged;
        private LoaderDialog scriptDialog;

        // TODO: extract the scripting interface into a separate class

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            ChangeSelection(Highlighted = new DescriptionView("Welcome to UndertaleModTool!", "Open data.win file to get started, then double click on the items on the left to view them"));
            SelectionHistory.Clear();

            TitleMain = "UndertaleModTool by krzys_h v" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

            CanSave = false;
            CanSafelySave = false;
        }

        private void SetIDString(string str)
        {
            ((Label)this.FindName("ObjectLabel")).Content = str;
        }

        [DllImport("shell32.dll")]
        static extern void SHChangeNotify(long wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
        const long SHCNE_ASSOCCHANGED = 0x08000000;

        private void UpdateTree()
        {
            foreach (var child in (MainTree.Items[0] as TreeViewItem).Items)
                ((child as TreeViewItem).ItemsSource as ICollectionView)?.Refresh();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var HKCU_Classes = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true);
                var UndertaleModTool_app = HKCU_Classes.CreateSubKey(@"UndertaleModTool");
                UndertaleModTool_app.SetValue("", "UndertaleModTool");
                UndertaleModTool_app.CreateSubKey(@"shell\open\command").SetValue("", "\"" + Assembly.GetExecutingAssembly().Location + "\" \"%1\"", RegistryValueKind.String);
                UndertaleModTool_app.CreateSubKey(@"shell\launch\command").SetValue("", "\"" + Assembly.GetExecutingAssembly().Location + "\" \"%1\" launch", RegistryValueKind.String);
                UndertaleModTool_app.CreateSubKey(@"shell\launch").SetValue("", "Run game", RegistryValueKind.String);
                foreach (var extStr in new string[] { ".win", ".unx", ".ios", ".droid" })
                {
                    var ext = HKCU_Classes.CreateSubKey(extStr);
                    ext.SetValue("", "UndertaleModTool", RegistryValueKind.String);
                }
                SHChangeNotify(SHCNE_ASSOCCHANGED, 0, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                var fileName = args[1];
                if (File.Exists(fileName))
                {
                    await LoadFile(fileName);
                }
            }
            if (args.Length > 2)
            {
                if (args[2] == "launch")
                {
                    RuntimePicker picker = new RuntimePicker();
                    picker.Owner = this;
                    var runtime = picker.Pick(FilePath, Data);
                    if (runtime == null)
                        return;

                    Process.Start(runtime.Path, "-game \"" + FilePath + "\"");
                    Close();
                }
                else
                {
                    ListenChildConnection(args[2]);
                }
            }
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
                catch(IOException e)
                {
                    Debug.WriteLine(e);
                    childFiles.Remove(filename);
                }
            }
            
            string key = Guid.NewGuid().ToString();

            string dir = System.IO.Path.GetDirectoryName(FilePath);
            Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location, "\"" + System.IO.Path.Combine(dir, filename) + "\" " + key);

            var server = new NamedPipeServerStream(key);
            server.WaitForConnection();
            childFiles.Add(filename, server);

            StreamWriter writer = new StreamWriter(childFiles[filename]);
            writer.WriteLine(chunkName + ":" + itemIndex);
            writer.Flush();
        }

        public void CloseChildFiles()
        {
            foreach(var pair in childFiles)
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
                ChangeSelection(Data.EmbeddedAudio[Int32.Parse(thingToOpen[1])]);
                Activate();
            }
        }

        private void Command_New(object sender, ExecutedRoutedEventArgs e)
        {
            if (Data != null)
            {
                if (MessageBox.Show("Warning: you currently have a project open.\nAre you sure you want to make a new project?", "UndertaleModTool", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    return;
            }

            FilePath = null;
            Data = UndertaleData.CreateNew();
            CloseChildFiles();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Data"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsGMS2"));
            ChangeSelection(Highlighted = new DescriptionView("Welcome to UndertaleModTool!", "New file created, have fun making a game out of nothing\nI TOLD YOU to open data.win, not create a new file! :P"));
            SelectionHistory.Clear();

            CanSave = true;
            CanSafelySave = true;
        }

        private async Task<bool> DoOpenDialog()
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.DefaultExt = "win";
            dlg.Filter = "Game Maker Studio data files (.win, .unx, .ios, audiogroup*.dat)|*.win;*.unx;*.ios;audiogroup*.dat|All files|*";

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
            dlg.Filter = "Game Maker Studio data files (.win, .unx, .ios, audiogroup*.dat)|*.win;*.unx;*.ios;audiogroup*.dat|All files|*";
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
            if (CanSave)
            {
                if (!CanSafelySave)
                    MessageBox.Show("Errors occurred during loading. High chance of data loss! Proceed at your own risk.", "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Warning);

                DoSaveDialog();
            }
        }

        private async void Command_Close(object sender, ExecutedRoutedEventArgs e)
        {
            if (Data != null)
                if (MessageBox.Show("Save changes first?", "UndertaleModTool", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    await DoSaveDialog();
            Close();
        }

        private async Task LoadFile(string filename)
        {
            LoaderDialog dialog = new LoaderDialog("Loading", "Loading, please wait...");
            dialog.Owner = this;
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
                        });
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
                        if (data.UnsupportedBytecodeVersion)
                        {
                            MessageBox.Show("Only bytecode versions 14 to 17 are supported for now, you are trying to load " + data.GeneralInfo.BytecodeVersion + ". A lot of code is disabled and will likely break something. Saving/exporting is disabled.", "Unsupported bytecode version", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        }
                        if (data.GMS2_3)
                        {
                            MessageBox.Show("This game was built using GameMaker Studio 2.3 (or above). Support for this version is a work in progress, and you will likely run into issues decompiling code or in other places.", "GMS 2.3", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        if (data.IsYYC())
                        {
                            MessageBox.Show("This game uses YYC (YoYo Compiler), which means the code is embedded into the game executable. This configuration is currently not fully supported; continue at your own risk.", "YYC", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        if (System.IO.Path.GetDirectoryName(FilePath) != System.IO.Path.GetDirectoryName(filename))
                            CloseChildFiles();
                        this.Data = data;
                        this.FilePath = filename;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Data"));
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FilePath"));
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
            if (Data == null || Data.UnsupportedBytecodeVersion)
                return;

            LoaderDialog dialog = new LoaderDialog("Saving", "Saving, please wait...");
            dialog.PreventClose = true;
            IProgress<Tuple<int, string>> progress = new Progress<Tuple<int, string>>(i => { dialog.ReportProgress(i.Item2, i.Item1); });
            IProgress<double?> setMax = new Progress<double?>(i => { dialog.Maximum = i; });
            dialog.Owner = this;
            FilePath = filename;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FilePath"));
            if (System.IO.Path.GetDirectoryName(FilePath) != System.IO.Path.GetDirectoryName(filename))
                CloseChildFiles();

            DebugDataDialog.DebugDataMode debugMode = DebugDataDialog.DebugDataMode.NoDebug;
            if (Data.GeneralInfo != null && !Data.GeneralInfo.DisableDebugger) // TODO: I think the game itself can also use the .yydebug file on crash reports
            {
                DebugDataDialog debugDialog = new DebugDataDialog();
                debugDialog.Owner = this;
                debugDialog.ShowDialog();
                debugMode = debugDialog.Result;
            }
            Task t = Task.Run(() =>
            {
                try
                {
                    using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                    {
                        UndertaleIO.Write(stream, Data);
                    }

                    if (debugMode != DebugDataDialog.DebugDataMode.NoDebug)
                    {
                        Debug.WriteLine("Generating debugger data...");

                        UndertaleDebugData debugData = UndertaleDebugData.CreateNew();

                        setMax.Report(Data.Code.Count);
                        int count = 0;
                        object countLock = new object();
                        string[] outputs = new string[Data.Code.Count];
                        UndertaleDebugInfo[] outputsOffsets = new UndertaleDebugInfo[Data.Code.Count];
                        DecompileContext context = new DecompileContext(Data, false);
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
                                    sb.Append(instr.ToString(code, Data.Variables));
                                    sb.Append("\n");
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
                            foreach(var local in Data.CodeLocals[i].Locals)
                                if (debugData.Strings.IndexOf(local.Name) < 0)
                                    debugData.Strings.Add(local.Name);
                        }

                        using (UndertaleWriter writer = new UndertaleWriter(new FileStream(System.IO.Path.ChangeExtension(FilePath, ".yydebug"), FileMode.Create, FileAccess.Write)))
                        {
                            debugData.FORM.Serialize(writer);
                            writer.ThrowIfUnwrittenObjects();
                            writer.Flush();
                        }
                    }
                }
                catch(Exception e)
                {
                    MessageBox.Show("An error occured while trying to save:\n" + e.Message, "Save error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                switch (item)
                {
                    case "General info":
                        Highlighted = new GeneralInfoEditor(Data?.GeneralInfo, Data?.Options, Data?.Language);
                        break;
                    case "Global init":
                        Highlighted = new GlobalInitEditor(Data?.GlobalInitScripts);
                        break;
                    case "Code locals (unused?)":
                        Highlighted = new DescriptionView(item, "This seems to be unused as far as I can tell - you can remove the whole list and nothing happens");
                        break;
                    case "Variables":
                        Highlighted = (object)Data.FORM.Chunks["VARI"];
                        break;
                    default:
                        Highlighted = new DescriptionView(item, "Expand the list on the left to edit items");
                        break;
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
                UpdateTree();
            }
        }

        private void MainTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (Highlighted != null && Highlighted is UndertaleObject)
                {
                    UndertaleObject obj = Highlighted as UndertaleObject;
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
                        loader.RegisterDependency(GetType().GetTypeInfo().Assembly);
                        loader.RegisterDependency(typeof(JsonConvert).GetTypeInfo().Assembly);

                        var script = CSharpScript.Create<object>(CommandBox.Text, ScriptOptions.Default
                            .AddImports("UndertaleModLib", "UndertaleModLib.Models", "UndertaleModLib.Decompiler", "UndertaleModLib.Scripting", "UndertaleModLib.Compiler")
                            .AddImports("UndertaleModTool", "System", "System.IO", "System.Collections.Generic", "System.Text.RegularExpressions")
                            .AddReferences(Program.GetAssemblyMetadata(typeof(UndertaleObject).GetTypeInfo().Assembly))
                            .AddReferences(GetType().GetTypeInfo().Assembly)
                            .AddReferences(Program.GetAssemblyMetadata(typeof(JsonConvert).GetTypeInfo().Assembly))
                            .AddReferences(typeof(System.Text.RegularExpressions.Regex).GetTypeInfo().Assembly),
                            typeof(IScriptInterface), loader);
                            
                        ScriptPath = null;

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
            UpdateTree();
            ChangeSelection(obj);
        }

        private void MenuItem_RunBuiltinScript_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            item.Items.Clear();
            try
            {
                var appDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                foreach (var path in Directory.EnumerateFiles(System.IO.Path.Combine(appDir, "SampleScripts")))
                {
                    var filename = System.IO.Path.GetFileName(path);
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

        private void MenuItem_RunExperimentalScript_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            item.Items.Clear();
            try
            {
                var appDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                foreach (var path in Directory.EnumerateFiles(System.IO.Path.Combine(appDir, "ExperimentalScripts")))
                {
                    var filename = System.IO.Path.GetFileName(path);
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
            scriptDialog.Update(message, status, progressValue, maxValue);
            scriptDialog.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { })); // Updates the UI, so you can see the progress.
        }

        public void HideProgressBar()
        {
            scriptDialog.TryHide();
        }

        public async Task RunScript(string path) {
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
            Debug.WriteLine(path);

            Dispatcher.Invoke(() => CommandBox.Text = "Running " + System.IO.Path.GetFileName(path) + " ...");
            try
            {
                using (var loader = new InteractiveAssemblyLoader())
                {
                    loader.RegisterDependency(typeof(UndertaleObject).GetTypeInfo().Assembly);
                    loader.RegisterDependency(GetType().GetTypeInfo().Assembly);
                    loader.RegisterDependency(typeof(JsonConvert).GetTypeInfo().Assembly);

                    var script = CSharpScript.Create<object>(File.ReadAllText(path), ScriptOptions.Default
                        .AddImports("UndertaleModLib", "UndertaleModLib.Models", "UndertaleModLib.Decompiler", "UndertaleModLib.Scripting", "UndertaleModLib.Compiler")
                        .AddImports("UndertaleModTool", "System", "System.IO", "System.Collections.Generic", "System.Text.RegularExpressions")
                        .AddReferences(Program.GetAssemblyMetadata(typeof(UndertaleObject).GetTypeInfo().Assembly))
                        .AddReferences(GetType().GetTypeInfo().Assembly)
                        .AddReferences(Program.GetAssemblyMetadata(typeof(JsonConvert).GetTypeInfo().Assembly))
                        .AddReferences(typeof(System.Text.RegularExpressions.Regex).GetTypeInfo().Assembly),
                        typeof(IScriptInterface), loader);
                        
                    ScriptPath = path;

                    object result = (await script.RunAsync(this)).ReturnValue;
                    Dispatcher.Invoke(() => CommandBox.Text = result != null ? result.ToString() : System.IO.Path.GetFileName(path) + " finished!");
                }
            }
            catch (CompilationErrorException exc)
            {
                Console.WriteLine(exc.ToString());
                Dispatcher.Invoke(() => CommandBox.Text = exc.Message);
                MessageBox.Show(exc.Message, "Script compile error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
                Dispatcher.Invoke(() => CommandBox.Text = exc.Message);
                MessageBox.Show(exc.Message + "\n\n" + exc.ToString(), "Script error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string PromptLoadFile(string defaultExt, string filter)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = defaultExt != null ? defaultExt : "win";
            dlg.Filter = filter != null ? filter : "Game Maker Studio data files (.win, .unx, .ios, audiogroup*.dat)|*.win;*.unx;*.ios;audiogroup*.dat|All files|*";
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        public string PromptChooseDirectory(string prompt)
        {
            OpenFileDialog folderBrowser = new OpenFileDialog();
            // Set validate names and check file exists to false otherwise windows will
            // not let you select "Folder Selection."
            folderBrowser.ValidateNames = false;
            folderBrowser.CheckFileExists = false;
            folderBrowser.CheckPathExists = true;
            folderBrowser.FileName = prompt != null ? prompt + "." : "Folder Selection."; // Adding the . at the end makes sure it will accept the folder.
            return folderBrowser.ShowDialog() == true ? System.IO.Path.GetDirectoryName(folderBrowser.FileName) + System.IO.Path.DirectorySeparatorChar : null;
        }

        public void ScriptMessage(string message)
        {
            MessageBox.Show(message, "Script message", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ScriptError(string error, string title)
        {
            MessageBox.Show(error, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public bool ScriptQuestion(string message)
        {
            return MessageBox.Show(message, "Script message", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        public string ScriptTextInput(string message, string title, string defaultValue, bool allowMultiline)
        {
            using (TextInput input = new TextInput(message, title, defaultValue, allowMultiline))
            {
                var result = input.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    return input.ReturnString;            //values preserved after close
                else
                    return null;
            }
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

        private async void Command_Run(object sender, ExecutedRoutedEventArgs e)
        {
            if (Data == null)
                return;

            bool origDbg = Data.GeneralInfo.DisableDebugger;
            Data.GeneralInfo.DisableDebugger = true;
            bool saveOk = true;
            if (MessageBox.Show("Save changes first?", "UndertaleModTool", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                saveOk = await DoSaveDialog();

            if (FilePath == null) 
            {
                MessageBox.Show("The file must be saved in order to be run.", "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            } else if (saveOk)
            {
                RuntimePicker picker = new RuntimePicker();
                picker.Owner = this;
                var runtime = picker.Pick(FilePath, Data);
                if (runtime != null)
                    Process.Start(runtime.Path, "-game \"" + FilePath + "\" -debugoutput \"" + System.IO.Path.ChangeExtension(FilePath, ".gamelog.txt") + "\"");
            }

            Data.GeneralInfo.DisableDebugger = origDbg;
        }
        
        private async void Command_RunDebug(object sender, ExecutedRoutedEventArgs e)
        {
            if (Data == null)
                return;

            bool origDbg = Data.GeneralInfo.DisableDebugger;
            Data.GeneralInfo.DisableDebugger = false;

            bool saveOk = await DoSaveDialog();
            if (FilePath == null)
            {
                MessageBox.Show("The file must be saved in order to be run.", "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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

                Process.Start(runtime.Path, "-game \"" + FilePath + "\" -debugoutput \"" + System.IO.Path.ChangeExtension(FilePath, ".gamelog.txt") + "\"");
                Process.Start(runtime.DebuggerPath, "-d=\"" + System.IO.Path.ChangeExtension(FilePath, ".yydebug") + "\" -t=\"127.0.0.1\" -tp=" + Data.GeneralInfo.DebuggerPort + " -p=\"" + tempProject + "\"");
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

        private void UpdateObjectLabel(object obj)
        {
            int foundIndex = obj is UndertaleNamedResource ? Data.IndexOf(obj as UndertaleNamedResource, false) : -1;
            SetIDString(foundIndex == -1 ? "None" : (foundIndex == -2 ? "N/A" : Convert.ToString(foundIndex)));
        }

        public void ChangeSelection(object newsel)
        {
            SelectionHistory.Add(Selected);
            Selected = newsel;
            UpdateObjectLabel(newsel);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Selected = SelectionHistory.Last();
            SelectionHistory.RemoveAt(SelectionHistory.Count - 1);
            UpdateObjectLabel(Selected);
        }

        public void EnsureDataLoaded()
        {
            if (Data == null)
                throw new Exception("Please load data.win first!");
        }

        private async void MenuItem_OffsetMap_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.DefaultExt = "win";
            dlg.Filter = "Game Maker Studio data files (.win, .unx, .ios)|*.win;*.unx;*.ios|All files|*";

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
                                    foreach(var off in offsets.OrderBy((x) => x.Key))
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
    // TODO: Don't ever implement this. Toby Fox did not autorize you to do this.
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
