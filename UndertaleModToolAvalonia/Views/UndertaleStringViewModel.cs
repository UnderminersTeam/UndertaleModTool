using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleStringViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => String;
    public UndertaleString String { get; set; }

    public UndertaleStringViewModel(UndertaleString _string)
    {
        String = _string;
    }
}
