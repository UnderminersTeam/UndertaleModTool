using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleStringViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => String;
    public UndertaleString String { get; }

    public UndertaleStringViewModel(UndertaleString _string)
    {
        String = _string;
    }
}
