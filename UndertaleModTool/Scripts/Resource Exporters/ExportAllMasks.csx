using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

string texFolder = PromptChooseDirectory();
if (texFolder is null)
{
    return;
}

SetProgressBar(null, "Sprite masks", 0, Data.Sprites.Count);
StartProgressBarUpdater();

TextureWorker worker = null;
using (worker = new())
{
    await DumpSprites();
}

await StopProgressBarUpdater();
HideProgressBar();

async Task DumpSprites()
{
    await Task.Run(() => Parallel.ForEach(Data.Sprites, DumpSprite));
}

void DumpSprite(UndertaleSprite sprite)
{
    if (sprite is null)
    {
        return;
    }

    for (int i = 0; i < sprite.CollisionMasks.Count; i++)
    {
        if (sprite.CollisionMasks[i]?.Data is not null)
        {
            (int maskWidth, int maskHeight) = sprite.CalculateMaskDimensions(Data);
            TextureWorker.ExportCollisionMaskPNG(sprite.CollisionMasks[i], Path.Combine(texFolder, $"{sprite.Name.Content}_{i}.png"), maskWidth, maskHeight);
        }
    }

    IncrementProgressParallel();
}
