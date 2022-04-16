using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject([In] IntPtr hObject);

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


            if (tileRectList is not null)
            {
                Rectangle rect = new(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight);
                ProcessTileSet(texName, CreateSpriteBitmap(rect, in texture), tileRectList);

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
                Rectangle rect;

                // how many pixels are out of bounds of tile texture page
                int diffW = 0;
                int diffH = 0;

                if (isTile)
                {
                    diffW = (int)(tile.SourceX + tile.Width - texture.BoundingWidth);
                    diffH = (int)(tile.SourceY + tile.Height - texture.BoundingHeight);
                    rect = new((int)(texture.SourceX + tile.SourceX), (int)(texture.SourceY + tile.SourceY), (int)tile.Width, (int)tile.Height);
                }
                else
                    rect = new(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight);

                spriteSrc = CreateSpriteSource(in rect, in texture, diffW, diffH);

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

        public static Bitmap CreateSpriteBitmap(Rectangle rect, in UndertaleTexturePageItem texture, int diffW = 0, int diffH = 0)
        {
            using MemoryStream stream = new(texture.TexturePage.TextureData.TextureBlob);
            Bitmap spriteBMP = new(rect.Width, rect.Height);

            rect.Width -= (diffW > 0) ? diffW : 0;
            rect.Height -= (diffH > 0) ? diffH : 0;

            using (Graphics g = Graphics.FromImage(spriteBMP))
            {
                using Image img = Image.FromStream(stream); // "ImageConverter.ConvertFrom()" does the same, except it doesn't explicitly dispose MemoryStream
                g.DrawImage(img, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
            }

            return spriteBMP;
        }
        private ImageSource CreateSpriteSource(in Rectangle rect, in UndertaleTexturePageItem texture, int diffW = 0, int diffH = 0)
        {
            Bitmap spriteBMP = CreateSpriteBitmap(rect, in texture, diffW, diffH);

            IntPtr bmpPtr = spriteBMP.GetHbitmap();
            ImageSource spriteSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmpPtr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(bmpPtr);
            spriteBMP.Dispose();
            spriteSrc.Freeze(); // allow UI thread access

            return spriteSrc;
        }
        private void ProcessTileSet(string textureName, Bitmap bmp, List<Tuple<uint, uint, uint, uint>> tileRectList)
        {
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            int depth = Image.GetPixelFormatSize(data.PixelFormat) / 8;

            int bufferLen = data.Stride * bmp.Height;
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

            Marshal.Copy(data.Scan0, buffer, 0, bufferLen);

            _ = Parallel.ForEach(tileRectList, (tileRect) =>
            {
                int x = (int)tileRect.Item1;
                int y = (int)tileRect.Item2;
                int w = (int)tileRect.Item3;
                int h = (int)tileRect.Item4;

                if (w == 0 || h == 0)
                    return;

                /// Sometimes, tile size can be bigger than texture size
                /// (for example, BG tile of "room_torielroom")
                /// Also, it can be out of texture bounds
                /// (for example, tile 10055649 of "room_fire_core_topright")
                /// (both examples are from Undertale)
                /// This algorithm doesn't support that, so this tile will be processed by "CreateSpriteSource()"
                if (w > data.Width || h > data.Height || x + w > data.Width || y + h > data.Height)
                    return;

                int bufferResLen = w * h * depth;
                byte[] bufferRes = ArrayPool<byte>.Shared.Rent(bufferResLen); // may return bigger array than requested

                // Source - https://stackoverflow.com/a/9691388/12136394
                // There was faster solution, but it uses "unsafe" code
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w * depth; j += depth)
                    {
                        int origIndex = (y * data.Stride) + (i * data.Stride) + (x * depth) + j;
                        int croppedIndex = (i * w * depth) + j;

                        Buffer.BlockCopy(buffer, origIndex, bufferRes, croppedIndex, depth);
                    }
                }

                Bitmap tileBMP = new(w, h);
                BitmapData dataNew = tileBMP.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, data.PixelFormat);
                Marshal.Copy(bufferRes, 0, dataNew.Scan0, bufferResLen);
                tileBMP.UnlockBits(dataNew);
                ArrayPool<byte>.Shared.Return(bufferRes);

                IntPtr bmpPtr = tileBMP.GetHbitmap();
                ImageSource spriteSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmpPtr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                DeleteObject(bmpPtr);
                tileBMP.Dispose();

                spriteSrc.Freeze(); // allow UI thread access

                Tuple<string, Tuple<uint, uint, uint, uint>> tileKey = new(textureName, new((uint)x, (uint)y, (uint)w, (uint)h));
                tileCache.TryAdd(tileKey, spriteSrc);
            });

            bmp.UnlockBits(data);
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
        private static UndertaleCachedImageLoader loader = new();
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is null) // tile
                return null;

            if ((uint)values[3] == 0 || (uint)values[4] == 0) // width, height
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

            int index = (int)(float)values[1];
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
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject([In] IntPtr hObject);

        // Tile text. page, tile ID - tile pixel data
        public static ConcurrentDictionary<Tuple<string, uint>, Bitmap> TileCache { get; set; } = new();
        private static readonly ConcurrentDictionary<string, Bitmap> tilePageCache = new();

        public static void Reset()
        {
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

            try
            {
                string texName = tilesBG.Texture?.Name?.Content;
                if (texName is null or "PageItem Unknown Index")
                {
                    texName = ((Application.Current.MainWindow as MainWindow).Data.TexturePageItems.IndexOf(tilesBG.Texture) + 1).ToString();
                    if (texName == "0")
                        return null;
                }

                Bitmap tilePageBMP;
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

                BitmapData data = tilePageBMP.LockBits(new Rectangle(0, 0, tilePageBMP.Width, tilePageBMP.Height), ImageLockMode.ReadOnly, tilePageBMP.PixelFormat);
                int depth = Image.GetPixelFormatSize(data.PixelFormat) / 8;
                byte[] buffer = new byte[data.Stride * tilePageBMP.Height];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                tilePageBMP.UnlockBits(data);

                int w = (int)tilesBG.GMS2TileWidth;
                int h = (int)tilesBG.GMS2TileHeight;
                int outX = (int)tilesBG.GMS2OutputBorderX;
                int outY = (int)tilesBG.GMS2OutputBorderY;
                int tileRows = (int)Math.Ceiling(tilesBG.GMS2TileCount / (double)tilesBG.GMS2TileColumns);
                System.Drawing.Imaging.PixelFormat format = tilePageBMP.PixelFormat;

                bool outOfBounds = false;
                _ = Parallel.For(0, tileRows, (y) =>
                {
                    int y1 = ((y + 1) * outY) + (y * (h + outY));

                    for (int x = 0; x < tilesBG.GMS2TileColumns; x++)
                    {
                        int x1 = ((x + 1) * outX) + (x * (w + outX));

                        if (x1 + w > data.Width || y1 + h > data.Height)
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
                                    int origIndex = (y1 * data.Stride) + (i * data.Stride) + (x1 * depth) + j;
                                    int croppedIndex = (i * w * depth) + j;

                                    Buffer.BlockCopy(buffer, origIndex, bufferRes, croppedIndex, depth);
                                }
                            }
                        }

                        Bitmap tileBMP = new(w, h);
                        BitmapData tileData = tileBMP.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, format);
                        Marshal.Copy(bufferRes, 0, tileData.Scan0, bufferResLen);
                        tileBMP.UnlockBits(tileData);
                        ArrayPool<byte>.Shared.Return(bufferRes);

                        TileCache.TryAdd(new(texName, (uint)((tilesBG.GMS2TileColumns * y) + x)), tileBMP);
                    }
                });

                if (outOfBounds)
                {
                    MainWindow.ShowError($"Tileset of \"{tilesData.ParentLayer.LayerName.Content}\" tile layer has wrong parameters (tile size, output border, etc.).\n" +
                                          "It can't be displayed.", false);
                    return "Error";
                }

                return cache ? null : CreateLayerSource(in tilesData, in tilesBG, in w, in h);
            }
            catch (Exception ex)
            {
                MainWindow.ShowError($"An error occured while rendering tile layer \"{tilesData.ParentLayer.LayerName.Content}\".\n\n{ex}", false);
                return "Error";
            }
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public ImageSource CreateLayerSource(in Layer.LayerTilesData tilesData, in UndertaleBackground tilesBG, in int w, in int h)
        {
            Bitmap layerBMP = new(w * (int)tilesData.TilesX, h * (int)tilesData.TilesY);
            uint maxID = tilesData.Background.GMS2TileIds.Select(x => x.ID).Max();

            using Graphics g = Graphics.FromImage(layerBMP);
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

                        Bitmap resBMP = (Bitmap)TileCache[new(tilesBG.Texture.Name.Content, realID)].Clone();

                        switch (id >> 28)
                        {
                            case 1:
                                resBMP.RotateFlip(RotateFlipType.RotateNoneFlipX);
                                break;
                            case 2:
                                resBMP.RotateFlip(RotateFlipType.RotateNoneFlipY);
                                break;
                            case 3:
                                resBMP.RotateFlip(RotateFlipType.RotateNoneFlipXY);
                                break;
                            case 4:
                                resBMP.RotateFlip(RotateFlipType.Rotate90FlipNone);
                                break;
                            case 5:
                                resBMP.RotateFlip(RotateFlipType.Rotate90FlipY);
                                break;
                            case 6:
                                resBMP.RotateFlip(RotateFlipType.Rotate270FlipY);
                                break;
                            case 7:
                                resBMP.RotateFlip(RotateFlipType.Rotate270FlipNone);
                                break;

                            default:
                                Debug.WriteLine("Tile of " + tilesData.ParentLayer.LayerName + " located at (" + x + ", " + y + ") has unknown flag.");
                                break;
                        }

                        g.DrawImageUnscaled(resBMP, x * w, y * h);

                        resBMP.Dispose();
                    }
                    else
                        g.DrawImageUnscaled(TileCache[new(tilesBG.Texture.Name.Content, id)], x * w, y * h);
                }
            }

            IntPtr bmpPtr = layerBMP.GetHbitmap();
            ImageSource spriteSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmpPtr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(bmpPtr);
            layerBMP.Dispose();

            return spriteSrc;
        }
    }
}
