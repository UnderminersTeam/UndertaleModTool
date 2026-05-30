using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleAudioGroupViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => AudioGroup;
    public UndertaleAudioGroup AudioGroup { get; }

    public UndertaleAudioGroupViewModel(UndertaleAudioGroup audioGroup)
    {
        AudioGroup = audioGroup;
    }
}
