using System;
using System.Collections;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleGameObjectViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => GameObject;
    public UndertaleGameObject GameObject { get; set; }

    public UndertaleGameObjectViewModel(UndertaleGameObject gameObject, IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();

        GameObject = gameObject;
    }

    public static UndertaleGameObject.UndertalePhysicsVertex CreatePhysicsVertex() => new();
    public static UndertaleGameObject.Event CreateEvent() => new();
    public static UndertaleGameObject.EventAction CreateEventAction() => new();

    public async Task<UndertaleResource?> CreateEventActionCode(object? argument)
    {
        if (argument is not IList list || list is not [EventType eventType, uint eventSubtype])
            return null;

        return GameObject?.EventHandlerFor(eventType, eventSubtype, MainVM.Data);
    }
}