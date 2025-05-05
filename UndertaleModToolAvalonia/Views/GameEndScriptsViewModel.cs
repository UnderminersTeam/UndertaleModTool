using System.Collections.ObjectModel;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public class GameEndScriptsViewModel : ViewModelBase
{
    public ObservableCollection<UndertaleGlobalInit> GameEndScripts { get; set; }

    public GameEndScriptsViewModel(ObservableCollection<UndertaleGlobalInit> gameEndScripts)
    {
        GameEndScripts = gameEndScripts;
    }

    public static UndertaleGlobalInit CreateGlobalInit() => new();
}
