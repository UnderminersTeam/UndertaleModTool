using GraphVizWrapper;
using GraphVizWrapper.Commands;
using GraphVizWrapper.Queries;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Xml;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using static UndertaleModTool.MainWindow.CodeEditorMode;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleCodeEditor.xaml
    /// </summary>
    [SupportedOSPlatform("windows7.0")]
    public partial class UndertaleCodeEditor : DataUserControl
    {
        private static MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public UndertaleCode CurrentDisassembled = null;
        public UndertaleCode CurrentDecompiled = null;
        public List<string> CurrentLocals = null;
        public UndertaleCode CurrentGraphed = null;
        public string ProfileHash = mainWindow.ProfileHash;
        public string MainPath = Path.Combine(Settings.ProfilesFolder, mainWindow.ProfileHash, "Main");
        public string TempPath = Path.Combine(Settings.ProfilesFolder, mainWindow.ProfileHash, "Temp");

        public bool DecompiledFocused = false;
        public bool DecompiledChanged = false;
        public bool DecompiledYet = false;
        public bool DecompiledSkipped = false;
        public SearchPanel DecompiledSearchPanel;

        public bool DisassemblyFocused = false;
        public bool DisassemblyChanged = false;
        public bool DisassembledYet = false;
        public bool DisassemblySkipped = false;
        public SearchPanel DisassemblySearchPanel;

        public static RoutedUICommand Compile = new RoutedUICommand("Compile code", "Compile", typeof(UndertaleCodeEditor));

        public UndertaleCodeEditor()
        {
            InitializeComponent();

            // Decompiled editor styling and functionality
            DecompiledSearchPanel = SearchPanel.Install(DecompiledEditor.TextArea);
            DecompiledSearchPanel.MarkerBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90));

            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("UndertaleModTool.Resources.GML.xshd"))
            {
                using (XmlTextReader reader = new XmlTextReader(stream))
                {
                    DecompiledEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    var def = DecompiledEditor.SyntaxHighlighting;
                    if (mainWindow.Data.GeneralInfo.Major < 2)
                    {
                        foreach (var span in def.MainRuleSet.Spans)
                        {
                            string expr = span.StartExpression.ToString();
                            if (expr == "\"" || expr == "'")
                            {
                                span.RuleSet.Spans.Clear();
                            }
                        }
                    }
                    // This was an attempt to only highlight
                    // GMS 2.3+ keywords if the game is
                    // made in such a version.
                    // However despite what StackOverflow
                    // says, this isn't working so it's just
                    // hardcoded in the XML for now
                    /*
                    if(mainWindow.Data.GMS2_3)
                    {
                        HighlightingColor color = null;
                        foreach (var rule in def.MainRuleSet.Rules)
                        {
                            if (rule.Regex.IsMatch("if"))
                            {
                                color = rule.Color;
                                break;
                            }
                        }
                        if (color != null)
                        {
                            string[] keywords =
                            {
                                "new",
                                "function",
                                "keywords"
                            };
                            var rule = new HighlightingRule();
                            var regex = String.Format(@"\b(?>{0})\b", String.Join("|", keywords));

                            rule.Regex = new Regex(regex);
                            rule.Color = color;

                            def.MainRuleSet.Rules.Add(rule);
                        }
                    }*/
                }
            }

            DecompiledEditor.Options.ConvertTabsToSpaces = true;

            DecompiledEditor.TextArea.TextView.ElementGenerators.Add(new NumberGenerator());
            DecompiledEditor.TextArea.TextView.ElementGenerators.Add(new NameGenerator());

            DecompiledEditor.TextArea.TextView.Options.HighlightCurrentLine = true;
            DecompiledEditor.TextArea.TextView.CurrentLineBackground = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            DecompiledEditor.TextArea.TextView.CurrentLineBorder = new Pen() { Thickness = 0 };

            DecompiledEditor.Document.TextChanged += (s, e) =>
            {
                DecompiledFocused = true;
                DecompiledChanged = true;
            };

            DecompiledEditor.TextArea.SelectionBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            DecompiledEditor.TextArea.SelectionForeground = null;
            DecompiledEditor.TextArea.SelectionBorder = null;
            DecompiledEditor.TextArea.SelectionCornerRadius = 0;

            // Disassembly editor styling and functionality
            DisassemblySearchPanel = SearchPanel.Install(DisassemblyEditor.TextArea);
            DisassemblySearchPanel.MarkerBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90));

            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("UndertaleModTool.Resources.VMASM.xshd"))
            {
                using (XmlTextReader reader = new XmlTextReader(stream))
                {
                    DisassemblyEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }

            DisassemblyEditor.TextArea.TextView.ElementGenerators.Add(new NameGenerator());

            DisassemblyEditor.TextArea.TextView.Options.HighlightCurrentLine = true;
            DisassemblyEditor.TextArea.TextView.CurrentLineBackground = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            DisassemblyEditor.TextArea.TextView.CurrentLineBorder = new Pen() { Thickness = 0 };

            DisassemblyEditor.Document.TextChanged += (s, e) => DisassemblyChanged = true;

            DisassemblyEditor.TextArea.SelectionBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            DisassemblyEditor.TextArea.SelectionForeground = null;
            DisassemblyEditor.TextArea.SelectionBorder = null;
            DisassemblyEditor.TextArea.SelectionCornerRadius = 0;
        }

        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UndertaleCode code = this.DataContext as UndertaleCode;
            Directory.CreateDirectory(MainPath);
            Directory.CreateDirectory(TempPath);
            if (code == null)
                return;
            DecompiledSearchPanel.Close();
            DisassemblySearchPanel.Close();
            await DecompiledLostFocusBody(sender, null);
            DisassemblyEditor_LostFocus(sender, null);
            if (DisassemblyTab.IsSelected && code != CurrentDisassembled)
            {
                DisassembleCode(code, !DisassembledYet);
                DisassembledYet = true;
            }
            if (DecompiledTab.IsSelected && code != CurrentDecompiled)
            {
                _ = DecompileCode(code, !DecompiledYet);
                DecompiledYet = true;
            }
            if (GraphTab.IsSelected && code != CurrentGraphed)
            {
                GraphCode(code);
            }
        }

        private async void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UndertaleCode code = this.DataContext as UndertaleCode;
            if (code == null)
                return;

            // compile/disassemble previously edited code (save changes)
            if (DecompiledTab.IsSelected && DecompiledFocused && DecompiledChanged &&
                CurrentDecompiled is not null && CurrentDecompiled != code)
            {
                DecompiledSkipped = true;
                DecompiledEditor_LostFocus(sender, null);

            }
            else if (DisassemblyTab.IsSelected && DisassemblyFocused && DisassemblyChanged &&
                     CurrentDisassembled is not null && CurrentDisassembled != code)
            {
                DisassemblySkipped = true;
                DisassemblyEditor_LostFocus(sender, null);
            }

            DecompiledEditor_LostFocus(sender, null);
            DisassemblyEditor_LostFocus(sender, null);

            if (MainWindow.CodeEditorDecompile != Unstated) //if opened from the code search results "link"
            {
                if (MainWindow.CodeEditorDecompile == DontDecompile && code != CurrentDisassembled)
                {
                    if (CodeModeTabs.SelectedItem != DisassemblyTab)
                        CodeModeTabs.SelectedItem = DisassemblyTab;
                    else
                        DisassembleCode(code, true);
                }

                if (MainWindow.CodeEditorDecompile == Decompile && code != CurrentDecompiled)
                {
                    if (CodeModeTabs.SelectedItem != DecompiledTab)
                        CodeModeTabs.SelectedItem = DecompiledTab;
                    else
                        _ = DecompileCode(code, true);
                }

                MainWindow.CodeEditorDecompile = Unstated;
            }
            else
            {
                if (DisassemblyTab.IsSelected && code != CurrentDisassembled)
                {
                    DisassembleCode(code, true);
                }
                if (DecompiledTab.IsSelected && code != CurrentDecompiled)
                {
                    _ = DecompileCode(code, true);
                }
                if (GraphTab.IsSelected && code != CurrentGraphed)
                {
                    GraphCode(code);
                }
            }
        }

        public static readonly RoutedEvent CtrlKEvent = EventManager.RegisterRoutedEvent(
            "CtrlK", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UndertaleCodeEditor));

        private async Task CompileCommandBody(object sender, EventArgs e)
        {
            if (DecompiledFocused)
            {
                await DecompiledLostFocusBody(sender, new RoutedEventArgs(CtrlKEvent));
            }
            else if (DisassemblyFocused)
            {
                DisassemblyEditor_LostFocus(sender, new RoutedEventArgs(CtrlKEvent));
                DisassemblyEditor_GotFocus(sender, null);
            }

            await Task.Delay(1); //dummy await
        }
        private void Command_Compile(object sender, EventArgs e)
        {
            _ = CompileCommandBody(sender, e);
        }
        public async Task SaveChanges()
        {
            await CompileCommandBody(null, null);
        }

        private void DisassembleCode(UndertaleCode code, bool first)
        {
            code.UpdateAddresses();

            string text;

            DisassemblyEditor.TextArea.ClearSelection();
            if (code.ParentEntry != null)
            {
                DisassemblyEditor.IsReadOnly = true;
                text = "; This code entry is a reference to an anonymous function within " + code.ParentEntry.Name.Content + ", view it there";
            }
            else
            {
                DisassemblyEditor.IsReadOnly = false;

                var data = mainWindow.Data;
                text = code.Disassemble(data.Variables, data.CodeLocals.For(code));

                CurrentLocals = new List<string>();
            }

            DisassemblyEditor.Document.BeginUpdate();
            DisassemblyEditor.Document.Text = text;
            DisassemblyEditor.Document.EndUpdate();

            if (first)
                DisassemblyEditor.Document.UndoStack.ClearAll();

            CurrentDisassembled = code;
            DisassemblyChanged = false;
        }

        public static Dictionary<string, string> gettext = null;
        private void UpdateGettext(UndertaleCode gettextCode)
        {
            gettext = new Dictionary<string, string>();
            string[] decompilationOutput;
            if (!SettingsWindow.ProfileModeEnabled)
                decompilationOutput = Decompiler.Decompile(gettextCode, new GlobalDecompileContext(null, false)).Replace("\r\n", "\n").Split('\n');
            else
            {
                try
                {
                    string path = Path.Combine(TempPath, gettextCode.Name.Content + ".gml");
                    if (File.Exists(path))
                        decompilationOutput = File.ReadAllText(path).Replace("\r\n", "\n").Split('\n');
                    else
                        decompilationOutput = Decompiler.Decompile(gettextCode, new GlobalDecompileContext(null, false)).Replace("\r\n", "\n").Split('\n');
                }
                catch
                {
                    decompilationOutput = Decompiler.Decompile(gettextCode, new GlobalDecompileContext(null, false)).Replace("\r\n", "\n").Split('\n');
                }
            }
            Regex textdataRegex = new Regex("^ds_map_add\\(global\\.text_data_en, \\\"(.*)\\\", \\\"(.*)\\\"\\)");
            foreach (var line in decompilationOutput)
            {
                Match m = textdataRegex.Match(line);
                if (m.Success)
                {
                    try
                    {
                        gettext.Add(m.Groups[1].Value, m.Groups[2].Value);
                    }
                    catch (ArgumentException)
                    {
                        mainWindow.ShowError("There is a duplicate key in textdata_en, being " + m.Groups[1].Value + ". This may cause errors in the comment display of text.");
                    }
                    catch
                    {
                        mainWindow.ShowError("Unknown error in textdata_en. This may cause errors in the comment display of text.");
                    }
                }
            }
        }

        public static Dictionary<string, string> gettextJSON = null;
        private string UpdateGettextJSON(string json)
        {
            try
            {
                gettextJSON = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
            catch (Exception e)
            {
                gettextJSON = new Dictionary<string, string>();
                return "Failed to parse language file: " + e.Message;
            }
            return null;
        }

        private async Task DecompileCode(UndertaleCode code, bool first, LoaderDialog existingDialog = null)
        {
            DecompiledEditor.IsReadOnly = true;
            DecompiledEditor.TextArea.ClearSelection();
            if (code.ParentEntry != null)
            {
                DecompiledEditor.Text = "// This code entry is a reference to an anonymous function within " + code.ParentEntry.Name.Content + ", view it there";
                CurrentDecompiled = code;
                existingDialog?.TryClose();
            }
            else
            {
                LoaderDialog dialog;
                if (existingDialog != null)
                {
                    dialog = existingDialog;
                    dialog.Message = "Decompiling, please wait...";
                }
                else
                {
                    dialog = new LoaderDialog("Decompiling", "Decompiling, please wait... This can take a while on complex scripts.");
                    dialog.Owner = Window.GetWindow(this);
                    try
                    {
                        _ = Dispatcher.BeginInvoke(new Action(() => { if (!dialog.IsClosed) dialog.TryShowDialog(); }));
                    }
                    catch
                    {
                        // This is still a problem in rare cases for some unknown reason
                    }
                }

                bool openSaveDialog = false;

                UndertaleCode gettextCode = null;
                if (gettext == null)
                    gettextCode = mainWindow.Data.Code.ByName("gml_Script_textdata_en");

                string dataPath = Path.GetDirectoryName(mainWindow.FilePath);
                string gettextJsonPath = null;
                if (dataPath is not null)
                {
                    gettextJsonPath = Path.Combine(dataPath, "lang", "lang_en.json");
                    if (!File.Exists(gettextJsonPath))
                        gettextJsonPath = Path.Combine(dataPath, "lang", "lang_en_ch1.json");
                }

                var dataa = mainWindow.Data;
                Task t = Task.Run(() =>
                {
                    GlobalDecompileContext context = new GlobalDecompileContext(dataa, false);
                    string decompiled = null;
                    Exception e = null;
                    try
                    {
                        string path = Path.Combine(TempPath, code.Name.Content + ".gml");
                        if (!SettingsWindow.ProfileModeEnabled || !File.Exists(path))
                        {
                            decompiled = Decompiler.Decompile(code, context);
                        }
                        else
                            decompiled = File.ReadAllText(path);
                    }
                    catch (Exception ex)
                    {
                        e = ex;
                    }

                    if (gettextCode != null)
                        UpdateGettext(gettextCode);

                    try
                    {
                        if (gettextJSON == null && gettextJsonPath != null && File.Exists(gettextJsonPath))
                        {
                            string err = UpdateGettextJSON(File.ReadAllText(gettextJsonPath));
                            if (err != null)
                                e = new Exception(err);
                        }
                    }
                    catch (Exception exc)
                    {
                        mainWindow.ShowErrorInvoke(exc.ToString());
                    }

                    if (decompiled != null)
                    {
                        string[] decompiledLines;
                        if (gettext != null && decompiled.Contains("scr_gettext"))
                        {
                            decompiledLines = decompiled.Split('\n');
                            for (int i = 0; i < decompiledLines.Length; i++)
                            {
                                var matches = Regex.Matches(decompiledLines[i], "scr_gettext\\(\\\"(\\w*)\\\"\\)");
                                foreach (Match match in matches)
                                {
                                    if (match.Success)
                                    {
                                        if (gettext.TryGetValue(match.Groups[1].Value, out string text))
                                            decompiledLines[i] += $" // {text}";
                                    }
                                }
                            }
                            decompiled = string.Join('\n', decompiledLines);
                        }
                        else if (gettextJSON != null && decompiled.Contains("scr_84_get_lang_string"))
                        {
                            decompiledLines = decompiled.Split('\n');
                            for (int i = 0; i < decompiledLines.Length; i++)
                            {
                                var matches = Regex.Matches(decompiledLines[i], "scr_84_get_lang_string(\\w*)\\(\\\"(\\w*)\\\"\\)");
                                foreach (Match match in matches)
                                {
                                    if (match.Success)
                                    {
                                        if (gettextJSON.TryGetValue(match.Groups[^1].Value, out string text))
                                            decompiledLines[i] += $" // {text}";
                                    }
                                }
                            }
                            decompiled = string.Join('\n', decompiledLines);
                        }
                    }

                    Dispatcher.Invoke(() =>
                    {
                        if (DataContext != code)
                            return; // Switched to another code entry or otherwise

                        DecompiledEditor.Document.BeginUpdate();
                        if (e != null)
                            DecompiledEditor.Document.Text = "/* EXCEPTION!\n   " + e.ToString() + "\n*/";
                        else if (decompiled != null)
                        {
                            DecompiledEditor.Document.Text = decompiled;
                            CurrentLocals = new List<string>();

                            var locals = dataa.CodeLocals.ByName(code.Name.Content);
                            if (locals != null)
                            {
                                foreach (var local in locals.Locals)
                                    CurrentLocals.Add(local.Name.Content);
                            }

                            if (existingDialog is not null)                      //if code was edited (and compiles after it)
                            {
                                dataa.GMLCacheChanged.Add(code.Name.Content);
                                dataa.GMLCacheFailed?.Remove(code.Name.Content); //remove that code name, since that code compiles now

                                openSaveDialog = mainWindow.IsSaving;
                            }
                        }
                        DecompiledEditor.Document.EndUpdate();
                        DecompiledEditor.IsReadOnly = false;
                        if (first)
                            DecompiledEditor.Document.UndoStack.ClearAll();
                        DecompiledChanged = false;

                        CurrentDecompiled = code;
                        dialog.Hide();
                    });
                });
                await t;
                dialog.Close();

                mainWindow.IsSaving = false;

                if (openSaveDialog)
                    await mainWindow.DoSaveDialog();
            }
        }

        private async void GraphCode(UndertaleCode code)
        {
            if (code.ParentEntry != null)
            {
                GraphView.Source = null;
                CurrentGraphed = code;
                return;
            }

            LoaderDialog dialog = new LoaderDialog("Generating graph", "Generating graph, please wait...");
            dialog.Owner = Window.GetWindow(this);
            Task t = Task.Run(() =>
            {
                ImageSource image = null;
                try
                {
                    code.UpdateAddresses();
                    List<uint> entryPoints = new List<uint>();
                    entryPoints.Add(0);
                    foreach (UndertaleCode duplicate in code.ChildEntries)
                        entryPoints.Add(duplicate.Offset / 4);
                    var blocks = Decompiler.DecompileFlowGraph(code, entryPoints);
                    string dot = Decompiler.ExportFlowGraph(blocks);

                    try
                    {
                        var getStartProcessQuery = new GetStartProcessQuery();
                        var getProcessStartInfoQuery = new GetProcessStartInfoQuery();
                        var registerLayoutPluginCommand = new RegisterLayoutPluginCommand(getProcessStartInfoQuery, getStartProcessQuery);
                        var wrapper = new GraphGeneration(getStartProcessQuery, getProcessStartInfoQuery, registerLayoutPluginCommand);
                        wrapper.GraphvizPath = Settings.Instance.GraphVizPath;

                        byte[] output = wrapper.GenerateGraph(dot, Enums.GraphReturnType.Png); // TODO: Use SVG instead

                        image = new ImageSourceConverter().ConvertFrom(output) as ImageSource;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                        if (mainWindow.ShowQuestionInvoke("Unable to execute GraphViz: " + e.Message + "\nMake sure you have downloaded it and set the path in settings.\nDo you want to open the download page now?", MessageBoxImage.Error) == MessageBoxResult.Yes)
                            MainWindow.OpenBrowser("https://graphviz.gitlab.io/_pages/Download/Download_windows.html");
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                    mainWindow.ShowErrorInvoke(e.Message, "Graph generation failed");
                }

                Dispatcher.Invoke(() =>
                {
                    GraphView.Source = image;
                    CurrentGraphed = code;
                    dialog.Hide();
                });
            });
            dialog.ShowDialog();
            await t;
        }

        private void DecompiledEditor_GotFocus(object sender, RoutedEventArgs e)
        {
            if (DecompiledEditor.IsReadOnly)
                return;
            DecompiledFocused = true;
        }

        private static string Truncate(string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
        }

        private async Task DecompiledLostFocusBody(object sender, RoutedEventArgs e)
        {
            if (!DecompiledFocused)
                return;
            if (DecompiledEditor.IsReadOnly)
                return;
            DecompiledFocused = false;

            if (!DecompiledChanged)
                return;

            UndertaleCode code;
            if (DecompiledSkipped)
            {
                code = CurrentDecompiled;
                DecompiledSkipped = false;
            }
            else
                code = this.DataContext as UndertaleCode;

            if (code == null)
            {
                if (IsLoaded)
                    code = CurrentDecompiled; // switched to the tab with different object type
                else
                    return;                   // probably loaded another data.win or something.
            }

            if (code.ParentEntry != null)
                return;

            // Check to make sure this isn't an element inside of the textbox, or another tab
            IInputElement elem = Keyboard.FocusedElement;
            if (elem is UIElement)
            {
                if (e != null && e.RoutedEvent?.Name != "CtrlK" && (elem as UIElement).IsDescendantOf(DecompiledEditor))
                    return;
            }

            UndertaleData data = mainWindow.Data;

            LoaderDialog dialog = new LoaderDialog("Compiling", "Compiling, please wait...");
            dialog.Owner = Window.GetWindow(this);
            try
            {
                _ = Dispatcher.BeginInvoke(new Action(() => { if (!dialog.IsClosed) dialog.TryShowDialog(); }));
            }
            catch
            {
                // This is still a problem in rare cases for some unknown reason
            }

            CompileContext compileContext = null;
            string text = DecompiledEditor.Text;
            var dispatcher = Dispatcher;
            Task t = Task.Run(() =>
            {
                compileContext = Compiler.CompileGMLText(text, data, code, (f) => { dispatcher.Invoke(() => f()); });
            });
            await t;

            if (compileContext == null)
            {
                dialog.TryClose();
                mainWindow.ShowErrorInvoke("Compile context was null for some reason...", "This shouldn't happen");
                return;
            }

            if (compileContext.HasError)
            {
                dialog.TryClose();
                mainWindow.ShowErrorInvoke(Truncate(compileContext.ResultError, 512), "Compiler error");
                return;
            }

            if (!compileContext.SuccessfulCompile)
            {
                dialog.TryClose();
                mainWindow.ShowErrorInvoke("(unknown error message)", "Compile failed");
                return;
            }

            code.Replace(compileContext.ResultAssembly);

            if (!mainWindow.Data.GMS2_3)
            {
                try
                {
                    string path = Path.Combine(TempPath, code.Name.Content + ".gml");
                    if (SettingsWindow.ProfileModeEnabled)
                    {
                        // Write text, only if in the profile mode.
                        File.WriteAllText(path, DecompiledEditor.Text);
                    }
                    else
                    {
                        // Destroy file with comments if it's been edited outside the profile mode.
                        // We're dealing with the decompiled code only, it has to happen.
                        // Otherwise it will cause a desync, which is more important to prevent.
                        if (File.Exists(path))
                            File.Delete(path);
                    }
                }
                catch (Exception exc)
                {
                    mainWindow.ShowErrorInvoke("Error during writing of GML code to profile:\n" + exc);
                }
            }

            // Invalidate gettext if necessary
            if (code.Name.Content == "gml_Script_textdata_en")
                gettext = null;

            // Show new code, decompiled.
            CurrentDisassembled = null;
            CurrentDecompiled = null;
            CurrentGraphed = null;

            // Tab switch
            if (e == null)
            {
                dialog.TryClose();
                return;
            }

            // Decompile new code
            await DecompileCode(code, false, dialog);

            //GMLCacheChanged.Add() is inside DecompileCode()
        }
        private void DecompiledEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            _ = DecompiledLostFocusBody(sender, e);
        }

        private void DisassemblyEditor_GotFocus(object sender, RoutedEventArgs e)
        {
            if (DisassemblyEditor.IsReadOnly)
                return;
            DisassemblyFocused = true;
        }

        private void DisassemblyEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!DisassemblyFocused)
                return;
            if (DisassemblyEditor.IsReadOnly)
                return;
            DisassemblyFocused = false;

            if (!DisassemblyChanged)
                return;

            UndertaleCode code;
            if (DisassemblySkipped)
            {
                code = CurrentDisassembled;
                DisassemblySkipped = false;
            }
            else
                code = this.DataContext as UndertaleCode;

            if (code == null)
            {
                if (IsLoaded)
                    code = CurrentDisassembled; // switched to the tab with different object type
                else
                    return;                     // probably loaded another data.win or something.
            }

            // Check to make sure this isn't an element inside of the textbox, or another tab
            IInputElement elem = Keyboard.FocusedElement;
            if (elem is UIElement)
            {
                if (e != null && e.RoutedEvent?.Name != "CtrlK" && (elem as UIElement).IsDescendantOf(DisassemblyEditor))
                    return;
            }

            UndertaleData data = mainWindow.Data;
            try
            {
                var instructions = Assembler.Assemble(DisassemblyEditor.Text, data);
                code.Replace(instructions);
                mainWindow.NukeProfileGML(code.Name.Content);
            }
            catch (Exception ex)
            {
                mainWindow.ShowError(ex.ToString(), "Assembler error");
                return;
            }

            // Get rid of old code
            CurrentDisassembled = null;
            CurrentDecompiled = null;
            CurrentGraphed = null;

            // Tab switch
            if (e == null)
                return;

            // Disassemble new code
            DisassembleCode(code, false);

            if (!DisassemblyEditor.IsReadOnly)
            {
                data.GMLCacheChanged.Add(code.Name.Content);

                if (mainWindow.IsSaving)
                {
                    mainWindow.IsSaving = false;

                    _ = mainWindow.DoSaveDialog();
                }
            }
        }

        // Based on https://stackoverflow.com/questions/28379206/custom-hyperlinks-using-avalonedit
        public class NumberGenerator : VisualLineElementGenerator
        {
            readonly static Regex regex = new Regex(@"-?\d+\.?");

            public NumberGenerator()
            {
            }

            Match FindMatch(int startOffset, Regex r)
            {
                // fetch the end offset of the VisualLine being generated
                int endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
                TextDocument document = CurrentContext.Document;
                string relevantText = document.GetText(startOffset, endOffset - startOffset);
                return r.Match(relevantText);
            }

            /// Gets the first offset >= startOffset where the generator wants to construct
            /// an element.
            /// Return -1 to signal no interest.
            public override int GetFirstInterestedOffset(int startOffset)
            {
                Match m = FindMatch(startOffset, regex);

                var textArea = CurrentContext.TextView.GetService(typeof(TextArea)) as TextArea;
                var highlighter = textArea.GetService(typeof(IHighlighter)) as IHighlighter;
                int line = CurrentContext.Document.GetLocation(startOffset).Line;
                HighlightedLine highlighted = null;
                try
                {
                    highlighted = highlighter.HighlightLine(line);
                }
                catch
                {
                }

                while (m.Success)
                {
                    int res = startOffset + m.Index;
                    int currLine = CurrentContext.Document.GetLocation(res).Line;
                    if (currLine != line)
                    {
                        line = currLine;
                        highlighted = highlighter.HighlightLine(line);
                    }

                    foreach (var section in highlighted.Sections)
                    {
                        if (section.Color.Name == "Number" &&
                            section.Offset == res)
                            return res;
                    }

                    startOffset += m.Length;
                    m = FindMatch(startOffset, regex);
                }

                return -1;
            }

            /// Constructs an element at the specified offset.
            /// May return null if no element should be constructed.
            public override VisualLineElement ConstructElement(int offset)
            {
                Match m = FindMatch(offset, regex);

                if (m.Success && m.Index == 0)
                {
                    var line = new ClickVisualLineText(m.Value, CurrentContext.VisualLine, m.Length);
                    var doc = CurrentContext.Document;
                    var textArea = CurrentContext.TextView.GetService(typeof(TextArea)) as TextArea;
                    var editor = textArea.GetService(typeof(TextEditor)) as TextEditor;
                    var parent = VisualTreeHelper.GetParent(editor);
                    do
                    {
                        if ((parent as FrameworkElement) is UserControl)
                            break;
                        parent = VisualTreeHelper.GetParent(parent);
                    } while (parent != null);
                    line.Clicked += (text) =>
                    {
                        if (text.EndsWith("."))
                            return;
                        if (int.TryParse(text, out int id))
                        {
                            (parent as UndertaleCodeEditor).DecompiledFocused = true;
                            UndertaleData data = mainWindow.Data;

                            List<UndertaleObject> possibleObjects = new List<UndertaleObject>();
                            if (id >= 0)
                            {
                                if (id < data.Sprites.Count)
                                    possibleObjects.Add(data.Sprites[id]);
                                if (id < data.Rooms.Count)
                                    possibleObjects.Add(data.Rooms[id]);
                                if (id < data.GameObjects.Count)
                                    possibleObjects.Add(data.GameObjects[id]);
                                if (id < data.Backgrounds.Count)
                                    possibleObjects.Add(data.Backgrounds[id]);
                                if (id < data.Scripts.Count)
                                    possibleObjects.Add(data.Scripts[id]);
                                if (id < data.Paths.Count)
                                    possibleObjects.Add(data.Paths[id]);
                                if (id < data.Fonts.Count)
                                    possibleObjects.Add(data.Fonts[id]);
                                if (id < data.Sounds.Count)
                                    possibleObjects.Add(data.Sounds[id]);
                                if (id < data.Shaders.Count)
                                    possibleObjects.Add(data.Shaders[id]);
                                if (id < data.Timelines.Count)
                                    possibleObjects.Add(data.Timelines[id]);
                            }

                            ContextMenu contextMenu = new ContextMenu();
                            foreach (UndertaleObject obj in possibleObjects)
                            {
                                MenuItem item = new MenuItem();
                                item.Header = obj.ToString().Replace("_", "__");
                                item.Click += (sender2, ev2) =>
                                {
                                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                                        mainWindow.ChangeSelection(obj);
                                    else
                                    {
                                        doc.Replace(line.ParentVisualLine.StartOffset + line.RelativeTextOffset,
                                                    text.Length, (obj as UndertaleNamedResource).Name.Content, null);
                                        (parent as UndertaleCodeEditor).DecompiledChanged = true;
                                    }
                                };
                                contextMenu.Items.Add(item);
                            }
                            if (id > 0x00050000)
                            {
                                MenuItem item = new MenuItem();
                                item.Header = "0x" + id.ToString("X6") + " (color)";
                                item.Click += (sender2, ev2) =>
                                {
                                    if (!((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift))
                                    {
                                        doc.Replace(line.ParentVisualLine.StartOffset + line.RelativeTextOffset,
                                                    text.Length, "0x" + id.ToString("X6"), null);
                                        (parent as UndertaleCodeEditor).DecompiledChanged = true;
                                    }
                                };
                                contextMenu.Items.Add(item);
                            }
                            BuiltinList list = mainWindow.Data.BuiltinList;
                            var myKey = list.Constants.FirstOrDefault(x => x.Value == (double)id).Key;
                            if (myKey != null)
                            {
                                MenuItem item = new MenuItem();
                                item.Header = myKey.Replace("_", "__") + " (constant)";
                                item.Click += (sender2, ev2) =>
                                {
                                    if (!((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift))
                                    {
                                        doc.Replace(line.ParentVisualLine.StartOffset + line.RelativeTextOffset,
                                                    text.Length, myKey, null);
                                        (parent as UndertaleCodeEditor).DecompiledChanged = true;
                                    }
                                };
                                contextMenu.Items.Add(item);
                            }
                            contextMenu.Items.Add(new MenuItem() { Header = id + " (number)", IsEnabled = false });

                            contextMenu.IsOpen = true;
                        }
                    };
                    return line;
                }

                return null;
            }
        }

        public class NameGenerator : VisualLineElementGenerator
        {
            readonly static Regex regex = new Regex(@"[_a-zA-Z][_a-zA-Z0-9]*");

            public NameGenerator()
            {
            }

            Match FindMatch(int startOffset, Regex r)
            {
                // fetch the end offset of the VisualLine being generated
                int endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
                TextDocument document = CurrentContext.Document;
                string relevantText = document.GetText(startOffset, endOffset - startOffset);
                return r.Match(relevantText);
            }

            /// Gets the first offset >= startOffset where the generator wants to construct
            /// an element.
            /// Return -1 to signal no interest.
            public override int GetFirstInterestedOffset(int startOffset)
            {
                Match m = FindMatch(startOffset, regex);

                var textArea = CurrentContext.TextView.GetService(typeof(TextArea)) as TextArea;
                var highlighter = textArea.GetService(typeof(IHighlighter)) as IHighlighter;
                int line = CurrentContext.Document.GetLocation(startOffset).Line;
                HighlightedLine highlighted = null;
                try
                {
                    highlighted = highlighter.HighlightLine(line);
                }
                catch
                {
                }

                while (m.Success)
                {
                    int res = startOffset + m.Index;
                    int currLine = CurrentContext.Document.GetLocation(res).Line;
                    if (currLine != line)
                    {
                        line = currLine;
                        highlighted = highlighter.HighlightLine(line);
                    }

                    foreach (var section in highlighted.Sections)
                    {
                        if (section.Color.Name == "Identifier" || section.Color.Name == "Function")
                        {
                            if (section.Offset == res)
                                return res;
                        }
                    }

                    startOffset += m.Length;
                    m = FindMatch(startOffset, regex);
                }
                return -1;
            }

            /// Constructs an element at the specified offset.
            /// May return null if no element should be constructed.
            public override VisualLineElement ConstructElement(int offset)
            {
                Match m = FindMatch(offset, regex);

                if (m.Success && m.Index == 0)
                {
                    UndertaleData data = mainWindow.Data;
                    bool func = (offset + m.Length + 1 < CurrentContext.VisualLine.LastDocumentLine.EndOffset) &&
                                (CurrentContext.Document.GetCharAt(offset + m.Length) == '(');
                    UndertaleNamedResource val = null;

                    var doc = CurrentContext.Document;
                    var textArea = CurrentContext.TextView.GetService(typeof(TextArea)) as TextArea;
                    var editor = textArea.GetService(typeof(TextEditor)) as TextEditor;
                    var parent = VisualTreeHelper.GetParent(editor);
                    do
                    {
                        if ((parent as FrameworkElement) is UserControl)
                            break;
                        parent = VisualTreeHelper.GetParent(parent);
                    } while (parent != null);

                    // Process the content of this identifier/function
                    if (func)
                    {
                        val = null;
                        if (!data.GMS2_3) // in GMS2.3 every custom "function" is in fact a member variable and scripts are never referenced directly
                            val = data.Scripts.ByName(m.Value);
                        if (val == null)
                        {
                            val = data.Functions.ByName(m.Value);
                            if (data.GMS2_3)
                            {
                                if (val != null)
                                {
                                    if (data.Code.ByName(val.Name.Content) != null)
                                        val = null; // in GMS2.3 every custom "function" is in fact a member variable, and the names in functions make no sense (they have the gml_Script_ prefix)
                                }
                                else
                                {
                                    // Resolve 2.3 sub-functions for their parent entry
                                    UndertaleFunction f = null;
                                    if (data.KnownSubFunctions?.TryGetValue(m.Value, out f) == true)
                                        val = data.Scripts.ByName(f.Name.Content).Code?.ParentEntry;
                                }
                            }
                        }
                        if (val == null)
                        {
                            if (data.BuiltinList.Functions.ContainsKey(m.Value))
                            {
                                var res = new ColorVisualLineText(m.Value, CurrentContext.VisualLine, m.Length,
                                                                  new SolidColorBrush(Color.FromRgb(0xFF, 0xB8, 0x71)));
                                res.Bold = true;
                                return res;
                            }
                        }
                    }
                    else
                    {
                        val = data.ByName(m.Value);
                        if (data.GMS2_3 & val is UndertaleScript)
                            val = null; // in GMS2.3 scripts are never referenced directly
                    }
                    if (val == null)
                    {
                        if (offset >= 7)
                        {
                            if (CurrentContext.Document.GetText(offset - 7, 7) == "global.")
                            {
                                return new ColorVisualLineText(m.Value, CurrentContext.VisualLine, m.Length,
                                                                new SolidColorBrush(Color.FromRgb(0xF9, 0x7B, 0xF9)));
                            }
                        }
                        if (data.BuiltinList.Constants.ContainsKey(m.Value))
                            return new ColorVisualLineText(m.Value, CurrentContext.VisualLine, m.Length,
                                                            new SolidColorBrush(Color.FromRgb(0xFF, 0x80, 0x80)));
                        if (data.BuiltinList.GlobalNotArray.ContainsKey(m.Value) ||
                            data.BuiltinList.Instance.ContainsKey(m.Value) ||
                            data.BuiltinList.GlobalArray.ContainsKey(m.Value))
                            return new ColorVisualLineText(m.Value, CurrentContext.VisualLine, m.Length,
                                                            new SolidColorBrush(Color.FromRgb(0x58, 0xE3, 0x5A)));
                        if ((parent as UndertaleCodeEditor).CurrentLocals.Contains(m.Value))
                            return new ColorVisualLineText(m.Value, CurrentContext.VisualLine, m.Length,
                                                            new SolidColorBrush(Color.FromRgb(0xFF, 0xF8, 0x99)));
                        return null;
                    }

                    var line = new ClickVisualLineText(m.Value, CurrentContext.VisualLine, m.Length,
                                                        func ? new SolidColorBrush(Color.FromRgb(0xFF, 0xB8, 0x71)) :
                                                               new SolidColorBrush(Color.FromRgb(0xFF, 0x80, 0x80)));
                    if (func)
                        line.Bold = true;
                    line.Clicked += (text) =>
                    {
                        mainWindow.ChangeSelection(val);
                    };

                    return line;
                }

                return null;
            }
        }

        public class ColorVisualLineText : VisualLineText
        {
            private string Text { get; set; }
            private Brush ForegroundBrush { get; set; }

            public bool Bold { get; set; } = false;

            /// <summary>
            /// Creates a visual line text element with the specified length.
            /// It uses the <see cref="ITextRunConstructionContext.VisualLine"/> and its
            /// <see cref="VisualLineElement.RelativeTextOffset"/> to find the actual text string.
            /// </summary>
            public ColorVisualLineText(string text, VisualLine parentVisualLine, int length, Brush foregroundBrush)
                : base(parentVisualLine, length)
            {
                Text = text;
                ForegroundBrush = foregroundBrush;
            }

            public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
            {
                if (ForegroundBrush != null)
                    TextRunProperties.SetForegroundBrush(ForegroundBrush);
                if (Bold)
                    TextRunProperties.SetTypeface(new Typeface(TextRunProperties.Typeface.FontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal));
                return base.CreateTextRun(startVisualColumn, context);
            }

            protected override VisualLineText CreateInstance(int length)
            {
                return new ColorVisualLineText(Text, ParentVisualLine, length, null);
            }
        }

        public class ClickVisualLineText : VisualLineText
        {

            public delegate void ClickHandler(string text);

            public event ClickHandler Clicked;

            private string Text { get; set; }
            private Brush ForegroundBrush { get; set; }

            public bool Bold { get; set; } = false;

            /// <summary>
            /// Creates a visual line text element with the specified length.
            /// It uses the <see cref="ITextRunConstructionContext.VisualLine"/> and its
            /// <see cref="VisualLineElement.RelativeTextOffset"/> to find the actual text string.
            /// </summary>
            public ClickVisualLineText(string text, VisualLine parentVisualLine, int length, Brush foregroundBrush = null)
                : base(parentVisualLine, length)
            {
                Text = text;
                ForegroundBrush = foregroundBrush;
            }


            public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
            {
                if (ForegroundBrush != null)
                    TextRunProperties.SetForegroundBrush(ForegroundBrush);
                if (Bold)
                    TextRunProperties.SetTypeface(new Typeface(TextRunProperties.Typeface.FontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal));
                return base.CreateTextRun(startVisualColumn, context);
            }

            bool LinkIsClickable()
            {
                if (string.IsNullOrEmpty(Text))
                    return false;
                return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            }


            protected override void OnQueryCursor(QueryCursorEventArgs e)
            {
                if (LinkIsClickable())
                {
                    e.Handled = true;
                    e.Cursor = Cursors.Hand;
                }
            }

            protected override void OnMouseDown(MouseButtonEventArgs e)
            {
                if (e.Handled)
                    return;
                if ((e.ChangedButton == System.Windows.Input.MouseButton.Left && LinkIsClickable()) ||
                     e.ChangedButton == System.Windows.Input.MouseButton.Middle)
                {
                    if (Clicked != null)
                    {
                        Clicked(Text);
                        e.Handled = true;
                    }
                }
            }

            protected override VisualLineText CreateInstance(int length)
            {
                var res = new ClickVisualLineText(Text, ParentVisualLine, length);
                res.Clicked += Clicked;
                return res;
            }
        }
    }
}
