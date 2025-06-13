using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleVariableViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => Variable;
    public UndertaleVariable Variable { get; set; }

    public UndertaleVariableViewModel(UndertaleVariable variable)
    {
        Variable = variable;
    }
}
