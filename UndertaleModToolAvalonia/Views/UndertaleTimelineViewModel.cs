using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public class UndertaleTimelineViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => Timeline;
    public UndertaleTimeline Timeline { get; set; }

    public UndertaleTimelineViewModel(UndertaleTimeline timeline)
    {
        Timeline = timeline;
    }

    public static UndertaleTimeline.UndertaleTimelineMoment CreateMoment()
    {
        UndertaleTimeline.UndertaleTimelineMoment moment = new();
        moment.Event = [];
        return moment;
    }

    public static UndertaleGameObject.EventAction CreateEventAction() => new();
}
