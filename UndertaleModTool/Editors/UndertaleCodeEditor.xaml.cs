﻿using GraphVizWrapper;
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
            code.UpdateAddresses();

            FlowDocument document = new FlowDocument();
            document.PagePadding = new Thickness(0);
            document.FontFamily = new FontFamily("Lucida Console");
            Paragraph par = new Paragraph();

            if (code.Instructions.Count > 5000)
            {
                // Disable syntax highlighting. Loading it can take a few MINUTES on large scripts.
                var data = (Application.Current.MainWindow as MainWindow).Data;
                par.Inlines.Add(new Run(code.Disassemble(data.Variables, data.CodeLocals.For(code))));
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
                    par.Inlines.Add(new Run(instr.Kind.ToString().ToLower()) { Foreground = opcodeBrush, FontWeight = FontWeights.Bold });

                    switch (UndertaleInstruction.GetInstructionType(instr.Kind))
                    {
                        case UndertaleInstruction.InstructionType.SingleTypeInstruction:
                            par.Inlines.Add(new Run("." + instr.Type1.ToOpcodeParam()) { Foreground = typeBrush });

                            if (instr.Kind == UndertaleInstruction.Opcode.Dup)
                            {
                                par.Inlines.Add(new Run(" "));
                                par.Inlines.Add(new Run(instr.DupExtra.ToString()) { Foreground = argBrush });
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
                            if (instr.Type1 == UndertaleInstruction.DataType.Variable && instr.TypeInst != UndertaleInstruction.InstanceType.Undefined)
                            {
                                par.Inlines.Add(new Run(instr.TypeInst.ToString().ToLower()) { Foreground = typeBrush });
                                par.Inlines.Add(new Run("."));
                            }
                            Run runDest = new Run(instr.Destination.ToString()) { Foreground = argBrush, Cursor = Cursors.Hand };
                            runDest.MouseDown += (sender, e) =>
                            {
                                (Application.Current.MainWindow as MainWindow).ChangeSelection(instr.Destination);
                            };
                            par.Inlines.Add(runDest);
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
                            par.Inlines.Add(new Run(" "));
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

        private static Dictionary<string, string> gettextJSON = null;
        private void UpdateGettextJSON(string json)
        {
            gettextJSON = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
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
                gettextCode = (Application.Current.MainWindow as MainWindow).Data.Code.ByName("gml_Script_textdata_en");

            string gettextJsonPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName((Application.Current.MainWindow as MainWindow).FilePath), "lang/lang_en.json");

            var dataa = (Application.Current.MainWindow as MainWindow).Data;
            Task t = Task.Run(() =>
            {
                string decompiled = null;
                Exception e = null;
                try
                {
                    decompiled = Decompiler.Decompile(code, dataa).Replace("\r\n", "\n");
                }
                catch (Exception ex)
                {
                    e = ex;
                }

                if (gettextCode != null)
                    UpdateGettext(gettextCode);

                if (gettextJSON == null && File.Exists(gettextJsonPath))
                    UpdateGettextJSON(File.ReadAllText(gettextJsonPath));

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
                            Brush constBrush = new SolidColorBrush(Color.FromRgb(0, 100, 150));
                            Brush stringBrush = new SolidColorBrush(Color.FromRgb(0, 0, 200));
                            Brush commentBrush = new SolidColorBrush(Color.FromRgb(0, 150, 0));
                            Brush funcBrush = new SolidColorBrush(Color.FromRgb(100, 100, 0));
                            Brush assetBrush = new SolidColorBrush(Color.FromRgb(0, 150, 100));

                            Dictionary<string, UndertaleFunction> funcs = new Dictionary<string, UndertaleFunction>();
                            foreach (var x in (Application.Current.MainWindow as MainWindow).Data.Functions)
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

                                Dictionary<string, object> usedObjects = new Dictionary<string, object>();
                                for (int i = 0; i < split.Count; i++)
                                {
                                    int? val = null;
                                    string token = split[i];
                                    if (token == "if" || token == "else" || token == "return" || token == "break" || token == "continue" || token == "while" || token == "with" || token == "switch" || token == "case" || token == "default")
                                        par.Inlines.Add(new Run(token) { Foreground = keywordBrush, FontWeight = FontWeights.Bold });
                                    else if (token == "self" || token == "global" || token == "local" || token == "other" || token == "noone" || token == "true" || token == "false")
                                        par.Inlines.Add(new Run(token) { Foreground = keywordBrush });
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
                                                if (!usedObjects.ContainsKey(id))
                                                    usedObjects.Add(id, (Application.Current.MainWindow as MainWindow).Data.Strings[gettext[id]]);
                                            }
                                        }
                                        if (token == "scr_84_get_lang_string" && gettextJSON != null)
                                        {
                                            if (split[i + 1] == "(" && split[i + 2].StartsWith("\"") && split[i + 3].StartsWith("@") && split[i + 4] == ")")
                                            {
                                                string id = split[i + 2].Substring(1, split[i + 2].Length - 2);
                                                if (!usedObjects.ContainsKey(id))
                                                    usedObjects.Add(id, gettextJSON[id]);
                                            }
                                        }
                                    }
                                    else if (funcs.ContainsKey(token))
                                    {
                                        par.Inlines.Add(new Run(token) { Foreground = funcBrush, Cursor = Cursors.Hand });
                                        par.Inlines.LastInline.MouseDown += (sender, ev) => (Application.Current.MainWindow as MainWindow).ChangeSelection(funcs[token]);
                                    }
                                    else if (Char.IsDigit(token[0]))
                                    {
                                        par.Inlines.Add(new Run(token) { Cursor = Cursors.Hand });
                                        par.Inlines.LastInline.MouseDown += (sender, ev) =>
                                        {
                                            UndertaleData data = (Application.Current.MainWindow as MainWindow).Data;
                                            int id = Int32.Parse(token);
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
                                            foreach(UndertaleObject obj in possibleObjects)
                                            {
                                                MenuItem item = new MenuItem();
                                                item.Header = obj.ToString().Replace("_", "__");
                                                item.Click += (sender2, ev2) => (Application.Current.MainWindow as MainWindow).ChangeSelection(obj);
                                                contextMenu.Items.Add(item);
                                            }
                                            if (id > 0x00050000)
                                            {
                                                contextMenu.Items.Add(new MenuItem() { Header = "#" + id.ToString("X6") + " (color)", IsEnabled = false });
                                            }
                                            contextMenu.Items.Add(new MenuItem() { Header = id + " (number)", IsEnabled = false });
                                            (sender as Run).ContextMenu = contextMenu;
                                            contextMenu.IsOpen = true;
                                            ev.Handled = true;
                                        };
                                    }
                                    else
                                        par.Inlines.Add(new Run(token));

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
                                foreach (var gt in usedObjects)
                                {
                                    par.Inlines.Add(new Run(" // ") { Foreground = commentBrush });
                                    par.Inlines.Add(new Run(gt.Key) { Foreground = commentBrush });
                                    par.Inlines.Add(new Run(" = ") { Foreground = commentBrush });
                                    par.Inlines.Add(new Run(gt.Value is string ? "\"" + (string)gt.Value + "\"" : gt.Value.ToString()) { Foreground = commentBrush, Cursor = Cursors.Hand });
                                    if (gt.Value is UndertaleObject)
                                        par.Inlines.LastInline.MouseDown += (sender, ev) => (Application.Current.MainWindow as MainWindow).ChangeSelection(gt.Value);
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

        private void DisassemblyView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
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
            Debug.Assert(code != null);

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
