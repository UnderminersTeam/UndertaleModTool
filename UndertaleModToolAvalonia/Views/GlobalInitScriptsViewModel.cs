using System.Collections.ObjectModel;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public class GlobalInitScriptsViewModel
{
    public ObservableCollection<UndertaleGlobalInit> GlobalInitScripts { get; set; }

    public GlobalInitScriptsViewModel(ObservableCollection<UndertaleGlobalInit> globalInitScripts)
    {
        GlobalInitScripts = globalInitScripts;
    }

    public static UndertaleGlobalInit CreateGlobalInit() => new();
}
