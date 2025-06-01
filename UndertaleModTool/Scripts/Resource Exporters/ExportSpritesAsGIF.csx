/*
    Exports sprites as a GIF.
    Script made by CST1229, with parts based off of ExportAllSprites.csx.
    
    Was originally ExportSpritesAsGIFDLL.csx and used an external library,
    but UTMT now uses ImageMagick and that has gif support so I'm using it.
 */

// revision 2: handle breaking Magick.NET changes

using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using UndertaleModLib.Scripting;
using ImageMagick;

EnsureDataLoaded();

string folder = PromptChooseDirectory();
if (folder is null)
{
    return;
}

string filter = SimpleTextInput("Filter sprites", "String that the sprite names must start with (or leave blank to export all):", "", false);
await ExtractSprites(folder, filter);

async Task ExtractSprites(string folder, string prefix)
{
    using TextureWorker worker = new TextureWorker();
    IList<UndertaleSprite> sprites = Data.Sprites;
    if (prefix != "")
    {
        sprites = new List<UndertaleSprite> { };
        foreach (UndertaleSprite sprite in Data.Sprites)
        {
            if (sprite.Name.Content.StartsWith(prefix))
            {
                sprites.Add(sprite);
            }
        }
    }

    SetProgressBar(null, "Exporting sprites to GIF...", 0, sprites.Count);
    StartProgressBarUpdater();

    bool isParallel = true;
    await Task.Run(() => 
    {
        if (isParallel) 
        {
            Parallel.ForEach(sprites, (sprite) => 
            {
                IncrementProgressParallel();
                ExtractSprite(sprite, folder, worker);
            });
        } 
        else 
        {
            foreach (UndertaleSprite sprite in sprites) 
            {
                ExtractSprite(sprite, folder, worker);
                IncrementProgressParallel();
            }
        }
    });
    await StopProgressBarUpdater();
    HideProgressBar();
}

void ExtractSprite(UndertaleSprite sprite, string folder, TextureWorker worker)
{
    using MagickImageCollection gif = new();
    for (int picCount = 0; picCount < sprite.Textures.Count; picCount++)
    {
        if (sprite.Textures[picCount]?.Texture != null)
        {
            IMagickImage<byte> image = worker.GetTextureFor(sprite.Textures[picCount].Texture, sprite.Name.Content + " (frame " + picCount + ")", true);
            image.GifDisposeMethod = GifDisposeMethod.Previous;
            // the animation delay unit seems to be 100 per second, not milliseconds (1000 per second)
            if (sprite.IsSpecialType && Data.IsGameMaker2()) 
            {
                if (sprite.GMS2PlaybackSpeed == 0f) 
                {
                    image.AnimationDelay = 10;
                } 
                else if (sprite.GMS2PlaybackSpeedType is AnimSpeedType.FramesPerGameFrame) 
                {
                    image.AnimationDelay = (uint)Math.Max((int)(Math.Round(100f / (sprite.GMS2PlaybackSpeed * Data.GeneralInfo.GMS2FPS))), 1);
                } 
                else 
                {
                    image.AnimationDelay = (uint)Math.Max((int)(Math.Round(100 / sprite.GMS2PlaybackSpeed)), 1);
                }
            } 
            else 
            {
                image.AnimationDelay = 3; // 30fps
            }
            gif.Add(image);
        }
    }
    gif.Optimize();
    gif.Write(Path.Join(folder, sprite.Name.Content + ".gif"));
}