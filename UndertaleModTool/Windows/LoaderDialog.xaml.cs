using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy LoaderDialog.xaml
    /// </summary>
    public partial class LoaderDialog : Window, INotifyPropertyChanged
    {

        private string _Message;

        public string MessageTitle { get; set; }
        public string Message { get => _Message; set { _Message = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Message")); } }
        public bool PreventClose { get; set; }

        public string StatusText { get; set; } = "Please wait...";
        public double? Maximum
        {
            get
            {
                return !ProgressBar.IsIndeterminate ? ProgressBar.Maximum : (double?)null;
            }

            set
            {
                ProgressBar.IsIndeterminate = !value.HasValue;
                if (value.HasValue)
                    ProgressBar.Maximum = value.Value;
            }
        }

        private DebugTraceListener listener;

        public event PropertyChangedEventHandler PropertyChanged;

        public LoaderDialog(string title, string msg)
        {
            MessageTitle = title;
            Message = msg;

            InitializeComponent();
            this.DataContext = this;

            listener = new DebugTraceListener(this);
            Debug.Listeners.Add(listener);
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.Listeners.Remove(listener);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = this.PreventClose;
        }

        public void TryHide()
        {
            Dispatcher.Invoke(() =>
            {
                if (IsVisible)
                {
                    this.PreventClose = false;
                    Hide();
                }
            });
        }

        public void ReportProgress(string message)
        {
            StatusText = message;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StatusText"));
        }

        public void ReportProgress(string message, double value)
        {
            Dispatcher.Invoke(() =>
            {
                ReportProgress(value + "/" + Maximum + (!String.IsNullOrEmpty(message) ? ": " + message : ""));
                ProgressBar.Value = value;
            });
        }

        public void Update(string message, string status, double progressValue, double maxValue) {
            if (!IsVisible)
                Dispatcher.Invoke(Show);

            if (message != null)
                Dispatcher.Invoke(() => Message = message);

            if (maxValue != 0)
                Dispatcher.Invoke(() => Maximum = maxValue);

            ReportProgress(status, progressValue);
        }

        private class DebugTraceListener : TraceListener
        {
            private LoaderDialog loaderDialog;

            public DebugTraceListener(LoaderDialog loaderDialog)
            {
                this.loaderDialog = loaderDialog;
            }

            public override void Write(string message)
            {
                WriteLine(message);
            }

            public override void WriteLine(string message)
            {
                loaderDialog.ReportProgress(message);
            }
        }
    }
}
