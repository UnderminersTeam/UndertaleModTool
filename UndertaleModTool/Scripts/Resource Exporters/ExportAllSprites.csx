using System;
using System.IO;
using System.Text;
using System.Threading;
using UndertaleModLib.Util;
using System.Threading.Tasks;
using System.Collections.Concurrent;

EnsureDataLoaded();

string texFolder = PromptChooseDirectory();
if (texFolder is null)
{
    return;
}

// Prompt for export settings.
bool padded = ScriptQuestion("Export sprites with padding?");
bool useSubDirectories = ScriptQuestion("Export sprites into subdirectories?");

ConcurrentDictionary<string, ConcurrentBag<TextureToExport>> texturesToExport = new();

SetProgressBar(null, "Generating Cache", 0, Data.Sprites.Count);
StartProgressBarUpdater();

await Task.Run(() => Parallel.ForEach(Data.Sprites, spr =>
{
    FetchTexturesFromSprite(spr);
}));

await StopProgressBarUpdater();
HideProgressBar();



SetProgressBar(null, "Exporting Texture Pages", 0, texturesToExport.Count);
StartProgressBarUpdater();

await Task.Run(() => ExportTextures());

await StopProgressBarUpdater();
HideProgressBar();

void FetchTexturesFromSprite(UndertaleSprite sprite)
{
    // empty, null, or not raster image? we cant do anything with it.
    if (sprite is not { SSpriteType: UndertaleSprite.SpriteType.Normal, Textures.Count: > 0 })
    {
        IncrementProgressParallel();
        return;
    }
    
    string outputFolder = texFolder;
    if (useSubDirectories)
    {
        outputFolder = Path.Combine(outputFolder, sprite.Name.Content);

        Directory.CreateDirectory(outputFolder);
    }

    for (int i = 0; i < sprite.Textures.Count; i++)
    {
        if (sprite.Textures[i]?.Texture is not null)
        {
            UndertaleTexturePageItem pageItem = sprite.Textures[i].Texture;
            // get the bag, create it if necessary $$$
            var bag = texturesToExport.GetOrAdd(pageItem.TexturePage.Name.Content, _ => new ConcurrentBag<TextureToExport>());
        
            bag.Add(new TextureToExport(pageItem, Path.Combine(outputFolder, $"{sprite.Name.Content}_{i}.png")));
        }
    }
    IncrementProgressParallel();
}

void ExportTextures()
{
    int totalCores = Environment.ProcessorCount;
    int outerLimit = Math.Max(1, totalCores / 4); // save some memory
    Parallel.ForEach(texturesToExport, new ParallelOptions { MaxDegreeOfParallelism = outerLimit }, kvp =>
    {
        // separate worker for each page to bound memory usage
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
