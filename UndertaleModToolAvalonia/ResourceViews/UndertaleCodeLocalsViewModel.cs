using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleCodeLocalsViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => CodeLocals;
    public UndertaleCodeLocals CodeLocals { get; }

    public UndertaleCodeLocalsViewModel(UndertaleCodeLocals codeLocals)
    {
        CodeLocals = codeLocals;
    }

    public static UndertaleCodeLocals.LocalVar CreateLocalVar(int index) => new() { Index = (uint)index };
}
