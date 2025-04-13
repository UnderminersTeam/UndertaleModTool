using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib.Util;
using ImageMagick;

// By Grossley

EnsureDataLoaded();

if (!ScriptQuestion("Visual glitches are very likely to occur in game. Do you accept this?"))
{
    ScriptError("Aborted!");
    return;
}

using (TextureWorker worker = new())
{
    double scale = -1;
    bool selectScale = true;

    if (selectScale)
    {
        bool success = false;
        while (scale <= 0 || scale > 10)
        {
            while (!success)
            {
                success = double.TryParse(SimpleTextInput("Enter scale between 0 and 10 (not including 0).", "Enter scale", "", false), out scale);
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
        if (fnt is null)
            continue;
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
        if (room is null)
            continue;
        foreach (UndertaleRoom.Background background in room.Backgrounds)
        {
            if (background.Enabled)
            {
                background.Stretch = true;
            }
        }
        //room.Width = (uint)((double)(room.Width) * ((double)scale));
        //room.Height = (uint)((double)(room.Height) * ((double)scale));
        //room.Top = (uint)((double)(room.Top) * ((double)scale));
        //room.Left = (uint)((double)(room.Left) * ((double)scale));
        //room.Right = (uint)((double)(room.Right) * ((double)scale));
        //room.Bottom = (uint)((double)(room.Bottom) * ((double)scale));
        //foreach (UndertaleRoom.View myView in room.Views)
        //{
        //myView.ViewX = (int)((double)(myView.ViewX) * ((double)scale));
        //myView.ViewY = (int)((double)(myView.ViewY) * ((double)scale));
        //myView.ViewWidth = (int)((double)(myView.ViewWidth) * ((double)scale));
        //myView.ViewHeight = (int)((double)(myView.ViewHeight) * ((double)scale));
        //myView.PortX = (int)((double)(myView.PortX) * ((double)scale));
        //myView.PortY = (int)((double)(myView.PortY) * ((double)scale));
        //myView.PortWidth = (int)((double)(myView.PortWidth) * ((double)scale));
        //myView.PortHeight = (int)((double)(myView.PortHeight) * ((double)scale));
        //myView.BorderX = (uint)((double)(myView.BorderX) * ((double)scale));
        //myView.BorderY = (uint)((double)(myView.BorderY) * ((double)scale));
        //}
        foreach (UndertaleRoom.GameObject myGameObject in room.GameObjects)
        {
            //myGameObject.X = (int)((double)(myGameObject.X) / ((double)scale));
            //myGameObject.Y = (int)((double)(myGameObject.Y) / ((double)scale));
            //myGameObject.ScaleX = (float)((double)(myGameObject.ScaleX) / ((double)scale));
            //myGameObject.ScaleY = (float)((double)(myGameObject.ScaleY) / ((double)scale));
        }
        foreach (UndertaleRoom.Tile myTile in room.Tiles)
        {
            //myTile.X = (int)((double)(myTile.X) * ((double)scale));
            //myTile.Y = (int)((double)(myTile.Y) * ((double)scale));
            myTile.SourceX = (uint)((double)(myTile.SourceX) * ((double)scale));
            myTile.SourceY = (uint)((double)(myTile.SourceY) * ((double)scale));
            myTile.Width = (uint)((double)(myTile.Width) * ((double)scale));
            myTile.Height = (uint)((double)(myTile.Height) * ((double)scale));
            myTile.ScaleX = (float)((double)(myTile.ScaleX) / ((double)scale)); ;
            myTile.ScaleY = (float)((double)(myTile.ScaleY) / ((double)scale)); ;
        }
    }

    ChangeSelection(Data.Rooms.ByName("room_ruins1"));
    //ChangeSelection(Data.TexturePageItems[2832]);

    void ScaleEmbeddedTexture(UndertaleEmbeddedTexture tex)
    {
        MagickImage embImage = worker.GetEmbeddedTexture(tex);
        using IMagickImage<byte> scaledEmbImage = TextureWorker.ResizeImage(embImage, (int)(embImage.Width * scale), (int)(embImage.Height * scale), PixelInterpolateMethod.Nearest);
        try
        {
            uint width = (uint)scaledEmbImage.Width;
            uint height = (uint)scaledEmbImage.Height;
            if ((width & (width - 1)) != 0 || (height & (height - 1)) != 0)
            {
                //ScriptError("WARNING: texture page dimensions are not powers of 2. Sprite blurring is very likely in game.", "Unexpected texture dimensions");
            }
            tex.TextureData.Image = GMImage.FromMagickImage(scaledEmbImage).ConvertToFormat(tex.TextureData.Image.Format);
        }
        catch (Exception ex)
        {
            ScriptError("Failed to import file: " + ex.Message, "Failed to import file");
        }
    }
}