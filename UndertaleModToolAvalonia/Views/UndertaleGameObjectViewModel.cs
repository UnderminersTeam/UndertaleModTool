using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleGameObjectViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => GameObject;
    public UndertaleGameObject GameObject { get; set; }

    public UndertaleGameObjectViewModel(UndertaleGameObject gameObject)
    {
        GameObject = gameObject;
    }

    public static UndertaleGameObject.UndertalePhysicsVertex CreatePhysicsVertex() => new();
    public static UndertaleGameObject.Event CreateEvent() => new();
    public static UndertaleGameObject.EventAction CreateEventAction() => new();
}