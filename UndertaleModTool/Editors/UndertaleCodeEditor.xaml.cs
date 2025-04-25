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
using System.Collections;
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
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shell;
using System.Xml;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using Input = System.Windows.Input;

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
        public List<string> CurrentLocals = new();

        public bool DecompiledFocused = false;
        public bool DecompiledChanged = false;
        public bool DecompiledYet = false;
        public bool DecompiledSkipped = false;
        public SearchPanel DecompiledSearchPanel;
        public static (int Line, int Column, double ScrollPos) OverriddenDecompPos { get; set; }

        public bool DisassemblyFocused = false;
        public bool DisassemblyChanged = false;
        public bool DisassembledYet = false;
        public bool DisassemblySkipped = false;
        public SearchPanel DisassemblySearchPanel;
        public static (int Line, int Column, double ScrollPos) OverriddenDisasmPos { get; set; }

        public static RoutedUICommand Compile = new RoutedUICommand("Compile code", "Compile", typeof(UndertaleCodeEditor));

        private static readonly Dictionary<string, UndertaleNamedResource> NamedObjDict = new();
        private static readonly Dictionary<string, UndertaleNamedResource> ScriptsDict = new();
        private static readonly Dictionary<string, UndertaleNamedResource> FunctionsDict = new();
        private static readonly Dictionary<string, UndertaleNamedResource> CodeDict = new();

        public enum CodeEditorTab
        {
            Unknown,
            Disassembly,
            Decompiled
        }
        public static CodeEditorTab EditorTab { get; set; } = CodeEditorTab.Unknown;

        public UndertaleCodeEditor()
        {
            InitializeComponent();

            // Decompiled editor styling and functionality
            DecompiledSearchPanel = SearchPanel.Install(DecompiledEditor.TextArea);
            DecompiledSearchPanel.LostFocus += SearchPanel_LostFocus;
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
                    if(mainWindow.Data.IsVersionAtLeast(2, 3))
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

            TextArea textArea = DecompiledEditor.TextArea;
            textArea.TextView.ElementGenerators.Add(new NumberGenerator(this, textArea));
            textArea.TextView.ElementGenerators.Add(new NameGenerator(this, textArea));

            textArea.TextView.Options.HighlightCurrentLine = true;
            textArea.TextView.CurrentLineBackground = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            textArea.TextView.CurrentLineBorder = new Pen() { Thickness = 0 };

            DecompiledEditor.Document.TextChanged += (s, e) =>
            {
                DecompiledFocused = true;
                DecompiledChanged = true;
            };

            textArea.SelectionBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            textArea.SelectionForeground = null;
            textArea.SelectionBorder = null;
            textArea.SelectionCornerRadius = 0;

            // Disassembly editor styling and functionality
            DisassemblySearchPanel = SearchPanel.Install(DisassemblyEditor.TextArea);
            DisassemblySearchPanel.LostFocus += SearchPanel_LostFocus;
            DisassemblySearchPanel.MarkerBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90));

            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("UndertaleModTool.Resources.VMASM.xshd"))
            {
                using (XmlTextReader reader = new XmlTextReader(stream))
                {
                    DisassemblyEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }

            textArea = DisassemblyEditor.TextArea;
            textArea.TextView.ElementGenerators.Add(new NameGenerator(this, textArea));

            textArea.TextView.Options.HighlightCurrentLine = true;
            textArea.TextView.CurrentLineBackground = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            textArea.TextView.CurrentLineBorder = new Pen() { Thickness = 0 };

            DisassemblyEditor.Document.TextChanged += (s, e) => DisassemblyChanged = true;

            textArea.SelectionBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            textArea.SelectionForeground = null;
            textArea.SelectionBorder = null;
            textArea.SelectionCornerRadius = 0;
        }

        private void UndertaleCodeEditor_Unloaded(object sender, RoutedEventArgs e)
        {
            OverriddenDecompPos = default;
            OverriddenDisasmPos = default;
        }

        private void SearchPanel_LostFocus(object sender, RoutedEventArgs e)
        {
            SearchPanel panel = sender as SearchPanel;
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance;
            FieldInfo toolTipField = typeof(SearchPanel).GetField("messageView", flags);
            if (toolTipField is null)
            {
                Debug.WriteLine("The source code of \"AvalonEdit.Search.SearchPanel\" was changed - can't find \"messageView\" field.");
                return;
            }

            ToolTip noMatchesTT = toolTipField.GetValue(panel) as ToolTip;
            if (noMatchesTT is null)
            {
                Debug.WriteLine("Can't get an instance of the \"SearchPanel.messageView\" popup.");
                return;
            }

            noMatchesTT.IsOpen = false;
        }

        private void UndertaleCodeEditor_Loaded(object sender, RoutedEventArgs e)
        {
            FillInCodeViewer();
        }
        private void FillInCodeViewer(bool overrideFirst = false)
        {
            UndertaleCode code = DataContext as UndertaleCode;
            if (DisassemblyTab.IsSelected && code != CurrentDisassembled)
            {
                if (!overrideFirst)
                {
                    DisassembleCode(code, !DisassembledYet);
                    DisassembledYet = true;
                }
                else
                    DisassembleCode(code, true);
            }
            if (DecompiledTab.IsSelected && code != CurrentDecompiled)
            {
                if (!overrideFirst)
                {
                    _ = DecompileCode(code, !DecompiledYet);
                    DecompiledYet = true;
                }
                else
                    _ = DecompileCode(code, true);
            }
            if (DecompiledTab.IsSelected)
            {
                // Re-populate local variables when in decompiled code, fixing #1320
                PopulateCurrentLocals(mainWindow.Data, code);
            }
        }

        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UndertaleCode code = this.DataContext as UndertaleCode;
            if (code == null)
                return;

            DecompiledSearchPanel.Close();
            DisassemblySearchPanel.Close();

            await DecompiledLostFocusBody(sender, null);
            DisassemblyEditor_LostFocus(sender, null);

            if (!IsLoaded)
            {
                // If it's not loaded, then "FillInCodeViewer()" will be executed on load.
                // This prevents a bug with freezing on code opening.
                return;
            }

            FillInCodeViewer();
        }

        private async void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UndertaleCode code = this.DataContext as UndertaleCode;
            if (code == null)
                return;

            FillObjectDicts();

            // compile/disassemble previously edited code (save changes)
            if (DecompiledTab.IsSelected && DecompiledFocused && DecompiledChanged &&
                CurrentDecompiled is not null && CurrentDecompiled != code)
            {
                DecompiledSkipped = true;
                await DecompiledLostFocusBody(sender, null);
            }
            else if (DisassemblyTab.IsSelected && DisassemblyFocused && DisassemblyChanged &&
                     CurrentDisassembled is not null && CurrentDisassembled != code)
            {
                DisassemblySkipped = true;
                DisassemblyEditor_LostFocus(sender, null);
            }

            await DecompiledLostFocusBody(sender, null);
            DisassemblyEditor_LostFocus(sender, null);

            DecompiledYet = false;
            DisassembledYet = false;
            CurrentDecompiled = null;
            CurrentDisassembled = null;

            if (EditorTab != CodeEditorTab.Unknown) // If opened from the code search results "link"
            {
                if (EditorTab == CodeEditorTab.Disassembly && code != CurrentDisassembled)
                {
                    if (CodeModeTabs.SelectedItem != DisassemblyTab)
                        CodeModeTabs.SelectedItem = DisassemblyTab;
                    else
                        DisassembleCode(code, true);
                }

                if (EditorTab == CodeEditorTab.Decompiled && code != CurrentDecompiled)
                {
                    if (CodeModeTabs.SelectedItem != DecompiledTab)
                        CodeModeTabs.SelectedItem = DecompiledTab;
                    else
                        _ = DecompileCode(code, true);
                }

                EditorTab = CodeEditorTab.Unknown;
            }
            else
                FillInCodeViewer(true);
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

        public void RestoreState(CodeTabState tabState)
        {
            if (tabState.IsDecompiledOpen)
                CodeModeTabs.SelectedItem = DecompiledTab;
            else
                CodeModeTabs.SelectedItem = DisassemblyTab;

            TextEditor textEditor = DecompiledEditor;
            (int linePos, int columnPos, double scrollPos) = tabState.DecompiledCodePosition;
            RestoreCaretPosition(textEditor, linePos, columnPos, scrollPos);

            textEditor = DisassemblyEditor;
            (linePos, columnPos, scrollPos) = tabState.DisassemblyCodePosition;
            RestoreCaretPosition(textEditor, linePos, columnPos, scrollPos);
        }
        private static void RestoreCaretPosition(TextEditor textEditor, int linePos, int columnPos, double scrollPos)
        {
            if (linePos <= textEditor.LineCount)
            {
                if (linePos == -1)
                    linePos = textEditor.Document.LineCount;

                int lineLen = textEditor.Document.GetLineByNumber(linePos).Length;
                textEditor.TextArea.Caret.Line = linePos;
                if (columnPos != -1)
                    textEditor.TextArea.Caret.Column = columnPos;
                else
                    textEditor.TextArea.Caret.Column = lineLen + 1;

                textEditor.ScrollToLine(linePos);
                if (scrollPos != -1)
                    textEditor.ScrollToVerticalOffset(scrollPos);
            }
            else
            {
                textEditor.CaretOffset = textEditor.Text.Length;
                textEditor.ScrollToEnd();
            }
        }
        public static void ChangeLineNumber(int lineNum, CodeEditorTab editorTab)
        {
            if (lineNum < 1)
                return;

            if (editorTab == CodeEditorTab.Unknown)
            {
                Debug.WriteLine($"The \"{nameof(editorTab)}\" argument of \"{nameof(ChangeLineNumber)}()\" is \"{nameof(CodeEditorTab.Unknown)}\".");
                return;
            }

            if (editorTab == CodeEditorTab.Decompiled)
                OverriddenDecompPos = (lineNum, -1, -1);
            else
                OverriddenDisasmPos = (lineNum, -1, -1);
        }
        public static void ChangeLineNumber(int lineNum, TextEditor textEditor)
        {
            if (lineNum < 1)
                return;

            if (textEditor is null)
            {
                Debug.WriteLine($"The \"{nameof(textEditor)}\" argument of \"{nameof(ChangeLineNumber)}()\" is null.");
                return;
            }

            RestoreCaretPosition(textEditor, lineNum, -1, -1);
        }

        private static void FillObjectDicts()
        {
            var data = mainWindow.Data;
            var objLists = new IEnumerable[] {
                data.Sounds,
                data.Sprites,
                data.Backgrounds,
                data.Paths,
                data.Scripts,
                data.Fonts,
                data.GameObjects,
                data.Rooms,
                data.Extensions,
                data.Shaders,
                data.Timelines,
                data.AnimationCurves,
                data.Sequences,
                data.AudioGroups
            };

            NamedObjDict.Clear();
            ScriptsDict.Clear();
            FunctionsDict.Clear();
            CodeDict.Clear();

            foreach (var list in objLists)
            {
                if (list is null)
                    continue;

                foreach (var obj in list)
                {
                    if (obj is not UndertaleNamedResource namedObj)
                        return;

                    NamedObjDict[namedObj.Name.Content] = namedObj;
                }
            }
            foreach (var scr in data.Scripts)
            {
                if (scr is null)
                    continue;

                ScriptsDict[scr.Name.Content] = scr;
            }
            foreach (var func in data.Functions)
            {
                if (func is null)
                    continue;

                FunctionsDict[func.Name.Content] = func;
            }
            foreach (var code in data.Code)
            {
                if (code is null)
                    continue;

                CodeDict[code.Name.Content] = code;
            }
        }

        private void DisassembleCode(UndertaleCode code, bool first)
        {
            string text;

            int currLine = 1;
            int currColumn = 1;
            double scrollPos = 0;
            if (!first)
            {
                var caret = DisassemblyEditor.TextArea.Caret;
                currLine = caret.Line;
                currColumn = caret.Column;
                scrollPos = DisassemblyEditor.VerticalOffset;
            }
            else if (OverriddenDisasmPos != default)
            {
                currLine = OverriddenDisasmPos.Line;
                currColumn = OverriddenDisasmPos.Column;
                scrollPos = OverriddenDisasmPos.ScrollPos;

                OverriddenDisasmPos = default;
            }

            DisassemblyEditor.TextArea.ClearSelection();
            if (code.ParentEntry != null)
            {
                DisassemblyEditor.IsReadOnly = true;
                text = "; This code entry is a reference to an anonymous function within " + code.ParentEntry.Name.Content + ", view it there.";
                DisassemblyChanged = false;
            }
            else
            {
                DisassemblyEditor.IsReadOnly = false;

                try
                {
                    var data = mainWindow.Data;
                    text = code.Disassemble(data.Variables, data.CodeLocals?.For(code));

                    CurrentLocals.Clear();
                }
                catch (Exception ex)
                {
                    DisassemblyEditor.IsReadOnly = true;

                    string exStr = ex.ToString();
                    exStr = String.Join("\n;", exStr.Split('\n'));
                    text = $";  EXCEPTION!\n;   {exStr}\n";
                }
            }

            DisassemblyEditor.Document.BeginUpdate();
            DisassemblyEditor.Document.Text = text;

            if (!DisassemblyEditor.IsReadOnly)
                RestoreCaretPosition(DisassemblyEditor, currLine, currColumn, scrollPos);

            DisassemblyEditor.Document.EndUpdate();

            if (first)
                DisassemblyEditor.Document.UndoStack.ClearAll();

            CurrentDisassembled = code;
            DisassemblyChanged = false;
        }

        public static Dictionary<string, string> gettext = null;
        private void UpdateGettext(UndertaleData data, UndertaleCode gettextCode)
        {
            gettext = new Dictionary<string, string>();
            string[] decompilationOutput;
            GlobalDecompileContext context = new(data);
            if (!SettingsWindow.ProfileModeEnabled || mainWindow.ProfileHash is not string currentMD5)
            {
                decompilationOutput = new Underanalyzer.Decompiler.DecompileContext(context, gettextCode, data.ToolInfo.DecompilerSettings)
                    .DecompileToString().Split('\n');
            }
            else
            {
                string path = Path.Combine(MainWindow.ProfilesFolder, currentMD5, "Temp", gettextCode.Name.Content + ".gml");
                if (File.Exists(path))
                {
                    try
                    {
                        decompilationOutput = File.ReadAllText(path).Replace("\r\n", "\n").Split('\n');
                    }
                    catch
                    {
                        decompilationOutput = new Underanalyzer.Decompiler.DecompileContext(context, gettextCode, data.ToolInfo.DecompilerSettings)
                            .DecompileToString().Split('\n');
                    }
                }
                else
                {
                    decompilationOutput = new Underanalyzer.Decompiler.DecompileContext(context, gettextCode, data.ToolInfo.DecompilerSettings)
                        .DecompileToString().Split('\n');
                }
            }
            Regex textdataRegex = new("^ds_map_add\\(global\\.text_data_en, \\\"(.*)\\\", \\\"(.*)\\\"\\)", RegexOptions.Compiled);
            Regex textdataRegex2 = new("^ds_map_add\\(global\\.text_data_en, \\\"(.*)\\\", '(.*)'\\)", RegexOptions.Compiled);
            foreach (var line in decompilationOutput)
            {
                Match m = textdataRegex.Match(line);
                if (m.Success)
                {
                    try
                    {
                        if (!data.IsGameMaker2() && m.Groups[2].Value.Contains("'\"'"))
                        {
                            gettext.Add(m.Groups[1].Value, $"\"{m.Groups[2].Value}\"");
                        }
                        else
                        {
                            gettext.Add(m.Groups[1].Value, m.Groups[2].Value);
                        }
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
                else
                {
                    m = textdataRegex2.Match(line);
                    if (m.Success)
                    {
                        try
                        {
                            if (!data.IsGameMaker2() && m.Groups[2].Value.Contains("\"'\""))
                            {
                                gettext.Add(m.Groups[1].Value, $"\'{m.Groups[2].Value}\'");
                            }
                            else
                            {
                                gettext.Add(m.Groups[1].Value, m.Groups[2].Value);
                            }
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
        }

        public static Dictionary<string, string> gettextJSON = null;
        private static readonly Regex gettextRegex = new(@"scr_gettext\(\""(.*?)\""\)(?!(.*?\/\/.*?$))", RegexOptions.Compiled);
        private static readonly Regex getlangRegex = new(@"scr_84_get_lang_string(?:.*?)\(\""(.*?)\""\)(?!(.*?\/\/.*?$))", RegexOptions.Compiled);
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

            int currLine = 1;
            int currColumn = 1;
            double scrollPos = 0;
            if (!first)
            {
                var caret = DecompiledEditor.TextArea.Caret;
                currLine = caret.Line;
                currColumn = caret.Column;
                scrollPos = DecompiledEditor.VerticalOffset;
            }
            else if (OverriddenDecompPos != default)
            {
                currLine = OverriddenDecompPos.Line;
                currColumn = OverriddenDecompPos.Column;
                scrollPos = OverriddenDecompPos.ScrollPos;

                OverriddenDecompPos = default;
            }

            DecompiledEditor.TextArea.ClearSelection();

            if (code.ParentEntry != null)
            {
                DecompiledEditor.Text = "// This code entry is a reference to an anonymous function within " + code.ParentEntry.Name.Content + ", view it there.";
                DecompiledChanged = false;
                CurrentDecompiled = code;
                existingDialog?.TryClose();
            }
            else
            {
                LoaderDialog dialog;
                if (existingDialog != null)
                {
                    dialog = existingDialog;
                    dialog.Message = "Decompiling, please wait... This can take a while on complex scripts.";
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
                    GlobalDecompileContext context = new(dataa);
                    string decompiled = null;
                    Exception e = null;
                    try
                    {
                        if (!SettingsWindow.ProfileModeEnabled || mainWindow.ProfileHash is not string currentMD5)
                        {
                            decompiled = new Underanalyzer.Decompiler.DecompileContext(context, code, dataa.ToolInfo.DecompilerSettings)
                                .DecompileToString();
                        }
                        else
                        {
                            string path = Path.Combine(MainWindow.ProfilesFolder, currentMD5, "Temp", code.Name.Content + ".gml");
                            if (!File.Exists(path))
                            {
                                decompiled = new Underanalyzer.Decompiler.DecompileContext(context, code, dataa.ToolInfo.DecompilerSettings)
                                    .DecompileToString();
                            }
                            else
                            {
                                decompiled = File.ReadAllText(path);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        e = ex;
                    }

                    if (gettextCode != null)
                        UpdateGettext(dataa, gettextCode);

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
                        mainWindow.ShowError(exc.ToString());
                    }

                    // Add `// string` at the end of lines with `scr_gettext()` or `scr_84_get_lang_string()`
                    if (decompiled is not null)
                    {
                        StringReader decompLinesReader;
                        StringBuilder decompLinesBuilder;
                        Dictionary<string, string> currDict = null;
                        Regex currRegex = null;
                        if (gettext is not null && decompiled.Contains("scr_gettext"))
                        {
                            currDict = gettext;
                            currRegex = gettextRegex;
                        }
                        else if (gettextJSON is not null && decompiled.Contains("scr_84_get_lang_string"))
                        {
                            currDict = gettextJSON;
                            currRegex = getlangRegex;
                        }

                        if (currDict is not null && currRegex is not null)
                        {
                            decompLinesReader = new(decompiled);
                            decompLinesBuilder = new();
                            string line;
                            while ((line = decompLinesReader.ReadLine()) is not null)
                            {
                                // Not `currRegex.Match()`, because one line could contain several calls
                                // if the "Profile mode" is enabled.
                                var matches = currRegex.Matches(line).Where(m => m.Success).ToArray();
                                if (matches.Length > 0)
                                {
                                    decompLinesBuilder.Append($"{line} // ");

                                    for (int i = 0; i < matches.Length; i++)
                                    {
                                        Match match = matches[i];
                                        if (!currDict.TryGetValue(match.Groups[1].Value, out string text))
                                            text = "<localization fetch error>";

                                        if (i != matches.Length - 1) // If not the last
                                            decompLinesBuilder.Append($"{text}; ");
                                        else
                                            decompLinesBuilder.Append(text + '\n');
                                    }
                                }
                                else
                                {
                                    decompLinesBuilder.Append(line + '\n');
                                }
                            }

                            decompiled = decompLinesBuilder.ToString();
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
                            PopulateCurrentLocals(dataa, code);

                            RestoreCaretPosition(DecompiledEditor, currLine, currColumn, scrollPos);

                            if (existingDialog is not null)                      //if code was edited (and compiles after it)
                            {
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

        private void PopulateCurrentLocals(UndertaleData data, UndertaleCode code)
        {
            CurrentLocals.Clear();

            // Look up locals for given code entry's name, for syntax highlighting
            var locals = data.CodeLocals?.ByName(code.Name.Content);
            if (locals != null)
            {
                foreach (var local in locals.Locals)
                    CurrentLocals.Add(local.Name.Content);
            }
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

            CompileResult compileResult = new();
            string rootException = null;
            string text = DecompiledEditor.Text;
            var dispatcher = Dispatcher;
            Task t = Task.Run(() =>
            {
                try
                {
                    CompileGroup group = new(data);
                    group.MainThreadAction = (f) => { dispatcher.Invoke(() => f()); };
                    group.QueueCodeReplace(code, text);
                    compileResult = group.Compile();
                }
                catch (Exception ex)
                {
                    rootException = ex.ToString();
                }
            });
            await t;

            if (rootException is not null)
            {
                dialog.TryClose();
                mainWindow.ShowError(Truncate(rootException, 512), "Compiler error");
                return;
            }

            if (!compileResult.Successful)
            {
                dialog.TryClose();
                mainWindow.ShowError(Truncate(compileResult.PrintAllErrors(false), 512), "Compiler error");
                return;
            }

            if (SettingsWindow.ProfileModeEnabled && mainWindow.ProfileHash is string currentMD5)
            {
                try
                {
                    // Write text, only if in the profile mode.
                    string path = Path.Combine(MainWindow.ProfilesFolder, currentMD5, "Temp", code.Name.Content + ".gml");
                    File.WriteAllText(path, DecompiledEditor.Text);
                }
                catch (Exception exc)
                {
                    mainWindow.ShowError("Error during writing of GML code to profile:\n" + exc);
                }
            }

            // Invalidate gettext if necessary
            if (code.Name.Content == "gml_Script_textdata_en")
                gettext = null;

            // Show new code, decompiled.
            CurrentDisassembled = null;
            CurrentDecompiled = null;

            // Tab switch
            if (e == null)
            {
                dialog.TryClose();
                return;
            }

            // Decompile new code
            await DecompileCode(code, false, dialog);
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
            }
            catch (Exception ex)
            {
                mainWindow.ShowError(ex.ToString(), "Assembler error");
                return;
            }

            // Get rid of old code
            CurrentDisassembled = null;
            CurrentDecompiled = null;

            // Tab switch
            if (e == null)
                return;

            // Disassemble new code
            DisassembleCode(code, false);

            if (!DisassemblyEditor.IsReadOnly)
            {
                if (mainWindow.IsSaving)
                {
                    mainWindow.IsSaving = false;

                    _ = mainWindow.DoSaveDialog();
                }
            }
        }

        public class NumberGenerator : VisualLineElementGenerator
        {
            private readonly IHighlighter highlighterInst;
            private readonly UndertaleCodeEditor codeEditorInst;

            // <offset, length>
            private readonly Dictionary<int, int> lineNumberSections = new();

            public NumberGenerator(UndertaleCodeEditor codeEditorInst, TextArea textAreaInst)
            {
                this.codeEditorInst = codeEditorInst;

                highlighterInst = textAreaInst.GetService(typeof(IHighlighter)) as IHighlighter;
            }

            public override void StartGeneration(ITextRunConstructionContext context)
            {
                lineNumberSections.Clear();

                var docLine = context.VisualLine.FirstDocumentLine;
                if (docLine.Length != 0)
                {
                    int line = docLine.LineNumber;
                    var highlighter = highlighterInst;
                    
                    HighlightedLine highlighted;
                    try
                    {
                        highlighted = highlighter.HighlightLine(line);
                    }
                    catch
                    {
                        Debug.WriteLine($"(NumberGenerator) Code editor line {line} highlight error.");
                        base.StartGeneration(context);
                        return;
                    }

                    foreach (var section in highlighted.Sections)
                    {
                        if (section.Color.Name == "Number")
                            lineNumberSections[section.Offset] = section.Length;
                    }
                }

                base.StartGeneration(context);
            }

            /// Gets the first offset >= startOffset where the generator wants to construct
            /// an element.
            /// Return -1 to signal no interest.
            public override int GetFirstInterestedOffset(int startOffset)
            {
                foreach (var section in lineNumberSections)
                {
                    if (startOffset <= section.Key)
                        return section.Key;
                }

                return -1;
            }

            /// Constructs an element at the specified offset.
            /// May return null if no element should be constructed.
            public override VisualLineElement ConstructElement(int offset)
            {
                int numLength = -1;
                if (!lineNumberSections.TryGetValue(offset, out numLength))
                    return null;

                var doc = CurrentContext.Document;
                string numText = doc.GetText(offset, numLength); 

                var line = new ClickVisualLineText(numText, CurrentContext.VisualLine, numLength);
                
                line.Clicked += (text, inNewTab) =>
                {
                    if (int.TryParse(text, out int id))
                    {
                        codeEditorInst.DecompiledFocused = true;
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
                            if (id < (data.AnimationCurves?.Count ?? 0))
                                possibleObjects.Add(data.AnimationCurves[id]);
                            if (id < (data.Sequences?.Count ?? 0))
                                possibleObjects.Add(data.Sequences[id]);
                            if (id < (data.ParticleSystems?.Count ?? 0))
                                possibleObjects.Add(data.ParticleSystems[id]);
                        }

                        ContextMenu contextMenu = new();
                        foreach (UndertaleObject obj in possibleObjects)
                        {
                            MenuItem item = new();
                            item.Header = obj.ToString().Replace("_", "__");
                            item.PreviewMouseDown += (sender2, ev2) =>
                            {
                                if (ev2.ChangedButton != Input.MouseButton.Left
                                    && ev2.ChangedButton != Input.MouseButton.Middle)
                                    return;

                                if (ev2.ChangedButton == Input.MouseButton.Middle)
                                {
                                    mainWindow.Focus();
                                    mainWindow.ChangeSelection(obj, true);
                                    
                                }
                                else if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                                    mainWindow.ChangeSelection(obj);
                                else
                                {
                                    doc.Replace(line.ParentVisualLine.StartOffset + line.RelativeTextOffset,
                                                text.Length, (obj as UndertaleNamedResource).Name.Content, null);
                                    codeEditorInst.DecompiledChanged = true;
                                }
                            };
                            contextMenu.Items.Add(item);
                        }
                        if (id > 0x00050000)
                        {
                            MenuItem item = new();
                            item.Header = "0x" + id.ToString("X6") + " (color)";
                            item.Click += (sender2, ev2) =>
                            {
                                if (!((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift))
                                {
                                    doc.Replace(line.ParentVisualLine.StartOffset + line.RelativeTextOffset,
                                                text.Length, "0x" + id.ToString("X6"), null);
                                    codeEditorInst.DecompiledChanged = true;
                                }
                            };
                            contextMenu.Items.Add(item);
                        }
                        BuiltinList list = mainWindow.Data.BuiltinList;
                        var myKey = list.Constants.FirstOrDefault(x => x.Value == (double)id).Key;
                        if (myKey != null)
                        {
                            MenuItem item = new();
                            item.Header = myKey.Replace("_", "__") + " (constant)";
                            item.Click += (sender2, ev2) =>
                            {
                                if (!((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift))
                                {
                                    doc.Replace(line.ParentVisualLine.StartOffset + line.RelativeTextOffset,
                                                text.Length, myKey, null);
                                    codeEditorInst.DecompiledChanged = true;
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
        }

        public class NameGenerator : VisualLineElementGenerator
        {
            private readonly IHighlighter highlighterInst;
            private readonly TextEditor textEditorInst;
            private readonly UndertaleCodeEditor codeEditorInst;

            private static readonly SolidColorBrush FunctionBrush = new(Color.FromRgb(0xFF, 0xB8, 0x71));
            private static readonly SolidColorBrush GlobalBrush = new(Color.FromRgb(0xF9, 0x7B, 0xF9));
            private static readonly SolidColorBrush ConstantBrush = new(Color.FromRgb(0xFF, 0x80, 0x80));
            private static readonly SolidColorBrush InstanceBrush = new(Color.FromRgb(0x58, 0xE3, 0x5A));
            private static readonly SolidColorBrush LocalBrush = new(Color.FromRgb(0xFF, 0xF8, 0x99));

            private static ContextMenu contextMenu;

            // <offset, length>
            private readonly Dictionary<int, int> lineNameSections = new();

            public NameGenerator(UndertaleCodeEditor codeEditorInst, TextArea textAreaInst)
            {
                this.codeEditorInst = codeEditorInst;

                highlighterInst = textAreaInst.GetService(typeof(IHighlighter)) as IHighlighter;
                textEditorInst = textAreaInst.GetService(typeof(TextEditor)) as TextEditor;

                var menuItem = new MenuItem()
                {
                    Header = "Open in new tab"
                };
                menuItem.Click += (sender, _) =>
                {
                    mainWindow.ChangeSelection((sender as FrameworkElement).DataContext, true);
                };
                contextMenu = new()
                {
                    Items = { menuItem },
                    Placement = PlacementMode.MousePoint
                };
            }

            public override void StartGeneration(ITextRunConstructionContext context)
            {
                lineNameSections.Clear();

                var docLine = context.VisualLine.FirstDocumentLine;
                if (docLine.Length != 0)
                {
                    int line = docLine.LineNumber;
                    var highlighter = highlighterInst;

                    HighlightedLine highlighted;
                    try
                    {
                        highlighted = highlighter.HighlightLine(line);
                    }
                    catch
                    {
                        Debug.WriteLine($"(NameGenerator) Code editor line {line} highlight error.");
                        base.StartGeneration(context);
                        return;
                    }

                    foreach (var section in highlighted.Sections)
                    {
                        if (section.Color.Name == "Identifier" || section.Color.Name == "Function")
                            lineNameSections[section.Offset] = section.Length;
                    }
                }

                base.StartGeneration(context);
            }

            /// Gets the first offset >= startOffset where the generator wants to construct
            /// an element.
            /// Return -1 to signal no interest.
            public override int GetFirstInterestedOffset(int startOffset)
            {
                foreach (var section in lineNameSections)
                {
                    if (startOffset <= section.Key)
                        return section.Key;
                }

                return -1;
            }

            /// Constructs an element at the specified offset.
            /// May return null if no element should be constructed.
            public override VisualLineElement ConstructElement(int offset)
            {
                int nameLength = -1;
                if (!lineNameSections.TryGetValue(offset, out nameLength))
                    return null;

                var doc = CurrentContext.Document;
                string nameText = doc.GetText(offset, nameLength);

                UndertaleData data = mainWindow.Data;
                bool func = (offset + nameLength + 1 < CurrentContext.VisualLine.LastDocumentLine.EndOffset) &&
                            (doc.GetCharAt(offset + nameLength) == '(');
                UndertaleNamedResource val = null;
                bool nonResourceReference = false;

                var editor = textEditorInst;

                // Process the content of this identifier/function
                if (func)
                {
                    val = null;
                    if (!data.IsVersionAtLeast(2, 3)) // in GMS2.3 every custom "function" is in fact a member variable and scripts are never referenced directly
                        ScriptsDict.TryGetValue(nameText, out val);
                    if (val == null)
                    {
                        FunctionsDict.TryGetValue(nameText, out val);
                        if (data.IsVersionAtLeast(2, 3))
                        {
                            if (val != null)
                            {
                                if (CodeDict.TryGetValue(val.Name.Content, out _))
                                    val = null; // in GMS2.3 every custom "function" is in fact a member variable, and the names in functions make no sense (they have the gml_Script_ prefix)
                            }
                            else
                            {
                                // Resolve 2.3 sub-functions for their parent entry
                                if (data.GlobalFunctions?.TryGetFunction(nameText, out Underanalyzer.IGMFunction f) == true)
                                {
                                    ScriptsDict.TryGetValue(f.Name.Content, out val);
                                    val = (val as UndertaleScript)?.Code?.ParentEntry;
                                }
                            }
                        }
                    }
                    if (val == null)
                    {
                        if (data.BuiltinList.Functions.ContainsKey(nameText))
                        {
                            var res = new ColorVisualLineText(nameText, CurrentContext.VisualLine, nameLength,
                                                              FunctionBrush);
                            res.Bold = true;
                            return res;
                        }
                    }
                }
                else
                {
                    NamedObjDict.TryGetValue(nameText, out val);
                    if (data.IsVersionAtLeast(2, 3))
                    { 
                        if (val is UndertaleScript)
                            val = null; // in GMS2.3 scripts are never referenced directly

                        if (data.GlobalFunctions?.TryGetFunction(nameText, out Underanalyzer.IGMFunction globalFunc) == true &&
                            globalFunc is UndertaleFunction utGlobalFunc)
                        {
                            // Try getting script that this function reference belongs to
                            if (NamedObjDict.TryGetValue("gml_Script_" + nameText, out val) && val is UndertaleScript script)
                            {
                                // Highlight like a function as well
                                val = script.Code;
                                func = true;
                            }
                        }

                        if (val == null)
                        {
                            // Try to get basic function
                            if (FunctionsDict.TryGetValue(nameText, out val))
                            {
                                func = true;
                            }
                        }

                        if (val == null)
                        {
                            // Try resolving to room instance ID
                            string instanceIdPrefix = data.ToolInfo.InstanceIdPrefix();
                            if (nameText.StartsWith(instanceIdPrefix) &&
                                int.TryParse(nameText[instanceIdPrefix.Length..], out int id) && id >= 100000)
                            {
                                // TODO: We currently mark this as a non-resource reference, but ideally
                                // we resolve this to the room that this instance ID occurs in.
                                // However, we should only do this when actually clicking on it.
                                nonResourceReference = true;
                            }
                        }
                    }
                }
                if (val == null && !nonResourceReference)
                {
                    // Check for variable name colors
                    if (offset >= 7)
                    {
                        if (doc.GetText(offset - 7, 7) == "global.")
                        {
                            return new ColorVisualLineText(nameText, CurrentContext.VisualLine, nameLength,
                                                           GlobalBrush);
                        }
                    }
                    if (data.BuiltinList.Constants.ContainsKey(nameText))
                        return new ColorVisualLineText(nameText, CurrentContext.VisualLine, nameLength,
                                                       ConstantBrush);
                    if (data.BuiltinList.GlobalNotArray.ContainsKey(nameText) ||
                        data.BuiltinList.Instance.ContainsKey(nameText) ||
                        data.BuiltinList.GlobalArray.ContainsKey(nameText))
                        return new ColorVisualLineText(nameText, CurrentContext.VisualLine, nameLength,
                                                       InstanceBrush);
                    if (codeEditorInst?.CurrentLocals?.Contains(nameText) == true)
                        return new ColorVisualLineText(nameText, CurrentContext.VisualLine, nameLength,
                                                       LocalBrush);
                    return null;
                }

                var line = new ClickVisualLineText(nameText, CurrentContext.VisualLine, nameLength,
                                                   func ? FunctionBrush : ConstantBrush);
                if (func)
                {
                    // Make function references bold as well as a different color
                    line.Bold = true;
                }
                if (val is not null)
                {
                    // Add click operation when we have a resource
                    line.Clicked += async (text, button) =>
                    {
                        await codeEditorInst?.SaveChanges();

                        if (button == Input.MouseButton.Right)
                        {
                            contextMenu.DataContext = val;
                            contextMenu.IsOpen = true;
                        }
                        else
                            mainWindow.ChangeSelection(val, button == Input.MouseButton.Middle);
                    };
                }

                return line;
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

            public delegate void ClickHandler(string text, Input.MouseButton button);

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
                if ((e.ChangedButton == Input.MouseButton.Left && LinkIsClickable())
                    || e.ChangedButton == Input.MouseButton.Middle || e.ChangedButton == Input.MouseButton.Right)
                {
                    if (Clicked != null)
                    {
                        Clicked(Text, e.ChangedButton);
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
