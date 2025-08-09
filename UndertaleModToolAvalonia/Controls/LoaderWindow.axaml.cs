using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace UndertaleModToolAvalonia.Controls;

public interface ILoaderWindow
{
    public void EnsureShown();
    void SetMessage(string message);
    void SetStatus(string status);
    void SetValue(int value);
    void SetMaximum(int maximum);
    void SetText(string text);
    void SetTextToMessageAndStatus(string status);
    void Close();
}

public partial class LoaderWindow : Window, ILoaderWindow
{
    public string TitleText { get; set; } = "UndertaleModToolAvalonia";

    int value;
    string? message;
    string? status;
    int maximum = -1;
    bool hasClosed = false;
    Window? showOwner;

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
        showOwner = owner;
        Task.Delay(100).ContinueWith(_ =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!hasClosed)
                    Show(owner);
            });
        });
    }

    public void EnsureShown()
    {
        if (showOwner is not null)
            Show(showOwner);
    }

    public void UpdateText()
    {
        MessageTextBlock.Text = $"{(!String.IsNullOrEmpty(message) ? message + " - " : "")}{value}/{maximum}{(!String.IsNullOrEmpty(status) ? ": " + status : "")}";
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
        this.value = value;
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