using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using UndertaleModLib;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleGameObject;

namespace UndertaleModToolAvalonia.Helpers;

public class EventsToExtendedEventConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is UndertalePointerList<UndertalePointerList<Event>> list)
        {
            List<ExtendedEvent> newList = new();
            for (int i = 0; i < list.Count; i++)
            {
                newList.Add(new ExtendedEvent()
                {
                    SubEvents = list[i],
                    EventType = (EventType)i,
                    EventName = ((EventType)i).ToString(),
                });
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

public class ExtendedEvent
{
    public UndertalePointerList<Event> SubEvents { get; set; }
    public EventType EventType { get; set; }
    public string EventName { get; set; }
}