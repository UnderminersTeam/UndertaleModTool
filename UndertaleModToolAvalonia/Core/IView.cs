using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Platform.Storage;

namespace UndertaleModToolAvalonia;

public interface IView
{
    private Control View => (Control)this;

    public async Task<IReadOnlyList<IStorageFile>> OpenFileDialog(FilePickerOpenOptions options)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(View)!;
        return await topLevel.StorageProvider.OpenFilePickerAsync(options);
    }

    public async Task<IStorageFile?> SaveFileDialog(FilePickerSaveOptions options)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(View)!;
        return await topLevel.StorageProvider.SaveFilePickerAsync(options);
    }

    public async Task<IReadOnlyList<IStorageFolder>> OpenFolderDialog(FolderPickerOpenOptions options)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(View)!;
        return await topLevel.StorageProvider.OpenFolderPickerAsync(options);
    }

    public async Task<bool> LaunchUriAsync(Uri uri)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(View)!;
        return await topLevel.Launcher.LaunchUriAsync(uri);
    }

    public async Task<MessageWindow.Result> MessageDialog(string message, string? title = null, MessageWindow.Buttons buttons = MessageWindow.Buttons.OK)
    {
        Window window = View.FindLogicalAncestorOfType<Window>() ?? throw new InvalidOperationException("Window not found");
        return await new MessageWindow(message, title, buttons).ShowDialog<MessageWindow.Result>(window);
    }

    public async Task<string?> TextBoxDialog(string message, string text = "", string? title = null, bool isMultiline = false, bool isReadOnly = false,
        string? okText = null, string? cancelText = null, bool preventClose = false)
    {
        Window window = View.FindLogicalAncestorOfType<Window>() ?? throw new InvalidOperationException("Window not found");
        return await new TextBoxWindow(message, text, title, isMultiline, isReadOnly, okText, cancelText, preventClose).ShowDialog<string?>(window);
    }

    /// <summary>
    /// Shows a text box window without waiting for it to close.
    /// </summary>
    public void TextBoxShow(string message, string text = "", string? title = null, bool isMultiline = false)
    {
        Window window = View.FindLogicalAncestorOfType<Window>() ?? throw new InvalidOperationException("Window not found");
        new TextBoxWindow(message, text, title, isMultiline).Show(window);
    }

    public ILoaderWindow LoaderOpen()
    {
        Window window = View.FindLogicalAncestorOfType<Window>(true) ?? throw new InvalidOperationException("Window not found");
        LoaderWindow loaderWindow = new();
        loaderWindow.ShowDelayed(window);
        return loaderWindow;
    }

    public IInputElement? GetFocusedElement()
    {
        TopLevel topLevel = TopLevel.GetTopLevel(View)!;
        return topLevel.FocusManager?.GetFocusedElement();
    }
}