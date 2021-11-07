using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UndertaleModTool.Windows
{
    /// <summary>
    /// Interaction logic for ClickableTextOutput.xaml
    /// </summary>
    public partial class ClickableTextOutput : Window
    {
        public string Query { get; }
        public int ResultsCount { get; }
        private IDictionary<string, List<string>> resultsDict;
        private IEnumerable<string> failedList;
        
        public ClickableTextOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<string>>> resultsDict, IOrderedEnumerable<string> failedList = null)
        {
            InitializeComponent();

            Title = title;
            Query = query;
            ResultsCount = resultsCount;
            this.resultsDict = resultsDict.ToDictionary(x => x.Key, x => x.Value);
            this.failedList = failedList?.ToList();
        }
        public ClickableTextOutput(string title, string query, int resultsCount, IDictionary<string, List<string>> resultsDict, IEnumerable<string> failedList = null)
        {
            InitializeComponent();

            Title = title;
            Query = query;
            ResultsCount = resultsCount;
            this.resultsDict = resultsDict;
            this.failedList = failedList;
        }

        public void GenerateResults()
        {
            Dispatcher.Invoke(() => {
                FlowDocument doc = new();

                if (failedList is not null)
                {
                    int failedCount = failedList.Count();
                    if (failedCount > 0)
                    {
                        string errorStr = string.Empty;
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
            });
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Inline inline = (sender as Hyperlink).Inlines.FirstInline;
            string codeName = new TextRange(inline.ContentStart, inline.ContentEnd).Text;
            MessageBox.Show($"Clicked {codeName}");
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
    }
}
