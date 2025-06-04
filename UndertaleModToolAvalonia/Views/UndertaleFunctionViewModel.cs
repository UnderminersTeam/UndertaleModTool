using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public class UndertaleFunctionViewModel
{
    public UndertaleFunction Function { get; set; }

    public UndertaleFunctionViewModel(UndertaleFunction function)
    {
        Function = function;
    }
}
