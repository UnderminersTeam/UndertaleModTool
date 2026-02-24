using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleAnimationCurveViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => AnimationCurve;
    public UndertaleAnimationCurve AnimationCurve { get; set; }

    [Notify]
    private UndertaleAnimationCurve.Channel? _ChannelSelected;

    public UndertaleAnimationCurveViewModel(UndertaleAnimationCurve animationCurve)
    {
        AnimationCurve = animationCurve;
    }

    public static UndertaleAnimationCurve.Channel CreateChannel() => new();
    public static UndertaleAnimationCurve.Channel.Point CreatePoint() => new();

    public void ChannelSelectedChanged(object? item)
    {
        ChannelSelected = (UndertaleAnimationCurve.Channel?)item;
    }
}
