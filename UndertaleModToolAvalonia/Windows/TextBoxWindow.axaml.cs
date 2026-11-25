using Avalonia.Controls;

namespace UndertaleModToolAvalonia;

public partial class TextBoxWindow : Window
{
    public string Message { get; set; } = "Message.";
    public string TitleText { get; set; } = "UndertaleModToolAvalonia v" + App.VersionString;

    public TextBoxWindow() { }

    public TextBoxWindow(string message, string text = "", string? title = null, bool isMultiline = false, bool isReadOnly = false,
        string? okText = null, string? cancelText = null, bool preventClose = false)
    {
        Message = message;

        if (title is not null)
            TitleText = title;

        InitializeComponent();

        TextTextBox.Text = text;
        TextTextBox.IsReadOnly = isReadOnly;
        TextTextBox.AcceptsReturn = isMultiline;

        if (okText is not null)
            OkButton.Content = okText;
        if (cancelText is not null)
            CancelButton.Content = cancelText;

        // The window can then only be closed by submitting.
        if (preventClose)
        {
            CancelButton.IsVisible = false;
            Closing += (_, e) =>
            {
                if (!e.IsProgrammatic)
                    e.Cancel = true;
            };
        }

        Loaded += (_, __) =>
        {
            if (!isReadOnly)
                TextTextBox.Focus();
        };
    }

    public void OkClick()
    {
        Close(TextTextBox.Text);
    }

    public void CancelClick()
    {
        Close(null);
    }
}