// Basically a mashup of ExportAllSprites and DumpSpecificCode

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

bool padded = ScriptQuestion("Export sprites with padding?");

List<UndertaleSprite> spritesToDump = new();
List<String> splitStringsList = new();

string inputtedText = SimpleTextInput("Menu", "Enter the name of the sprites", "", true);
string[] individualLineArray = inputtedText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
foreach (var oneLine in individualLineArray)
{
    splitStringsList.Add(oneLine.Trim());
}
foreach (string listElement in splitStringsList)
{
    foreach (UndertaleSprite spr in Data.Sprites)
    {
        if (spr is null)
        {
            continue;
        }
        if (listElement.Equals(spr.Name.Content, StringComparison.InvariantCultureIgnoreCase))
        {
            spritesToDump.Add(spr);
        }
    }
}

SetProgressBar(null, "Sprites", 0, spritesToDump.Count);
StartProgressBarUpdater();

TextureWorker worker = null;
using (worker = new())
{
    await Task.Run(() =>
    {
        foreach (UndertaleSprite sprToDump in spritesToDump)
        {
            DumpSprite(sprToDump);
        }
    });
}
await StopProgressBarUpdater();
HideProgressBar();

void DumpSprite(UndertaleSprite sprite)
{
    for (int i = 0; i < sprite.Textures.Count; i++)
    {
        if (sprite.Textures[i]?.Texture is not null)
        {
            worker.ExportAsPNG(sprite.Textures[i].Texture, Path.Combine(texFolder, $"{sprite.Name.Content}_{i}.png"), null, padded);
        }
    }
    IncrementProgress();
}