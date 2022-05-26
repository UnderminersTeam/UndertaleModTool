//Texture packer by Samuel Roy
// Uses code from https://github.com/mfascia/TexturePacker

using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib.Util;

EnsureDataLoaded();

string importFolder = PromptChooseDirectory();
if (importFolder == null)
    throw new ScriptException("The import folder was not set.");

System.IO.Directory.CreateDirectory("Packager");
string sourcePath = importFolder;
string searchPattern = "*.png";
string outName = "Packager/atlas.txt";
int textureSize = 2048;
int border = 2;
bool debug = false;
Packer packer = new Packer();
packer.Process(sourcePath, searchPattern, textureSize, border, debug);
packer.SaveAtlasses(outName);

int lastTextPage = Data.EmbeddedTextures.Count - 1;
int lastTextPageItem = Data.TexturePageItems.Count - 1;

string prefix = outName.Replace(Path.GetExtension(outName), "");
int atlasCount = 0;
foreach (Atlas atlas in packer.Atlasses)
{
    string atlasName = String.Format(prefix + "{0:000}" + ".png", atlasCount);
    Bitmap atlasBitmap = new Bitmap(atlasName);
    UndertaleEmbeddedTexture texture = new UndertaleEmbeddedTexture();
    texture.Name = new UndertaleString("Texture " + ++lastTextPage);
    texture.TextureData.TextureBlob = File.ReadAllBytes(atlasName);
    Data.EmbeddedTextures.Add(texture);
    foreach (Node n in atlas.Nodes)
    {
        if (n.Texture != null)
        {
            UndertaleTexturePageItem texturePageItem = new UndertaleTexturePageItem();
            texturePageItem.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
            texturePageItem.SourceX = (ushort)n.Bounds.X;
            texturePageItem.SourceY = (ushort)n.Bounds.Y;
            texturePageItem.SourceWidth = (ushort)n.Bounds.Width;
            texturePageItem.SourceHeight = (ushort)n.Bounds.Height;
            texturePageItem.TargetX = 0;
            texturePageItem.TargetY = 0;
            texturePageItem.TargetWidth = (ushort)n.Bounds.Width;
            texturePageItem.TargetHeight = (ushort)n.Bounds.Height;
            texturePageItem.BoundingWidth = (ushort)n.Bounds.Width;
            texturePageItem.BoundingHeight = (ushort)n.Bounds.Height;
            texturePageItem.TexturePage = texture;
            Data.TexturePageItems.Add(texturePageItem);
            string spriteName = Path.GetFileNameWithoutExtension(n.Texture.Source);

            UndertaleFont font = null;
            font = Data.Fonts.ByName(spriteName);

            if (font == null)
            {
                UndertaleString fontUTString = Data.Strings.MakeString(spriteName);
                UndertaleFont newFont = new UndertaleFont();
                newFont.Name = fontUTString;

                fontUpdate(newFont);
                newFont.Texture = texturePageItem;
                Data.Fonts.Add(newFont);
                continue;
            }

            fontUpdate(font);
            font.Texture = texturePageItem;
            UndertaleSprite.TextureEntry texentry = new UndertaleSprite.TextureEntry();
            texentry.Texture = texturePageItem;
        }
    }
    atlasCount++;
}



HideProgressBar();
ScriptMessage("Import Complete!");

public void fontUpdate(UndertaleFont newFont)
{
    using(StreamReader reader = new StreamReader(sourcePath + "glyphs_" + newFont.Name.Content + ".csv"))
    {
        newFont.Glyphs.Clear();
        string line;
        int head = 0;
        while((line = reader.ReadLine()) != null)
        {
            string[] s = line.Split(';');

            if (head == 1)
            {
                newFont.RangeStart = UInt16.Parse(s[0]);
                head++;
            }

            if (head == 0)
            {
                String namae = s[0].Replace("\"", "");
                newFont.DisplayName = Data.Strings.MakeString(namae);
                newFont.EmSize = UInt16.Parse(s[1]);
                newFont.Bold = Boolean.Parse(s[2]);
                newFont.Italic = Boolean.Parse(s[3]);
                newFont.Charset = Byte.Parse(s[4]);
                newFont.AntiAliasing = Byte.Parse(s[5]);
                newFont.ScaleX = UInt16.Parse(s[6]);
                newFont.ScaleY = UInt16.Parse(s[7]);
                head++;
            }

            if (head > 1)
            {
                newFont.Glyphs.Add(new UndertaleFont.Glyph()
                {
                    Character = UInt16.Parse(s[0]),
                    SourceX = UInt16.Parse(s[1]),
                    SourceY = UInt16.Parse(s[2]),
                    SourceWidth = UInt16.Parse(s[3]),
                    SourceHeight = UInt16.Parse(s[4]),
                    Shift = Int16.Parse(s[5]),
                    Offset = Int16.Parse(s[6]),
                });
                newFont.RangeEnd = UInt32.Parse(s[0]);
            }
        }

    }
}

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

public class Node
{
    public Rectangle Bounds;
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
            string atlasName = String.Format(prefix + "{0:000}" + ".png", atlasCount);
            //1: Save images
            Image img = CreateAtlasImage(atlas);
            //DPI fix start
            Bitmap ResolutionFix = new Bitmap(img);
            ResolutionFix.SetResolution(96.0F, 96.0F);
            Image img2 = ResolutionFix;
            //DPI fix end
            img2.Save(atlasName, System.Drawing.Imaging.ImageFormat.Png);
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
            Image img = Image.FromFile(fi.FullName);
            if (img != null)
            {
                if (img.Width <= AtlasSize && img.Height <= AtlasSize)
                {
                    TextureInfo ti = new TextureInfo();

                    ti.Source = fi.FullName;
                    ti.Width = img.Width;
                    ti.Height = img.Height;

                    SourceTextures.Add(ti);

                    Log.WriteLine("Added " + fi.FullName);
                }
                else
                {
                    Error.WriteLine(fi.FullName + " is too large to fix in the atlas. Skipping!");
                }
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
        root.Bounds.Size = new Size(_Atlas.Width, _Atlas.Height);
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

    private Image CreateAtlasImage(Atlas _Atlas)
    {
        Image img = new Bitmap(_Atlas.Width, _Atlas.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        Graphics g = Graphics.FromImage(img);
        if (DebugMode)
        {
            g.FillRectangle(Brushes.Green, new Rectangle(0, 0, _Atlas.Width, _Atlas.Height));
        }
        foreach (Node n in _Atlas.Nodes)
        {
            if (n.Texture != null)
            {
                Image sourceImg = Image.FromFile(n.Texture.Source);
                g.DrawImage(sourceImg, n.Bounds);
                if (DebugMode)
                {
                    string label = Path.GetFileNameWithoutExtension(n.Texture.Source);
                    SizeF labelBox = g.MeasureString(label, SystemFonts.MenuFont, new SizeF(n.Bounds.Size));
                    RectangleF rectBounds = new Rectangle(n.Bounds.Location, new Size((int)labelBox.Width, (int)labelBox.Height));
                    g.FillRectangle(Brushes.Black, rectBounds);
                    g.DrawString(label, SystemFonts.MenuFont, Brushes.White, rectBounds);
                }
            }
            else
            {
                g.FillRectangle(Brushes.DarkMagenta, n.Bounds);
                if (DebugMode)
                {
                    string label = n.Bounds.Width.ToString() + "x" + n.Bounds.Height.ToString();
                    SizeF labelBox = g.MeasureString(label, SystemFonts.MenuFont, new SizeF(n.Bounds.Size));
                    RectangleF rectBounds = new Rectangle(n.Bounds.Location, new Size((int)labelBox.Width, (int)labelBox.Height));
                    g.FillRectangle(Brushes.Black, rectBounds);
                    g.DrawString(label, SystemFonts.MenuFont, Brushes.White, rectBounds);
                }
            }
        }
        return img;
    }
}
