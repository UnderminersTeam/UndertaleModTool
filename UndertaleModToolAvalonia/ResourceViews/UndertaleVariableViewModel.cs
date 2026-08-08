using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleVariableViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => Variable;
    public UndertaleVariable Variable { get; }

    public UndertaleVariableViewModel(UndertaleVariable variable)
    {
        Variable = variable;
    }
}
