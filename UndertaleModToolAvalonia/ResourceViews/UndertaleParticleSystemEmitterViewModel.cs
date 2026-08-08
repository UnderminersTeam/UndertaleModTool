using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleParticleSystemEmitterViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => ParticleSystemEmitter;
    public UndertaleParticleSystemEmitter ParticleSystemEmitter { get; }

    public UndertaleParticleSystemEmitterViewModel(UndertaleParticleSystemEmitter particleSystemEmitter)
    {
        ParticleSystemEmitter = particleSystemEmitter;
    }
}
