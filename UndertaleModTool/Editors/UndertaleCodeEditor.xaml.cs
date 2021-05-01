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

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleCodeEditor.xaml
    /// </summary>
    public partial class UndertaleCodeEditor : UserControl
    {
        public UndertaleCode CurrentDisassembled = null;
        public UndertaleCode CurrentDecompiled = null;
        public List<string> CurrentDecompiledLocals = null;
        public UndertaleCode CurrentGraphed = null;
        public string UMTAppDataPath = (Application.Current.MainWindow as MainWindow).AppDataFolder;
        public string ProfilesFolder = (Application.Current.MainWindow as MainWindow).ProfilesFolder;
        public string ProfileHash = (Application.Current.MainWindow as MainWindow).ProfileHash;
        public string MainPath = Path.Combine((Application.Current.MainWindow as MainWindow).ProfilesFolder, (Application.Current.MainWindow as MainWindow).ProfileHash, "Main");
        public string TempPath = Path.Combine((Application.Current.MainWindow as MainWindow).ProfilesFolder, (Application.Current.MainWindow as MainWindow).ProfileHash, "Temp");

        public bool DecompiledFocused = false;
        public bool DecompiledChanged = false;

        public bool DisassemblyFocused = false;
        public bool DisassemblyChanged = false;

        public UndertaleCodeEditor()
        {
            InitializeComponent();

            // Decompiled editor styling and functionality
            SearchPanel.Install(DecompiledEditor.TextArea).MarkerBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90));

            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("UndertaleModTool.Resources.GML.xshd"))
            {
                using (XmlTextReader reader = new XmlTextReader(stream))
                {
                    DecompiledEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }

            DecompiledEditor.TextArea.TextView.ElementGenerators.Add(new NumberGenerator());
            DecompiledEditor.TextArea.TextView.ElementGenerators.Add(new NameGenerator());

            DecompiledEditor.TextArea.TextView.Options.HighlightCurrentLine = true;
            DecompiledEditor.TextArea.TextView.CurrentLineBackground = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            DecompiledEditor.TextArea.TextView.CurrentLineBorder = null;

            DecompiledEditor.Document.TextChanged += (s, e) => DecompiledChanged = true;

            DecompiledEditor.TextArea.SelectionBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            DecompiledEditor.TextArea.SelectionForeground = null;
            DecompiledEditor.TextArea.SelectionBorder = null;
            DecompiledEditor.TextArea.SelectionCornerRadius = 0;

            // Disassembly editor styling and functionality
            SearchPanel.Install(DisassemblyEditor.TextArea).MarkerBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90));

            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("UndertaleModTool.Resources.VMASM.xshd"))
            {
                using (XmlTextReader reader = new XmlTextReader(stream))
                {
                    DisassemblyEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }

            DisassemblyEditor.TextArea.TextView.Options.HighlightCurrentLine = true;
            DisassemblyEditor.TextArea.TextView.CurrentLineBackground = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            DisassemblyEditor.TextArea.TextView.CurrentLineBorder = null;

            DisassemblyEditor.Document.TextChanged += (s, e) => DisassemblyChanged = true;

            DisassemblyEditor.TextArea.SelectionBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            DisassemblyEditor.TextArea.SelectionForeground = null;
            DisassemblyEditor.TextArea.SelectionBorder = null;
            DisassemblyEditor.TextArea.SelectionCornerRadius = 0;
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UndertaleCode code = this.DataContext as UndertaleCode;
            Directory.CreateDirectory(MainPath);
            Directory.CreateDirectory(TempPath);
            if (code == null)
                return;
            DecompiledEditor_LostFocus(sender, null);
            DisassemblyEditor_LostFocus(sender, null);
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
            DecompiledEditor_LostFocus(sender, null);
            DisassemblyEditor_LostFocus(sender, null);
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

            string text;

            if (code.DuplicateEntry)
            {
                DisassemblyEditor.IsReadOnly = true;
                text = "; Duplicate code entry; cannot edit here.";
            }
            else
            {
                DisassemblyEditor.IsReadOnly = false;

                var data = (Application.Current.MainWindow as MainWindow).Data;
                text = code.Disassemble(data.Variables, data.CodeLocals.For(code));
            }

            DisassemblyEditor.Text = text;

            CurrentDisassembled = code;
            DisassemblyChanged = false;
        }

        private static Dictionary<string, int> gettext = null;
        private void UpdateGettext(UndertaleCode gettextCode)
        {
            gettext = new Dictionary<string, int>();
            string[] DecompilationOutput;
            if (SettingsWindow.ProfileModeEnabled == "False")
                DecompilationOutput = Decompiler.Decompile(gettextCode, new DecompileContext(null, true)).Replace("\r\n", "\n").Split('\n');
            else
            {
                try
                {
                    if (File.Exists(Path.Combine(TempPath, gettextCode.Name.Content + ".gml")))
                        DecompilationOutput = File.ReadAllText(Path.Combine(TempPath, gettextCode.Name.Content + ".gml")).Replace("\r\n", "\n").Split('\n');
                    else
                        DecompilationOutput = Decompiler.Decompile(gettextCode, new DecompileContext(null, true)).Replace("\r\n", "\n").Split('\n');
                }
                catch
                {
                    DecompilationOutput = Decompiler.Decompile(gettextCode, new DecompileContext(null, true)).Replace("\r\n", "\n").Split('\n');
                }
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
            }
            catch (Exception e)
            {
                gettextJSON = new Dictionary<string, string>();
                return "Failed to parse language file: " + e.Message;
            }
            return null;
        }
        private async void DecompileCode(UndertaleCode code)
        {
            DecompiledEditor.IsReadOnly = true;
            if (code.DuplicateEntry)
            {
                DecompiledEditor.Text = "// Duplicate code entry; cannot edit here.";
                CurrentDecompiled = code;
            }
            else
            {
                LoaderDialog dialog = new LoaderDialog("Decompiling", "Decompiling, please wait... This can take a while on complex scripts");
                dialog.Owner = Window.GetWindow(this);
                _ = Dispatcher.BeginInvoke(new Action(() => { if (!dialog.IsClosed) dialog.ShowDialog(); }));

                UndertaleCode gettextCode = null;
                if (gettext == null)
                    gettextCode = (Application.Current.MainWindow as MainWindow).Data.Code.ByName("gml_Script_textdata_en");

                string dataPath = Path.GetDirectoryName((Application.Current.MainWindow as MainWindow).FilePath);
                string gettextJsonPath = (dataPath != null) ? Path.Combine(dataPath, "lang", "lang_en.json") : null;

                var dataa = (Application.Current.MainWindow as MainWindow).Data;
                Task t = Task.Run(() =>
                {
                    DecompileContext context = new DecompileContext(dataa, false);
                    string decompiled = null;
                    Exception e = null;
                    try
                    {
                        decompiled = ((SettingsWindow.ProfileModeEnabled == "False" || !File.Exists(Path.Combine(TempPath, code.Name.Content + ".gml"))) ? Decompiler.Decompile(code, context).Replace("\r\n", "\n") : File.ReadAllText(Path.Combine(TempPath, code.Name.Content + ".gml")).Replace("\r\n", "\n"));
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
                        MessageBox.Show(exc.ToString());
                    }

                    if (gettextJSON == null && gettextJsonPath != null && File.Exists(gettextJsonPath))
                    {
                        string err = UpdateGettextJSON(File.ReadAllText(gettextJsonPath));
                        if (err != null)
                            e = new Exception(err);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        if (e != null)
                            DecompiledEditor.Text = "/* EXCEPTION!\n   " + e.ToString() + "\n*/";
                        else if (decompiled != null)
                        {
                            DecompiledEditor.Text = decompiled;
                            CurrentDecompiledLocals = new List<string>();

                            var locals = dataa.CodeLocals.ByName(code.Name.Content);
                            if (locals != null)
                            {
                                foreach (var local in locals.Locals)
                                    CurrentDecompiledLocals.Add(local.Name.Content);
                            }
                        }
                        DecompiledEditor.IsReadOnly = false;
                        DecompiledChanged = false;

                        CurrentDecompiled = code;
                        dialog.Hide();
                    });
                });
                await t;
                dialog.Close();
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
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                        if (MessageBox.Show("Unable to execute GraphViz: " + e.Message + "\nMake sure you have downloaded it and set the path in settings.\nDo you want to open the download page now?", "Graph generation failed", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                            Process.Start("https://graphviz.gitlab.io/_pages/Download/Download_windows.html");
                    }
                }
                catch (Exception e)
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

        private void DecompiledEditor_GotFocus(object sender, RoutedEventArgs e)
        {
            if (DecompiledEditor.IsReadOnly)
                return;
            DecompiledFocused = true;
        }

        private void DecompiledEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!DecompiledFocused)
                return;
            if (DecompiledEditor.IsReadOnly)
                return;
            DecompiledFocused = false;

            if (!DecompiledChanged)
                return;

            UndertaleCode code = this.DataContext as UndertaleCode;
            if (code == null)
                return; // Probably loaded another data.win or something.
            if (code.DuplicateEntry)
                return;

            // Check to make sure this isn't an element inside of the textbox, or another tab
            IInputElement elem = Keyboard.FocusedElement;
            if (elem is UIElement)
            {
                if (e != null && (elem as UIElement).IsDescendantOf(DecompiledEditor))
                    return;
            }

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
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Assembler error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                if (CodeEditSuccessful && (!(Application.Current.MainWindow as MainWindow).Data.GMS2_3) && (SettingsWindow.ProfileModeEnabled == "True"))
                {
                    //Write text, if only in the profile mode.
                    File.WriteAllText(Path.Combine(TempPath, code.Name.Content + ".gml"), DecompiledEditor.Text);
                }
                else if (CodeEditSuccessful && (!(Application.Current.MainWindow as MainWindow).Data.GMS2_3) && (SettingsWindow.ProfileModeEnabled == "False"))
                {
                    //Destroy file with comments if it's been edited outside the profile mode.
                    //We're dealing with the decompiled code only, it has to happen.
                    //Otherwise it will cause a desync, which is more important to prevent.
                    if (File.Exists(Path.Combine(TempPath, code.Name.Content + ".gml")))
                    {
                        File.Delete(Path.Combine(TempPath, code.Name.Content + ".gml"));
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error during writing of GML code to profile:\n" + exc.ToString());
            }
            // Show new code, decompiled.
            CurrentDisassembled = null;
            CurrentDecompiled = null;
            CurrentGraphed = null;

            // Tab switch
            if (e == null)
                return;

            // Decompile new code
            DecompileCode(code);
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

            UndertaleCode code = this.DataContext as UndertaleCode;
            if (code == null)
                return; // Probably loaded another data.win or something.
            if (code.DuplicateEntry)
                return;

            // Check to make sure this isn't an element inside of the textbox, or another tab
            IInputElement elem = Keyboard.FocusedElement;
            if (elem is UIElement)
            {
                if (e != null && (elem as UIElement).IsDescendantOf(DisassemblyEditor))
                    return;
            }

            UndertaleData data = (Application.Current.MainWindow as MainWindow).Data;
            try
            {
                var instructions = Assembler.Assemble(DisassemblyEditor.Text, data);
                code.Replace(instructions);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Assembler error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            DisassembleCode(code);
        }


        // Based on https://stackoverflow.com/questions/28379206/custom-hyperlinks-using-avalonedit
        public class NumberGenerator : VisualLineElementGenerator
        {
            readonly static Regex regex = new Regex(@"\b\d+\.?\b");

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
                if (m.Success)
                {
                    int res = startOffset + m.Index;
                    int line = CurrentContext.Document.GetLocation(res).Line;
                    var textArea = CurrentContext.TextView.GetService(typeof(TextArea)) as TextArea;
                    var highlighter = textArea.GetService(typeof(IHighlighter)) as IHighlighter;
                    HighlightedLine highlighted = highlighter.HighlightLine(line);

                    foreach (var section in highlighted.Sections)
                    {
                        if (section.Color.Name == "Number" &&
                            section.Offset == res)
                            return res;
                    }
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
                            UndertaleData data = (Application.Current.MainWindow as MainWindow).Data;

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
                            foreach (UndertaleObject obj in possibleObjects)
                            {
                                MenuItem item = new MenuItem();
                                item.Header = obj.ToString().Replace("_", "__");
                                item.Click += (sender2, ev2) =>
                                {
                                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                                    {
                                        doc.Replace(line.ParentVisualLine.StartOffset + line.RelativeTextOffset,
                                                    text.Length, (obj as UndertaleNamedResource).Name.Content, null);
                                        (parent as UndertaleCodeEditor).DecompiledChanged = true;
                                    }
                                    else
                                        (Application.Current.MainWindow as MainWindow).ChangeSelection(obj);
                                };
                                contextMenu.Items.Add(item);
                            }
                            if (id > 0x00050000)
                            {
                                contextMenu.Items.Add(new MenuItem() { Header = "#" + id.ToString("X6") + " (color)", IsEnabled = false });
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
                HighlightedLine highlighted = highlighter.HighlightLine(line);

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
                    UndertaleData data = (Application.Current.MainWindow as MainWindow).Data;
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
                        val = data.Scripts.ByName(m.Value);
                        if (val == null)
                            val = data.Functions.ByName(m.Value);
                    }
                    else
                        val = data.ByName(m.Value);
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
                        if ((parent as UndertaleCodeEditor).CurrentDecompiledLocals.Contains(m.Value))
                            return new ColorVisualLineText(m.Value, CurrentContext.VisualLine, m.Length,
                                                            new SolidColorBrush(Color.FromRgb(0xFF, 0xF8, 0x99)));
                        return null;
                    }

                    var line = new ClickVisualLineText(m.Value, CurrentContext.VisualLine, m.Length,
                                                        func ? null : new SolidColorBrush(Color.FromRgb(0xFF, 0x80, 0x80)));
                    line.Clicked += (text) =>
                    {
                        (Application.Current.MainWindow as MainWindow).ChangeSelection(val);
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
                if (e.ChangedButton == System.Windows.Input.MouseButton.Left && !e.Handled && LinkIsClickable())
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