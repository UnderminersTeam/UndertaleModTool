// Basically a mashup of ExportAllSprites and DumpSpecificCode

using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

bool padded = ScriptQuestion("Export all sprites WITH padding?");

string texFolder = PromptChooseDirectory();
if (texFolder is null)
    throw new ScriptException("The export folder was not set.");
Directory.CreateDirectory(Path.Combine(texFolder, "Sprites"));
texFolder = Path.Combine(texFolder, "Sprites");

TextureWorker worker = new TextureWorker();

List<UndertaleSprite> spritesToDump = new List<UndertaleSprite>();
List<String> splitStringsList = new List<String>();

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
        if (listElement.Equals(spr.Name.Content, StringComparison.InvariantCultureIgnoreCase))
        {
            spritesToDump.Add(spr);
        }
    }
}

SetProgressBar(null, "Sprites", 0, spritesToDump.Count);
StartProgressBarUpdater();



await Task.Run(() => {
    foreach(UndertaleSprite sprToDump in spritesToDump)
    {
        DumpSprite(sprToDump);
    }
});

worker.Cleanup();
await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + texFolder);

void DumpSprite(UndertaleSprite sprite)
{
    for (int i = 0; i < sprite.Textures.Count; i++)
    {
        if (sprite.Textures[i]?.Texture is not null)
        {
            worker.ExportAsPNG(sprite.Textures[i].Texture, Path.Combine(texFolder , sprite.Name.Content + "_" + i + ".png"), null, padded); // Include padding to make sprites look neat!
        }
    }
    IncrementProgress();
}