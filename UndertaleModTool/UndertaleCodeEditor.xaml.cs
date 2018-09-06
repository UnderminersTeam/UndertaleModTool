using GraphVizWrapper;
using GraphVizWrapper.Commands;
using GraphVizWrapper.Queries;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public UndertaleCodeEditor()
        {
            InitializeComponent();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            string disasm = code.Disassembly;

            FlowDocument document = new FlowDocument();
            document.PagePadding = new Thickness(0);
            document.FontFamily = new FontFamily("Lucida Console");
            Paragraph par = new Paragraph();

            if (code.Instructions.Count > 5000)
            {
                // Disable syntax highlighting. Loading it can take a few MINUTES on large scripts.
                par.Inlines.Add(new Run(code.Disassembly));
            }
            else
            {
                Brush addressBrush = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                Brush opcodeBrush = new SolidColorBrush(Color.FromRgb(0, 100, 0));
                Brush argBrush = new SolidColorBrush(Color.FromRgb(0, 0, 150));
                Brush typeBrush = new SolidColorBrush(Color.FromRgb(0, 0, 50));
                foreach (var instr in code.Instructions)
                {
                    par.Inlines.Add(new Run(instr.Address.ToString("D5") + ": ") { Foreground = addressBrush });
                    par.Inlines.Add(new Run(instr.Kind.ToString().ToUpper()) { Foreground = opcodeBrush, FontWeight = FontWeights.Bold });

                    switch (UndertaleInstruction.GetInstructionType(instr.Kind))
                    {
                        case UndertaleInstruction.InstructionType.SingleTypeInstruction:
                            par.Inlines.Add(new Run(" "));
                            par.Inlines.Add(new Run("(" + instr.Type1.ToString().ToLower() + ")") { Foreground = typeBrush });

                            if (instr.Kind == UndertaleInstruction.Opcode.Dup)
                            {
                                par.Inlines.Add(new Run(" "));
                                par.Inlines.Add(new Run(instr.DupExtra.ToString()) { Foreground = argBrush });
                            }
                            break;

                        case UndertaleInstruction.InstructionType.DoubleTypeInstruction:
                            par.Inlines.Add(new Run(" "));
                            par.Inlines.Add(new Run("(" + instr.Type1.ToString().ToLower() + ")") { Foreground = typeBrush });
                            par.Inlines.Add(new Run(", "));
                            par.Inlines.Add(new Run("(" + instr.Type2.ToString().ToLower() + ")") { Foreground = typeBrush });
                            break;

                        case UndertaleInstruction.InstructionType.ComparisonInstruction:
                            par.Inlines.Add(new Run(" "));
                            par.Inlines.Add(new Run("(" + instr.Type1.ToString().ToLower() + ")") { Foreground = typeBrush });
                            par.Inlines.Add(new Run(" "));
                            par.Inlines.Add(new Run(instr.ComparisonKind.ToString()) { Foreground = opcodeBrush });
                            par.Inlines.Add(new Run(" "));
                            par.Inlines.Add(new Run("(" + instr.Type2.ToString().ToLower() + ")") { Foreground = typeBrush });
                            break;

                        case UndertaleInstruction.InstructionType.GotoInstruction:
                            par.Inlines.Add(new Run(" "));
                            par.Inlines.Add(new Run("$" + instr.JumpOffset.ToString("+#;-#;0")) { Foreground = argBrush, ToolTip = (instr.Address + instr.JumpOffset).ToString("D5") });
                            break;

                        case UndertaleInstruction.InstructionType.PopInstruction:
                            par.Inlines.Add(new Run(" "));
                            par.Inlines.Add(new Run("(" + instr.Type1.ToString().ToLower() + ")") { Foreground = typeBrush });
                            Run runDest = new Run(instr.Destination.ToString()) { Foreground = argBrush, Cursor = Cursors.Hand };
                            runDest.MouseDown += (sender, e) =>
                            {
                                (Application.Current.MainWindow as MainWindow).Selected = instr.Destination;
                            };
                            par.Inlines.Add(runDest);
                            par.Inlines.Add(new Run(", "));
                            par.Inlines.Add(new Run("(" + instr.Type2.ToString().ToLower() + ")") { Foreground = typeBrush });
                            par.Inlines.Add(new Run(", "));
                            par.Inlines.Add(new Run("0x" + instr.DupExtra.ToString("X2")) { Foreground = argBrush });
                            break;

                        case UndertaleInstruction.InstructionType.PushInstruction:
                            par.Inlines.Add(new Run(" "));
                            par.Inlines.Add(new Run("(" + instr.Type1.ToString().ToLower() + ")") { Foreground = typeBrush });
                            Run valueRun = new Run(instr.Value.ToString()) { Foreground = argBrush, Cursor = (instr.Value is UndertaleObject || instr.Value is UndertaleResource) ? Cursors.Hand : Cursors.Arrow };
                            if (instr.Value is UndertaleResource)
                            {
                                valueRun.MouseDown += (sender, e) =>
                                {
                                    (Application.Current.MainWindow as MainWindow).Selected = (instr.Value as UndertaleResource).Resource;
                                };
                            }
                            else if (instr.Value is UndertaleObject)
                            {
                                valueRun.MouseDown += (sender, e) =>
                                {
                                    (Application.Current.MainWindow as MainWindow).Selected = instr.Value;
                                };
                            }
                            par.Inlines.Add(valueRun);
                            break;

                        case UndertaleInstruction.InstructionType.CallInstruction:
                            par.Inlines.Add(new Run(" "));
                            par.Inlines.Add(new Run("(" + instr.Type1.ToString().ToLower() + ")") { Foreground = typeBrush });
                            par.Inlines.Add(new Run(", "));
                            par.Inlines.Add(new Run(instr.Function.ToString()) { Foreground = argBrush });
                            par.Inlines.Add(new Run(", "));
                            par.Inlines.Add(new Run(instr.ArgumentsCount.ToString()) { Foreground = argBrush });
                            break;

                        case UndertaleInstruction.InstructionType.BreakInstruction:
                            par.Inlines.Add(new Run(" "));
                            par.Inlines.Add(new Run("(" + instr.Type1.ToString().ToLower() + ")") { Foreground = typeBrush });
                            par.Inlines.Add(new Run(instr.Value.ToString()) { Foreground = argBrush });
                            break;
                    }

                    par.Inlines.Add(new Run("\n"));
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
            foreach(var line in Decompiler.Decompile(gettextCode).Replace("\r\n", "\n").Split('\n'))
            {
                Match m = Regex.Match(line, "^ds_map_add\\(global.text_data_en, \"(.*)\"@([0-9]+), \"(.*)\"@([0-9]+)\\)");
                if (m.Success)
                    gettext.Add(m.Groups[1].Value, Int32.Parse(m.Groups[4].Value));
            }
        }

        private async void DecompileCode(UndertaleCode code)
        {
            LoaderDialog dialog = new LoaderDialog("Decompiling", "Decompiling, please wait... This can take a while on complex scripts");
            dialog.Owner = Window.GetWindow(this);

            FlowDocument document = new FlowDocument();
            document.PagePadding = new Thickness(0);
            document.FontFamily = new FontFamily("Lucida Console");
            Paragraph par = new Paragraph();

            UndertaleCode gettextCode = null;
            if (gettext == null)
                foreach (var c in (Application.Current.MainWindow as MainWindow).Data.Code)
                    if (c.Name.Content == "gml_Script_textdata_en")
                        gettextCode = c;

            Task t = Task.Run(() =>
            {
                string decompiled = null;
                Exception e = null;
                try
                {
                    decompiled = Decompiler.Decompile(code).Replace("\r\n", "\n");
                }
                catch (Exception ex)
                {
                    e = ex;
                }

                if (gettextCode != null)
                    UpdateGettext(gettextCode);

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
                        if (lines.Length > 5000)
                        {
                            par.Inlines.Add(new Run(decompiled));
                        }
                        else
                        {
                            Brush keywordBrush = new SolidColorBrush(Color.FromRgb(0, 0, 150));
                            Brush stringBrush = new SolidColorBrush(Color.FromRgb(0, 0, 200));
                            Brush commentBrush = new SolidColorBrush(Color.FromRgb(0, 150, 0));
                            Brush funcBrush = new SolidColorBrush(Color.FromRgb(100, 100, 0));

                            Dictionary<string, UndertaleFunctionDeclaration> funcs = new Dictionary<string, UndertaleFunctionDeclaration>();
                            foreach (var x in (Application.Current.MainWindow as MainWindow).Data.FunctionDeclarations)
                                funcs.Add(x.Name.Content, x);

                            foreach (var line in lines)
                            {
                                char[] special = { '.', ',', ')', '(', '[', ']', '>', '<', ':', ';', '=', '"' };
                                Func<char, bool> IsSpecial = (c) => Char.IsWhiteSpace(c) || special.Contains(c);
                                List<string> split = new List<string>();
                                string tok = "";
                                bool readingString = false;
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
                                    split.Add(tok);

                                Dictionary<string, UndertaleObject> usedObjects = new Dictionary<string, UndertaleObject>();
                                for (int i = 0; i < split.Count; i++)
                                {
                                    string token = split[i];
                                    if (token == "if" || token == "else" || token == "return" || token == "break")
                                        par.Inlines.Add(new Run(token) { Foreground = keywordBrush, FontWeight = FontWeights.Bold });
                                    else if (token == "self" || token == "global" || token == "local")
                                        par.Inlines.Add(new Run(token) { Foreground = keywordBrush });
                                    else if (token.StartsWith("\""))
                                        par.Inlines.Add(new Run(token) { Foreground = stringBrush });
                                    else if (token.StartsWith("//"))
                                        par.Inlines.Add(new Run(token) { Foreground = commentBrush });
                                    else if (token.StartsWith("@"))
                                    {
                                        par.Inlines.LastInline.Cursor = Cursors.Hand;
                                        par.Inlines.LastInline.MouseDown += (sender, ev) =>
                                        {
                                            MainWindow mw = Application.Current.MainWindow as MainWindow;
                                            mw.Selected = mw.Data.Strings[Int32.Parse(token.Substring(1))];
                                        };
                                    }
                                    else if (funcs.ContainsKey(token))
                                    {
                                        par.Inlines.Add(new Run(token) { Foreground = funcBrush, Cursor = Cursors.Hand });
                                        par.Inlines.LastInline.MouseDown += (sender, ev) => (Application.Current.MainWindow as MainWindow).Selected = funcs[token];
                                        if (token == "scr_gettext" && gettext != null)
                                        {
                                            if (split[i + 1] == "(" && split[i + 2].StartsWith("\"") && split[i + 3].StartsWith("@") && split[i + 4] == ")")
                                            {
                                                string id = split[i + 2].Substring(1, split[i + 2].Length - 2);
                                                if(!usedObjects.ContainsKey(id))
                                                    usedObjects.Add(id, (Application.Current.MainWindow as MainWindow).Data.Strings[gettext[id]]);
                                            }
                                        }
                                        if (token == "instance_exists")
                                        {
                                            if (split[i + 1] == "(" && split[i + 3] == ")")
                                            {
                                                int id;
                                                if (Int32.TryParse(split[i + 2], out id))
                                                {
                                                    if(!usedObjects.ContainsKey(split[i + 2]))
                                                        usedObjects.Add(split[i + 2], (Application.Current.MainWindow as MainWindow).Data.GameObjects[id]);
                                                }
                                            }
                                        }
                                        if (token == "instance_create")
                                        {
                                            if (split[i + 1] == "(")
                                            {
                                                int end;
                                                for (end = i; split[end] != ")"; end++) ;
                                                if (split[end-2].Trim() == "" && split[end-3] == ",")
                                                {
                                                    int id;
                                                    if (Int32.TryParse(split[end - 1], out id))
                                                    {
                                                        if (!usedObjects.ContainsKey(split[end - 1]))
                                                            usedObjects.Add(split[end - 1], (Application.Current.MainWindow as MainWindow).Data.GameObjects[id]);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                        par.Inlines.Add(new Run(token));

                                    if (token == ".")
                                    {
                                        int id;
                                        if (Int32.TryParse(split[i - 1], out id))
                                        {
                                            if (!usedObjects.ContainsKey(split[i - 1]))
                                                usedObjects.Add(split[i - 1], (Application.Current.MainWindow as MainWindow).Data.GameObjects[id]);
                                        }
                                    }
                                }
                                foreach (var gt in usedObjects)
                                {
                                    par.Inlines.Add(new Run(" // ") { Foreground = commentBrush });
                                    par.Inlines.Add(new Run(gt.Key) { Foreground = commentBrush });
                                    par.Inlines.Add(new Run(" = ") { Foreground = commentBrush });
                                    par.Inlines.Add(new Run(gt.Value.ToString()) { Foreground = commentBrush, Cursor = Cursors.Hand });
                                    par.Inlines.LastInline.MouseDown += (sender, ev) => (Application.Current.MainWindow as MainWindow).Selected = gt.Value;
                                }
                                par.Inlines.Add(new Run("\n"));
                            }
                        }
                    }

                    document.Blocks.Add(par);
                    DecompiledView.Document = document;
                    CurrentDecompiled = code;
                    dialog.Hide();
                });
            });
            dialog.ShowDialog();
            await t;
        }

        private async void GraphCode(UndertaleCode code)
        {
            LoaderDialog dialog = new LoaderDialog("Generating graph", "Generating graph, please wait...");
            dialog.Owner = Window.GetWindow(this);
            Task t = Task.Run(() =>
            {
                ImageSource image = null;
                try
                {
                    var getStartProcessQuery = new GetStartProcessQuery();
                    var getProcessStartInfoQuery = new GetProcessStartInfoQuery();
                    var registerLayoutPluginCommand = new RegisterLayoutPluginCommand(getProcessStartInfoQuery, getStartProcessQuery);
                    var wrapper = new GraphGeneration(getStartProcessQuery, getProcessStartInfoQuery, registerLayoutPluginCommand);

                    var blocks = Decompiler.DecompileFlowGraph(code);
                    string dot = Decompiler.ExportFlowGraph(blocks);
                    Debug.WriteLine(dot);
                    byte[] output = wrapper.GenerateGraph(dot, Enums.GraphReturnType.Png); // TODO: Use SVG instead

                    image = new ImageSourceConverter().ConvertFrom(output) as ImageSource;
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
    }
}
