using System;
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

    public static Func<int, object> CreateLocalVar => index => new UndertaleCodeLocals.LocalVar() { Index = (uint)index };
}
