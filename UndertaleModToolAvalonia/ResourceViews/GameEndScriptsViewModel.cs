using System.Collections.ObjectModel;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public class GameEndScriptsViewModel : ITabContent
{
    public ObservableCollection<UndertaleGlobalInit> GameEndScripts { get; }

    public GameEndScriptsViewModel(ObservableCollection<UndertaleGlobalInit> gameEndScripts)
    {
        GameEndScripts = gameEndScripts;
    }

    public static UndertaleGlobalInit CreateGlobalInit() => new();
}
