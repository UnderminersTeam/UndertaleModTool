using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Controls;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleCodeViewModel : IUndertaleResourceViewModel
{
    public enum Tab
    {
        GML = 0,
        ASM = 1,
    }

    // TODO: A billion things. Syntax highlighting.
    public MainViewModel MainVM;
    public UndertaleResource Resource => Code;
    public UndertaleCode Code { get; set; }

    [Notify]
    private Tab _SelectedTab;
    [Notify]
    private (Tab, int)? _LastGoToLocation = null;

    public TextDocument GMLTextDocument { get; set; } = new TextDocument();
    public TextDocument ASMTextDocument { get; set; } = new TextDocument();

    public bool IsCompilingOrDecompiling = false;

    public bool GMLOutdated = true;
    public bool ASMOutdated = true;

    public UndertaleCodeViewModel(UndertaleCode code, IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();

        Code = code;

        DecompileToGML().Wait();
        DecompileToASM().Wait();
    }

    public async Task<bool> DecompileToGML()
    {
        if (Code.ParentEntry is not null)
            return false;

        // TODO: Decompiler settings
        GlobalDecompileContext context = new(MainVM.Data);

        try
        {
            GMLTextDocument.Text = new Underanalyzer.Decompiler.DecompileContext(context, Code).DecompileToString();
            GMLOutdated = false;
        }
        catch (Underanalyzer.Decompiler.DecompilerException e)
        {
            await MainVM.ShowMessageDialog(e.ToString(), title: "GML decompilation error", ok: true);
            return false;
        }

        return true;
    }

    public async Task<bool> CompileFromGML()
    {
        if (!GMLOutdated)
            return false;
        if (Code.ParentEntry is not null)
            return false;

        CompileGroup group = new(MainVM.Data);
        group.MainThreadAction = Dispatcher.UIThread.Invoke;
        group.QueueCodeReplace(Code, GMLTextDocument.Text);
        CompileResult result = group.Compile();

        if (!result.Successful)
        {
            MessageWindow.Result undoChanges = await MainVM.ShowMessageDialog(result.PrintAllErrors(codeEntryNames: false)
                + "\n\nUndo changes?", title: "GML compilation error", yes: true, no: true);
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

        try
        {
            ASMTextDocument.Text = Code.Disassemble(MainVM.Data!.Variables, MainVM.Data!.CodeLocals?.For(Code));
            ASMOutdated = false;
        }
        catch (Exception e)
        {
            await MainVM.ShowMessageDialog(e.ToString(), title: "ASM decompilation error", ok: true);
            return false;
        }

        return true;
    }

    public async Task<bool> CompileFromASM()
    {
        if (!ASMOutdated)
            return false;
        if (Code.ParentEntry is not null)
            return false;

        try
        {
            Code.Replace(Assembler.Assemble(ASMTextDocument.Text, MainVM.Data));
        }
        catch (Exception e)
        {
            MessageWindow.Result undoChanges = await MainVM.ShowMessageDialog(e.ToString()
                + "\n\nUndo changes?", title: "ASM compilation error", yes: true, no: true);
            if (undoChanges == MessageWindow.Result.Yes)
            {
                await DecompileToASM();
            }

            return false;
        }

        return true;
    }

    public async void CompileAndDecompileGML()
    {
        if (IsCompilingOrDecompiling)
            return;

        IsCompilingOrDecompiling = true;

        if (await CompileFromGML())
        {
            await DecompileToGML();
            await DecompileToASM();
        }

        IsCompilingOrDecompiling = false;
    }

    public async void CompileAndDecompileASM()
    {
        if (IsCompilingOrDecompiling)
            return;

        IsCompilingOrDecompiling = true;

        if (await CompileFromASM())
        {
            await DecompileToGML();
            await DecompileToASM();
        }

        IsCompilingOrDecompiling = false;
    }
}
