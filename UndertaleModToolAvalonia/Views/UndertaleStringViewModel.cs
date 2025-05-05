using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleStringViewModel : ViewModelBase
{
    public UndertaleString String { get; set; }

    public UndertaleStringViewModel(UndertaleString _string)
    {
        String = _string;
    }
}
