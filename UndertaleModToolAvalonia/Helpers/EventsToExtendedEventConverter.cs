using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Helpers;

public class EventsToExtendedEventConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is UndertalePointerList<UndertalePointerList<UndertaleGameObject.Event>> list)
        {
            List<ExtendedEvent> newList = new();
            for (int i = 0; i < list.Count; i++)
            {
                newList.Add(new ExtendedEvent(
                    subEvents: list[i],
                    eventType: (EventType)i,
                    eventName: ((EventType)i).ToString()));
            }
            return newList;
        }
        return BindingNotification.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ExtendedEvent(UndertalePointerList<UndertaleGameObject.Event> subEvents, EventType eventType, string eventName)
{
    public UndertalePointerList<UndertaleGameObject.Event> SubEvents { get; set; } = subEvents;
    public EventType EventType { get; set; } = eventType;
    public string EventName { get; set; } = eventName;
}