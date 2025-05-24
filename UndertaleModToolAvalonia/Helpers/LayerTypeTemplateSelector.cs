using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Helpers;

public class LayerTypeTemplateSelector : ITreeDataTemplate
{
    public ITreeDataTemplate? PathTemplate { get; set; }
    public ITreeDataTemplate? BackgroundTemplate { get; set; }
    public ITreeDataTemplate? InstancesTemplate { get; set; }
    public ITreeDataTemplate? AssetsTemplate { get; set; }
    public ITreeDataTemplate? TilesTemplate { get; set; }
    public ITreeDataTemplate? EffectTemplate { get; set; }

    public bool Match(object? data)
    {
        if (data is UndertaleRoom.Layer layer)
        {
            return layer.LayerType switch
            {
                UndertaleRoom.LayerType.Path => PathTemplate is not null,
                UndertaleRoom.LayerType.Path2 => PathTemplate is not null,
                UndertaleRoom.LayerType.Background => BackgroundTemplate is not null,
                UndertaleRoom.LayerType.Instances => InstancesTemplate is not null,
                UndertaleRoom.LayerType.Assets => AssetsTemplate is not null,
                UndertaleRoom.LayerType.Tiles => TilesTemplate is not null,
                UndertaleRoom.LayerType.Effect => EffectTemplate is not null,
                _ => false,
            };
        }
        return false;
    }

    public InstancedBinding? ItemsSelector(object item)
    {
        if (item is UndertaleRoom.Layer layer)
        {
            return layer.LayerType switch
            {
                UndertaleRoom.LayerType.Path => PathTemplate?.ItemsSelector(layer),
                UndertaleRoom.LayerType.Path2 => PathTemplate?.ItemsSelector(layer),
                UndertaleRoom.LayerType.Background => BackgroundTemplate?.ItemsSelector(layer),
                UndertaleRoom.LayerType.Instances => InstancesTemplate?.ItemsSelector(layer),
                UndertaleRoom.LayerType.Assets => AssetsTemplate?.ItemsSelector(layer),
                UndertaleRoom.LayerType.Tiles => TilesTemplate?.ItemsSelector(layer),
                UndertaleRoom.LayerType.Effect => EffectTemplate?.ItemsSelector(layer),
                _ => null,
            };
        }
        return null;
    }

    public Control? Build(object? param)
    {
        if (param is UndertaleRoom.Layer layer)
        {
            return layer.LayerType switch
            {
                UndertaleRoom.LayerType.Path => PathTemplate?.Build(layer),
                UndertaleRoom.LayerType.Path2 => PathTemplate?.Build(layer),
                UndertaleRoom.LayerType.Background => BackgroundTemplate?.Build(layer),
                UndertaleRoom.LayerType.Instances => InstancesTemplate?.Build(layer),
                UndertaleRoom.LayerType.Assets => AssetsTemplate?.Build(layer),
                UndertaleRoom.LayerType.Tiles => TilesTemplate?.Build(layer),
                UndertaleRoom.LayerType.Effect => EffectTemplate?.Build(layer),
                _ => null,
            };
        }
        return null;
    }
}
