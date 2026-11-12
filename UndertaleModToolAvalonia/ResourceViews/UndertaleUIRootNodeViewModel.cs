using System.Collections.ObjectModel;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleUIRootNodeViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => UIRootNode;
    public UndertaleUIRootNode UIRootNode { get; }

    public ObservableCollection<UndertaleUINode> Nodes { get; set; } = [];

    public UndertaleUIRootNodeViewModel(UndertaleUIRootNode uiRootNode)
    {
        UIRootNode = uiRootNode;

        Nodes.Add(UIRootNode);
    }

    public static UndertaleRoom.EffectProperty CreateEffectProperty() => new();
}
