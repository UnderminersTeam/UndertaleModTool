using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib.Util;

// By Grossley

if (!ScriptQuestion("Visual glitches are very likely to occur in game. Do you accept this?"))
{
    ScriptError("Aborted!");
    return;
}

TextureWorker worker = new TextureWorker();
double scale = -1;
bool SelectScale = true;

if (SelectScale)
{
    bool success = false;
    while (scale <= 0 || scale > 10)
    {
        while (!success)
        {
            success = Double.TryParse(SimpleTextInput("Enter scale between 0 and 10 (not including 0).", "Enter scale", "", false), out scale);
        }
        success = false;
    }
}
else
    scale = 2;

for (var i = 0; i < Data.EmbeddedTextures.Count; i++)
{
    ScaleEmbeddedTexture(Data.EmbeddedTextures[i]);
}
for (int i = 0; i < Data.TexturePageItems.Count; i++)
{
    var tpage = Data.TexturePageItems[i];
    double offset = (Math.Pow(2, Math.Floor(Math.Abs(Math.Log2(scale))) - (Math.Log2(scale) >= 0 ? 1 : 0)));
    offset *= (Math.Log2(scale) >= 0 ? 1 : Math.Floor((Math.Log2(scale))) / 2);
    double sourceOffset = -(offset + (Math.Log2(scale) >= 0 ? 0 : Math.Floor(Math.Abs(Math.Log2(scale)))));
    tpage.SourceX = (ushort)((tpage.SourceX * scale) + sourceOffset);
    tpage.SourceY = (ushort)((tpage.SourceY * scale) + sourceOffset);
    tpage.TargetX = (ushort)(tpage.TargetX * scale);
    tpage.TargetY = (ushort)(tpage.TargetY * scale);
    double newOffset = -(offset >= 0 ? 0 : offset * 0.5);
    tpage.SourceWidth = (ushort)((tpage.SourceWidth * scale) + newOffset);
    tpage.SourceHeight = (ushort)((tpage.SourceHeight * scale) + newOffset);
    tpage.TargetWidth = (ushort)((tpage.TargetWidth * scale) + newOffset);
    tpage.TargetHeight = (ushort)((tpage.TargetHeight * scale) + newOffset);
    tpage.BoundingWidth = (ushort)(tpage.BoundingWidth * scale);
    tpage.BoundingHeight = (ushort)(tpage.BoundingHeight * scale);
}
foreach (UndertaleFont fnt in Data.Fonts)
{
    //fnt.ScaleX = scale;
    //fnt.ScaleY = scale;
    foreach (UndertaleFont.Glyph glyph in fnt.Glyphs)
    {
        double offset = (Math.Pow(2, Math.Floor(Math.Abs(Math.Log2(scale))) - (Math.Log2(scale) >= 0 ? 1 : 0)));
        offset *= (Math.Log2(scale) >= 0 ? 1 : Math.Floor((Math.Log2(scale))) / 2);
        double sourceOffset = -(offset + (Math.Log2(scale) >= 0 ? 0 : Math.Floor(Math.Abs(Math.Log2(scale)))));
        double newOffset = -(offset >= 0 ? 0 : offset * 0.5);
        glyph.SourceX = (ushort)((glyph.SourceX * scale) + sourceOffset);
        glyph.SourceY = (ushort)((glyph.SourceY * scale) + sourceOffset);
        glyph.SourceWidth = (ushort)((glyph.SourceWidth * scale) + newOffset);
        glyph.SourceHeight = (ushort)((glyph.SourceHeight * scale) + newOffset);
        glyph.Shift = (short)(((double)glyph.Shift) * ((double)scale));
        glyph.Offset = (short)(((double)glyph.Offset) * ((double)scale));
    }
}
foreach (UndertaleRoom room in Data.Rooms)
{
    foreach (UndertaleRoom.Background abc123 in room.Backgrounds)
    {
        if (abc123.Enabled)
        {
            abc123.Stretch = true;
        }
    }
    //room.Width = (uint)((double)(room.Width) * ((double)scale));
    //room.Height = (uint)((double)(room.Height) * ((double)scale));
    //room.Top = (uint)((double)(room.Top) * ((double)scale));
    //room.Left = (uint)((double)(room.Left) * ((double)scale));
    //room.Right = (uint)((double)(room.Right) * ((double)scale));
    //room.Bottom = (uint)((double)(room.Bottom) * ((double)scale));
    //foreach (UndertaleRoom.View abc123 in room.Views)
    //{
    //abc123.ViewX = (int)((double)(abc123.ViewX) * ((double)scale));
    //abc123.ViewY = (int)((double)(abc123.ViewY) * ((double)scale));
    //abc123.ViewWidth = (int)((double)(abc123.ViewWidth) * ((double)scale));
    //abc123.ViewHeight = (int)((double)(abc123.ViewHeight) * ((double)scale));
    //abc123.PortX = (int)((double)(abc123.PortX) * ((double)scale));
    //abc123.PortY = (int)((double)(abc123.PortY) * ((double)scale));
    //abc123.PortWidth = (int)((double)(abc123.PortWidth) * ((double)scale));
    //abc123.PortHeight = (int)((double)(abc123.PortHeight) * ((double)scale));
    //abc123.BorderX = (uint)((double)(abc123.BorderX) * ((double)scale));
    //abc123.BorderY = (uint)((double)(abc123.BorderY) * ((double)scale));
    //}
    foreach (UndertaleRoom.GameObject abc123 in room.GameObjects)
    {
        //abc123.X = (int)((double)(abc123.X) / ((double)scale));
        //abc123.Y = (int)((double)(abc123.Y) / ((double)scale));
        //abc123.ScaleX = (float)((double)(abc123.ScaleX) / ((double)scale));
        //abc123.ScaleY = (float)((double)(abc123.ScaleY) / ((double)scale));
    }
    foreach (UndertaleRoom.Tile abc123 in room.Tiles)
    {
        //abc123.X = (int)((double)(abc123.X) * ((double)scale));
        //abc123.Y = (int)((double)(abc123.Y) * ((double)scale));
        abc123.SourceX = (uint)((double)(abc123.SourceX) * ((double)scale));
        abc123.SourceY = (uint)((double)(abc123.SourceY) * ((double)scale));
        abc123.Width = (uint)((double)(abc123.Width) * ((double)scale));
        abc123.Height = (uint)((double)(abc123.Height) * ((double)scale));
        abc123.ScaleX = (float)((double)(abc123.ScaleX) / ((double)scale)); ;
        abc123.ScaleY = (float)((double)(abc123.ScaleY) / ((double)scale)); ;
    }
}

ChangeSelection(Data.Rooms.ByName("room_ruins1"));
//ChangeSelection(Data.TexturePageItems[2832]);

void ScaleEmbeddedTexture(UndertaleEmbeddedTexture tex)
{
    Bitmap embImage = worker.GetEmbeddedTexture(tex);
    embImage = ResizeBitmap(embImage, (int)(embImage.Width * scale), (int)(embImage.Height * scale));
    embImage.SetResolution(96.0F, 96.0F);
    try
    {
        var width = (uint)embImage.Width;
        var height = (uint)embImage.Height;
        if ((width & (width - 1)) != 0 || (height & (height - 1)) != 0)
        {
            //ScriptError("WARNING: texture page dimensions are not powers of 2. Sprite blurring is very likely in game.", "Unexpected texture dimensions");
        }
        using (var stream = new MemoryStream())
        {
            embImage.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            tex.TextureData.TextureBlob = stream.ToArray();
        }
    }
    catch (Exception ex)
    {
        //ScriptError("Failed to import file: " + ex.Message, "Failed to import file");
    }
}

private Bitmap ResizeBitmap(Bitmap sourceBMP, int width, int height)
{
    Bitmap result = new Bitmap(width, height);
    using (Graphics g = Graphics.FromImage(result))
    {
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.DrawImage(sourceBMP, 0, 0, width, height);
    }
    return result;
}
