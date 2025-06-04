using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public class UndertaleCodeLocalsViewModel
{
    public UndertaleCodeLocals CodeLocals { get; set; }

    public UndertaleCodeLocalsViewModel(UndertaleCodeLocals codeLocals)
    {
        CodeLocals = codeLocals;
    }

    public static UndertaleCodeLocals.LocalVar CreateLocalVar(int index) => new() { Index = (uint)index };
}
