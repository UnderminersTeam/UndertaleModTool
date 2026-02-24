using System;
using Avalonia.Controls;

namespace UndertaleModToolAvalonia;

public partial class MessageWindow : Window
{
    public string Message { get; set; } = "Message.";
    public string TitleText { get; set; } = "UndertaleModToolAvalonia";
    public bool HasOKButton { get; set; } = false;
    public bool HasYesButton { get; set; } = false;
    public bool HasNoButton { get; set; } = false;
    public bool HasCancelButton { get; set; } = false;

    public enum Buttons
    {
        None = 0,
        OK,
        YesNo,
        YesNoCancel,
        YesCancel,
    }

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
        Initialize();
    }

    public MessageWindow(string message, string? title = null, Buttons buttons = Buttons.OK)
    {
        Message = message;

        if (title is not null)
            TitleText = title;

        HasOKButton = buttons is Buttons.OK;
        HasYesButton = buttons is Buttons.YesNo or Buttons.YesNoCancel or Buttons.YesCancel;
        HasNoButton = buttons is Buttons.YesNo or Buttons.YesNoCancel;
        HasCancelButton = buttons is Buttons.YesCancel or Buttons.YesNoCancel;

        Initialize();
    }

    public void Initialize()
    {
        InitializeComponent();

        double frameHeight = (FrameSize is not null) ? (FrameSize!.Value.Height - ClientSize.Height) : 0;
        MaxHeight = (Screens.Primary?.WorkingArea.Height - frameHeight) ?? Double.PositiveInfinity;
    }

    public void OkClick()
    {
        Close(Result.OK);
    }

    public void YesClick()
    {
        Close(Result.Yes);
    }

    public void NoClick()
    {
        Close(Result.No);
    }

    public void CancelClick()
    {
        Close(Result.Cancel);
    }

    public async void Copy()
    {
        TopLevel topLevel = TopLevel.GetTopLevel(this)!;
        await topLevel.Clipboard!.SetTextAsync(Message);
    }
}