using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public class LayerAssetsDataToAllAssetsList : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is UndertaleRoom.Layer.LayerAssetsData layerAssetsData)
        {
            var allAssets = new List<IEnumerable<object?>>();
            if (layerAssetsData.LegacyTiles != null)
                allAssets.Add(layerAssetsData.LegacyTiles);
            if (layerAssetsData.Sprites != null)
                allAssets.Add(layerAssetsData.Sprites);
            if (layerAssetsData.Sequences != null)
                allAssets.Add(layerAssetsData.Sequences);
            if (layerAssetsData.NineSlices != null)
                allAssets.Add(layerAssetsData.NineSlices);
            if (layerAssetsData.ParticleSystems != null)
                allAssets.Add(layerAssetsData.ParticleSystems);
            if (layerAssetsData.TextItems != null)
                allAssets.Add(layerAssetsData.TextItems);
            return allAssets;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
