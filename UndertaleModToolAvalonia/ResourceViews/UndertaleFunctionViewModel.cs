using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleFunctionViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => Function;
    public UndertaleFunction Function { get; }

    public UndertaleFunctionViewModel(UndertaleFunction function)
    {
        Function = function;
    }
}
