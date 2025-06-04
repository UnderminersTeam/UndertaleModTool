using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public class UndertalePathViewModel
{
    public UndertalePath Path { get; set; }

    public UndertalePathViewModel(UndertalePath path)
    {
        Path = path;
    }

    public static UndertalePath.PathPoint CreatePathPoint() => new();
}
