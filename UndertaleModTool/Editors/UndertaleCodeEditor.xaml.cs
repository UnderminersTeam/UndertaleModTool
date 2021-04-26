using GraphVizWrapper;
using GraphVizWrapper.Commands;
using GraphVizWrapper.Queries;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleCodeEditor.xaml
    /// </summary>
    public partial class UndertaleCodeEditor : UserControl
    {
        public UndertaleCode CurrentDisassembled = null;
        public UndertaleCode CurrentDecompiled = null;
        public UndertaleCode CurrentGraphed = null;
        public string UMTAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "UndertaleModTool" + System.IO.Path.DirectorySeparatorChar;
        public string ProfilesFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "UndertaleModTool" + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar;
        public string ProfileHash = (Application.Current.MainWindow as MainWindow).ProfileHash;
        public string MainPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "UndertaleModTool" + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + (Application.Current.MainWindow as MainWindow).ProfileHash + System.IO.Path.DirectorySeparatorChar + "Main" + System.IO.Path.DirectorySeparatorChar;
        public string TempPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "UndertaleModTool" + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + (Application.Current.MainWindow as MainWindow).ProfileHash + System.IO.Path.DirectorySeparatorChar + "Temp" + System.IO.Path.DirectorySeparatorChar;

        public UndertaleCodeEditor()
        {
            InitializeComponent();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UndertaleCode code = this.DataContext as UndertaleCode;
            Directory.CreateDirectory(MainPath);
            Directory.CreateDirectory(TempPath);
            if (code == null)
                return;
            if (DisassemblyTab.IsSelected && code != CurrentDisassembled)
            {
                DisassembleCode(code);
            }
            if (DecompiledTab.IsSelected && code != CurrentDecompiled)
            {
                DecompileCode(code);
            }
            if (GraphTab.IsSelected && code != CurrentGraphed)
            {
                GraphCode(code);
            }
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UndertaleCode code = this.DataContext as UndertaleCode;
            if (code == null)
                return;
            if (DisassemblyTab.IsSelected && code != CurrentDisassembled)
            {
                DisassembleCode(code);
            }
            if (DecompiledTab.IsSelected && code != CurrentDecompiled)
            {
                DecompileCode(code);
            }
            if (GraphTab.IsSelected && code != CurrentGraphed)
            {
                GraphCode(code);
            }
        }

        private void DisassembleCode(UndertaleCode code)
        {
            code.UpdateAddresses();

            FlowDocument document = new FlowDocument();
            document.PagePadding = new Thickness(0);
            document.PageWidth = 2048; // Speed-up.
            document.FontFamily = new FontFamily("Lucida Console");
            Paragraph par = new Paragraph();
            par.Margin = new Thickness(0);

            if (code.DuplicateEntry)
            {
                par.Inlines.Add(new Run("Duplicate code entry; cannot edit here."));
            }
            //Maybe if I'm feeling wild, I'll try again one day, but for now, have this commented out code
            //And yes, it really DOES take that long to load
            //else if ((SettingsWindow.FormattingOnLongCodeEnabled == "False") ? (code.Instructions.Count > 5000) : false)
            else if (code.Instructions.Count > 5000)
            {
                // Disable syntax highlighting. Loading it can take a few MINUTES on large scripts.
                var data = (Application.Current.MainWindow as MainWindow).Data;
                string[] split = code.Disassemble(data.Variables, data.CodeLocals.For(code)).Split('\n');

                for (var i = 0; i < split.Length; i++)
                { // Makes it possible to select text.
                    if (i > 0 && (i % 100) == 0)
                    {
                        document.Blocks.Add(par);
                        par = new Paragraph();
                        par.Margin = new Thickness(0);
                    }

                    par.Inlines.Add(split[i] + (split.Length > i + 1 && ((i + 1) % 100) != 0 ? "\n" : ""));
                }

            }

            else
            {
                Brush addressBrush = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                Brush opcodeBrush = new SolidColorBrush(Color.FromRgb(0, 100, 0));
                Brush argBrush = new SolidColorBrush(Color.FromRgb(0, 0, 150));
                Brush typeBrush = new SolidColorBrush(Color.FromRgb(0, 0, 50));
                var data = (Application.Current.MainWindow as MainWindow).Data;
                par.Inlines.Add(new Run(code.GenerateLocalVarDefinitions(data.Variables, data.CodeLocals.For(code))) { Foreground = addressBrush });
                foreach (var instr in code.Instructions)
                {
                    par.Inlines.Add(new Run(instr.Address.ToString("D5") + ": ") { Foreground = addressBrush });
                    string kind = instr.Kind.ToString();
                    var type = UndertaleInstruction.GetInstructionType(instr.Kind);
                    if (type == UndertaleInstruction.InstructionType.BreakInstruction)
                        kind = Assembler.BreakIDToName[(short)instr.Value];
                    else
                        kind = kind.ToLower();
                    par.Inlines.Add(new Run(kind) { Foreground = opcodeBrush, FontWeight = FontWeights.Bold });

                    switch (type)
                    {
                        case UndertaleInstruction.InstructionType.SingleTypeInstruction:
                            par.Inlines.Add(new Run("." + instr.Type1.ToOpcodeParam()) { Foreground = typeBrush });

                            if (instr.Kind == UndertaleInstruction.Opcode.Dup || instr.Kind == UndertaleInstruction.Opcode.CallV)
                            {
                                par.Inlines.Add(new Run(" "));
                                par.Inlines.Add(new Run(instr.Extra.ToString()) { Foreground = argBrush });
                                if (instr.Kind == UndertaleInstruction.Opcode.Dup)
                                {
                                    if ((byte)instr.ComparisonKind == 0x88)
                                    {
                                        // No idea what this is right now (seems to be used at least with @@GetInstance@@), this is the "temporary" solution
                                        par.Inlines.Add(new Run(" spec"));
                                    }
                                }
                            }
                            break;

                        case UndertaleInstruction.InstructionType.DoubleTypeInstruction:
                            par.Inlines.Add(new Run("." + instr.Type1.ToOpcodeParam()) { Foreground = typeBrush });
                            par.Inlines.Add(new Run("." + instr.Type2.ToOpcodeParam()) { Foreground = typeBrush });
                            break;

                        case UndertaleInstruction.InstructionType.ComparisonInstruction:
                            par.Inlines.Add(new Run("." + instr.Type1.ToOpcodeParam()) { Foreground = typeBrush });
                            par.Inlines.Add(new Run("." + instr.Type2.ToOpcodeParam()) { Foreground = typeBrush });
                            par.Inlines.Add(new Run(" "));
                            par.Inlines.Add(new Run(instr.ComparisonKind.ToString()) { Foreground = opcodeBrush });
                            break;

                        case UndertaleInstruction.InstructionType.GotoInstruction:
                            par.Inlines.Add(new Run(" "));
                            string tgt = (instr.Address + instr.JumpOffset).ToString("D5");
                            if (instr.Address + instr.JumpOffset == code.Length / 4)
                                tgt = "func_end";
                            if (instr.JumpOffsetPopenvExitMagic)
                                tgt = "[drop]";
                            par.Inlines.Add(new Run(tgt) { Foreground = argBrush, ToolTip = "$" + instr.JumpOffset.ToString("+#;-#;0") });
                            break;

                        case UndertaleInstruction.InstructionType.PopInstruction:
                            par.Inlines.Add(new Run("." + instr.Type1.ToOpcodeParam()) { Foreground = typeBrush });
                            par.Inlines.Add(new Run("." + instr.Type2.ToOpcodeParam()) { Foreground = typeBrush });
                            par.Inlines.Add(new Run(" "));
                            if (instr.Type1 == UndertaleInstruction.DataType.Int16)
                            {
                                // Special scenario - the swap instruction
                                // TODO: Figure out the proper syntax, see #129
                                Run runType = new Run(instr.SwapExtra.ToString().ToLower()) { Foreground = argBrush };
                                par.Inlines.Add(runType);
                            }
                            else
                            {
                                if (instr.Type1 == UndertaleInstruction.DataType.Variable && instr.TypeInst != UndertaleInstruction.InstanceType.Undefined)
                                {
                                    par.Inlines.Add(new Run(instr.TypeInst.ToString().ToLower()) { Foreground = typeBrush });
                                    par.Inlines.Add(new Run("."));
                                }
                                Run runDest = new Run(instr.Destination.ToString()) { Foreground = argBrush, Cursor = Cursors.Hand };
                                runDest.MouseDown += (sender, e) =>
                                {
                                    (Application.Current.MainWindow as MainWindow).ChangeSelection(instr.Destination.Target);
                                };
                                par.Inlines.Add(runDest);
                            }
                            break;

                        case UndertaleInstruction.InstructionType.PushInstruction:
                            par.Inlines.Add(new Run("." + instr.Type1.ToOpcodeParam()) { Foreground = typeBrush });
                            par.Inlines.Add(new Run(" "));
                            if (instr.Type1 == UndertaleInstruction.DataType.Variable && instr.TypeInst != UndertaleInstruction.InstanceType.Undefined)
                            {
                                par.Inlines.Add(new Run(instr.TypeInst.ToString().ToLower()) { Foreground = typeBrush });
                                par.Inlines.Add(new Run("."));
                            }
                            Run valueRun = new Run((instr.Value as IFormattable)?.ToString(null, CultureInfo.InvariantCulture) ?? instr.Value.ToString()) { Foreground = argBrush, Cursor = (instr.Value is UndertaleObject || instr.Value is UndertaleResourceRef) ? Cursors.Hand : Cursors.Arrow };
                            if (instr.Value is UndertaleResourceRef)
                            {
                                valueRun.MouseDown += (sender, e) =>
                                {
                                    (Application.Current.MainWindow as MainWindow).ChangeSelection((instr.Value as UndertaleResourceRef).Resource);
                                };
                            }
                            else if (instr.Value is UndertaleObject)
                            {
                                valueRun.MouseDown += (sender, e) =>
                                {
                                    (Application.Current.MainWindow as MainWindow).ChangeSelection(instr.Value);
                                };
                            }
                            par.Inlines.Add(valueRun);
                            break;

                        case UndertaleInstruction.InstructionType.CallInstruction:
                            par.Inlines.Add(new Run("." + instr.Type1.ToOpcodeParam()) { Foreground = typeBrush });
                            par.Inlines.Add(new Run(" "));
                            par.Inlines.Add(new Run(instr.Function.ToString()) { Foreground = argBrush });
                            par.Inlines.Add(new Run("(argc="));
                            par.Inlines.Add(new Run(instr.ArgumentsCount.ToString()) { Foreground = argBrush });
                            par.Inlines.Add(new Run(")"));
                            break;

                        case UndertaleInstruction.InstructionType.BreakInstruction:
                            par.Inlines.Add(new Run("." + instr.Type1.ToOpcodeParam()) { Foreground = typeBrush });
                            //par.Inlines.Add(new Run(" "));
                            //par.Inlines.Add(new Run(instr.Value.ToString()) { Foreground = argBrush });
                            break;
                    }

                    if (par.Inlines.Count >= 250)
                    { // Makes selecting text possible.
                        document.Blocks.Add(par);
                        par = new Paragraph();
                        par.Margin = new Thickness(0);
                    }
                    else
                    {
                        par.Inlines.Add(new Run("\n"));
                    }
                }
            }
            document.Blocks.Add(par);

            DisassemblyView.Document = document;

            CurrentDisassembled = code;
        }

        private static Dictionary<string, int> gettext = null;
        private void UpdateGettext(UndertaleCode gettextCode)
        {
            gettext = new Dictionary<string, int>();
            string[] DecompilationOutput;
            if (SettingsWindow.DecompileOnceCompileManyEnabled == "False")
                DecompilationOutput = Decompiler.Decompile(gettextCode, new DecompileContext(null, true)).Replace("\r\n", "\n").Split('\n');
            else
            {
                if (File.Exists(TempPath + gettextCode.Name.Content + ".gml"))
                    DecompilationOutput = File.ReadAllText(TempPath + gettextCode.Name.Content + ".gml").Replace("\r\n", "\n").Split('\n');
                else
                    DecompilationOutput = Decompiler.Decompile(gettextCode, new DecompileContext(null, true)).Replace("\r\n", "\n").Split('\n');
            }
            foreach (var line in DecompilationOutput)
            {
                Match m = Regex.Match(line, "^ds_map_add\\(global.text_data_en, \"(.*)\"@([0-9]+), \"(.*)\"@([0-9]+)\\)");
                if (m.Success)
                {
                    try
                    {
                        gettext.Add(m.Groups[1].Value, Int32.Parse(m.Groups[4].Value));
                    }
                    catch (ArgumentException)
                    {
                        MessageBox.Show("There is a duplicate key in textdata_en. This may cause errors in the comment display of text.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch
                    {
                        MessageBox.Show("Unknown error in textdata_en. This may cause errors in the comment display of text.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private static Dictionary<string, string> gettextJSON = null;
        private string UpdateGettextJSON(string json)
        {
            try
            {
                gettextJSON = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            } catch (Exception e)
            {
                gettextJSON = new Dictionary<string, string>();
                return "Failed to parse language file: " + e.Message;
            }
            return null;
        }
        private async void DecompileCode(UndertaleCode code)
        {
            FlowDocument document = new FlowDocument();
            document.PagePadding = new Thickness(0);
            document.PageWidth = 2048; // Speed-up.
            document.FontFamily = new FontFamily("Lucida Console");
            Paragraph par = new Paragraph();
            par.Margin = new Thickness(0);

            if (code.DuplicateEntry)
            {
                par.Inlines.Add(new Run("Duplicate code entry; cannot edit here."));
                document.Blocks.Add(par);
                DecompiledView.Document = document;
                CurrentDecompiled = code;
            }
            else
            {
                LoaderDialog dialog = new LoaderDialog("Decompiling", "Decompiling, please wait... This can take a while on complex scripts");
                dialog.Owner = Window.GetWindow(this);

                UndertaleCode gettextCode = null;
                if (gettext == null)
                    gettextCode = (Application.Current.MainWindow as MainWindow).Data.Code.ByName("gml_Script_textdata_en");

                string dataPath = System.IO.Path.GetDirectoryName((Application.Current.MainWindow as MainWindow).FilePath);
                string gettextJsonPath = (dataPath != null) ? System.IO.Path.Combine(dataPath, "lang/lang_en.json") : null;

                var dataa = (Application.Current.MainWindow as MainWindow).Data;
                Task t = Task.Run(() =>
                {
                    int estimatedLineCount = (int)Math.Round(code.Length * .056D);
                    bool skipFormatting = ((SettingsWindow.FormattingOnLongCodeEnabled == "False") ? (estimatedLineCount > 5000) : false);

                    DecompileContext context = new DecompileContext(dataa, !skipFormatting);
                    string decompiled = null;
                    Exception e = null;
                    try
                    {
                        decompiled = ((SettingsWindow.DecompileOnceCompileManyEnabled == "False" || !File.Exists(TempPath + code.Name.Content + ".gml")) ? Decompiler.Decompile(code, context).Replace("\r\n", "\n") : File.ReadAllText(TempPath + code.Name.Content + ".gml").Replace("\r\n", "\n"));
                    }
                    catch (Exception ex)
                    {
                        e = ex;
                    }

                    if (gettextCode != null)
                        UpdateGettext(gettextCode);

                    if (gettextJSON == null && gettextJsonPath != null && File.Exists(gettextJsonPath))
                    {
                        string err = UpdateGettextJSON(File.ReadAllText(gettextJsonPath));
                        if (err != null)
                            e = new Exception(err);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        if (e != null)
                        {
                            Brush exceptionBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                            par.Inlines.Add(new Run("EXCEPTION!\n") { Foreground = exceptionBrush, FontWeight = FontWeights.Bold });
                            par.Inlines.Add(new Run(e.ToString()) { Foreground = exceptionBrush });
                        }
                        else if (decompiled != null)
                        {
                            string[] lines = decompiled.Split('\n');
                            if (skipFormatting)
                            {
                                for (var i = 0; i < lines.Length; i++)
                                {
                                    string toWrite = lines[i];
                                    if (((i + 1) % 100) != 0 && lines.Length > i + 1)
                                        toWrite += "\n"; // Write a new-line if we're not making a new paragraph.

                                if (i > 0 && i % 100 == 0)
                                    { // Splitting into different paragraphs significantly increases selection performance.
                                    document.Blocks.Add(par);
                                        par = new Paragraph();
                                        par.Margin = new Thickness(0);
                                    }

                                    par.Inlines.Add(toWrite);
                                }
                            }
                            else
                            {
                                Brush keywordBrush = new SolidColorBrush(Color.FromRgb(0, 0, 150));
                                Brush constBrush = new SolidColorBrush(Color.FromRgb(0, 100, 150));
                                Brush stringBrush = new SolidColorBrush(Color.FromRgb(0, 0, 200));
                                Brush commentBrush = new SolidColorBrush(Color.FromRgb(0, 150, 0));
                                Brush funcBrush = new SolidColorBrush(Color.FromRgb(100, 100, 0));
                                Brush assetBrush = new SolidColorBrush(Color.FromRgb(0, 150, 100));
                                Brush argumentBrush = new SolidColorBrush(Color.FromRgb(80, 131, 80));

                                Dictionary<string, UndertaleFunction> funcs = new Dictionary<string, UndertaleFunction>();
                                foreach (var x in (Application.Current.MainWindow as MainWindow).Data.Functions)
                                    funcs.Add(x.Name.Content, x);

                                string storedStrTok = "";

                                foreach (var line in lines)
                                {
                                    char[] special = { '.', ',', ')', '(', '[', ']', '>', '<', ':', ';', '=', '"', '!' };
                                    Func<char, bool> IsSpecial = (c) => Char.IsWhiteSpace(c) || special.Contains(c);
                                    List<string> split = new List<string>();
                                    string tok = storedStrTok;
                                    storedStrTok = "";
                                    bool readingString = (tok != "");
                                    bool escaped = false;
                                    for (int i = 0; i < line.Length; i++)
                                    {
                                        if (tok == "//")
                                        {
                                            tok += line.Substring(i);
                                            break;
                                        }
                                        if (!readingString && tok.Length > 0 && (
                                            (Char.IsWhiteSpace(line[i]) != Char.IsWhiteSpace(tok[tok.Length - 1])) ||
                                            (special.Contains(line[i]) != special.Contains(tok[tok.Length - 1])) ||
                                            (special.Contains(line[i]) && special.Contains(tok[tok.Length - 1])) ||
                                            line[i] == '"'
                                            ))
                                        {
                                            split.Add(tok);
                                            tok = "";
                                        }

                                        if (readingString && context.isGameMaker2)
                                        {
                                            if (escaped)
                                            {
                                                escaped = false;
                                                if (line[i] == '"')
                                                {
                                                    tok += line[i];
                                                    continue;
                                                }
                                            }
                                            else if (line[i] == '\\')
                                            {
                                                escaped = true;
                                            }
                                        }

                                        tok += line[i];
                                        if (line[i] == '"')
                                        {
                                            if (readingString)
                                            {
                                                split.Add(tok);
                                                tok = "";
                                            }
                                            readingString = !readingString;
                                        }
                                    }
                                    if (tok != "")
                                    {
                                        if (readingString)
                                            storedStrTok = tok + "\n";
                                        else
                                            split.Add(tok);
                                    }

                                    Dictionary<string, object> usedObjects = new Dictionary<string, object>();
                                    for (int i = 0; i < split.Count; i++)
                                    {
                                        int? val = null;
                                        string token = split[i];
                                        if (token == "if" || token == "else" || token == "return" || token == "break" || token == "continue" || token == "while" || token == "for" || token == "repeat" || token == "with" || token == "switch" || token == "case" || token == "default" || token == "exit" || token == "var" || token == "do" || token == "until")
                                            par.Inlines.Add(new Run(token) { Foreground = keywordBrush, FontWeight = FontWeights.Bold });
                                        else if (token == "self" || token == "global" || token == "local" || token == "other" || token == "noone" || token == "true" || token == "false" || token == "undefined" || token == "all")
                                            par.Inlines.Add(new Run(token) { Foreground = keywordBrush });
                                        else if (token.StartsWith("argument"))
                                            par.Inlines.Add(new Run(token) { Foreground = argumentBrush });
                                        else if ((val = AssetTypeResolver.FindConstValue(token)) != null)
                                            par.Inlines.Add(new Run(token) { Foreground = constBrush, FontStyle = FontStyles.Italic, ToolTip = val.ToString() });
                                        else if (token.StartsWith("\""))
                                            par.Inlines.Add(new Run(token) { Foreground = stringBrush });
                                        else if (token.StartsWith("//"))
                                            par.Inlines.Add(new Run(token) { Foreground = commentBrush });
                                        else if (token.StartsWith("@") && split[i - 1][0] == '"' && split[i - 1][split[i - 1].Length - 1] == '"')
                                        {
                                            par.Inlines.LastInline.Cursor = Cursors.Hand;
                                            par.Inlines.LastInline.MouseDown += (sender, ev) =>
                                            {
                                                MainWindow mw = Application.Current.MainWindow as MainWindow;
                                                mw.ChangeSelection(mw.Data.Strings[Int32.Parse(token.Substring(1))]);
                                            };
                                        }
                                        else if (dataa.ByName(token) != null)
                                        {
                                            par.Inlines.Add(new Run(token) { Foreground = assetBrush, Cursor = Cursors.Hand });
                                            par.Inlines.LastInline.MouseDown += (sender, ev) => (Application.Current.MainWindow as MainWindow).ChangeSelection(dataa.ByName(token));
                                            if (token == "scr_gettext" && gettext != null)
                                            {
                                                if (split[i + 1] == "(" && split[i + 2].StartsWith("\"") && split[i + 3].StartsWith("@") && split[i + 4] == ")")
                                                {
                                                    string id = split[i + 2].Substring(1, split[i + 2].Length - 2);
                                                    if (!usedObjects.ContainsKey(id) && gettext.ContainsKey(id))
                                                        usedObjects.Add(id, (Application.Current.MainWindow as MainWindow).Data.Strings[gettext[id]]);
                                                }
                                            }
                                            if (token == "scr_84_get_lang_string" && gettextJSON != null)
                                            {
                                                if (split[i + 1] == "(" && split[i + 2].StartsWith("\"") && split[i + 3].StartsWith("@") && split[i + 4] == ")")
                                                {
                                                    string id = split[i + 2].Substring(1, split[i + 2].Length - 2);
                                                    if (!usedObjects.ContainsKey(id) && gettextJSON.ContainsKey(id))
                                                        usedObjects.Add(id, gettextJSON[id]);
                                                }
                                            }
                                        }
                                        else if (funcs.ContainsKey(token))
                                        {
                                            par.Inlines.Add(new Run(token) { Foreground = funcBrush, Cursor = Cursors.Hand });
                                            par.Inlines.LastInline.MouseDown += (sender, ev) => (Application.Current.MainWindow as MainWindow).ChangeSelection(funcs[token]);
                                        }
                                        else if (char.IsDigit(token[0]))
                                        {
                                            par.Inlines.Add(new Run(token) { Cursor = Cursors.Hand });
                                            par.Inlines.LastInline.MouseDown += (sender, ev) =>
                                            {
                                                if (token.Length > 2 && token[0] == '0' && token[1] == 'x')
                                                {
                                                    ev.Handled = true;
                                                    return; // Hex numbers aren't objects.
                                                }

                                                UndertaleData data = (Application.Current.MainWindow as MainWindow).Data;
                                                int id;
                                                if (int.TryParse(token, out id))
                                                {
                                                    List<UndertaleObject> possibleObjects = new List<UndertaleObject>();
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

                                                    ContextMenu contextMenu = new ContextMenu();
                                                    String ContextGetObject = "";
                                                    foreach (UndertaleObject obj in possibleObjects)
                                                    {
                                                        MenuItem item = new MenuItem();
                                                        item.Header = obj.ToString().Replace("_", "__");
                                                        item.Click += (sender2, ev2) => Clipboard.SetText(((UndertaleNamedResource)obj).Name.Content);
                                                        contextMenu.Items.Add(item);
                                                    }
                                                    if (id > 0x00050000)
                                                    {
                                                        contextMenu.Items.Add(new MenuItem() { Header = "#" + id.ToString("X6") + " (color)", IsEnabled = false });
                                                    }
                                                    contextMenu.Items.Add(new MenuItem() { Header = id + " (number)", IsEnabled = false });
                                                    (sender as Run).ContextMenu = contextMenu;
                                                    contextMenu.IsOpen = true;
                                                }
                                                ev.Handled = true;
                                            };
                                        }
                                        else
                                            par.Inlines.Add(token);

                                        if (token == "." && (Char.IsLetter(split[i + 1][0]) || split[i + 1][0] == '_'))
                                        {
                                            int id;
                                            if (Int32.TryParse(split[i - 1], out id))
                                            {
                                                var gos = (Application.Current.MainWindow as MainWindow).Data.GameObjects;
                                                if (!usedObjects.ContainsKey(split[i - 1]) && id >= 0 && id < gos.Count)
                                                    usedObjects.Add(split[i - 1], gos[id]);
                                            }
                                        }
                                    }

                                    // Add used object comments.
                                    foreach (var gt in usedObjects)
                                    {
                                        par.Inlines.Add(new Run(" // " + gt.Key + " = ") { Foreground = commentBrush });
                                        par.Inlines.Add(new Run(gt.Value is string ? "\"" + (string)gt.Value + "\"" : gt.Value.ToString()) { Foreground = commentBrush, Cursor = Cursors.Hand });
                                        if (gt.Value is UndertaleObject)
                                            par.Inlines.LastInline.MouseDown += (sender, ev) => (Application.Current.MainWindow as MainWindow).ChangeSelection(gt.Value);
                                    }

                                    if (par.Inlines.Count >= 250)
                                    { // Splitting into different paragraphs significantly increases selection performance.
                                    document.Blocks.Add(par);
                                        par = new Paragraph();
                                        par.Margin = new Thickness(0);
                                    }
                                    else if (!readingString)
                                    {
                                        par.Inlines.Add(new Run("\n"));
                                    }
                                }
                            }
                        }

                        document.Blocks.Add(par);
                        DecompiledView.Document = document;
                        CurrentDecompiled = code;
                        dialog.Hide();
                    });
                });
                try
                {
                    dialog.ShowDialog();
                }
                catch
                {
                    //Without this try catch block it will always crash when pulling up the decompiled code first
                    //But with it there don't seem to be any ill effects
                    //So I'm just going to leave it like this and hope nothing breaks
                    //And submit an issue about it
                    //If you, future developer, discover this hacky workaround
                    //Please do what I couldn't, and put in a proper fix
                    //
                    //Thank you
                    // - Grossley
                }
                await t;
            }
        }

        private async void GraphCode(UndertaleCode code)
        {
            if (code.DuplicateEntry)
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
                    var blocks = Decompiler.DecompileFlowGraph(code);
                    string dot = Decompiler.ExportFlowGraph(blocks);

                    try
                    {
                        var getStartProcessQuery = new GetStartProcessQuery();
                        var getProcessStartInfoQuery = new GetProcessStartInfoQuery();
                        var registerLayoutPluginCommand = new RegisterLayoutPluginCommand(getProcessStartInfoQuery, getStartProcessQuery);
                        var wrapper = new GraphGeneration(getStartProcessQuery, getProcessStartInfoQuery, registerLayoutPluginCommand);
                        
                        byte[] output = wrapper.GenerateGraph(dot, Enums.GraphReturnType.Png); // TODO: Use SVG instead
                        
                        image = new ImageSourceConverter().ConvertFrom(output) as ImageSource;
                    }
                    catch(Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                        if (MessageBox.Show("Unable to execute GraphViz: " + e.Message + "\nMake sure you have downloaded it and set the path in settings.\nDo you want to open the download page now?", "Graph generation failed", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                            Process.Start("https://graphviz.gitlab.io/_pages/Download/Download_windows.html");
                    }
                }
                catch(Exception e)
                {
                    Debug.WriteLine(e.ToString());
                    MessageBox.Show(e.Message, "Graph generation failed", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void DecompiledView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((this.DataContext as UndertaleCode)?.DuplicateEntry == true)
                return;
            DecompiledView.Visibility = Visibility.Collapsed;
            DecompiledEditor.Visibility = Visibility.Visible;
            DecompiledEditor.Text = new TextRange(DecompiledView.Document.ContentStart, DecompiledView.Document.ContentEnd).Text;
            int index = DisassemblyEditor.GetCharacterIndexFromPoint(Mouse.GetPosition(DecompiledView), true);
            if (index >= 0)
                DecompiledEditor.CaretIndex = index;
            DecompiledEditor.Focus();
        }

        private void DecompiledEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            UndertaleCode code = this.DataContext as UndertaleCode;
            if (code == null)
                return; // Probably loaded another data.win or something.
            if (code.DuplicateEntry)
                return;

            UndertaleData data = (Application.Current.MainWindow as MainWindow).Data;

            CompileContext compileContext = Compiler.CompileGMLText(DecompiledEditor.Text, data, code);

            if (compileContext.HasError)
            {
                MessageBox.Show(compileContext.ResultError, "Compiler error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!compileContext.SuccessfulCompile)
            {
                MessageBox.Show(compileContext.ResultAssembly, "Compile failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //The code should only be written after being successfully edited (if it doesn't successfully assemble for some reason, don't write it).
            bool CodeEditSuccessful = false;
            try
            {
                var instructions = Assembler.Assemble(compileContext.ResultAssembly, data);
                code.Replace(instructions);
                CodeEditSuccessful = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Assembler error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (CodeEditSuccessful)
            {
                //If the user is code editing outside of profile mode it will be written to disk when applied anyways, so that the code will always be ready immediately for profile mode (if they're toggling it on and off a lot for some reason)
                File.WriteAllText(TempPath + code.Name.Content + ".gml", DecompiledEditor.Text);
            }
            // Show new code, decompiled.
            CurrentDisassembled = null;
            CurrentDecompiled = null;
            CurrentGraphed = null;
            DecompileCode(code);

            DecompiledView.Visibility = Visibility.Visible;
            DecompiledEditor.Visibility = Visibility.Collapsed;
        }

        private void DisassemblyView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((this.DataContext as UndertaleCode)?.DuplicateEntry == true)
                return;
            DisassemblyView.Visibility = Visibility.Collapsed;
            DisassemblyEditor.Visibility = Visibility.Visible;
            DisassemblyEditor.Text = new TextRange(DisassemblyView.Document.ContentStart, DisassemblyView.Document.ContentEnd).Text;
            int index = DisassemblyEditor.GetCharacterIndexFromPoint(Mouse.GetPosition(DisassemblyView), true);
            if (index >= 0)
                DisassemblyEditor.CaretIndex = index;
            DisassemblyEditor.Focus();
        }

        private void DisassemblyEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            UndertaleCode code = this.DataContext as UndertaleCode;
            if (code == null)
                return; // Probably loaded another data.win or something.
            if (code.DuplicateEntry)
                return;

            UndertaleData data = (Application.Current.MainWindow as MainWindow).Data;
            try
            {
                var instructions = Assembler.Assemble(DisassemblyEditor.Text, data);
                code.Replace(instructions);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Assembler error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            CurrentDisassembled = null;
            CurrentDecompiled = null;
            CurrentGraphed = null;
            DisassembleCode(code);

            DisassemblyView.Visibility = Visibility.Visible;
            DisassemblyEditor.Visibility = Visibility.Collapsed;
        }
    }
}
