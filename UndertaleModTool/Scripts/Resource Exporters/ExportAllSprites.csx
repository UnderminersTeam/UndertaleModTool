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

// Prompt for export settings.
bool padded = ScriptQuestion("Export sprites with padding?");
bool useSubDirectories = ScriptQuestion("Export sprites into subdirectories?");

SetProgressBar(null, "Sprites", 0, Data.Sprites.Count);
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
    if (sprite is not null)
    {
        string outputFolder = texFolder;
        if (useSubDirectories)
        {
            outputFolder = Path.Combine(outputFolder, sprite.Name.Content);
            if (sprite.Textures.Count > 0)
            {
                Directory.CreateDirectory(outputFolder);
            }
        }
            
        for (int i = 0; i < sprite.Textures.Count; i++)
        {
            if (sprite.Textures[i]?.Texture is not null)
            {
                worker.ExportAsPNG(sprite.Textures[i].Texture, Path.Combine(outputFolder, $"{sprite.Name.Content}_{i}.png"), null, padded);
            }
        }
    }

    IncrementProgressParallel();
}
