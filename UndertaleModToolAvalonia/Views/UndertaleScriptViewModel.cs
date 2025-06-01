using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public class UndertaleScriptViewModel
{
    public UndertaleScript Script { get; set; }

    public UndertaleScriptViewModel(UndertaleScript script)
    {
        Script = script;
    }
}
