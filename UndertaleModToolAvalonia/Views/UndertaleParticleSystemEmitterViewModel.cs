using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleParticleSystemEmitterViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => ParticleSystemEmitter;
    public UndertaleParticleSystemEmitter ParticleSystemEmitter { get; set; }

    public UndertaleParticleSystemEmitterViewModel(UndertaleParticleSystemEmitter particleSystemEmitter)
    {
        ParticleSystemEmitter = particleSystemEmitter;
    }
}
