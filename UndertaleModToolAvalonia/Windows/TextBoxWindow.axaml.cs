using Avalonia.Controls;

namespace UndertaleModToolAvalonia;

public partial class TextBoxWindow : Window
{
    public string Message { get; set; } = "Message.";
    public string TitleText { get; set; } = "UndertaleModToolAvalonia";

    public TextBoxWindow(string message, string text = "", string? title = null, bool isMultiline = false, bool isReadOnly = false)
    {
        Message = message;

        if (title is not null)
            TitleText = title;

        InitializeComponent();

        TextTextBox.Text = text;
        TextTextBox.IsReadOnly = isReadOnly;
        TextTextBox.AcceptsReturn = isMultiline;

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