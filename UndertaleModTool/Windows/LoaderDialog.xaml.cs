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
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public partial class LoaderDialog : Window
    {
        public string MessageTitle { get; set; }
        public string Message { get; set; }
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
        public bool IsClosed { get; set; } = false;

        public LoaderDialog(string title, string msg)
        {
            MessageTitle = title;
            Message = msg;

            InitializeComponent();
            this.DataContext = this;

            (Application.Current.MainWindow as MainWindow).FileMessageEvent += ReportProgress;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = this.PreventClose;
        }

        protected override void OnClosed(EventArgs e)
        {
            IsClosed = true;
            base.OnClosed(e);
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

        public void TryClose()
        {
            PreventClose = false;
            Dispatcher.Invoke(() =>
            {
                if (!IsClosed)
                {
                    Close();
                }
            });
        }

        public void TryShowDialog()
        {
            Dispatcher.Invoke(() =>
            {
                if (!IsClosed)
                {
                    ShowDialog();
                }
            });
        }

        public void ReportProgress(string message)
        {
            StatusText = message;
        }

        public void ReportProgress(string message, double value)
        {
            Dispatcher.Invoke(() =>
            {
                ReportProgress(value + "/" + Maximum + (!String.IsNullOrEmpty(message) ? ": " + message : ""));
                ProgressBar.Value = value;
            });
        }

        public void Update(string message, string status, double progressValue, double maxValue)
        {
            if (!IsVisible)
                Dispatcher.Invoke(Show);

            if (message != null)
                Dispatcher.Invoke(() => Message = message);

            if (maxValue != 0)
                Dispatcher.Invoke(() => Maximum = maxValue);

            ReportProgress(status, progressValue);
        }
        public void Update(string status)
        {
            if (!IsVisible)
                Dispatcher.Invoke(Show);

            ReportProgress(status);
        }
    }
}
