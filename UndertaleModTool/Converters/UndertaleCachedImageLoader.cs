using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using SkiaSharp;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModTool
{
    public class UndertaleCachedImageLoader : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, ImageSource> imageCache = new();
        private static readonly ConcurrentDictionary<Tuple<string, Tuple<uint, uint, uint, uint>>, ImageSource> tileCache = new();
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        private static bool _reuseTileBuffer;
        public static bool ReuseTileBuffer
        {
            get => _reuseTileBuffer;
            set
            {
                sharedTileBuffer = value ? ArrayPool<byte>.Create() : null;

                _reuseTileBuffer = value;
            }
        }
        private static ArrayPool<byte> sharedTileBuffer;
        private static int currBufferSize = 1048576;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return null;

            bool isTile = false;
            bool cacheEnabled = true;
            bool generate = false;

            string par;
            List<Tuple<uint, uint, uint, uint>> tileRectList = null;
            if (parameter is string)
            {
                par = parameter as string;

                isTile = par.Contains("tile");
                cacheEnabled = !par.Contains("nocache");
                generate = par.Contains("generate");
            }
            else if (parameter is List<Tuple<uint, uint, uint, uint>>)
            {
                generate = true;
                tileRectList = parameter as List<Tuple<uint, uint, uint, uint>>;
            }

            Tile tile = null;
            if (isTile)
                tile = value as Tile;

            UndertaleTexturePageItem texture = isTile ? tile.Tpag : value as UndertaleTexturePageItem;
            if (texture is null || texture.TexturePage is null)
                return null;

            string texName = texture.Name?.Content;
            if (texName is null || texName == "PageItem Unknown Index")
            {
                if (generate)
                    texName = mainWindow.Dispatcher.Invoke(() =>
                    {
                        return (mainWindow.Data.TexturePageItems.IndexOf(texture) + 1).ToString();
                    });
                else
                    texName = (mainWindow.Data.TexturePageItems.IndexOf(texture) + 1).ToString();

                if (texName == "0")
                    return null;
            }

            if (texture.SourceWidth == 0 || texture.SourceHeight == 0)
                return null;

            if (tileRectList is not null)
            {
                Rect rect = new(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight);
                ProcessTileSet(texName, CreateSpriteBitmap(rect, in texture), tileRectList, texture.TargetX, texture.TargetY);

                return null;
            }

            ImageSource spriteSrc;
            if (isTile)
            {
                if (tileCache.TryGetValue(new(texName, new(tile.SourceX, tile.SourceY, tile.Width, tile.Height)), out spriteSrc))
                    return spriteSrc;
            }

            if (!imageCache.ContainsKey(texName) || !cacheEnabled)
            {
                Rect rect;

                // how many pixels are out of bounds of tile texture page
                int diffW = 0;
                int diffH = 0;

                if (isTile)
                {
                    diffW = (int)(tile.SourceX + tile.Width - texture.SourceWidth);
                    diffH = (int)(tile.SourceY + tile.Height - texture.SourceHeight);
                    rect = new((int)(texture.SourceX + tile.SourceX), (int)(texture.SourceY + tile.SourceY), (int)tile.Width, (int)tile.Height);
                }
                else
                    rect = new(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight);

                spriteSrc = CreateSpriteSource(in rect, in texture, diffW, diffH, isTile);

                if (cacheEnabled)
                {
                    if (isTile)
                        tileCache.TryAdd(new(texName, new(tile.SourceX, tile.SourceY, tile.Width, tile.Height)), spriteSrc);
                    else
                        imageCache.TryAdd(texName, spriteSrc);
                }

                if (generate)
                    return null;
                else
                    return spriteSrc;
            }

            return imageCache[texName];
        }

        public static void Reset()
        {
            imageCache.Clear();
            tileCache.Clear();
            ReuseTileBuffer = false;
            currBufferSize = 1048576;
        }

        public static SKBitmap CreateSpriteBitmap(Rect rect, in UndertaleTexturePageItem texture, int diffW = 0, int diffH = 0, bool isTile = false)
        {
            SKBitmap spriteBMP = new((int)rect.Width, (int)rect.Height);

            rect.Width -= (diffW > 0) ? diffW : 0;
            rect.Height -= (diffH > 0) ? diffH : 0;
            int x = isTile ? texture.TargetX : 0;
            int y = isTile ? texture.TargetY : 0;

            using (SKCanvas g = new(spriteBMP))
            {
                using SKBitmap img = SKBitmap.Decode(texture.TexturePage.TextureData.TextureBlob);
                
                var rectDest = SKRect.Create(x, y, (float)rect.Width, (float)rect.Height);
                var rectSrc = SKRect.Create((float)rect.Left, (float)rect.Top, (float)rect.Width, (float)rect.Height);
                g.DrawBitmap(img, rectSrc, rectDest);
            }
            spriteBMP.SetImmutable();

            return spriteBMP;
        }
        private ImageSource CreateSpriteSource(in Rect rect, in UndertaleTexturePageItem texture, int diffW = 0, int diffH = 0, bool isTile = false)
        {
            using SKBitmap spriteBMP = CreateSpriteBitmap(rect, in texture, diffW, diffH, isTile);
            using var data = spriteBMP.Encode(SKEncodedImageFormat.Png, 100);

            BitmapImage spriteSrc = new();
            spriteSrc.BeginInit();
            spriteSrc.CacheOption = BitmapCacheOption.OnLoad;
            spriteSrc.StreamSource = data.AsStream();
            spriteSrc.EndInit();

            spriteSrc.Freeze(); // allow UI thread access

            return spriteSrc;
        }
        private void ProcessTileSet(string textureName, SKBitmap bmp, List<Tuple<uint, uint, uint, uint>> tileRectList, int targetX, int targetY)
        {
            int depth = bmp.BytesPerPixel;

            int bufferLen = bmp.RowBytes * bmp.Height;
            byte[] buffer;
            if (ReuseTileBuffer)
            {
                if (bufferLen > currBufferSize)
                {
                    currBufferSize = bufferLen;
                    sharedTileBuffer = ArrayPool<byte>.Create(currBufferSize, 17); // 17 is default value
                }

                buffer = sharedTileBuffer.Rent(bufferLen);
            }
            else
                buffer = new byte[bufferLen];

            Marshal.Copy(bmp.GetPixels(), buffer, 0, bufferLen);

            _ = Parallel.ForEach(tileRectList, (tileRect) =>
            {
                int origX = (int)tileRect.Item1;
                int origY = (int)tileRect.Item2;
                int x = origX - targetX;
                int y = origY - targetY;
                int w = (int)tileRect.Item3;
                int h = (int)tileRect.Item4;

                if (w == 0 || h == 0)
                    return;

                // Sometimes, tile size can be bigger than texture size
                // (for example, BG tile of "room_torielroom")
                // Also, it can be out of texture bounds
                // (for example, tile 10055649 of "room_fire_core_topright")
                // (both examples are from Undertale)
                // This algorithm doesn't support that, so this tile will be processed by "CreateSpriteSource()"
                if (w > bmp.Width || h > bmp.Height || x < 0 || y < 0 || x + w > bmp.Width || y + h > bmp.Height)
                    return;

                int bufferResLen = w * h * depth;
                byte[] bufferRes = ArrayPool<byte>.Shared.Rent(bufferResLen); // may return bigger array than requested

                // Source - https://stackoverflow.com/a/9691388/12136394
                // There was faster solution, but it uses "unsafe" code
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w * depth; j += depth)
                    {
                        int origIndex = (y * bmp.RowBytes) + (i * bmp.RowBytes) + (x * depth) + j;
                        int croppedIndex = (i * w * depth) + j;

                        Buffer.BlockCopy(buffer, origIndex, bufferRes, croppedIndex, depth);
                    }
                }

                using SKBitmap tileBMP = new(w, h);
                Marshal.Copy(bufferRes, 0, tileBMP.GetPixels(), bufferResLen);
                ArrayPool<byte>.Shared.Return(bufferRes);

                using var data = tileBMP.Encode(SKEncodedImageFormat.Png, 100);
                BitmapImage spriteSrc = new();
                spriteSrc.BeginInit();
                spriteSrc.CacheOption = BitmapCacheOption.OnLoad;
                spriteSrc.StreamSource = data.AsStream();
                spriteSrc.EndInit();

                spriteSrc.Freeze(); // allow UI thread access

                Tuple<string, Tuple<uint, uint, uint, uint>> tileKey = new(textureName, new((uint)origX, (uint)origY, (uint)w, (uint)h));
                tileCache.TryAdd(tileKey, spriteSrc);
            });

            bmp.Dispose();

            if (ReuseTileBuffer)
                sharedTileBuffer.Return(buffer);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // UndertaleCachedImageLoader wrappers
    public class CachedTileImageLoader : IMultiValueConverter
    {
        private static readonly UndertaleCachedImageLoader loader = new();
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is null) // tile
                return null;

            if ((uint)values[1] == 0 || (uint)values[2] == 0) // width, height
                return null;

            return loader.Convert(values[0], null, "tile", null);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class CachedImageLoaderWithIndex : IMultiValueConverter
    {
        private static UndertaleCachedImageLoader loader = new();
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(x => x is null))
                return null;

            IList<UndertaleSprite.TextureEntry> textures = values[0] as IList<UndertaleSprite.TextureEntry>;
            if (textures is null)
                return null;

            int index = -1;
            if (values[1] is int indexInt)
                index = indexInt;
            else if (values[1] is float indexFloat)
                index = (int)indexFloat;

            if (index > textures.Count - 1 || index < 0)
                return null;
            else
                return loader.Convert(textures[index].Texture, null, null, null);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CachedTileDataLoader : IMultiValueConverter
    {
        // Tile text. page, tile ID - tile pixel data
        public static ConcurrentDictionary<Tuple<string, uint>, SKBitmap> TileCache { get; set; } = new();
        private static readonly ConcurrentDictionary<string, SKBitmap> tilePageCache = new();

        public static void Reset()
        {
            foreach (SKBitmap bmp in TileCache.Values)
                bmp.Dispose();
            foreach (SKBitmap bmp in tilePageCache.Values)
                bmp.Dispose();

            TileCache.Clear();
            tilePageCache.Clear();
            TileRectanglesConverter.TileCache.Clear();
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(x => x is null))
                return null;

            bool cache = parameter is string par && par == "cache";

            Layer.LayerTilesData tilesData = values[0] as Layer.LayerTilesData;
            UndertaleBackground tilesBG = tilesData.Background;

            if (tilesBG is null)
                return null;

            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            try
            {
                string texName = tilesBG.Texture?.Name?.Content;
                if (texName is null or "PageItem Unknown Index")
                {
                    texName = (mainWindow.Data.TexturePageItems.IndexOf(tilesBG.Texture) + 1).ToString();
                    if (texName == "0")
                        return null;
                }

                SKBitmap tilePageBMP;
                if (tilePageCache.ContainsKey(texName))
                {
                    tilePageBMP = tilePageCache[texName];
                }
                else
                {
                    tilePageBMP = UndertaleCachedImageLoader.CreateSpriteBitmap(new(tilesBG.Texture.SourceX,
                                                                                    tilesBG.Texture.SourceY,
                                                                                    tilesBG.Texture.SourceWidth,
                                                                                    tilesBG.Texture.SourceHeight), tilesBG.Texture);

                    tilePageCache[texName] = tilePageBMP;
                }

                int depth = tilePageBMP.BytesPerPixel;
                
                byte[] buffer = new byte[tilePageBMP.RowBytes * tilePageBMP.Height];
                Marshal.Copy(tilePageBMP.GetPixels(), buffer, 0, buffer.Length);

                int w = (int)tilesBG.GMS2TileWidth;
                int h = (int)tilesBG.GMS2TileHeight;
                int outX = (int)tilesBG.GMS2OutputBorderX;
                int outY = (int)tilesBG.GMS2OutputBorderY;
                int tileRows = (int)Math.Ceiling(tilesBG.GMS2TileCount / (double)tilesBG.GMS2TileColumns);

                bool outOfBounds = false;
                _ = Parallel.For(0, tileRows, (y) =>
                {
                    int y1 = ((y + 1) * outY) + (y * (h + outY));

                    for (int x = 0; x < tilesBG.GMS2TileColumns; x++)
                    {
                        int x1 = ((x + 1) * outX) + (x * (w + outX));

                        if (x1 + w > tilePageBMP.Width || y1 + h > tilePageBMP.Height)
                        {
                            outOfBounds = true;
                            return;
                        }

                        int bufferResLen = w * h * depth;
                        byte[] bufferRes = ArrayPool<byte>.Shared.Rent(bufferResLen);

                        if (!(x == 0 && y == 0))
                        {
                            for (int i = 0; i < h; i++)
                            {
                                for (int j = 0; j < w * depth; j += depth)
                                {
                                    int origIndex = (y1 * tilePageBMP.RowBytes) + (i * tilePageBMP.RowBytes) + (x1 * depth) + j;
                                    int croppedIndex = (i * w * depth) + j;

                                    Buffer.BlockCopy(buffer, origIndex, bufferRes, croppedIndex, depth);
                                }
                            }
                        }

                        SKBitmap tileBMP = new(w, h);
                        Marshal.Copy(bufferRes, 0, tileBMP.GetPixels(), bufferResLen);
                        ArrayPool<byte>.Shared.Return(bufferRes);
                        tileBMP.SetImmutable();

                        TileCache.TryAdd(new(texName, (uint)((tilesBG.GMS2TileColumns * y) + x)), tileBMP);
                    }
                });

                if (outOfBounds)
                {
                    mainWindow.ShowError($"Tileset of \"{tilesData.ParentLayer.LayerName.Content}\" tile layer has wrong parameters (tile size, output border, etc.).\n" +
                                          "It can't be displayed.");
                    return "Error";
                }

                return cache ? null : CreateLayerSource(in tilesData, in tilesBG, in w, in h);
            }
            catch (Exception ex)
            {
                mainWindow.ShowError($"An error occured while rendering tile layer \"{tilesData.ParentLayer.LayerName.Content}\".\n\n{ex}");
                return "Error";
            }
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public ImageSource CreateLayerSource(in Layer.LayerTilesData tilesData, in UndertaleBackground tilesBG, in int w, in int h)
        {
            using SKBitmap layerBMP = new(w * (int)tilesData.TilesX, h * (int)tilesData.TilesY);
            SKCanvas layerG = new(layerBMP);
            uint maxID = tilesData.Background.GMS2TileIds.Select(x => x.ID).Max();

            using SKSurface tileSurf = SKSurface.Create(new SKImageInfo(w, h));

            for (int y = 0; y < tilesData.TilesY; y++)
            {
                for (int x = 0; x < tilesData.TilesX; x++)
                {
                    uint id = tilesData.TileData[y][x];
                    if (id == 0)
                        continue;

                    if (id > maxID)
                    {
                        uint realID = id & 0x0FFFFFFF; // remove tile flag
                        if (realID > maxID)
                        {
                            Debug.WriteLine("Tileset \"" + tilesData.Background.Name.Content + "\" doesn't contain tile ID " + realID);
                            continue;
                        }

                        SKBitmap srcBMP = TileCache[new(tilesBG.Texture.Name.Content, realID)];
                        tileSurf.Canvas.ResetMatrix();

                        switch (id >> 28)
                        {
                            case 1:
                                // Flip X
                                tileSurf.Canvas.Scale(-1, 1, srcBMP.Width / 2, 0);
                                break;
                            case 2:
                                // Flip Y
                                tileSurf.Canvas.Scale(1, -1, 0, srcBMP.Height / 2);
                                break;
                            case 3:
                                // Flip X and Y
                                tileSurf.Canvas.Scale(-1, -1, srcBMP.Width / 2, srcBMP.Height / 2);
                                break;
                            case 4:
                                // Rotate 90 degrees clockwise
                                tileSurf.Canvas.RotateDegrees(90);
                                break;
                            case 5:
                                // Rotate 270 degrees clockwise and flip Y
                                tileSurf.Canvas.RotateDegrees(270);
                                tileSurf.Canvas.Scale(1, -1, 0, srcBMP.Height / 2);
                                break;
                            case 6:
                                // Rotate 90 degrees clockwise and flip Y
                                tileSurf.Canvas.RotateDegrees(90);
                                tileSurf.Canvas.Scale(1, -1, 0, srcBMP.Height / 2);
                                break;
                            case 7:
                                // Rotate 270 degrees clockwise
                                tileSurf.Canvas.RotateDegrees(270);
                                break;

                            default:
                                Debug.WriteLine("Tile of " + tilesData.ParentLayer.LayerName + " located at (" + x + ", " + y + ") has unknown flag.");
                                break;
                        }

                        tileSurf.Canvas.DrawBitmap(srcBMP, 0, 0);
                        tileSurf.Draw(layerG, x * w, y * h, null);
                    }
                    else
                        layerG.DrawBitmap(TileCache[new(tilesBG.Texture.Name.Content, id)], x * w, y * h);
                }
            }
            layerG.Dispose();

            using var data = layerBMP.Encode(SKEncodedImageFormat.Png, 100);

            BitmapImage spriteSrc = new();
            spriteSrc.BeginInit();
            spriteSrc.CacheOption = BitmapCacheOption.OnLoad;
            spriteSrc.StreamSource = data.AsStream();
            spriteSrc.EndInit();

            spriteSrc.Freeze();

            return spriteSrc;
        }
    }
}
