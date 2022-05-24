﻿using System;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

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
        public string SavedStatusText { get; set; }
        public double? Maximum
        {
            get
            {
                return !ProgressBar.IsIndeterminate ? ProgressBar.Maximum : null;
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
                try
                {
                    if (IsVisible)
                    {
                        this.PreventClose = false;
                        Hide();
                    }
                }
                catch
                {
                    // Silently fail...
                }
            });
        }

        public void TryClose()
        {
            PreventClose = false;
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (!IsClosed)
                    {
                        Close();
                    }
                }
                catch
                {
                    // Silently fail...
                }
            });
        }

        public void TryShowDialog()
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (!IsClosed)
                    {
                        ShowDialog();
                    }
                }
                catch
                {
                    // Silently fail...
                }
            });
        }

        public void ReportProgress(string status)
        {
            StatusText = status;
        }
        public void ReportProgress(double value) //update without status text changing
        {
            try
            {
                ReportProgress(value + "/" + Maximum + (!String.IsNullOrEmpty(SavedStatusText) ? ": " + SavedStatusText : ""));
                UpdateValue(value);
            }
            catch
            {
                //Silently fail...
            }
        }
        public void UpdateValue(double value)
        {
            if (value - ProgressBar.Value <= 1) //if value not changed or changed by 1
            {
                ProgressBar.Value = value;
            }
            else
            {
                DoubleAnimation animation = new(value, TimeSpan.FromMilliseconds(100)); //time is the same as in "ProgressUpdater()"
                ProgressBar.BeginAnimation(ProgressBar.ValueProperty, animation);       //smooth progress change
            }
        }

        public void ReportProgress(string status, double value)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    ReportProgress(value + "/" + Maximum + (!String.IsNullOrEmpty(status) ? ": " + status : "")); //update status
                    UpdateValue(value);
                }
                catch
                {
                    // Silently fail...
                }
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
