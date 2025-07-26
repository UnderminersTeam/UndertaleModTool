using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace UndertaleModToolAvalonia.Controls;

public partial class LoaderWindow : Window
{
    public string TitleText { get; set; } = "UndertaleModToolAvalonia";
    public int Value;

    private string? message;
    private string? status;
    private int maximum = -1;
    bool hasClosed = false;

    public LoaderWindow()
    {
        Initialize();
    }

    public void Initialize()
    {
        InitializeComponent();

        Closing += (object? sender, WindowClosingEventArgs e) =>
        {
            if (!e.IsProgrammatic)
                e.Cancel = true;
            else
                hasClosed = true;
        };
    }

    public void ShowDelayed(Window owner)
    {
        Task.Delay(100).ContinueWith(_ =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!hasClosed)
                    Show(owner);
            });
        });
    }

    public void UpdateText()
    {
        MessageTextBlock.Text = $"{(!String.IsNullOrEmpty(message) ? message + " " : "")} - {Value}/{maximum}{(!String.IsNullOrEmpty(status) ? ": " + status : "")}";
    }

    public void SetMessage(string message)
    {
        this.message = message;
        UpdateText();
    }

    public void SetStatus(string status)
    {
        this.status = status;
        UpdateText();
    }

    public void SetValue(int value)
    {
        this.Value = value;
        LoadingProgressBar.Value = value;
        UpdateText();
    }

    public void SetMaximum(int maximum)
    {
        this.maximum = maximum;
        LoadingProgressBar.IsIndeterminate = false;
        LoadingProgressBar.Maximum = maximum;
        UpdateText();
    }

    public void SetText(string text)
    {
        MessageTextBlock.Text = text;
    }

    public void SetTextToMessageAndStatus(string status)
    {
        MessageTextBlock.Text = $"{(!String.IsNullOrEmpty(message) ? message + " " : "")} - {status}";
    }
}