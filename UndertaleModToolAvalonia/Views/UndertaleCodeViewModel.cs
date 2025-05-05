using System;
using AvaloniaEdit.Document;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleCodeViewModel : ViewModelBase
{
    // TODO: A billion things. Syntax highlighting.
    public MainViewModel MainVM;
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
        GlobalDecompileContext context = new(MainVM.Data);
        GMLTextDocument.Text = new Underanalyzer.Decompiler.DecompileContext(context, Code).DecompileToString();
    }

    public void CompileFromGML()
    {
        CompileGroup group = new(MainVM.Data);
        group.QueueCodeReplace(Code, GMLTextDocument.Text);
        CompileResult result = group.Compile();

        if (!result.Successful)
        {
            // Show errors
        }
    }

    public void DecompileToASM()
    {
        ASMTextDocument.Text = Code.Disassemble(MainVM.Data!.Variables, MainVM.Data!.CodeLocals?.For(Code));
    }

    public void CompileFromASM()
    {
        Code.Replace(Assembler.Assemble(ASMTextDocument.Text, MainVM.Data));
    }
}
