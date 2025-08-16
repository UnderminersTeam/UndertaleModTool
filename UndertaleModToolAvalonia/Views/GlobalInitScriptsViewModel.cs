using System.Collections.ObjectModel;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public class GlobalInitScriptsViewModel
{
    public ObservableCollection<UndertaleGlobalInit> GlobalInitScripts { get; set; }

    public GlobalInitScriptsViewModel(ObservableCollection<UndertaleGlobalInit> globalInitScripts)
    {
        GlobalInitScripts = globalInitScripts;
    }

    public static UndertaleGlobalInit CreateGlobalInit() => new();
}
