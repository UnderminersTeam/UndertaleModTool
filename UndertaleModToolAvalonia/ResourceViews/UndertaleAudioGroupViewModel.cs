using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleAudioGroupViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => AudioGroup;
    public UndertaleAudioGroup AudioGroup { get; set; }

    public UndertaleAudioGroupViewModel(UndertaleAudioGroup audioGroup)
    {
        AudioGroup = audioGroup;
    }
}
