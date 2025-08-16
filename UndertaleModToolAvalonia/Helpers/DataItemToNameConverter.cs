using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public class DataItemToNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is UndertaleNamedResource undertaleNamedResource)
        {
            string typeName = undertaleNamedResource switch
            {
                UndertaleAudioGroup => "Audio Group",
                UndertaleSound => "Sound",
                UndertaleSprite => "Sprite",
                UndertaleBackground => "Background",
                UndertalePath => "Path",
                UndertaleScript => "Script",
                UndertaleShader => "Shader",
                UndertaleFont => "Font",
                UndertaleTimeline => "Timeline",
                UndertaleGameObject => "Game Object",
                UndertaleRoom => "Room",
                UndertaleExtension => "Extension",
                UndertaleTexturePageItem => "Texture Page Item",
                UndertaleCode => "Code",
                UndertaleVariable => "Variable",
                UndertaleFunction => "Function",
                UndertaleCodeLocals => "Code Locals",
                UndertaleEmbeddedTexture => "Embedded Texture",
                UndertaleEmbeddedAudio => "Embedded Audio",
                UndertaleTextureGroupInfo => "Texture Group Info",
                UndertaleEmbeddedImage => "Embedded Image",
                UndertaleSequence => "Sequence",
                UndertaleAnimationCurve => "Animation Curve",
                UndertaleParticleSystem => "Particle System",
                UndertaleParticleSystemEmitter => "Particle System Emitter",
                _ => "Unknown"
            };
            return typeName + " - " + undertaleNamedResource.Name.Content;
        }
        else if (value is UndertaleString undertaleString)
        {
            return "String - " + undertaleString.Content;
        }
        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}