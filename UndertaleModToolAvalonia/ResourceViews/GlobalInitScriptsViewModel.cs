using System.Collections.ObjectModel;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public class GlobalInitScriptsViewModel : ITabContent
{
    public ObservableCollection<UndertaleGlobalInit> GlobalInitScripts { get; }

    public GlobalInitScriptsViewModel(ObservableCollection<UndertaleGlobalInit> globalInitScripts)
    {
        GlobalInitScripts = globalInitScripts;
    }

    public static UndertaleGlobalInit CreateGlobalInit() => new();
}
