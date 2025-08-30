using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleParticleSystemViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => ParticleSystem;
    public UndertaleParticleSystem ParticleSystem { get; set; }

    public UndertaleParticleSystemViewModel(UndertaleParticleSystem particleSystem)
    {
        ParticleSystem = particleSystem;
    }

    public static UndertaleResourceById<UndertaleParticleSystemEmitter, UndertaleChunkPSEM> CreateParticleSystemEmitterItem() => new();
}
