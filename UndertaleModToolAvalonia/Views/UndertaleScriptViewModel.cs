using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleScriptViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => Script;
    public UndertaleScript Script { get; set; }

    public UndertaleScriptViewModel(UndertaleScript script)
    {
        Script = script;
    }
}
