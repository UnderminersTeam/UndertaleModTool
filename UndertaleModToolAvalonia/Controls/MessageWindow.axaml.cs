using Avalonia.Controls;

namespace UndertaleModToolAvalonia.Controls;

// TODO: Add results
public partial class MessageWindow : Window
{
    public string Message { get; set; } = "Message.";
    public string TitleText { get; set; } = "UndertaleModToolAvalonia";
    public bool HasOKButton { get; set; } = false;
    public bool HasYesButton { get; set; } = false;
    public bool HasNoButton { get; set; } = false;
    public bool HasCancelButton { get; set; } = false;

    public enum Result
    {
        None = 0,
        OK,
        Yes,
        No,
        Cancel,
    }

    public MessageWindow()
    {
        InitializeComponent();
    }

    public MessageWindow(string message, string? title=null, bool ok=false, bool yes=false, bool no=false, bool cancel=false)
    {
        Message = message;

        if (title is not null)
            TitleText = title;

        HasOKButton = ok;
        HasYesButton = yes;
        HasNoButton = no;
        HasCancelButton = cancel;

        InitializeComponent();
    }

    public void OkClick()
    {
        Close(Result.OK);
    }
}