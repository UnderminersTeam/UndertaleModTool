using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public class GeneralInfoViewModel
{
    public UndertaleGeneralInfo GeneralInfo { get; set; }
    public UndertaleOptions Options { get; set; }
    public UndertaleLanguage Language { get; set; }

    public GeneralInfoViewModel(UndertaleData data)
    {
        GeneralInfo = data.GeneralInfo;
        Options = data.Options;
        Language = data.Language;
    }

    public static UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM> CreateRoomOrderItem() => new();
    public static UndertaleOptions.Constant CreateConstant() => new();
}