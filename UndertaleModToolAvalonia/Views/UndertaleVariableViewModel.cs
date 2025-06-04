using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public class UndertaleVariableViewModel
{
    public UndertaleVariable Variable { get; set; }

    public UndertaleVariableViewModel(UndertaleVariable variable)
    {
        Variable = variable;
    }
}
