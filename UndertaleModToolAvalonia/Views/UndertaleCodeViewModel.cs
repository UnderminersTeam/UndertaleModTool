using System;
using AvaloniaEdit.Document;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleCodeViewModel : IUndertaleResourceViewModel
{
    // TODO: A billion things. Syntax highlighting.
    public MainViewModel MainVM;
    public UndertaleResource Resource => Code;
    public UndertaleCode Code { get; set; }

    public TextDocument GMLTextDocument { get; set; } = new TextDocument();
    public TextDocument ASMTextDocument { get; set; } = new TextDocument();

    public UndertaleCodeViewModel(UndertaleCode code, IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();

        Code = code;

        DecompileToGML();
        DecompileToASM();
    }

    public void DecompileToGML()
    {
        if (Code.ParentEntry is not null)
            return;

        // TODO: Decompiler settings
        GlobalDecompileContext context = new(MainVM.Data);
        GMLTextDocument.Text = new Underanalyzer.Decompiler.DecompileContext(context, Code).DecompileToString();
    }

    public async void CompileFromGML()
    {
        if (Code.ParentEntry is not null)
            return;

        CompileGroup group = new(MainVM.Data);
        // TODO: MainThreadAction
        group.QueueCodeReplace(Code, GMLTextDocument.Text);
        CompileResult result = group.Compile();

        if (!result.Successful)
        {
            await MainVM.ShowMessageDialog(result.PrintAllErrors(codeEntryNames: false), title: "GML compilation error", ok: true);
        }
    }

    public void DecompileToASM()
    {
        if (Code.ParentEntry is not null)
            return;

        ASMTextDocument.Text = Code.Disassemble(MainVM.Data!.Variables, MainVM.Data!.CodeLocals?.For(Code));
    }

    public async void CompileFromASM()
    {
        if (Code.ParentEntry is not null)
            return;

        try
        {
            Code.Replace(Assembler.Assemble(ASMTextDocument.Text, MainVM.Data));
        }
        catch (Exception e)
        {
            await MainVM.ShowMessageDialog(e.ToString(), title: "ASM compilation error", ok: true);
        }
    }
}
