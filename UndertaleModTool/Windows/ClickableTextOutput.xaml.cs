﻿using System;
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
using static UndertaleModTool.MainWindow;

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

        private IDictionary<string, List<string>> resultsDict;
        private IEnumerable<string> failedList;
        private CodeEditorMode editorDecompile;
        
        public ClickableTextOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<string>>> resultsDict, bool editorDecompile, IOrderedEnumerable<string> failedList = null)
        {
            InitializeComponent();

            linkContextMenu = FindResource("linkContextMenu") as ContextMenuDark;

            Title = title;
            Query = query;
            ResultsCount = resultsCount;
            this.resultsDict = resultsDict.ToDictionary(x => x.Key, x => x.Value);
            this.editorDecompile = editorDecompile ? CodeEditorMode.Decompile : CodeEditorMode.DontDecompile;
            this.failedList = failedList?.ToList();
        }
        public ClickableTextOutput(string title, string query, int resultsCount, IDictionary<string, List<string>> resultsDict, bool editorDecompile, IEnumerable<string> failedList = null)
        {
            InitializeComponent();

            linkContextMenu = FindResource("linkContextMenu") as ContextMenuDark;

            Title = title;
            Query = query;
            ResultsCount = resultsCount;
            this.resultsDict = resultsDict;
            this.editorDecompile = editorDecompile ? CodeEditorMode.Decompile : CodeEditorMode.DontDecompile;
            this.failedList = failedList;
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

            foreach (KeyValuePair<string, List<string>> result in resultsDict)
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
                foreach (string line in result.Value)
                {
                    resPara.Inlines.Add(new Run(line));

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
            if (e.OriginalSource is not Run linkRun || linkRun.Parent is not Hyperlink)
                return;

            string codeName = linkRun.Text;
            if (e.ChangedButton == MouseButton.Right && linkContextMenu is not null)
            {
                linkContextMenu.DataContext = codeName;
                linkContextMenu.IsOpen = true;
            }
            else
                mainWindow.OpenCodeFile(codeName, editorDecompile, e.ChangedButton == MouseButton.Middle);

            e.Handled = true;
        }
        private void OpenInNewTabItem_Click(object sender, RoutedEventArgs e)
        {
            string codeName = (sender as FrameworkElement)?.DataContext as string;
            if (String.IsNullOrEmpty(codeName))
                return;

            mainWindow.OpenCodeFile(codeName, editorDecompile, true);
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
