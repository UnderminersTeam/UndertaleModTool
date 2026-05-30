/*
    From "ExportSpritesAsGif.csx":

    "Exports sprites as a GIF.
    Script made by CST1229, with parts based off of ExportAllSprites.csx.
    
    Was originally ExportSpritesAsGIFDLL.csx and used an external library,
    but UTMT now uses ImageMagick and that has gif support so I'm using it."
*/

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using UndertaleModLib.Util;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;
using System.Threading.Tasks;
using ImageMagick;

EnsureDataLoaded();

var builder = CreateScriptOptionsBuilder()
    .AddDirectory("folder", "Output Folder:")
    .AddText("patterns", "Names (one per line, leave empty for all):", multiline: true)
    .AddRadio("filterMode", "Filter mode:", "Exact", "Regex", "Wildcard")
    .AddBool("padded", "Export sprites with padding?")
    .AddBool("useSubDirectories", "Export sprites into subdirectories?")
    .AddBool("asGif", "Export as GIF");

var result = ShowScriptOptionsDialog("Export Sprites", builder);
if (result is null)
{
    return;
}

string texFolder = result["folder"] as string;

if (!Directory.Exists(texFolder))
{
    ScriptError("The specified output folder does not exist.");
    return;
}

string rawPatterns = result["patterns"] as string;
bool exportAll = string.IsNullOrWhiteSpace(rawPatterns);
string[] patterns = rawPatterns.Split("\n", StringSplitOptions.RemoveEmptyEntries);

NameFilterMode filterMode = Enum.Parse<NameFilterMode>(result["filterMode"] as string);

bool padded = result["padded"] as bool? == true;
bool useSubDirectories = result["useSubDirectories"] as bool? == true;
bool asGif = result["asGif"] as bool? == true;

List<UndertaleSprite> spritesToExport = new();
foreach (var sprite in Data.Sprites)
{
    if (sprite is null) continue;
    if (!exportAll)
    {
        bool match = false;
        foreach (string pattern in patterns)
        {
            if (NameFilter.IsMatch(sprite.Name.Content, pattern, filterMode))
            {
                match = true;
                break;
            }
        }
        if (!match) continue;
    }
    spritesToExport.Add(sprite);
}

if (asGif)
{
    SetProgressBar(null, "Exporting sprites to GIF...", 0, spritesToExport.Count);
    StartProgressBarUpdater();

    using TextureWorker worker = new TextureWorker();
    await Task.Run(() =>
    {
        Parallel.ForEach(spritesToExport, sprite =>
        {
            string outputFolder = texFolder;
            if (useSubDirectories)
            {
                outputFolder = Paths.JoinVerifyWithinDirectory(outputFolder, sprite.Name.Content);
                Directory.CreateDirectory(outputFolder);
            }

            using MagickImageCollection gif = new();
            bool anyValidFrames = false;
            for (int picCount = 0; picCount < sprite.Textures.Count; picCount++)
            {
                if (sprite.Textures[picCount]?.Texture != null)
                {
                    IMagickImage<byte> image = worker.GetTextureFor(sprite.Textures[picCount].Texture, sprite.Name.Content + " (frame " + picCount + ")", true);
                    image.GifDisposeMethod = GifDisposeMethod.Previous;
                    if (sprite.IsSpecialType && Data.IsGameMaker2())
                    {
                        if (sprite.GMS2PlaybackSpeed == 0f)
                            image.AnimationDelay = 10;
                        else if (sprite.GMS2PlaybackSpeedType is AnimSpeedType.FramesPerGameFrame)
                            image.AnimationDelay = (uint)Math.Max((int)(Math.Round(100f / (sprite.GMS2PlaybackSpeed * Data.GeneralInfo.GMS2FPS))), 1);
                        else
                            image.AnimationDelay = (uint)Math.Max((int)(Math.Round(100 / sprite.GMS2PlaybackSpeed)), 1);
                    }
                    else
                    {
                        image.AnimationDelay = 3;
                    }
                    gif.Add(image);
                    anyValidFrames = true;
                }
            }
            if (anyValidFrames)
            {
                gif.Optimize();
                gif.Write(Path.Join(outputFolder, sprite.Name.Content + ".gif"));
            }

            IncrementProgressParallel();
        });
    });

    await StopProgressBarUpdater();
    HideProgressBar();
}
else
{
    ConcurrentDictionary<string, ConcurrentBag<TextureToExport>> texturesToExport = new();

    SetProgressBar(null, "Generating Cache", 0, spritesToExport.Count);
    StartProgressBarUpdater();

    await Task.Run(() => Parallel.ForEach(spritesToExport, spr =>
    {
        FetchTexturesFromSprite(spr, texturesToExport);
    }));

    await StopProgressBarUpdater();
    HideProgressBar();

    SetProgressBar(null, "Exporting Texture Pages", 0, texturesToExport.Count);
    StartProgressBarUpdater();

    await Task.Run(() => ExportTextures(texturesToExport));

    await StopProgressBarUpdater();
    HideProgressBar();
}

void FetchTexturesFromSprite(UndertaleSprite sprite, ConcurrentDictionary<string, ConcurrentBag<TextureToExport>> texturesToExport)
{
    if (sprite is not { SSpriteType: UndertaleSprite.SpriteType.Normal, Textures.Count: > 0 })
    {
        IncrementProgressParallel();
        return;
    }
    
    string outputFolder = texFolder;
    if (useSubDirectories)
    {
        outputFolder = Paths.JoinVerifyWithinDirectory(outputFolder, sprite.Name.Content);

        Directory.CreateDirectory(outputFolder);
    }

    for (int i = 0; i < sprite.Textures.Count; i++)
    {
        if (sprite.Textures[i]?.Texture is not null)
        {
            UndertaleTexturePageItem pageItem = sprite.Textures[i].Texture;
            
            // Get the bag, or create it if necessary
            var bag = texturesToExport.GetOrAdd(pageItem.TexturePage.Name.Content, _ => new ConcurrentBag<TextureToExport>());
        
            bag.Add(new TextureToExport(pageItem, Paths.JoinVerifyWithinDirectory(outputFolder, $"{sprite.Name.Content}_{i}.png")));
        }
    }
    IncrementProgressParallel();
}

void ExportTextures(ConcurrentDictionary<string, ConcurrentBag<TextureToExport>> texturesToExport)
{
    int totalCores = Environment.ProcessorCount;
    int outerLimit = Math.Max(1, totalCores / 4);
    Parallel.ForEach(texturesToExport, new ParallelOptions { MaxDegreeOfParallelism = outerLimit }, kvp =>
    {
        using (TextureWorker localWorker = new TextureWorker())
        {
            foreach (TextureToExport tte in kvp.Value)
            {
                localWorker.ExportAsPNG(tte.PageItem, tte.FileExportLocation, null, padded);
            }
        }
        IncrementProgressParallel();
    });
}

public class TextureToExport
{
    public UndertaleTexturePageItem PageItem { get; set; }
    public UndertaleEmbeddedTexture Page => PageItem.TexturePage;
    public string FileExportLocation { get; set; }
    
    public TextureToExport(UndertaleTexturePageItem pageItem, string fileExportLocation) => (PageItem, FileExportLocation) = (pageItem, fileExportLocation);
}
