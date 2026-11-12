using CommunityToolkit.Mvvm.ComponentModel;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleAnimationCurveViewModel : ObservableObject, IUndertaleResourceViewModel
{
    public UndertaleResource Resource => AnimationCurve;
    public UndertaleAnimationCurve AnimationCurve { get; }

    [ObservableProperty]
    public partial UndertaleAnimationCurve.Channel? ChannelSelected { get; set; }

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
