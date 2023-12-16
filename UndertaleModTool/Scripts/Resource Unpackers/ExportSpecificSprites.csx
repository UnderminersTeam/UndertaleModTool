//Basically a mashup of ExportAllSprites and DumpSpecificCode
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

bool padded = (ScriptQuestion("Export all sprites WITH padding?"));

string texFolder = PromptChooseDirectory();
if (texFolder == null)
    throw new ScriptException("The export folder was not set.");
texFolder = texFolder + "Sprites" + Path.DirectorySeparatorChar;
Directory.CreateDirectory(texFolder);

TextureWorker worker = new TextureWorker();

List<UndertaleSprite> spritesToDump = new List<UndertaleSprite>();
List<String> splitStringsList = new List<String>();

string InputtedText = "";
InputtedText = SimpleTextInput("Menu", "Enter the name of the sprites", InputtedText, true);
string[] IndividualLineArray = InputtedText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
foreach (var OneLine in IndividualLineArray)
{
    splitStringsList.Add(OneLine.Trim());
}
for (var j = 0; j < splitStringsList.Count; j++)
{
    foreach (UndertaleSprite spr in Data.Sprites)
    {
        if (splitStringsList[j].ToLower() == spr.Name.Content.ToLower())
        {
            spritesToDump.Add(spr);
        }
    }
}

SetProgressBar(null, "Sprites", 0, spritesToDump.Count);
StartProgressBarUpdater();



await Task.Run(() => {
    for (var j = 0; j < spritesToDump.Count; j++)
    {
        DumpSprite(spritesToDump[j]);
    }
});

worker.Cleanup();
await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + texFolder);

void DumpSprite(UndertaleSprite sprite)
{

    for (int i = 0; i < sprite.Textures.Count; i++)
        if (sprite.Textures[i]?.Texture != null)
            worker.ExportAsPNG(sprite.Textures[i].Texture, texFolder + sprite.Name.Content + "_" + i + ".png", null, padded); // Include padding to make sprites look neat!

    IncrementProgress();
}