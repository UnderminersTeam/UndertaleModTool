using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertalePathViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => Path;
    public UndertalePath Path { get; set; }

    public UndertalePathViewModel(UndertalePath path)
    {
        Path = path;
    }

    public static UndertalePath.PathPoint CreatePathPoint() => new();
}
