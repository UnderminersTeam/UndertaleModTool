using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaEdit.Rendering;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleCodeView : UserControl, IUndertaleCodeView
{
    private static IHighlightingDefinition? GMLHighlightingDefinition = null;
    private static IHighlightingDefinition? ASMHighlightingDefinition = null;
    private static uint HighlightingMajorVersion = 0;

    public (int, int) LastCaretOffsets;

    public UndertaleCodeView()
    {
        InitializeComponent();

        DataContextChanged += (_, __) =>
        {
            if (DataContext is UndertaleCodeViewModel vm)
            {
                vm.View = this;
                if (vm.MainVM.Settings!.EnableSyntaxHighlighting)
                {
                    // Reload highlighting if major version changed
                    if (HighlightingMajorVersion != vm.MainVM.Data!.GeneralInfo.Major)
                    {
                        UndertaleCodeView.GMLHighlightingDefinition = null;
                        UndertaleCodeView.ASMHighlightingDefinition = null;
                    }

                    HighlightingMajorVersion = vm.MainVM.Data!.GeneralInfo.Major;

                    UndertaleCodeView.GMLHighlightingDefinition ??= LoadHighlightingDefinition("GML");
                    GMLTextEditor.SyntaxHighlighting = UndertaleCodeView.GMLHighlightingDefinition;

                    UndertaleCodeView.ASMHighlightingDefinition ??= LoadHighlightingDefinition("ASM");
                    ASMTextEditor.SyntaxHighlighting = UndertaleCodeView.ASMHighlightingDefinition;

                    GMLTextEditor.TextArea.TextView.ElementGenerators.Add(new NumberGenerator(this));
                    GMLTextEditor.TextArea.TextView.ElementGenerators.Add(new NameGenerator(this));

                    ASMTextEditor.TextArea.TextView.ElementGenerators.Add(new NameGenerator(this));
                }
                else
                {
                    GMLTextEditor.SyntaxHighlighting = null;
                    ASMTextEditor.SyntaxHighlighting = null;
                    UndertaleCodeView.GMLHighlightingDefinition = null;
                    UndertaleCodeView.ASMHighlightingDefinition = null;
                }

                if (this.IsAttachedToVisualTree())
                {
                    ProcessLastGoToLocation();
                }
                else
                {
                    AttachedToLogicalTree += (_, __) =>
                    {
                        ProcessLastGoToLocation();
                    };
                }

                vm.PropertyChanged += (object? source, PropertyChangedEventArgs e) =>
                {
                    if (e.PropertyName == nameof(UndertaleCodeViewModel.LastGoToLocation) && vm.LastGoToLocation is not null)
                    {
                        ProcessLastGoToLocation();
                    }
                };
            }
        };

        InitializeTextEditor(GMLTextEditor);
        InitializeTextEditor(ASMTextEditor);

        GMLTextEditor.TextArea.GotFocus += GMLTextEditor_GotFocus;
        ASMTextEditor.TextArea.GotFocus += ASMTextEditor_GotFocus;

        GMLTextEditor.TextArea.LostFocus += GMLTextEditor_LostFocus;
        ASMTextEditor.TextArea.LostFocus += ASMTextEditor_LostFocus;
    }

    static IHighlightingDefinition LoadHighlightingDefinition(string name)
    {
        using (XmlReader reader = XmlReader.Create(AssetLoader.Open(new Uri($"avares://{Assembly.GetExecutingAssembly().FullName}/Assets/Syntax{name}.xshd"))))
        {
            IHighlightingDefinition definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);

            // Remove string escaping rule from GMS1, since it doesn't have that.
            if (HighlightingMajorVersion < 2)
            {
                foreach (HighlightingSpan span in definition.MainRuleSet.Spans)
                {
                    string expression = span.StartExpression.ToString();
                    if (expression == "\"" || expression == "'")
                        span.RuleSet.Spans.Clear();
                }
            }

            return definition;
        }
    }

    void InitializeTextEditor(TextEditor textEditor)
    {
        textEditor.Options.ConvertTabsToSpaces = true;
        textEditor.Options.HighlightCurrentLine = true;
    }

    public void ProcessLastGoToLocation()
    {
        if (DataContext is UndertaleCodeViewModel vm)
        {
            if (vm.LastGoToLocation is not null)
            {
                GoToLocation(vm.LastGoToLocation.Value);
                vm.LastGoToLocation = null;
            }
        }
    }

    public void GoToLocation((UndertaleCodeViewModel.Tab tab, int line) location)
    {
        if (DataContext is UndertaleCodeViewModel vm)
        {
            vm.SelectedTab = location.tab;
            AvaloniaEdit.TextEditor textEditor = (location.tab == UndertaleCodeViewModel.Tab.GML) ? GMLTextEditor : ASMTextEditor;

            textEditor.TextArea.Caret.Column = 0;
            textEditor.TextArea.Caret.Line = location.line;
            textEditor.Focus();

            EventHandler? func = null;
            func = (_, __) =>
            {
                textEditor.ScrollToLine(location.line);
                textEditor.LayoutUpdated -= func;
            };
            textEditor.LayoutUpdated += func;

            // HACK: I don't know how to check if the layout has updated already here or not, so I just invalidate it to call the above function.
            textEditor.InvalidateMeasure();
        }
    }

    private void GMLTextEditor_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (DataContext is not UndertaleCodeViewModel vm)
            return;

        vm.GMLFocused = true;
    }

    private void ASMTextEditor_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (DataContext is not UndertaleCodeViewModel vm)
            return;

        vm.ASMFocused = true;
    }

    private void GMLTextEditor_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UndertaleCodeViewModel vm)
            return;

        if (vm.GMLFocused && vm.MainVM.Settings!.AutomaticallyCompileAndDecompileCodeOnLostFocus)
        {
            vm.CompileAndDecompileGML(onlyIfOutdated: true);
            vm.GMLFocused = false;
        }
    }

    private void ASMTextEditor_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UndertaleCodeViewModel vm)
            return;

        if (vm.ASMFocused && vm.MainVM.Settings!.AutomaticallyCompileAndDecompileCodeOnLostFocus)
        {
            vm.CompileAndDecompileASM(onlyIfOutdated: true);
            vm.ASMFocused = false;
        }
    }

    private void GMLTextEditor_TextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not UndertaleCodeViewModel vm)
            return;

        vm.GMLOutdated = true;
    }

    private void ASMTextEditor_TextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not UndertaleCodeViewModel vm)
            return;

        vm.ASMOutdated = true;
    }

    // TODO: This code was mostly copied over, so it would be great if it could be made nicer. Or maybe do things differently.
    public class NumberGenerator : VisualLineElementGenerator
    {
        readonly UndertaleCodeView codeView;
        readonly ContextMenu contextMenu = new();

        // <offset, length>
        readonly Dictionary<int, int> lineNumberSections = [];

        public NumberGenerator(UndertaleCodeView codeView)
        {
            this.codeView = codeView;

            contextMenu.Placement = PlacementMode.Pointer;
        }

        public override void StartGeneration(ITextRunConstructionContext context)
        {
            base.StartGeneration(context);

            // Find sections of line that are highlighted as numbers
            lineNumberSections.Clear();

            DocumentLine documentLine = context.VisualLine.FirstDocumentLine;
            if (documentLine.Length != 0)
            {
                int line = documentLine.LineNumber;

                IHighlighter highlighter = (IHighlighter)CurrentContext.TextView.GetService(typeof(IHighlighter));
                HighlightedLine highlightedLine = highlighter.HighlightLine(line);

                foreach (var section in highlightedLine.Sections)
                {
                    if (section.Color.Name == "Number")
                        lineNumberSections[section.Offset] = section.Length;
                }
            }
        }

        public override int GetFirstInterestedOffset(int startOffset)
        {
            foreach ((int offset, _) in lineNumberSections)
            {
                if (startOffset <= offset)
                    return offset;
            }
            return -1;
        }

        public override VisualLineElement? ConstructElement(int offset)
        {
            if (!lineNumberSections.TryGetValue(offset, out int length))
                return null;

            TextDocument document = CurrentContext.Document;
            TextView textView = CurrentContext.TextView;
            TextEditor textEditor = (TextEditor)textView.GetService(typeof(TextEditor));

            UndertaleCodeViewModel codeViewModel = (UndertaleCodeViewModel)codeView.DataContext!;
            UndertaleData data = codeViewModel.MainVM.Data!;

            string text = document.GetText(offset, length);
            ClickVisualLineText visualLine = new(text, CurrentContext.VisualLine, length);

            visualLine.Clicked += (text, button) =>
            {
                if (button != MouseButton.Right)
                    return;

                if (!int.TryParse(text, out int id))
                    return;

                int documentOffset = visualLine.ParentVisualLine.StartOffset + visualLine.RelativeTextOffset;

                List<UndertaleNamedResource?> possibleObjects = [];

                if (id >= 0)
                {
                    // NOTE: Remember to add new types
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
                    if (id < data.AnimationCurves?.Count)
                        possibleObjects.Add(data.AnimationCurves[id]);
                    if (id < data.Sequences?.Count)
                        possibleObjects.Add(data.Sequences[id]);
                    if (id < data.ParticleSystems?.Count)
                        possibleObjects.Add(data.ParticleSystems[id]);
                }

                contextMenu.Items.Clear();

                foreach (UndertaleNamedResource? obj in possibleObjects)
                {
                    if (obj is null)
                        continue;

                    MenuItem item = new();
                    item.Header = obj.ToString()?.Replace("_", "__");
                    item.Click += (_, _) =>
                    {
                        document.Replace(documentOffset, text.Length, obj.Name.Content, null);
                    };
                    contextMenu.Items.Add(item);
                }

                if (id >= 0)
                {
                    string color = "0x" + id.ToString("X6");

                    MenuItem item = new();
                    item.Header = color + " (color)";
                    item.Click += (_, _) =>
                    {
                        document.Replace(documentOffset, text.Length, color, null);
                    };
                    contextMenu.Items.Add(item);
                }

                BuiltinList list = data.BuiltinList;

                foreach (var (constantName, constantValue) in list.Constants)
                {
                    if (constantValue == id)
                    {
                        MenuItem item = new();
                        item.Header = constantName.Replace("_", "__") + " (constant)";
                        item.Click += (_, _) =>
                        {
                            document.Replace(documentOffset, text.Length, constantName, null);
                        };
                        contextMenu.Items.Add(item);

                        // TODO: Ideally it would show all constants, but that's too cluttered!
                        break;
                    }
                }

                contextMenu.Items.Add(new MenuItem() { Header = id + " (number)", IsEnabled = false });

                codeViewModel.GMLFocused = false;
                codeViewModel.ASMFocused = false;
                contextMenu.Open(textEditor);
            };

            return visualLine;
        }
    }

    public class NameGenerator : VisualLineElementGenerator
    {
        static readonly SolidColorBrush FunctionBrush = new(Color.FromRgb(0xFF, 0xB8, 0x71));
        static readonly SolidColorBrush GlobalBrush = new(Color.FromRgb(0xF9, 0x7B, 0xF9));
        static readonly SolidColorBrush ConstantBrush = new(Color.FromRgb(0xFF, 0x80, 0x80));
        static readonly SolidColorBrush InstanceBrush = new(Color.FromRgb(0x58, 0xE3, 0x5A));
        static readonly SolidColorBrush LocalBrush = new(Color.FromRgb(0xFF, 0xF8, 0x99));

        readonly UndertaleCodeView codeView;
        readonly ContextMenu contextMenu = new();

        // <offset, length>
        readonly Dictionary<int, int> lineNameSections = [];

        public NameGenerator(UndertaleCodeView codeView)
        {
            this.codeView = codeView;
            contextMenu.Placement = PlacementMode.Pointer;
        }

        public override void StartGeneration(ITextRunConstructionContext context)
        {
            base.StartGeneration(context);

            // Find sections of line that are highlighted as identifiers or functions
            lineNameSections.Clear();

            DocumentLine documentLine = context.VisualLine.FirstDocumentLine;
            if (documentLine.Length != 0)
            {
                int line = documentLine.LineNumber;

                IHighlighter highlighter = (IHighlighter)CurrentContext.TextView.GetService(typeof(IHighlighter));
                HighlightedLine highlightedLine = highlighter.HighlightLine(line);

                foreach (var section in highlightedLine.Sections)
                {
                    if (section.Color.Name == "Identifier" || section.Color.Name == "Function")
                        lineNameSections[section.Offset] = section.Length;
                }
            }
        }

        public override int GetFirstInterestedOffset(int startOffset)
        {
            foreach ((int offset, _) in lineNameSections)
            {
                if (startOffset <= offset)
                    return offset;
            }
            return -1;
        }

        public override VisualLineElement? ConstructElement(int offset)
        {
            if (!lineNameSections.TryGetValue(offset, out int length))
                return null;

            TextDocument document = CurrentContext.Document;
            TextView textView = CurrentContext.TextView;
            TextEditor textEditor = (TextEditor)textView.GetService(typeof(TextEditor));

            UndertaleCodeViewModel codeViewModel = (UndertaleCodeViewModel)codeView.DataContext!;
            UndertaleData data = codeViewModel.MainVM.Data!;

            string text = document.GetText(offset, length);

            bool isFunction = (offset + length + 1 < CurrentContext.VisualLine.LastDocumentLine.EndOffset) &&
                (document.GetCharAt(offset + length) == '(');

            UndertaleNamedResource? namedResource = null;
            bool nonResourceReference = false;

            Dictionary<string, UndertaleNamedResource> scriptsDict = new();
            Dictionary<string, UndertaleNamedResource> functionsDict = new();
            Dictionary<string, UndertaleNamedResource> codeDict = new();
            Dictionary<string, UndertaleNamedResource> namedObjDict = new();

            foreach (var scr in data.Scripts)
            {
                if (scr is null)
                    continue;
                scriptsDict[scr.Name.Content] = scr;
            }

            foreach (var func2 in data.Functions)
            {
                if (func2 is null)
                    continue;
                functionsDict[func2.Name.Content] = func2;
            }

            foreach (var code in data.Code)
            {
                if (code is null)
                    continue;
                codeDict[code.Name.Content] = code;
            }

            // NOTE: Remember to add new types
            IEnumerable[] objLists = [
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
            ];

            foreach (var list in objLists)
            {
                if (list is null)
                    continue;

                foreach (var obj in list)
                {
                    if (obj is not UndertaleNamedResource namedObj)
                        continue;
                    namedObjDict[namedObj.Name.Content] = namedObj;
                }
            }

            List<string> CurrentLocals = [];
            UndertaleCodeLocals? locals = data.CodeLocals?.ByName(codeViewModel.Code.Name.Content);
            if (locals != null)
            {
                foreach (var local in locals.Locals)
                    CurrentLocals.Add(local.Name.Content);
            }

            // Process the content of this identifier/function
            if (isFunction)
            {
                namedResource = null;

                if (!data.IsVersionAtLeast(2, 3)) // in GMS2.3 every custom "function" is in fact a member variable and scripts are never referenced directly
                    scriptsDict.TryGetValue(text, out namedResource);

                if (namedResource == null)
                {
                    functionsDict.TryGetValue(text, out namedResource);
                    if (data.IsVersionAtLeast(2, 3))
                    {
                        if (namedResource != null)
                        {
                            if (codeDict.TryGetValue(namedResource.Name.Content, out _))
                                namedResource = null; // in GMS2.3 every custom "function" is in fact a member variable, and the names in functions make no sense (they have the gml_Script_ prefix)
                        }
                        else
                        {
                            // Resolve 2.3 sub-functions for their parent entry
                            if (data.GlobalFunctions?.TryGetFunction(text, out Underanalyzer.IGMFunction? f) == true)
                            {
                                scriptsDict.TryGetValue(f.Name.Content, out namedResource);
                                namedResource = (namedResource as UndertaleScript)?.Code?.ParentEntry;
                            }
                        }
                    }
                }
                if (namedResource == null)
                {
                    if (data.BuiltinList.Functions.ContainsKey(text))
                    {
                        ColorVisualLineText res = new(text, CurrentContext.VisualLine, length, FunctionBrush);
                        res.Bold = true;
                        return res;
                    }
                }
            }
            else
            {
                namedObjDict.TryGetValue(text, out namedResource);
                if (data.IsVersionAtLeast(2, 3))
                {
                    if (namedResource is UndertaleScript)
                        namedResource = null; // in GMS2.3 scripts are never referenced directly

                    if (data.GlobalFunctions?.TryGetFunction(text, out Underanalyzer.IGMFunction? globalFunc) == true &&
                        globalFunc is UndertaleFunction utGlobalFunc)
                    {
                        // Try getting script that this function reference belongs to
                        if (namedObjDict.TryGetValue("gml_Script_" + text, out namedResource) && namedResource is UndertaleScript script)
                        {
                            // Highlight like a function as well
                            namedResource = script.Code;
                            isFunction = true;
                        }
                    }

                    if (namedResource == null)
                    {
                        // Try to get basic function
                        if (functionsDict.TryGetValue(text, out namedResource))
                        {
                            isFunction = true;
                        }
                    }

                    if (namedResource == null)
                    {
                        // Try resolving to room instance ID
                        string instanceIdPrefix = data.ToolInfo.InstanceIdPrefix();
                        if (text.StartsWith(instanceIdPrefix) &&
                            int.TryParse(text[instanceIdPrefix.Length..], out int id) && id >= 100000)
                        {
                            // TODO: We currently mark this as a non-resource reference, but ideally
                            // we resolve this to the room that this instance ID occurs in.
                            // However, we should only do this when actually clicking on it.
                            nonResourceReference = true;
                        }
                    }
                }
            }
            if (namedResource == null && !nonResourceReference)
            {
                // Check for variable name colors
                if (offset >= 7)
                {
                    if (document.GetText(offset - 7, 7) == "global.")
                    {
                        return new ColorVisualLineText(text, CurrentContext.VisualLine, length, GlobalBrush);
                    }
                }
                if (data.BuiltinList.Constants.ContainsKey(text))
                    return new ColorVisualLineText(text, CurrentContext.VisualLine, length, ConstantBrush);
                if (data.BuiltinList.GlobalNotArray.ContainsKey(text) ||
                    data.BuiltinList.Instance.ContainsKey(text) ||
                    data.BuiltinList.GlobalArray.ContainsKey(text))
                    return new ColorVisualLineText(text, CurrentContext.VisualLine, length, InstanceBrush);
                if (CurrentLocals?.Contains(text) == true)
                    return new ColorVisualLineText(text, CurrentContext.VisualLine, length, LocalBrush);
                return null;
            }

            ClickVisualLineText line = new(text, CurrentContext.VisualLine, length, isFunction ? FunctionBrush : ConstantBrush);
            if (isFunction)
            {
                // Make function references bold as well as a different color
                line.Bold = true;
            }
            if (namedResource is not null)
            {
                // Add click operation when we have a resource
                line.Clicked += (text, button) =>
                {
                    // TODO: Middle click?
                    if (button == MouseButton.Right)
                    {
                        contextMenu.Items.Clear();

                        MenuItem openMenuItem = new();
                        openMenuItem.Header = "Open";
                        openMenuItem.Click += (sender, _) =>
                        {
                            textEditor.TextArea.Focus();
                            codeViewModel.MainVM.TabOpen(namedResource, false);
                        };
                        contextMenu.Items.Add(openMenuItem);

                        MenuItem openInNewTabMenuItem = new();
                        openInNewTabMenuItem.Header = "Open in new tab";
                        openInNewTabMenuItem.Click += (sender, _) =>
                        {
                            textEditor.TextArea.Focus();
                            codeViewModel.MainVM.TabOpen(namedResource, true);
                        };
                        contextMenu.Items.Add(openInNewTabMenuItem);

                        codeViewModel.GMLFocused = false;
                        codeViewModel.ASMFocused = false;

                        contextMenu.Open(textEditor);
                    }
                };
            }

            return line;
        }
    }

    public class ColorVisualLineText : VisualLineText
    {
        private string Text { get; set; }
        private Brush? ForegroundBrush { get; set; }
        public bool Bold { get; set; } = false;

        public ColorVisualLineText(string text, VisualLine parentVisualLine, int length, Brush? foregroundBrush) : base(parentVisualLine, length)
        {
            Text = text;
            ForegroundBrush = foregroundBrush;
        }

        public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
        {
            if (ForegroundBrush != null)
                TextRunProperties.SetForegroundBrush(ForegroundBrush);
            if (Bold)
                TextRunProperties.SetTypeface(new Typeface(TextRunProperties.Typeface.FontFamily, FontStyle.Normal, FontWeight.Bold, FontStretch.Normal));
            return base.CreateTextRun(startVisualColumn, context);
        }

        protected override VisualLineText CreateInstance(int length)
        {
            return new ColorVisualLineText(Text, ParentVisualLine, length, null);
        }
    }

    public class ClickVisualLineText : VisualLineText
    {
        public delegate void ClickHandler(string text, MouseButton button);
        public event ClickHandler? Clicked;

        private string Text { get; set; }
        private Brush? ForegroundBrush { get; set; }
        public bool Bold { get; set; } = false;

        public ClickVisualLineText(string text, VisualLine parentVisualLine, int length, Brush? foregroundBrush = null) : base(parentVisualLine, length)
        {
            Text = text;
            ForegroundBrush = foregroundBrush;
        }

        public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
        {
            if (ForegroundBrush != null)
                TextRunProperties.SetForegroundBrush(ForegroundBrush);
            if (Bold)
                TextRunProperties.SetTypeface(new Typeface(TextRunProperties.Typeface.FontFamily, FontStyle.Normal, FontWeight.Bold, FontStretch.Normal));
            return base.CreateTextRun(startVisualColumn, context);
        }

        bool LinkIsClickable(PointerEventArgs e)
        {
            return !string.IsNullOrEmpty(Text) && e.KeyModifiers.HasFlag(KeyModifiers.Control);
        }

        protected override void OnQueryCursor(PointerEventArgs e)
        {
            if (LinkIsClickable(e))
            {
                e.Handled = true;
            }
        }

        protected override void OnPointerReleased(PointerEventArgs e)
        {
            if (e.Handled)
                return;

            MouseButton button = e.GetCurrentPoint(null).Properties.PointerUpdateKind.GetMouseButton();

            if ((button == MouseButton.Left && LinkIsClickable(e))
                || button == MouseButton.Middle
                || button == MouseButton.Right)
            {
                if (Clicked != null)
                {
                    Clicked(Text, button);
                    e.Handled = true;
                }
            }
        }

        protected override VisualLineText CreateInstance(int length)
        {
            ClickVisualLineText res = new(Text, ParentVisualLine, length);
            res.Clicked += Clicked;
            return res;
        }
    }
}

public interface IUndertaleCodeView
{
    private UndertaleCodeView View => (UndertaleCodeView)this;

    public void SaveCaretOffsets()
    {
        View.LastCaretOffsets = (View.GMLTextEditor.CaretOffset, View.ASMTextEditor.CaretOffset);
    }

    public void RestoreCaretOffsets()
    {
        View.GMLTextEditor.CaretOffset = Math.Clamp(View.LastCaretOffsets.Item1, 0, View.GMLTextEditor.Text.Length);
        View.ASMTextEditor.CaretOffset = Math.Clamp(View.LastCaretOffsets.Item2, 0, View.ASMTextEditor.Text.Length);
    }

    public int GMLCaretOffset
    {
        get { return View.GMLTextEditor.CaretOffset; }
        set { View.GMLTextEditor.CaretOffset = value; }
    }
}