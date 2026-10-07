using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public enum TabState
    {
        Ok,
        NeedsCompile,
        NeedsDecompile,
        Error,
    }

    public MainViewModel MainVM;
    public UndertaleResource Resource => Code;
    public UndertaleCode Code { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCompiled))]
    public partial Tab SelectedTab { get; set; }

    [ObservableProperty]
    public partial (Tab Tab, int Line, int Column)? LastGoToLocation { get; set; } = null;

    [ObservableProperty]
    public partial TextDocument? GMLTextDocument { get; set; }

    [ObservableProperty]
    public partial TextDocument? ASMTextDocument { get; set; }

    [ObservableProperty]
    public partial bool IsCodeProcessing { get; set; } = false;

    public bool IsCompiled
    {
        get
        {
            return SelectedTab switch
            {
                Tab.GML => GMLTabState,
                Tab.ASM => ASMTabState,
                _ => throw new NotImplementedException(),
            } is TabState.Ok;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCompiled))]
    public partial TabState GMLTabState { get; set; } = TabState.NeedsDecompile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCompiled))]
    public partial TabState ASMTabState { get; set; } = TabState.NeedsDecompile;

    public bool GMLFocused = false;
    public bool ASMFocused = false;

    ILoaderWindow? loaderWindow;
    IInputElement? lastFocusedElement;

    readonly TaskQueue taskQueue = new();
    readonly TaskCompletionSource gmlReady = new();
    readonly TaskCompletionSource asmReady = new();

    public UndertaleCodeViewModel(UndertaleCode code, IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();

        Code = code;

        taskQueue.Add(() => gmlReady.Task);
        taskQueue.Add(() => asmReady.Task);
    }

    async Task<bool> ITabContent.OnSave()
    {
        return await DoCodeProcess(async () =>
        {
            if (GetTabState(SelectedTab) is TabState.NeedsCompile)
            {
                await CompileFromTab(SelectedTab);

                if (GetTabState(SelectedTab) is TabState.Error)
                {
                    return false;
                }

                if (GetTabState(SelectedTab) is TabState.NeedsDecompile)
                {
                    await DecompileToTab(SelectedTab);
                }
            }

            return true;
        });
    }

    public void CompileAndDecompileCurrent() => CompileAndDecompileTab(SelectedTab, force: true);

    public void CompileAndDecompileGML() => CompileAndDecompileTab(Tab.GML, force: true);

    public void CompileAndDecompileASM() => CompileAndDecompileTab(Tab.ASM, force: true);

    public void GoToLocation(Tab tab, int lineNumber, int columnNumber)
    {
        LastGoToLocation = (tab, lineNumber, columnNumber);
    }

    public async void CompileAndDecompileTab(Tab tab, bool force = false)
    {
        await DoCodeProcess(async () =>
        {
            if (force || (GetTabState(tab) is TabState.NeedsCompile or TabState.Error))
            {
                await CompileFromTab(tab);
            }

            if (GetTabState(tab) is TabState.NeedsDecompile)
            {
                await DecompileToTab(tab);
            }
        });
    }

    public async Task DecompileCurrent()
    {
        await DoCodeProcess(async () =>
        {
            if (GetTabState(SelectedTab) is TabState.NeedsDecompile)
            {
                await DecompileToTab(SelectedTab);
            }
        });
    }

    async partial void OnGMLTextDocumentChanged(TextDocument? value)
    {
        if (value is null)
            return;

        gmlReady.TrySetResult();
    }

    async partial void OnASMTextDocumentChanged(TextDocument? value)
    {
        if (value is null)
            return;

        asmReady.TrySetResult();
    }

    async partial void OnSelectedTabChanged(Tab oldValue, Tab newValue)
    {
        await DoCodeProcess(async () =>
        {
            if (GetTabState(oldValue) is TabState.NeedsCompile)
            {
                await CompileFromTab(oldValue);
            }

            if (GetTabState(newValue) is TabState.NeedsDecompile)
            {
                await DecompileToTab(newValue);
            }
        });
    }

    async Task DoCodeProcess(Func<Task> funcTask)
    {
        if (taskQueue.Count == 0)
            CodeProcessStart();

        await taskQueue.Add(funcTask);

        if (taskQueue.Count == 0)
            CodeProcessEnd();
    }
    async Task<T> DoCodeProcess<T>(Func<Task<T>> funcTask)
    {
        if (taskQueue.Count == 0)
            CodeProcessStart();

        T result = await taskQueue.Add(funcTask);

        if (taskQueue.Count == 0)
            CodeProcessEnd();

        return result;
    }

    void CodeProcessStart()
    {
        lastFocusedElement = MainVM.View!.GetFocusedElement();

        IsCodeProcessing = true;

        loaderWindow = MainVM.View!.LoaderOpen();
    }

    void CodeProcessEnd()
    {
        loaderWindow?.Close();
        loaderWindow = null;

        IsCodeProcessing = false;

        lastFocusedElement?.Focus();
    }

    TabState GetTabState(Tab tab) => tab switch
    {
        Tab.GML => GMLTabState,
        Tab.ASM => ASMTabState,
        _ => throw new NotImplementedException(),
    };

    Task<bool> CompileFromTab(Tab tab) => tab switch
    {
        Tab.GML => CompileFromGML(),
        Tab.ASM => CompileFromASM(),
        _ => throw new NotImplementedException(),
    };

    Task<bool> DecompileToTab(Tab tab) => tab switch
    {
        Tab.GML => DecompileToGML(),
        Tab.ASM => DecompileToASM(),
        _ => throw new NotImplementedException(),
    };

    async Task<bool> DecompileToGML()
    {
        Debug.WriteLine("DecompileToGML");

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
                GMLTabState = TabState.Error;
                if (string.IsNullOrEmpty(GMLTextDocument!.Text))
                {
                    GMLTextDocument!.Text = """#error""";
                }

                loaderWindow?.EnsureShown();
                await MainVM.View!.MessageDialog(e.ToString(),
                    title: "GML decompilation error - UndertaleModTooAvalonia v" + App.VersionString);
                return false;
            }
        }

        GMLTextDocument!.Text = text;
        GMLTabState = TabState.Ok;

        return true;
    }

    async Task<bool> CompileFromGML()
    {
        Debug.WriteLine("CompileFromGML");

        if (Code.ParentEntry is not null)
            return false;

        loaderWindow?.SetText("Compiling from GML...");

        CompileGroup group = new(MainVM.Data);
        group.MainThreadAction = Dispatcher.UIThread.Invoke;
        group.QueueCodeReplace(Code, GMLTextDocument!.Text);
        CompileResult result = await Task.Run(() => group.Compile());

        if (!result.Successful)
        {
            GMLTabState = TabState.Error;

            loaderWindow?.EnsureShown();
            await MainVM.View!.MessageDialog(result.PrintAllErrors(codeEntryNames: false),
                title: "GML compilation error - UndertaleModTooAvalonia v" + App.VersionString);
            return false;
        }

        if (MainVM.Project is not null)
        {
            MainVM.Project.UpdateCodeSource(Code, GMLTextDocument!.Text);
        }

        GMLTabState = TabState.NeedsDecompile; // TODO: Maybe not?
        ASMTabState = TabState.NeedsDecompile;

        return true;
    }

    async Task<bool> DecompileToASM()
    {
        Debug.WriteLine("DecompileToASM");

        if (Code.ParentEntry is not null)
            return false;

        loaderWindow?.SetText("Decompiling from ASM...");

        string text;

        try
        {
            text = await Task.Run(() => Code.Disassemble(MainVM.Data!.Variables, MainVM.Data!.CodeLocals?.For(Code)));
        }
        catch (Exception e)
        {
            ASMTabState = TabState.Error;
            if (string.IsNullOrEmpty(ASMTextDocument!.Text))
            {
                ASMTextDocument!.Text = """#error""";
            }

            loaderWindow?.EnsureShown();
            await MainVM.View!.MessageDialog(e.ToString(),
                title: "ASM decompilation error - UndertaleModTooAvalonia v" + App.VersionString);
            return false;
        }

        ASMTextDocument!.Text = text;

        ASMTabState = TabState.Ok;

        return true;
    }

    async Task<bool> CompileFromASM()
    {
        Debug.WriteLine("CompileFromASM");

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
            string text = ASMTextDocument!.Text;
            List<UndertaleInstruction> instructions = await Task.Run(() => Assembler.Assemble(text, MainVM.Data));
            Code.Replace(instructions);
        }
        catch (Exception e)
        {
            ASMTabState = TabState.Error;

            loaderWindow?.EnsureShown();
            await MainVM.View!.MessageDialog(e.ToString(),
                title: "ASM compilation error - UndertaleModTooAvalonia v" + App.VersionString);
            return false;
        }

        GMLTabState = TabState.NeedsDecompile;
        ASMTabState = TabState.NeedsDecompile; // TODO: Maybe not?

        return true;
    }
}
