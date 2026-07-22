using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleCodeViewModel : ObservableObject, IUndertaleResourceViewModel
{
    public enum Tab
    {
        GML = 0,
        ASM = 1,
    }

    public IUndertaleCodeView? View;

    public MainViewModel MainVM;
    public UndertaleResource Resource => Code;
    public UndertaleCode Code { get; }

    [ObservableProperty]
    public partial Tab SelectedTab { get; set; }

    [ObservableProperty]
    public partial (Tab Tab, int Line, int Column)? LastGoToLocation { get; set; } = null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnyOutdated))]
    public partial bool GMLOutdated { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnyOutdated))]
    public partial bool ASMOutdated { get; set; } = false;

    public bool AnyOutdated => GMLOutdated || ASMOutdated;

    public TextDocument GMLTextDocument { get; set; } = new TextDocument();
    public TextDocument ASMTextDocument { get; set; } = new TextDocument();

    public bool IsCodeProcessing = false;

    public bool GMLFocused = false;
    public bool ASMFocused = false;

    ILoaderWindow? loaderWindow;
    IInputElement? lastFocusedElement;

    public UndertaleCodeViewModel(UndertaleCode code, IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();

        Code = code;

        DecompileAll();
    }

    public async Task<bool> DecompileToGML()
    {
        if (Code.ParentEntry is not null)
            return false;

        loaderWindow?.SetText("Decompiling to GML...");

        string text;

        if (MainVM.Project is null || !MainVM.Project.TryGetCodeSource(Code, out text))
        {
            GlobalDecompileContext context = new(MainVM.Data);

            try
            {
                text = await Task.Run(() => new Underanalyzer.Decompiler.DecompileContext(context, Code, MainVM.Data!.ToolInfo.DecompilerSettings).DecompileToString());
            }
            catch (Underanalyzer.Decompiler.DecompilerException e)
            {
                loaderWindow?.EnsureShown();
                await MainVM.View!.MessageDialog(e.ToString(), title: "GML decompilation error - UndertaleModTooAvalonia v" + App.VersionString);
                return false;
            }
        }

        GMLTextDocument.Text = text;
        GMLOutdated = false;

        return true;
    }

    public async Task<bool> CompileFromGML()
    {
        if (Code.ParentEntry is not null)
            return false;

        loaderWindow?.SetText("Compiling from GML...");

        CompileGroup group = new(MainVM.Data);
        group.MainThreadAction = Dispatcher.UIThread.Invoke;
        group.QueueCodeReplace(Code, GMLTextDocument.Text);
        CompileResult result = await Task.Run(() => group.Compile());

        if (!result.Successful)
        {
            loaderWindow?.EnsureShown();
            MessageWindow.Result undoChanges = await MainVM.View!.MessageDialog(result.PrintAllErrors(codeEntryNames: false)
                + "\n\nUndo changes?",
                title: "GML compilation error - UndertaleModTooAvalonia v" + App.VersionString,
                buttons: MessageWindow.Buttons.YesNo);
            if (undoChanges == MessageWindow.Result.Yes)
            {
                await DecompileToGML();
            }
            return false;
        }

        if (MainVM.Project is not null)
        {
            MainVM.Project.UpdateCodeSource(Code, GMLTextDocument.Text);
        }

        return true;
    }

    public async Task<bool> DecompileToASM()
    {
        if (Code.ParentEntry is not null)
            return false;

        loaderWindow?.SetText("Decompiling from ASM...");

        try
        {
            ASMTextDocument.Text = await Task.Run(() => Code.Disassemble(MainVM.Data!.Variables, MainVM.Data!.CodeLocals?.For(Code)));
            ASMOutdated = false;
        }
        catch (Exception e)
        {
            loaderWindow?.EnsureShown();
            await MainVM.View!.MessageDialog(e.ToString(), title: "ASM decompilation error - UndertaleModTooAvalonia v" + App.VersionString);
            return false;
        }

        return true;
    }

    public async Task<bool> CompileFromASM()
    {
        if (Code.ParentEntry is not null)
            return false;

        loaderWindow?.SetText("Compiling from ASM...");

        if (MainVM.Project is not null && MainVM.Project.TryGetCodeSource(Code, out _))
        {
            // The user really shouldn't be editing disassembly - warn them about this in detail
            loaderWindow?.EnsureShown();
            await MainVM.View!.MessageDialog("Editing disassembly while in an open project (even through scripts) can cause " +
                "desyncs with source code in the project.\n\n" +
                "The source code will not change unless you directly modify it, " +
                "or if you remove the code asset from the project entirely.");
        }

        try
        {
            string text = ASMTextDocument.Text;
            List<UndertaleInstruction> instructions = await Task.Run(() => Assembler.Assemble(text, MainVM.Data));
            Code.Replace(instructions);
        }
        catch (Exception e)
        {
            loaderWindow?.EnsureShown();
            MessageWindow.Result undoChanges = await MainVM.View!.MessageDialog(e.ToString()
                + "\n\nUndo changes?",
                title: "ASM compilation error - UndertaleModTooAvalonia v" + App.VersionString,
                buttons: MessageWindow.Buttons.YesNo);
            if (undoChanges == MessageWindow.Result.Yes)
            {
                await DecompileToASM();
            }

            return false;
        }

        return true;
    }

    public void GoToLocation(Tab tab, int lineNumber, int columnNumber)
    {
        LastGoToLocation = (tab, lineNumber, columnNumber);
    }

    void CodeProcessStart()
    {
        loaderWindow = MainVM.View!.LoaderOpen();

        IsCodeProcessing = true;

        View?.SaveCaretPosition();
        lastFocusedElement = MainVM.View.GetFocusedElement();
    }

    void CodeProcessEnd()
    {
        loaderWindow!.Close();
        loaderWindow = null;

        IsCodeProcessing = false;

        lastFocusedElement?.Focus();
        View?.RestoreCaretPosition();
    }

    public async void DecompileAll()
    {
        CodeProcessStart();

        await DecompileToGML();
        await DecompileToASM();

        CodeProcessEnd();

        View?.ProcessLastGoToLocation();
    }

    public void CompileAndDecompileCurrent()
    {
        switch (SelectedTab)
        {
            case Tab.GML:
                CompileAndDecompileGML(false);
                break;
            case Tab.ASM:
                CompileAndDecompileASM(false);
                break;
        }
    }

    public void CompileAndDecompileGML() => CompileAndDecompileGML(false);

    public async void CompileAndDecompileGML(bool onlyIfOutdated)
    {
        if (!IsCodeProcessing && (onlyIfOutdated ? GMLOutdated : true))
        {
            CodeProcessStart();

            if (await CompileFromGML())
            {
                await DecompileToGML();
                await DecompileToASM();
            }

            CodeProcessEnd();
        }
    }

    public void CompileAndDecompileASM() => CompileAndDecompileASM(false);

    public async void CompileAndDecompileASM(bool onlyIfOutdated)
    {
        if (!IsCodeProcessing && (onlyIfOutdated ? ASMOutdated : true))
        {
            CodeProcessStart();

            if (await CompileFromASM())
            {
                await DecompileToGML();
                await DecompileToASM();
            }

            CodeProcessEnd();
        }
    }
}
