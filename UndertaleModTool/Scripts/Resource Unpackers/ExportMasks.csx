// Made by Grossley
// Version 1 is from 12/07/2020
// This is no longer that version

using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

string texFolder = Path.Combine(Path.GetDirectoryName(FilePath), "Export_Masks");
if (Directory.Exists(texFolder))
{
    ScriptError("A mask export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(texFolder);

SetProgressBar(null, "Sprites", 0, Data.Sprites.Count);
StartProgressBarUpdater();

TextureWorker worker = null;
using (worker = new())
{
    await DumpSprites();
}

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage($"Export Complete.\n\nLocation: {texFolder}");

async Task DumpSprites()
{
    await Task.Run(() => Parallel.ForEach(Data.Sprites, DumpSprite));
}

void DumpSprite(UndertaleSprite sprite)
{
    for (int i = 0; i < sprite.CollisionMasks.Count; i++)
    {
        if (sprite.CollisionMasks[i]?.Data != null)
        {
            (uint maskWidth, uint maskHeight) = sprite.CalculateMaskDimensions(Data);
            TextureWorker.ExportCollisionMaskPNG(sprite.CollisionMasks[i], Path.Combine(texFolder, $"{sprite.Name.Content}_{i}.png"), (int)maskWidth, (int)maskHeight);
        }
    }

    IncrementProgressParallel();
}
