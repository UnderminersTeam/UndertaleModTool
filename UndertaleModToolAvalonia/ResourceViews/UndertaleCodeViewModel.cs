using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleCodeViewModel : IUndertaleResourceViewModel
{
    public enum Tab
    {
        GML = 0,
        ASM = 1,
    }

    public IUndertaleCodeView? View;

    public MainViewModel MainVM;
    public UndertaleResource Resource => Code;
    public UndertaleCode Code { get; set; }

    [Notify]
    private Tab _SelectedTab;
    [Notify]
    private (Tab, int)? _LastGoToLocation = null;

    public TextDocument GMLTextDocument { get; set; } = new TextDocument();
    public TextDocument ASMTextDocument { get; set; } = new TextDocument();

    public bool IsCodeProcessing = false;

    public bool GMLOutdated = true;
    public bool ASMOutdated = true;

    public bool GMLFocused = false;
    public bool ASMFocused = false;

    ILoaderWindow? loaderWindow;
    IInputElement? lastFocusedElement;

    public UndertaleCodeViewModel(UndertaleCode code, IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();

        Code = code;

        DecompileAll();
    }

    public async Task<bool> DecompileToGML()
    {
        if (Code.ParentEntry is not null)
            return false;

        loaderWindow?.SetText("Decompiling to GML...");

        GlobalDecompileContext context = new(MainVM.Data);

        try
        {
            GMLTextDocument.Text = await Task.Run(() => new Underanalyzer.Decompiler.DecompileContext(context, Code, MainVM.Data!.ToolInfo.DecompilerSettings).DecompileToString());
            GMLOutdated = false;
        }
        catch (Underanalyzer.Decompiler.DecompilerException e)
        {
            loaderWindow?.EnsureShown();
            await MainVM.ShowMessageDialog(e.ToString(), title: "GML decompilation error");
            return false;
        }

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
            MessageWindow.Result undoChanges = await MainVM.ShowMessageDialog(result.PrintAllErrors(codeEntryNames: false)
                + "\n\nUndo changes?", title: "GML compilation error", ok: false, yes: true, no: true);
            if (undoChanges == MessageWindow.Result.Yes)
            {
                await DecompileToGML();
            }
            return false;
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
            await MainVM.ShowMessageDialog(e.ToString(), title: "ASM decompilation error");
            return false;
        }

        return true;
    }

    public async Task<bool> CompileFromASM()
    {
        if (Code.ParentEntry is not null)
            return false;

        loaderWindow?.SetText("Compiling from ASM...");

        try
        {
            string text = ASMTextDocument.Text;
            List<UndertaleInstruction> instructions = await Task.Run(() => Assembler.Assemble(text, MainVM.Data));
            Code.Replace(instructions);
        }
        catch (Exception e)
        {
            loaderWindow?.EnsureShown();
            MessageWindow.Result undoChanges = await MainVM.ShowMessageDialog(e.ToString()
                + "\n\nUndo changes?", title: "ASM compilation error", ok: false, yes: true, no: true);
            if (undoChanges == MessageWindow.Result.Yes)
            {
                await DecompileToASM();
            }

            return false;
        }

        return true;
    }

    void CodeProcessStart()
    {
        loaderWindow = MainVM.View!.LoaderOpen();

        IsCodeProcessing = true;

        View?.SaveCaretOffsets();
        lastFocusedElement = MainVM.View.GetFocusedElement();
        MainVM.IsEnabled = false;
    }

    void CodeProcessEnd()
    {
        loaderWindow!.Close();
        loaderWindow = null;

        IsCodeProcessing = false;

        MainVM.IsEnabled = true;
        lastFocusedElement?.Focus();
        View?.RestoreCaretOffsets();
    }

    public async void DecompileAll()
    {
        CodeProcessStart();

        await DecompileToGML();
        await DecompileToASM();

        CodeProcessEnd();
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
