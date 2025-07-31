using System;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleExtensionViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => Extension;
    public UndertaleExtension Extension { get; set; }

    [Notify]
    private UndertaleExtensionFile? _FilesSelected;
    [Notify]
    private UndertaleExtensionFunction? _FunctionsSelected;

    public UndertaleExtensionViewModel(UndertaleExtension extension, IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();

        Extension = extension;
    }

    public void FilesSelectedChanged(object? item)
    {
        FilesSelected = (UndertaleExtensionFile?)item!;
    }

    public void FunctionsSelectedChanged(object? item)
    {
        FunctionsSelected = (UndertaleExtensionFunction?)item!;
    }

    public UndertaleExtensionFile CreateExtensionFile()
    {
        return new()
        {
            Filename = MainVM.Data!.Strings.MakeString($"NewExtensionFile{Extension.Files.Count}.dll", createNew: true),
            Kind = UndertaleExtensionKind.Dll,
            Functions = [],
        };
    }

    public UndertaleExtensionFunction CreateExtensionFunction()
    {
        return new()
        {
            Name = MainVM.Data!.Strings.MakeString($"new_extension_function_{FilesSelected!.Functions.Count}", createNew: true),
            ID = MainVM.Data!.ExtensionFindLastId(),
            Kind = 11, // TODO: Probably find out what this is
            RetType = UndertaleExtensionVarType.Double,
            ExtName = MainVM.Data!.Strings.MakeString($"new_extension_function_{FilesSelected!.Functions.Count}_ext", createNew: true),
            Arguments = [],
        };
    }

    public static UndertaleExtensionFunctionArg CreateExtensionFunctionArg() => new();

    public UndertaleExtensionOption CreateExtensionOption()
    {
        return new()
        {
            Name = MainVM.Data!.Strings.MakeString($"extensionOption{Extension.Options.Count}", createNew: true),
            Value = MainVM.Data!.Strings.MakeString("", createNew: true),
        };
    }
}
