using System;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

/// <summary>
/// NOTE: Sync with <see cref="UndertaleModLib.Models.UndertaleUINode"/>
/// </summary>
public class UINodeTemplateSelector : ITreeDataTemplate
{
    public ITreeDataTemplate? LayerTemplate { get; set; }
    public ITreeDataTemplate? FlexPanelTemplate { get; set; }
    public ITreeDataTemplate? GameObjectTemplate { get; set; }
    public ITreeDataTemplate? SequenceInstanceTemplate { get; set; }
    public ITreeDataTemplate? SpriteInstanceTemplate { get; set; }
    public ITreeDataTemplate? TextItemInstanceTemplate { get; set; }
    public ITreeDataTemplate? EffectLayerTemplate { get; set; }

    public bool Match(object? data)
    {
        if (data is UndertaleUINode uiNode)
        {
            return uiNode.Node switch
            {
                UndertaleUILayer => LayerTemplate is not null,
                UndertaleUIFlexPanel => FlexPanelTemplate is not null,
                UndertaleUIGameObject => GameObjectTemplate is not null,
                UndertaleUISequenceInstance => SequenceInstanceTemplate is not null,
                UndertaleUISpriteInstance => SpriteInstanceTemplate is not null,
                UndertaleUITextItemInstance => TextItemInstanceTemplate is not null,
                UndertaleUIEffectLayer => EffectLayerTemplate is not null,
                _ => false
            };
        }
        return false;
    }

    public IDisposable BindChildren(AvaloniaObject target, AvaloniaProperty targetProperty, object item)
    {
        if (item is UndertaleUINode uiNode)
        {
            return uiNode.Node switch
            {
                UndertaleUILayer => LayerTemplate?.BindChildren(target, targetProperty, uiNode.Node),
                UndertaleUIFlexPanel => FlexPanelTemplate?.BindChildren(target, targetProperty, uiNode.Node),
                UndertaleUIGameObject => GameObjectTemplate?.BindChildren(target, targetProperty, uiNode.Node),
                UndertaleUISequenceInstance => SequenceInstanceTemplate?.BindChildren(target, targetProperty, uiNode.Node),
                UndertaleUISpriteInstance => SpriteInstanceTemplate?.BindChildren(target, targetProperty, uiNode.Node),
                UndertaleUITextItemInstance => TextItemInstanceTemplate?.BindChildren(target, targetProperty, uiNode.Node),
                UndertaleUIEffectLayer => EffectLayerTemplate?.BindChildren(target, targetProperty, uiNode.Node),
                _ => null,
            } ?? Disposable.Empty;
        }
        return Disposable.Empty;
    }

    public Control? Build(object? param)
    {
        if (param is UndertaleUINode uiNode)
        {
            return uiNode.Node switch
            {
                UndertaleUILayer => LayerTemplate?.Build(uiNode.Node),
                UndertaleUIFlexPanel => FlexPanelTemplate?.Build(uiNode.Node),
                UndertaleUIGameObject => GameObjectTemplate?.Build(uiNode.Node),
                UndertaleUISequenceInstance => SequenceInstanceTemplate?.Build(uiNode.Node),
                UndertaleUISpriteInstance => SpriteInstanceTemplate?.Build(uiNode.Node),
                UndertaleUITextItemInstance => TextItemInstanceTemplate?.Build(uiNode.Node),
                UndertaleUIEffectLayer => EffectLayerTemplate?.Build(uiNode.Node),
                _ => null
            };
        }
        return null;
    }
}
