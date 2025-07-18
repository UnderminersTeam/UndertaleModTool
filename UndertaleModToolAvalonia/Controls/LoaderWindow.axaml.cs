using Avalonia.Controls;

namespace UndertaleModToolAvalonia.Controls;

public partial class LoaderWindow : Window
{
    public string TitleText { get; set; } = "UndertaleModToolAvalonia";
    
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
        };
    }

    public void SetMessage(string message)
    {
        MessageTextBlock.Text = message;
    }
}