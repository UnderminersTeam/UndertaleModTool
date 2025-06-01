// Texture packer by Samuel Roy
// Uses code from https://github.com/mfascia/TexturePacker
// Uses code from ExportAllTextures.csx

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using ImageMagick;

EnsureDataLoaded();

// Get directory path
DirectoryInfo dir = Directory.CreateDirectory(Path.Combine(ExePath, "Packager"));

// Clear any files if they already exist
foreach (FileInfo file in dir.GetFiles())
    file.Delete();
foreach (DirectoryInfo di in dir.GetDirectories())
    di.Delete(true);

// Start export of all existing textures

string exportedTexturesFolder = Path.Combine(dir.FullName, "Textures");
TextureWorker worker = null;
Dictionary<string, int[]> assetCoordinateDict = new();
Dictionary<string, string> assetTypeDict = new();
using (worker = new())
{
    Directory.CreateDirectory(exportedTexturesFolder);

    SetProgressBar(null, "Existing Textures Exported", 0, Data.TexturePageItems.Count);
    StartProgressBarUpdater();

    await DumpSprites();
    await DumpFonts();
    await DumpBackgrounds();
}

await StopProgressBarUpdater();
HideProgressBar();

async Task DumpSprites()
{
    await Task.Run(() => Parallel.ForEach(Data.Sprites, DumpSprite));
}

async Task DumpBackgrounds()
{
    await Task.Run(() => Parallel.ForEach(Data.Backgrounds, DumpBackground));
}

async Task DumpFonts()
{
    await Task.Run(() => Parallel.ForEach(Data.Fonts, DumpFont));
}

void DumpSprite(UndertaleSprite sprite)
{
    if (sprite is not null)
    {
        for (int i = 0; i < sprite.Textures.Count; i++)
        {
            if (sprite.Textures[i]?.Texture != null)
            {
                UndertaleTexturePageItem tex = sprite.Textures[i].Texture;
                worker.ExportAsPNG(tex, Path.Combine(exportedTexturesFolder, $"{sprite.Name.Content}_{i}.png"));
                assetCoordinateDict.Add($"{sprite.Name.Content}_{i}", new int[] { tex.TargetX, tex.TargetY, tex.SourceWidth, tex.SourceHeight, tex.TargetWidth, tex.TargetHeight, tex.BoundingWidth, tex.BoundingHeight });
                assetTypeDict.Add($"{sprite.Name.Content}_{i}", "spr");
            }
        }
    }

    AddProgressParallel(sprite.Textures.Count);
}

void DumpFont(UndertaleFont font)
{
    if (font is null)
        return;
    if (font.Texture != null)
    {
        UndertaleTexturePageItem tex = font.Texture;
        worker.ExportAsPNG(tex, Path.Combine(exportedTexturesFolder, $"{font.Name.Content}.png"));
        assetCoordinateDict.Add(font.Name.Content, new int[] { tex.TargetX, tex.TargetY, tex.SourceWidth, tex.SourceHeight, tex.TargetWidth, tex.TargetHeight, tex.BoundingWidth, tex.BoundingHeight });
        assetTypeDict.Add(font.Name.Content, "fnt");

        IncrementProgressParallel();
    }
}

void DumpBackground(UndertaleBackground background)
{
    if (background is null)
        return;
    if (background.Texture != null)
    {
        UndertaleTexturePageItem tex = background.Texture;
        worker.ExportAsPNG(tex, Path.Combine(exportedTexturesFolder, $"{background.Name.Content}.png"));
        assetCoordinateDict.Add(background.Name.Content, new int[] { tex.TargetX, tex.TargetY, tex.SourceWidth, tex.SourceHeight, tex.TargetWidth, tex.TargetHeight, tex.BoundingWidth, tex.BoundingHeight });
        assetTypeDict.Add(background.Name.Content, "bg");
        IncrementProgressParallel();
    }
}

// End export

string sourcePath = exportedTexturesFolder;
string searchPattern = "*.png";
string outName = Path.Combine(dir.FullName, "atlas.txt");
int textureSize = 2048;
int PaddingValue = 2;
bool debug = false;

// Delete all existing Textures and TextureSheets
Data.TexturePageItems.Clear();
Data.EmbeddedTextures.Clear();

// Run the texture packer using borrowed and slightly modified code from the
// Texture packer sourced above
Packer packer = new Packer();
packer.Process(sourcePath, searchPattern, textureSize, PaddingValue, debug);
packer.SaveAtlasses(outName);

int lastTextPage = Data.EmbeddedTextures.Count - 1;
int lastTextPageItem = Data.TexturePageItems.Count - 1;

// Import everything into UMT
string prefix = outName.Replace(Path.GetExtension(outName), "");
int atlasCount = 0;
foreach (Atlas atlas in packer.Atlasses)
{
    string atlasName = $"{prefix}{atlasCount:000}.png";
    UndertaleEmbeddedTexture texture = new UndertaleEmbeddedTexture();
    texture.Name = new UndertaleString($"Texture {++lastTextPage}");
    texture.TextureData.Image = GMImage.FromPng(File.ReadAllBytes(atlasName)); // TODO: generate other formats
    Data.EmbeddedTextures.Add(texture);
    foreach (Node n in atlas.Nodes)
    {
        if (n.Texture != null)
        {
            // Initalize values of this texture
            UndertaleTexturePageItem texturePageItem = new UndertaleTexturePageItem();
            texturePageItem.Name = new UndertaleString($"PageItem {++lastTextPageItem}");
            texturePageItem.SourceX = (ushort)n.Bounds.X;
            texturePageItem.SourceY = (ushort)n.Bounds.Y;
            texturePageItem.SourceWidth = (ushort)n.Bounds.Width;
            texturePageItem.SourceHeight = (ushort)n.Bounds.Height;
            texturePageItem.BoundingWidth = (ushort)n.Bounds.Width;
            texturePageItem.BoundingHeight = (ushort)n.Bounds.Height;
            texturePageItem.TexturePage = texture;

            // Add this texture to UMT
            Data.TexturePageItems.Add(texturePageItem);

            // String processing
            string stripped = Path.GetFileNameWithoutExtension(n.Texture.Source);
            int firstUnderscore = stripped.IndexOf('_');
            string spriteType = "";
            try
            {
                if (assetTypeDict.ContainsKey(stripped))
                    spriteType = assetTypeDict[stripped];
                else
                    spriteType = stripped.Substring(0, firstUnderscore);
            }
            catch (Exception e)
            {
                if (stripped.Equals("background0") || stripped.Equals("background1"))
                {
                    UndertaleBackground background = Data.Backgrounds.ByName(stripped); //Use stripped instead of sprite name or else the last index calculation gives us a bad string.
                    background.Texture = texturePageItem;
                    setTextureTargetBounds(texturePageItem, stripped, n);
                    continue;
                }
                else
                {
                    ScriptMessage("Error: Image " + stripped + " has an invalid name.");
                    continue;
                }
            }
            setTextureTargetBounds(texturePageItem, stripped, n);
            // Special Cases for backgrounds and fonts
            if (spriteType.Equals("bg"))
            {
                UndertaleBackground background = Data.Backgrounds.ByName(stripped); // Use stripped instead of sprite name or else the last index calculation gives us a bad string.
                background.Texture = texturePageItem;
            }
            else if (spriteType.Equals("fnt"))
            {
                UndertaleFont font = Data.Fonts.ByName(stripped); // Use stripped instead of sprite name or else the last index calculation gives us a bad string.
                font.Texture = texturePageItem;
            }
            else
            {
                // Get sprite to add this texture to
                string spriteName;
                int lastUnderscore, frame;
                try
                {
                    lastUnderscore = stripped.LastIndexOf('_');
                    spriteName = stripped.Substring(0, lastUnderscore);
                    frame = Int32.Parse(stripped.Substring(lastUnderscore + 1));
                }
                catch (Exception e)
                {
                    ScriptMessage($"Error: Image {stripped} has an invalid name. Skipping...");
                    continue;
                }
                UndertaleSprite sprite = null;
                sprite = Data.Sprites.ByName(spriteName);

                // Create TextureEntry object
                UndertaleSprite.TextureEntry texentry = new UndertaleSprite.TextureEntry();
                texentry.Texture = texturePageItem;

                if (frame > sprite.Textures.Count - 1)
                {
                    while (frame > sprite.Textures.Count - 1)
                    {
                        sprite.Textures.Add(texentry);
                    }
                    continue;
                }
                sprite.Textures[frame] = texentry;
            }
        }
    }
    // Increment atlas
    atlasCount++;
}

void setTextureTargetBounds(UndertaleTexturePageItem tex, string textureName, Node n)
{
    if (assetCoordinateDict.ContainsKey(textureName))
    {
        int[] coords = assetCoordinateDict[textureName];
        tex.TargetX = (ushort)coords[0];
        tex.TargetY = (ushort)coords[1];
        tex.SourceWidth = (ushort)coords[2];
        tex.SourceHeight = (ushort)coords[3];
        tex.TargetWidth = (ushort)coords[4];
        tex.TargetHeight = (ushort)coords[5];
        tex.BoundingWidth = (ushort)coords[6];
        tex.BoundingHeight = (ushort)coords[7];
    }
    else
    {
        tex.TargetX = 0;
        tex.TargetY = 0;
        tex.TargetWidth = (ushort)n.Bounds.Width;
        tex.TargetHeight = (ushort)n.Bounds.Height;
    }
}

HideProgressBar();
ScriptMessage("Import Complete!");

public class TextureInfo
{
    public string Source;
    public int Width;
    public int Height;
}

public enum SplitType
{
    Horizontal,
    Vertical,
}

public enum BestFitHeuristic
{
    Area,
    MaxOneAxis,
}

public struct Rect
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class Node
{
    public Rect Bounds;
    public TextureInfo Texture;
    public SplitType SplitType;
}

public class Atlas
{
    public int Width;
    public int Height;
    public List<Node> Nodes;
}

public class Packer
{
    public List<TextureInfo> SourceTextures;
    public StringWriter Log;
    public StringWriter Error;
    public int Padding;
    public int AtlasSize;
    public bool DebugMode;
    public BestFitHeuristic FitHeuristic;
    public List<Atlas> Atlasses;

    public Packer()
    {
        SourceTextures = new List<TextureInfo>();
        Log = new StringWriter();
        Error = new StringWriter();
    }

    public void Process(string _SourceDir, string _Pattern, int _AtlasSize, int _Padding, bool _DebugMode)
    {
        Padding = _Padding;
        AtlasSize = _AtlasSize;
        DebugMode = _DebugMode;
        //1: scan for all the textures we need to pack
        ScanForTextures(_SourceDir, _Pattern);
        List<TextureInfo> textures = new List<TextureInfo>();
        textures = SourceTextures.ToList();
        //2: generate as many atlasses as needed (with the latest one as small as possible)
        Atlasses = new List<Atlas>();
        while (textures.Count > 0)
        {
            Atlas atlas = new Atlas();
            atlas.Width = _AtlasSize;
            atlas.Height = _AtlasSize;
            List<TextureInfo> leftovers = LayoutAtlas(textures, atlas);
            if (leftovers.Count == 0)
            {
                // we reached the last atlas. Check if this last atlas could have been twice smaller
                while (leftovers.Count == 0)
                {
                    atlas.Width /= 2;
                    atlas.Height /= 2;
                    leftovers = LayoutAtlas(textures, atlas);
                }
                // we need to go 1 step larger as we found the first size that is to small
                atlas.Width *= 2;
                atlas.Height *= 2;
                leftovers = LayoutAtlas(textures, atlas);
            }
            Atlasses.Add(atlas);
            textures = leftovers;
        }
    }

    public void SaveAtlasses(string _Destination)
    {
        int atlasCount = 0;
        string prefix = _Destination.Replace(Path.GetExtension(_Destination), "");
        string descFile = _Destination;
        StreamWriter tw = new StreamWriter(_Destination);
        tw.WriteLine("source_tex, atlas_tex, x, y, width, height");
        foreach (Atlas atlas in Atlasses)
        {
            string atlasName = $"{prefix}{atlasCount:000}.png";
            //1: Save images
            using MagickImage img = CreateAtlasImage(atlas);
            TextureWorker.SaveImageToFile(img, atlasName);
            //2: save description in file
            foreach (Node n in atlas.Nodes)
            {
                if (n.Texture != null)
                {
                    tw.Write(n.Texture.Source + ", ");
                    tw.Write(atlasName + ", ");
                    tw.Write((n.Bounds.X).ToString() + ", ");
                    tw.Write((n.Bounds.Y).ToString() + ", ");
                    tw.Write((n.Bounds.Width).ToString() + ", ");
                    tw.WriteLine((n.Bounds.Height).ToString());
                }
            }
            ++atlasCount;
        }
        tw.Close();
        tw = new StreamWriter(prefix + ".log");
        tw.WriteLine("--- LOG -------------------------------------------");
        tw.WriteLine(Log.ToString());
        tw.WriteLine("--- ERROR -----------------------------------------");
        tw.WriteLine(Error.ToString());
        tw.Close();
    }

    private void ScanForTextures(string _Path, string _Wildcard)
    {
        DirectoryInfo di = new DirectoryInfo(_Path);
        FileInfo[] files = di.GetFiles(_Wildcard, SearchOption.AllDirectories);
        foreach (FileInfo fi in files)
        {
            (int width, int height) = TextureWorker.GetImageSizeFromFile(fi.FullName);
            if (width == -1 || height == -1)
                continue;

            if (width <= AtlasSize && height <= AtlasSize)
            {
                TextureInfo ti = new TextureInfo();

                ti.Source = fi.FullName;
                ti.Width = width;
                ti.Height = height;

                SourceTextures.Add(ti);

                Log.WriteLine($"Added {fi.FullName}");
            }
            else
            {
                Error.WriteLine($"{fi.FullName} is too large to fix in the atlas. Skipping!");
            }
        }
    }

    private void HorizontalSplit(Node _ToSplit, int _Width, int _Height, List<Node> _List)
    {
        Node n1 = new Node();
        n1.Bounds.X = _ToSplit.Bounds.X + _Width + Padding;
        n1.Bounds.Y = _ToSplit.Bounds.Y;
        n1.Bounds.Width = _ToSplit.Bounds.Width - _Width - Padding;
        n1.Bounds.Height = _Height;
        n1.SplitType = SplitType.Vertical;
        Node n2 = new Node();
        n2.Bounds.X = _ToSplit.Bounds.X;
        n2.Bounds.Y = _ToSplit.Bounds.Y + _Height + Padding;
        n2.Bounds.Width = _ToSplit.Bounds.Width;
        n2.Bounds.Height = _ToSplit.Bounds.Height - _Height - Padding;
        n2.SplitType = SplitType.Horizontal;
        if (n1.Bounds.Width > 0 && n1.Bounds.Height > 0)
            _List.Add(n1);
        if (n2.Bounds.Width > 0 && n2.Bounds.Height > 0)
            _List.Add(n2);
    }

    private void VerticalSplit(Node _ToSplit, int _Width, int _Height, List<Node> _List)
    {
        Node n1 = new Node();
        n1.Bounds.X = _ToSplit.Bounds.X + _Width + Padding;
        n1.Bounds.Y = _ToSplit.Bounds.Y;
        n1.Bounds.Width = _ToSplit.Bounds.Width - _Width - Padding;
        n1.Bounds.Height = _ToSplit.Bounds.Height;
        n1.SplitType = SplitType.Vertical;
        Node n2 = new Node();
        n2.Bounds.X = _ToSplit.Bounds.X;
        n2.Bounds.Y = _ToSplit.Bounds.Y + _Height + Padding;
        n2.Bounds.Width = _Width;
        n2.Bounds.Height = _ToSplit.Bounds.Height - _Height - Padding;
        n2.SplitType = SplitType.Horizontal;
        if (n1.Bounds.Width > 0 && n1.Bounds.Height > 0)
            _List.Add(n1);
        if (n2.Bounds.Width > 0 && n2.Bounds.Height > 0)
            _List.Add(n2);
    }

    private TextureInfo FindBestFitForNode(Node _Node, List<TextureInfo> _Textures)
    {
        TextureInfo bestFit = null;
        float nodeArea = _Node.Bounds.Width * _Node.Bounds.Height;
        float maxCriteria = 0.0f;
        foreach (TextureInfo ti in _Textures)
        {
            switch (FitHeuristic)
            {
                // Max of Width and Height ratios
                case BestFitHeuristic.MaxOneAxis:
                    if (ti.Width <= _Node.Bounds.Width && ti.Height <= _Node.Bounds.Height)
                    {
                        float wRatio = (float)ti.Width / (float)_Node.Bounds.Width;
                        float hRatio = (float)ti.Height / (float)_Node.Bounds.Height;
                        float ratio = wRatio > hRatio ? wRatio : hRatio;
                        if (ratio > maxCriteria)
                        {
                            maxCriteria = ratio;
                            bestFit = ti;
                        }
                    }
                    break;
                // Maximize Area coverage
                case BestFitHeuristic.Area:
                    if (ti.Width <= _Node.Bounds.Width && ti.Height <= _Node.Bounds.Height)
                    {
                        float textureArea = ti.Width * ti.Height;
                        float coverage = textureArea / nodeArea;
                        if (coverage > maxCriteria)
                        {
                            maxCriteria = coverage;
                            bestFit = ti;
                        }
                    }
                    break;
            }
        }
        return bestFit;
    }

    private List<TextureInfo> LayoutAtlas(List<TextureInfo> _Textures, Atlas _Atlas)
    {
        List<Node> freeList = new List<Node>();
        List<TextureInfo> textures = new List<TextureInfo>();
        _Atlas.Nodes = new List<Node>();
        textures = _Textures.ToList();
        Node root = new Node();
        root.Bounds.Width = _Atlas.Width;
        root.Bounds.Height = _Atlas.Height;
        root.SplitType = SplitType.Horizontal;
        freeList.Add(root);
        while (freeList.Count > 0 && textures.Count > 0)
        {
            Node node = freeList[0];
            freeList.RemoveAt(0);
            TextureInfo bestFit = FindBestFitForNode(node, textures);
            if (bestFit != null)
            {
                if (node.SplitType == SplitType.Horizontal)
                {
                    HorizontalSplit(node, bestFit.Width, bestFit.Height, freeList);
                }
                else
                {
                    VerticalSplit(node, bestFit.Width, bestFit.Height, freeList);
                }
                node.Texture = bestFit;
                node.Bounds.Width = bestFit.Width;
                node.Bounds.Height = bestFit.Height;
                textures.Remove(bestFit);
            }
            _Atlas.Nodes.Add(node);
        }
        return textures;
    }

    private MagickImage CreateAtlasImage(Atlas _Atlas)
    {
        MagickImage img = new(MagickColors.Transparent, (uint)_Atlas.Width, (uint)_Atlas.Height);

        foreach (Node n in _Atlas.Nodes)
        {
            if (n.Texture is not null)
            {
                using MagickImage sourceImg = TextureWorker.ReadBGRAImageFromFile(n.Texture.Source);
                using IMagickImage<byte> resizedSourceImg = TextureWorker.ResizeImage(sourceImg, n.Bounds.Width, n.Bounds.Height);
                img.Composite(resizedSourceImg, n.Bounds.X, n.Bounds.Y, CompositeOperator.Copy);
            }
        }

        return img;
    }
}