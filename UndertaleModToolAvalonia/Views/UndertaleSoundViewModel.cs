using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public class UndertaleSoundViewModel
{
    public UndertaleSound Sound { get; set; }

    public UndertaleSoundViewModel(UndertaleSound sound)
    {
        Sound = sound;
    }
}
