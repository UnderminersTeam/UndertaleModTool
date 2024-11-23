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
int processTgin = 0;

string mainOutputFolder = Path.Combine(Path.GetDirectoryName(FilePath), "TextureGroups");
Directory.CreateDirectory(mainOutputFolder);

TextureWorker worker = null;
using (worker = new())
{
    await Task.Run(() =>
    {
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
            UpdateProgressBar(null, $"Processing \"{tgin.Name.Content}\" (TGIN Group {processTgin++})", progress, sum);
            string outputFolder = Path.Combine(mainOutputFolder, tgin.Name.Content); // TODO: replace invalid characters?
            Directory.CreateDirectory(outputFolder);
            if (tgin.TexturePages != null)
            {
                for (var i = 0; i < tgin.TexturePages.Count; i++)
                {
                    UpdateProgressBar(null, $"Processing \"{tgin.Name.Content}\" EmbeddedTextures (TGIN Group {processTgin})", progress++, sum);
                    DumpEmbeddedTexturePage(outputFolder, tgin.TexturePages[i].Resource);
                }
            }
            if (tgin.Sprites != null)
            {
                for (var i = 0; i < tgin.Sprites.Count; i++)
                {
                    UpdateProgressBar(null, $"Processing \"{tgin.Name.Content}\" Sprites (TGIN Group {processTgin})", progress++, sum);
                    DumpSprite(outputFolder, tgin.Sprites[i].Resource);
                }
            }
            if (tgin.Fonts != null)
            {
                for (var i = 0; i < tgin.Fonts.Count; i++)
                {
                    UpdateProgressBar(null, $"Processing \"{tgin.Name.Content}\" Fonts (TGIN Group {processTgin})", progress++, sum);
                    DumpFont(outputFolder, tgin.Fonts[i].Resource);
                }
            }
            if (tgin.Tilesets != null)
            {
                for (var i = 0; i < tgin.Tilesets.Count; i++)
                {
                    UpdateProgressBar(null, $"Processing \"{tgin.Name.Content}\" Tilesets (TGIN Group {processTgin})", progress++, sum);
                    DumpTileset(outputFolder, tgin.Tilesets[i].Resource);
                }
            }
        }
    });
}
HideProgressBar();
ScriptMessage(@"All graphics texture groups successfully exported.
Graphics are in the ""TextureGroups"" folder in the data.win directory.");

void DumpEmbeddedTexturePage(string outputFolder, UndertaleEmbeddedTexture Emb)
{
    string exportedTexturesFolder = Path.Combine(outputFolder, "EmbeddedTextures");
    Directory.CreateDirectory(exportedTexturesFolder);
    try
    {
        using (FileStream fs = new(Path.Combine(exportedTexturesFolder, $"{Data.EmbeddedTextures.IndexOf(Emb)}.png"), FileMode.Create))
            Emb.TextureData.Image.SavePng(fs);
    }
    catch (Exception ex) 
    {
        ScriptMessage("Failed to export file: " + ex.Message);
    }
}
void DumpSprite(string outputFolder, UndertaleSprite spr)
{
    for (int i = 0; i < spr.Textures.Count; i++)
    {
        if (spr.Textures[i]?.Texture != null)
        {
            string exportedTexturesFolder = Path.Combine(outputFolder, "Sprites");
            Directory.CreateDirectory(exportedTexturesFolder);
            UndertaleTexturePageItem tex = spr.Textures[i].Texture;
            worker.ExportAsPNG(tex, Path.Combine(exportedTexturesFolder, $"{spr.Name.Content}_{i}.png"), null, padding); // Include padding to make sprites look neat!
        }
    }
}
void DumpFont(string outputFolder, UndertaleFont fnt)
{
    if (fnt.Texture != null)
    {
        string exportedTexturesFolder = Path.Combine(outputFolder, "Fonts");
        Directory.CreateDirectory(exportedTexturesFolder);
        UndertaleTexturePageItem tex = fnt.Texture;
        worker.ExportAsPNG(tex, Path.Combine(exportedTexturesFolder, $"{fnt.Name.Content}.png"));
    }
}
void DumpTileset(string outputFolder, UndertaleBackground Tile)
{
    if (Tile.Texture != null)
    {
        string exportedTexturesFolder = Path.Combine(outputFolder, "Tilesets");
        Directory.CreateDirectory(exportedTexturesFolder);
        UndertaleTexturePageItem tex = Tile.Texture;
        worker.ExportAsPNG(tex, Path.Combine(exportedTexturesFolder, $"{Tile.Name.Content}.png"));
    }
}
