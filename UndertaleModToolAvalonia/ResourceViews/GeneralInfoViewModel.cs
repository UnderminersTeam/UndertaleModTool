using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public class GeneralInfoViewModel : ITabContent
{
    public UndertaleGeneralInfo GeneralInfo { get; }
    public UndertaleOptions Options { get; }
    public UndertaleLanguage Language { get; }

    public GeneralInfoViewModel(UndertaleData data)
    {
        GeneralInfo = data.GeneralInfo;
        Options = data.Options;
        Language = data.Language;
    }

    public static UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM> CreateRoomOrderItem() => new();
    public static UndertaleOptions.Constant CreateConstant() => new();
}