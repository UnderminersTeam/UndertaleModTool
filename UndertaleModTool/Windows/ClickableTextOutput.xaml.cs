using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using System.Windows.Shapes;
using static UndertaleModTool.UndertaleCodeEditor;

namespace UndertaleModTool.Windows
{
    /// <summary>
    /// Interaction logic for ClickableTextOutput.xaml
    /// </summary>
    public partial class ClickableTextOutput : Window
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        private static ContextMenuDark linkContextMenu;

        public string Query { get; }
        public int ResultsCount { get; }

        private readonly IDictionary<string, List<(int lineNum, string codeLine)>> resultsDict;
        private readonly IEnumerable<string> failedList;
        private readonly CodeEditorTab editorTab;
        
        public ClickableTextOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> resultsDict, bool editorDecompile, IOrderedEnumerable<string> failedList = null)
        {
            #pragma warning disable CA1416
            InitializeComponent();

            linkContextMenu = FindResource("linkContextMenu") as ContextMenuDark;

            Title = title;
            Query = query;
            ResultsCount = resultsCount;
            this.resultsDict = resultsDict.ToDictionary(x => x.Key, x => x.Value);
            this.editorTab = editorDecompile ? CodeEditorTab.Decompiled : CodeEditorTab.Disassembly;
            this.failedList = failedList?.ToList();
            #pragma warning restore CA1416
        }
        public ClickableTextOutput(string title, string query, int resultsCount, IDictionary<string, List<(int lineNum, string codeLine)>> resultsDict, bool editorDecompile, IEnumerable<string> failedList = null)
        {
            #pragma warning disable CA1416
            InitializeComponent();

            linkContextMenu = FindResource("linkContextMenu") as ContextMenuDark;

            Title = title;
            Query = query;
            ResultsCount = resultsCount;
            this.resultsDict = resultsDict;
            this.editorTab = editorDecompile ? CodeEditorTab.Decompiled : CodeEditorTab.Disassembly;
            this.failedList = failedList;
            #pragma warning restore CA1416
        }
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }

        public void GenerateResults()
        {
            //(Not used because it has bad performance)
            /*MemoryStream docStream = new();
            ProcessResults(ref docStream);
            docStream.Seek(0, SeekOrigin.Begin);

            Dispatcher.Invoke(() =>
            {
                OutTextBox.Document = XamlReader.Load(docStream) as FlowDocument;
            });
            
            docStream.Dispose();*/

            FlowDocument doc = new();

            if (failedList is not null)
            {
                int failedCount = failedList.Count();
                if (failedCount > 0)
                {
                    string errorStr;
                    Paragraph errPara = new() { Foreground = Brushes.OrangeRed };
                    InlineCollection errLines = errPara.Inlines;

                    if (failedCount == 1)
                    {
                        errorStr = "There is 1 code entry that encountered an error while searching:";
                        errLines.Add(new Run(errorStr) { FontWeight = FontWeights.Bold });
                        errLines.Add(new LineBreak());
                        errLines.Add(new Run(failedList.First()));
                    }
                    else
                    {
                        errorStr = $"There are {failedCount} code entries that encountered an error while searching:";
                        errLines.Add(new Run(errorStr) { FontWeight = FontWeights.Bold });
                        errLines.Add(new LineBreak());

                        int i = 1;
                        foreach (string entry in failedList)
                        {
                            if (i < failedCount)
                            {
                                errLines.Add(new Run(entry + ','));
                                errLines.Add(new LineBreak());
                            }
                            else
                                errLines.Add(new Run(entry));

                            i++;
                        }
                    }
                    errLines.Add(new LineBreak());
                    errLines.Add(new LineBreak());

                    doc.Blocks.Add(errPara);
                }
            }

            int resCount = resultsDict.Count;
            Paragraph headerPara = new(new Run($"{ResultsCount} results in {resCount} code entries for \"{Query}\".")) { FontWeight = FontWeights.Bold };
            headerPara.Inlines.Add(new LineBreak());
            doc.Blocks.Add(headerPara);

            int totalLineCount = resultsDict.Select(x => x.Value.Count).Sum();
            bool tooManyLines = totalLineCount > 10000;
            if (tooManyLines)
                mainWindow.ShowWarning($"There are too many code lines to display ({totalLineCount}), so there would be no clickable line numbers.");

            foreach (KeyValuePair<string, List<(int lineNum, string codeLine)>> result in resultsDict)
            {
                int lineCount = result.Value.Count;
                Paragraph resPara = new();

                Underline resHeader = new();
                resHeader.Inlines.Add(new Run("Results in "));
                resHeader.Inlines.Add(new Hyperlink(new Run(result.Key)));
                resHeader.Inlines.Add(new Run(":"));
                resHeader.Inlines.Add(new LineBreak());
                resPara.Inlines.Add(resHeader);

                int i = 1;
                foreach (var (lineNum, codeLine) in result.Value)
                {
                    if (!tooManyLines)
                    {
                        Hyperlink lineLink = new(new Run($"Line {lineNum}")
                        {
                            Tag = result.Key // code entry name
                        });

                        resPara.Inlines.Add(lineLink);
                        resPara.Inlines.Add(new Run($": {codeLine}"));
                    }
                    else
                    {
                        Run lineRun = new($"Line {lineNum}: {codeLine}");

                        resPara.Inlines.Add(lineRun);
                    }

                    if (i < lineCount)
                        resPara.Inlines.Add(new LineBreak());

                    i++;
                }
                resPara.Inlines.Add(new LineBreak());

                doc.Blocks.Add(resPara);
            }

            OutTextBox.Document = doc;
        }

        public void FillingNotifier()
        {
            Thread.Sleep(500);

            double prevEnd = OutTextBox.ExtentHeight;

            while (true)
            {
                Thread.Sleep(500);

                bool done = Dispatcher.Invoke(delegate {
                    if (OutTextBox.ExtentHeight > prevEnd) //if length increased
                    {
                        if (FillingLabel.Visibility != Visibility.Visible)
                            FillingLabel.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        FillingLabel.Visibility = Visibility.Hidden;
                        return true;
                    }

                    prevEnd = OutTextBox.ExtentHeight;

                    return false;
                }, System.Windows.Threading.DispatcherPriority.Background);

                if (done)
                    break;
            }
        }

        private void OutTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (mainWindow is null)
                return;
            if (e.OriginalSource is not Run linkRun || linkRun.Parent is not Hyperlink
                || String.IsNullOrEmpty(linkRun.Text))
                return;

            if (linkRun.Text.StartsWith("Line "))
            {
                if (!Int32.TryParse(linkRun.Text[5..], out int lineNum))
                {
                    e.Handled = true;
                    return;
                }

                string codeName = linkRun.Tag as string;
                if (String.IsNullOrEmpty(codeName))
                {
                    e.Handled = true;
                    return;
                }
                    
                if (e.ChangedButton == MouseButton.Right && linkContextMenu is not null)
                {
                    linkContextMenu.DataContext = (lineNum, codeName);
                    linkContextMenu.IsOpen = true;
                }
                else
                    mainWindow.OpenCodeEntry(codeName, lineNum, editorTab, e.ChangedButton == MouseButton.Middle);
            }
            else
            {
                string codeName = linkRun.Text;
                if (e.ChangedButton == MouseButton.Right && linkContextMenu is not null)
                {
                    linkContextMenu.DataContext = (1, codeName);
                    linkContextMenu.IsOpen = true;
                }
                else
                    mainWindow.OpenCodeEntry(codeName, editorTab, e.ChangedButton == MouseButton.Middle);
            }

            e.Handled = true;
        }
        private void OpenInNewTabItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not ValueTuple<int, string> codeNamePair
                || String.IsNullOrEmpty(codeNamePair.Item2))
                return;

            mainWindow.OpenCodeEntry(codeNamePair.Item2, codeNamePair.Item1, editorTab, true);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void copyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string outText = OutTextBox.Selection.Text;

            if (outText.Length > 0)
                Clipboard.SetText(outText, TextDataFormat.Text);
        }

        private void copyAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string outText = new TextRange(OutTextBox.Document.ContentStart, OutTextBox.Document.ContentEnd).Text;

            if (outText.Length > 0)
                Clipboard.SetText(outText, TextDataFormat.Text);
        }

        private void OnCopyCommand(object sender, ExecutedRoutedEventArgs e)
        {
            copyMenuItem_Click(null, null);
        }
    }
}
