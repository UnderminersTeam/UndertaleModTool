// Texture Repacker by JohnnyonFlame
// Licensed under GPLv3 for UndertaleModTool
// This script is meant to be edited by the user - search for "User Configurable".

// By allowing you to layout assets into different page sizes, this script can fix 
// or significantly lower stuttering on low end hardware due to high VRAM and 
// Texture Streaming pressure.

// Special thanks to:
// Jukka Jylänki (2010), for "A Thousand Ways to Pack the Bin - A Practical Approach to Two-Dimensional Rectangle Bin Packing"

// Notes:
// - Sometimes shaders will require pages to have specific geometry, such as palette LUTs,
// this script does attempt to keep that in mind by not allowing page items that don't share a page
// with other items to get thrown into common bins. If you have graphical glitches, you might want to
// investigate the input textures in a graphical debugger and compare.
// - Some GPUs will have troubles with Non Power of Two textures - check out "forcePOT" to work around that.
// - Reducing page sizes is a tradeoff, you might want to experiment with different sizes.

using System;
using System.Linq;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UndertaleModLib.Scripting;
using UndertaleModLib.Util;
using UndertaleModLib.Models;
using System.Numerics;
using ImageMagick;

public class Rect
{
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public int Right { get { return X + Width; } }
    public int Down { get { return Y + Height; } }
    public int Area { get { return Width * Height; } }
}

public class Split : Rect
{
    public bool Invalidated;

    public Split(int X, int Y, int Width, int Height)
    {
        this.X = X;
        this.Y = Y;
        this.Width = Width;
        this.Height = Height;
        this.Invalidated = false;
    }

    public bool containsRect(Rect rect)
    {
        return (rect.X >= this.X) && (rect.Y >= this.Y) && (this.Right >= rect.Right) && (this.Down >= rect.Down);
    }

    public bool overlapsRect(Rect rect)
    {
        return (((rect.X >= this.X) && (rect.X <= this.Right))
            ||  ((this.X >= rect.X) && (this.X <= rect.Right)))
            && (((rect.Y >= this.Y) && (rect.Y <= this.Down))
            ||  ((this.Y >= rect.Y) && (this.Y <= rect.Down)));
    }

    public bool fits(int Width, int Height)
    {
        return (this.Width >= Width) && (this.Height >= Height);
    }

    public IEnumerable<Split> splitNode(Rect rect)
    {
        // If the rect isn't contained or has been invalidated, create no new Splits and don't invalidate
        if (!overlapsRect(rect) || Invalidated)
            return new List<Split>();

        // Rect overlaps - invalidate it
        this.Invalidated = true;

        // And now split this rect in four - returning only splits with non-zero area
        return new List<Split> {
            new Split(this.X, this.Y, this.Width, rect.Y - this.Y),              /* up    */ 
            new Split(this.X, this.Y, rect.X - this.X, this.Height),             /* left  */ 
            new Split(this.X, rect.Down, this.Width, this.Down - rect.Down),     /* down  */ 
            new Split(rect.Right, this.Y, this.Right - rect.Right, this.Height), /* right */ 
        }.Where(item => item.Area > 0);
    }
};

public class TextureAtlas
{

    public int Width;
    public int Height;
    public int Padding;
    public List<Split> Splits;
    public List<Rect> Textures;

    public TextureAtlas(int Width, int Height, int Padding)
    {
        this.Splits = new List<Split> { new Split(0, 0, Width, Height) };
        this.Textures = new List<Rect>();
        this.Width = Width;
        this.Height = Height;
        this.Padding = Padding;
    }

    // Finds the best split to fit a rectangle based on a provided heuristic function
    public Split findBestFit(int Width, int Height, Func<Split, float> heuristics)
    {
        var possibleNodes =
            from item in Splits
            where item.fits(Width, Height)
            orderby heuristics(item) ascending
            select item;

        return possibleNodes.DefaultIfEmpty(null).First();
    }

    // Allocate space on this atlas
    // returns the Rect containing said rectangle or null on failure
    public Rect Allocate(int Width, int Height)
    {
        // Dimensions with added padding
        var pWidth = Width + 2 * this.Padding;
        var pHeight = Height + 2 * this.Padding;

        // Best Long Side fit.
        var bestFit = findBestFit(pWidth, pHeight,
            split => Math.Max(split.Width - pWidth, split.Height - pHeight)
        );

        // No space available, return null
        if (bestFit == null)
            return null;

        Rect rect = new Rect()
        {
            X = bestFit.X,
            Y = bestFit.Y,
            Width = pWidth,
            Height = pHeight
        };

        // Create list of new splits.
        // Has to call `ToList()` otherwise lazy-eval won't invalidate affected splits
        var newSplits = Splits
            .AsParallel()
            .Select(item => item.splitNode(rect))
            .SelectMany(item => item)
            .ToList();

        // Merge non-invalidated splits with the new splits
        Splits = Enumerable.Concat(Splits.Where(item => item.Invalidated == false), newSplits).ToList();

        // Invalidate splits that are fully contained inside other splits
        foreach (var split1 in Splits)
        {
            foreach (var split2 in Splits)
            {
                if (split1 == split2)
                    continue;

                if (split1.containsRect((Rect)split2))
                    split2.Invalidated = true;
            }
        }

        // Remove all the redundant or free'd splits
        Splits.RemoveAll(item => item.Invalidated);

        var tex = new Rect()
        {
            X = bestFit.X + Padding,
            Y = bestFit.Y + Padding,
            Width = Width,
            Height = Height
        };

        // Done, register texture and return
        Textures.Add(tex);
        return tex;
    }
}

public class TPageItem
{
    public uint Scaled;
    public string Filename;
    public Rect OriginalRect;
    public Rect NewRect;
    public TextureAtlas Atlas;
    public UndertaleTexturePageItem Item;
}

// TODO: make the script use new progress bar updater when the progress bar will be fixed
volatile int progress = 0;
string updateText = "";
void UpdateProgress(int updateAmount)
{
    UpdateProgressBar(null, updateText, progress += updateAmount, Data.TexturePageItems.Count);
}

void ResetProgress(string text)
{
    progress = 0;
    updateText = text;
    UpdateProgress(0);
}

TPageItem dumpTexturePageItem(UndertaleTexturePageItem pageItem, TextureWorker worker, string pageItemFile, bool reuse)
{
    TPageItem page = new TPageItem();
    page.Filename = pageItemFile;
    page.Item = pageItem;
    page.Scaled = page.Item.TexturePage.Scaled;

    page.OriginalRect = new Rect()
    {
        X = pageItem.SourceX,
        Y = pageItem.SourceY,
        Width = pageItem.SourceWidth,
        Height = pageItem.SourceHeight
    };

    if (!reuse)
        worker.ExportAsPNG(pageItem, pageItemFile);
    UpdateProgress(1);

    return page;
}

async Task<List<TPageItem>> dumpTexturePageItems(string dir, bool reuse)
{
    using var worker = new TextureWorker();

    var tpageitems = await Task.Run(() => Data.TexturePageItems
        .AsParallel()
        .Select(item => dumpTexturePageItem(item, worker, Path.Combine(dir, $"texture_page_{Data.TexturePageItems.IndexOf(item)}.png"), reuse))
        .ToList());

    return tpageitems;
}

/* 
 * User Configurable:: This function controls how texture items are
 * grouped, and forces different items into separate pages depending on
 * a user implemented heuristic.
 * See the commented version as an example.
 */

int doItemGrouping(TPageItem item)
{
    return 1;
}

/* 
 * This example of `doItemGrouping` attempts to force known Chapter 1 page items
 * into a separate group so they don't pollute Chapter 2 pages.
 */
// bool doItemGrouping(TPageItem item)
// {
//     foreach (var asset in Data.Sprites)
//     {
//         foreach (var page in asset.Textures)
//         {
//             if (page.Texture == item.Item)
//                 return asset.Name.SearchMatches("_ch1");
//         }
//     }
// 
//     foreach (var asset in Data.Backgrounds)
//     {
//         if (asset.Texture == item.Item)
//             return asset.Name.SearchMatches("_ch1");
//     }
// 
//     foreach (var asset in Data.Fonts)
//     {
//         if (asset.Texture == item.Item)
//             return asset.Name.SearchMatches("_ch1");
//     }
// 
//     return false;
// }

List<TextureAtlas> layoutPageItemList(List<TPageItem> items, int pageSizeWidth, int pageSizeHeight, int padding)
{
    var atlas_list = new List<TextureAtlas>();
    while (items.Count > 0)
    {
        var atlas = new TextureAtlas(pageSizeWidth, pageSizeHeight, padding);
        foreach (var page in items)
        {
            // If failed to allocate atlas space, then retry with a new one
            var rect = atlas.Allocate(page.OriginalRect.Width, page.OriginalRect.Height);
            if (rect == null)
                break;

            page.NewRect = rect;
            page.Atlas = atlas;
            UpdateProgress(1);
        }

        // Remove items that have already been layed out somewhere
        items.RemoveAll(item => item.Atlas != null);

        // If this atlas had items added to it, then save it.
        if (atlas.Textures.Count > 0)
            atlas_list.Add(atlas);
        else
            break;
    }

    return atlas_list;
}

async Task<List<TextureAtlas>> layoutPageItemLists<K>(ILookup<K, TPageItem> lookup, int pageSizeWidth, int pageSizeHeight, int padding)
{
    return await Task.Run(() => lookup
        .AsParallel()
        .Select(list => layoutPageItemList(list.ToList(), pageSizeWidth, pageSizeHeight, padding))
        .SelectMany(item => item)
        .ToList());
}

// From https://stackoverflow.com/a/62366455
private static int NearestPowerOf2(uint x)
{
    return 1 << (sizeof(uint) * 8 - BitOperations.LeadingZeroCount(x - 1));
}

EnsureDataLoaded();

// User Configurable:: Atlas page size and item padding
var pageSizeWidth = 1024;
var pageSizeHeight = 1024;

var padding = 1;

// User Configurable:: Dimension cutoffs (gets thrown off the atlas pool)
var maxDims = 256;
var maxArea = 256 * 128;

// User Configurable:: Force Power of Two (POT) sizes. (fixes graphical artifacts on platforms like PSVita)
// potBlacklist allows you to (manually or with heuristics) block certain textures from receiving that post-processing.
bool forcePOT = false;
List<TPageItem> potBlacklist = new List<TPageItem>();

// Ensure pageSize is POT
if (forcePOT)
{
    pageSizeWidth = NearestPowerOf2((uint)pageSizeWidth);
    pageSizeHeight = NearestPowerOf2((uint)pageSizeHeight);
}

bool reuseTextures = false;

// Setup packager directory
string packagerDirectory = Path.Combine(ExePath, "Packager");
if (Directory.Exists(packagerDirectory))
{
    DialogResult dr = MessageBox.Show("Do you want to reuse previously extracted page items?", 
        "Texture Repacker", MessageBoxButtons.YesNo);

    reuseTextures = dr == DialogResult.Yes;
}

Directory.CreateDirectory(packagerDirectory);

// Dump all the texture page items
ResetProgress("Existing Textures Exported");
var texPageItems = await dumpTexturePageItems(packagerDirectory, reuseTextures);

// Clear embedded textures and any possibly stale references to them
Data.EmbeddedTextures.Clear();
if (Data.TextureGroupInfo != null)
{
    foreach (var texInfo in Data.TextureGroupInfo)
    {
        texInfo.TexturePages.Clear();
    }
}

ILookup<(uint Scaled, int), TPageItem> texPageLookup = null;
await Task.Run(() =>
{
    // This query:
    // - Sorts and groups textures that are inside the bounds defined by maxDims, maxArea
    // - Eliminates from the pool any Page Item that sits alone in a Texture Page 
    //   (since there's probably a reason for that, e.g. shaders)
    // - Attempts to preserve texture page properties
    texPageLookup = texPageItems.OrderBy(
        // Order by smallest textures first
        item => Math.Max(item.OriginalRect.Width, item.OriginalRect.Height)
    ).Where(
        // Select textures that are small enough to be worth get paged
        item => (item.OriginalRect.Area < maxArea)                                          // area too big
             && (item.OriginalRect.Width <= maxDims && item.OriginalRect.Height <= maxDims) // both axis too big
             && (texPageItems.Any(item2 => (item2 != item) && (item.Item.TexturePage == item2.Item.TexturePage))) // shares a page with a different item
    ).ToLookup(
        // Preserve texture page settings by grouping items with similar settings
        item => (item.Item.TexturePage.Scaled, doItemGrouping(item))
    );
});

// Layout all the texture items (grouped by doItemGrouping) into atlases
ResetProgress("Laying out texture items");
var atlases = await layoutPageItemLists(texPageLookup, pageSizeWidth, pageSizeHeight, padding);

int lastTextPage = Data.EmbeddedTextures.Count - 1;

// Now recreate texture pages and link the items to the pages
ResetProgress("Regenerating Texture Pages");

var f = new StreamWriter(Path.Combine(packagerDirectory, "log.txt"));
int atlasCount = 0;

// Group items based on which atlas they belong to, if they do
var groups = texPageItems.GroupBy(item => item.Atlas);
SyncBinding("EmbeddedTextures", true);
await Task.Run(() =>
{
    foreach (var group in groups)
    {
        TextureAtlas atlas = group.Key;
        var atlasName = atlas != null ? (atlasCount++).ToString() : "null";
        f.WriteLine($" -- ATLAS {atlasName} -- ");

        if (atlas != null)
        {
            // Textures that are contained into an atlas
            UndertaleEmbeddedTexture tex = new UndertaleEmbeddedTexture();
            tex.Name = new UndertaleString($"Texture {++lastTextPage}");
            Data.EmbeddedTextures.Add(tex);
            
            using MagickImage newAtlasImage = new(MagickColors.Transparent, (uint)atlas.Width, (uint)atlas.Height);

            tex.Scaled = group.First().Scaled; // Make sure the original pane "Scaled" value is mantained.

            // Dump debug info regarding splits
            foreach (var split in atlas.Splits)
                f.WriteLine($"split: {atlas.Splits.IndexOf(split)}: {split.X}, {split.Y}, {split.Width}, {split.Height}");

            // Link texture items to the page and blit the needed image into the atlas
            foreach (var item in group)
            {
                f.WriteLine($"tex: {texPageItems.IndexOf(item)}: {item.NewRect.X}, {item.NewRect.Y}, {item.NewRect.Width}, {item.NewRect.Height}");

                using (MagickImage source = TextureWorker.ReadBGRAImageFromFile(item.Filename))
                {
                    newAtlasImage.Composite(source, item.NewRect.X, item.NewRect.Y, CompositeOperator.Copy);
                }

                item.Item.TexturePage = tex;
                item.Item.SourceX = (ushort)item.NewRect.X;
                item.Item.SourceY = (ushort)item.NewRect.Y;
                item.Item.SourceWidth = (ushort)item.NewRect.Width;
                item.Item.SourceHeight = (ushort)item.NewRect.Height;
                UpdateProgress(1);
            }

            // Save atlas into a file
            string atlasFile = Path.Combine(packagerDirectory, $"atlas_{atlasName}.png");
            TextureWorker.SaveImageToFile(newAtlasImage, atlasFile);

            // Assign new texture image
            tex.TextureData.Image = GMImage.FromMagickImage(newAtlasImage).ConvertToPng(); // TODO: generate other formats
        }
        else
        {
            // Textures not allocated into any atlas - just load them directly into a new page and link the item to the page
            foreach (var item in group)
            {
                f.WriteLine($"tex: {texPageItems.IndexOf(item)}: {0}, {0}, {item.OriginalRect.Width}, {item.OriginalRect.Height}");

                UndertaleEmbeddedTexture tex = new UndertaleEmbeddedTexture();
                tex.Name = new UndertaleString($"Texture {++lastTextPage}");
                Data.EmbeddedTextures.Add(tex);

                // Create POT texture if needed
                string itemFile = item.Filename;
                if (forcePOT && !potBlacklist.Contains(item))
                {
                    int potw = NearestPowerOf2((uint)item.OriginalRect.Width),
                        poth = NearestPowerOf2((uint)item.OriginalRect.Height);

                    using MagickImage newAtlasImage = new(MagickColors.Transparent, (uint)potw, (uint)poth);

                    // Load texture, composite onto top left of new atlas
                    using (MagickImage source = TextureWorker.ReadBGRAImageFromFile(item.Filename))
                    {
                        newAtlasImage.Composite(source, 0, 0, CompositeOperator.Copy);
                    }

                    itemFile = Path.Combine(packagerDirectory, $"pot_{texPageItems.IndexOf(item)}.png");
                    TextureWorker.SaveImageToFile(newAtlasImage, itemFile);

                    // Assign new texture image
                    tex.TextureData.Image = GMImage.FromMagickImage(newAtlasImage).ConvertToPng(); // TODO: generate other formats
                }
                else
                {
                    // Load image from file, and assign it
                    tex.TextureData.Image = GMImage.FromPng(File.ReadAllBytes(itemFile)); // TODO: generate other formats
                }

                tex.Scaled = item.Scaled;

                item.Item.TexturePage = tex;
                item.Item.SourceX = 0;
                item.Item.SourceY = 0;
                item.Item.SourceWidth = (ushort)item.OriginalRect.Width;
                item.Item.SourceHeight = (ushort)item.OriginalRect.Height;
                UpdateProgress(1);
            }
        }
    }
});

DisableAllSyncBindings();
f.Close();

// Done.
HideProgressBar();
