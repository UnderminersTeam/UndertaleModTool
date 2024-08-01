using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

// By Grossley

EnsureDataLoaded();

if (Data.TextureGroupInfo == null)
{
    ScriptError("No TGIN!");
    return;
}
ScriptMessage("Exports graphics by texture group.");
bool padding = ScriptQuestion("Use padding?");
int progress_tgin = 0;
TextureWorker worker = new TextureWorker();
foreach (UndertaleTextureGroupInfo tgin in Data.TextureGroupInfo)
{
    int progress = 0;
    int sum = 0;
    if (tgin.TexturePages != null)
        sum += tgin.TexturePages.Count;
    if (tgin.Sprites != null)
        sum += tgin.Sprites.Count;
    if (tgin.Fonts != null)
        sum += tgin.Fonts.Count;
    if (tgin.Tilesets != null)
        sum += tgin.Tilesets.Count;
    UpdateProgressBar(null, "Processing \"" + tgin.Name.Content + "\" (TGIN Group " + (progress_tgin++) + ")", progress, sum);
    string output_folder = Path.Combine(GetFolder(FilePath), "TextureGroups"); // The folder data.win is located in.
    Directory.CreateDirectory(output_folder);
    output_folder = Path.Combine(output_folder, tgin.Name.Content); // The folder data.win is located in.
    Directory.CreateDirectory(output_folder);
    if (tgin.TexturePages != null)
    {
        for (var i = 0; i < tgin.TexturePages.Count; i++)
        {
            UpdateProgressBar(null, "Processing \"" + tgin.Name.Content + "\" EmbeddedTextures (TGIN Group " + (progress_tgin) + ")", progress++, sum);
            DumpEmbeddedTexturePage(output_folder, tgin.TexturePages[i].Resource);
        }
    }
    if (tgin.Sprites != null)
    {
        for (var i = 0; i < tgin.Sprites.Count; i++)
        {
            UpdateProgressBar(null, "Processing \"" + tgin.Name.Content + "\" Sprites (TGIN Group " + (progress_tgin) + ")", progress++, sum);
            DumpSprite(output_folder, tgin.Sprites[i].Resource);
        }
    }
    if (tgin.Fonts != null)
    {
        for (var i = 0; i < tgin.Fonts.Count; i++)
        {
            UpdateProgressBar(null, "Processing \"" + tgin.Name.Content + "\" Fonts (TGIN Group " + (progress_tgin) + ")", progress++, sum);
            DumpFont(output_folder, tgin.Fonts[i].Resource);
        }
    }
    if (tgin.Tilesets != null)
    {
        for (var i = 0; i < tgin.Tilesets.Count; i++)
        {
            UpdateProgressBar(null, "Processing \"" + tgin.Name.Content + "\" Tilesets (TGIN Group " + (progress_tgin) + ")", progress++, sum);
            DumpTileset(output_folder, tgin.Tilesets[i].Resource);
        }
    }
}
HideProgressBar();
ScriptMessage(@"All graphics texture groups successfully exported.
Graphics are in the ""TextureGroups"" folder in the data.win directory.");

void DumpEmbeddedTexturePage(string output_folder, UndertaleEmbeddedTexture Emb)
{
    string exportedTexturesFolder = Path.Combine(output_folder, "EmbeddedTextures");
    Directory.CreateDirectory(exportedTexturesFolder);
    try
    {
        File.WriteAllBytes(Path.Combine(exportedTexturesFolder, Data.EmbeddedTextures.IndexOf(Emb) + ".png"), Emb.TextureData.TextureBlob);
    }
    catch (Exception ex) 
    {
        ScriptMessage("Failed to export file: " + ex.Message);
    }
}
void DumpSprite(string output_folder, UndertaleSprite Spr)
{
    for (int i = 0; i < Spr.Textures.Count; i++)
    {
        if (Spr.Textures[i]?.Texture != null)
        {
            string exportedTexturesFolder = Path.Combine(output_folder, "Sprites");
            Directory.CreateDirectory(exportedTexturesFolder);
            UndertaleTexturePageItem tex = Spr.Textures[i].Texture;
            worker.ExportAsPNG(tex, Path.Combine(exportedTexturesFolder, Spr.Name.Content + "_" + i + ".png"), null, padding); // Include padding to make sprites look neat!
        }
    }
}
void DumpFont(string output_folder, UndertaleFont Fnt)
{
    if (Fnt.Texture != null)
    {
        string exportedTexturesFolder = Path.Combine(output_folder, "Fonts");
        Directory.CreateDirectory(exportedTexturesFolder);
        UndertaleTexturePageItem tex = Fnt.Texture;
        worker.ExportAsPNG(tex, Path.Combine(exportedTexturesFolder, Fnt.Name.Content + ".png"));
    }
}
void DumpTileset(string output_folder, UndertaleBackground Tile)
{
    if (Tile.Texture != null)
    {
        string exportedTexturesFolder = Path.Combine(output_folder, "Tilesets");
        Directory.CreateDirectory(exportedTexturesFolder);
        UndertaleTexturePageItem tex = Tile.Texture;
        worker.ExportAsPNG(tex, Path.Combine(exportedTexturesFolder, Tile.Name.Content + ".png"));
    }
}
string GetFolder(string path) 
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}
