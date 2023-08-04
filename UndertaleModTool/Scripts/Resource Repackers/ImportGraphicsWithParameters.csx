// Texture packer by Samuel Roy
// Uses code from https://github.com/mfascia/TexturePacker

using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib.Util;
using UndertaleModLib.Models;
using System.Windows.Forms;

EnsureDataLoaded();

bool importAsSprite = false;

string[] offsets = {"Top Left", "Top Center", "Top Right", "Center Left", "Center", "Center Right", "Bottom Left", "Bottom Center", "Bottom Right"};

string[] playbacks = {"Frames Per Second", "Frames Per Game Frame"};

float animSpd = 1;

string offresult;

int playback;

string importFolder = CheckValidity();

string packDir = Path.Combine(ExePath, "Packager");
Directory.CreateDirectory(packDir);

string sourcePath = importFolder;
string searchPattern = "*.png";
string outName = Path.Combine(packDir, "atlas.txt");
int textureSize = 2048;
int PaddingValue = 2;
bool debug = false;
Packer packer = new Packer();
packer.Process(sourcePath, searchPattern, textureSize, PaddingValue, debug);
packer.SaveAtlasses(outName);

int lastTextPage = Data.EmbeddedTextures.Count - 1;
int lastTextPageItem = Data.TexturePageItems.Count - 1;

// Import everything into UMT
string prefix = outName.Replace(Path.GetExtension(outName), "");
int atlasCount = 0;
OffsetResult();
foreach (Atlas atlas in packer.Atlasses)
{
    string atlasName = Path.Combine(packDir, String.Format(prefix + "{0:000}" + ".png", atlasCount));
    Bitmap atlasBitmap = new Bitmap(atlasName);
    UndertaleEmbeddedTexture texture = new UndertaleEmbeddedTexture();
    texture.Name = new UndertaleString("Texture " + ++lastTextPage);
    texture.TextureData.TextureBlob = File.ReadAllBytes(atlasName);
    Data.EmbeddedTextures.Add(texture);
    foreach (Node n in atlas.Nodes)
    {
        if (n.Texture != null)
        {
            // Initalize values of this texture
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

            // Add this texture to UMT
            Data.TexturePageItems.Add(texturePageItem);

            // String processing
            string stripped = Path.GetFileNameWithoutExtension(n.Texture.Source);

            SpriteType spriteType = GetSpriteType(n.Texture.Source);

            if (importAsSprite)
            {
                if ((spriteType == SpriteType.Unknown) || (spriteType == SpriteType.Font))
                {
                    spriteType = SpriteType.Sprite;
                }
            }

            setTextureTargetBounds(texturePageItem, stripped, n);


            if (spriteType == SpriteType.Background)
            {
                UndertaleBackground background = Data.Backgrounds.ByName(stripped);
                if (background != null)
                {
                    background.Texture = texturePageItem;
                }
                else
                {
                    // No background found, let's make one
                    UndertaleString backgroundUTString = Data.Strings.MakeString(stripped);
                    UndertaleBackground newBackground = new UndertaleBackground();
                    newBackground.Name = backgroundUTString;
                    newBackground.Transparent = false;
                    newBackground.Preload = false;
                    newBackground.Texture = texturePageItem;
                    Data.Backgrounds.Add(newBackground);
                }
            }
            else if (spriteType == SpriteType.Sprite)
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
                    ScriptMessage("Error: Image " + stripped + " has an invalid name. Skipping...");
                    continue;
                }
                UndertaleSprite sprite = null;
                sprite = Data.Sprites.ByName(spriteName);

                // Create TextureEntry object
                UndertaleSprite.TextureEntry texentry = new UndertaleSprite.TextureEntry();
                texentry.Texture = texturePageItem;

                // Set values for new sprites
                if (sprite == null)
                {
                    UndertaleString spriteUTString = Data.Strings.MakeString(spriteName);
                    UndertaleSprite newSprite = new UndertaleSprite();
                    newSprite.Name = spriteUTString;
                    newSprite.Width = (uint)n.Bounds.Width;
                    newSprite.Height = (uint)n.Bounds.Height;
                    newSprite.MarginLeft = 0;
                    newSprite.MarginRight = n.Bounds.Width - 1;
                    newSprite.MarginTop = 0;
                    newSprite.MarginBottom = n.Bounds.Height - 1;
                    newSprite.GMS2PlaybackSpeedType = (AnimSpeedType)playback;
                    newSprite.GMS2PlaybackSpeed = animSpd;
                    switch (offresult)
                    {
                        case ("Top Left"):
                            newSprite.OriginX = 0;
                            newSprite.OriginY = 0;
                        break;
                        case ("Top Center"):
                            newSprite.OriginX = (int)(newSprite.Width/2);
                            newSprite.OriginY = 0;
                        break;
                        case ("Top Right"):
                            newSprite.OriginX = (int)(newSprite.Width);
                            newSprite.OriginY = 0;
                        break;
                        case ("Center Left"):
                            newSprite.OriginX = 0;
                            newSprite.OriginY = (int)(newSprite.Height/2);
                        break;
                        case ("Center"):
                            newSprite.OriginX = (int)(newSprite.Width/2);
                            newSprite.OriginY = (int)(newSprite.Height/2);
                        break;
                        case ("Center Right"):
                            newSprite.OriginX = (int)(newSprite.Width);
                            newSprite.OriginY = (int)(newSprite.Height/2);
                        break;
                        case ("Bottom Left"):
                            newSprite.OriginX = 0;
                            newSprite.OriginY = (int)(newSprite.Height);
                        break;
                        case ("Bottom Center"):
                            newSprite.OriginX = (int)(newSprite.Width/2);
                            newSprite.OriginY = (int)(newSprite.Height);
                        break;
                        case ("Bottom Right"):
                            newSprite.OriginX = (int)(newSprite.Width);
                            newSprite.OriginY = (int)(newSprite.Height);
                        break;
                    }
                    if (frame > 0)
                    {
                        for (int i = 0; i < frame; i++)
                            newSprite.Textures.Add(null);
                    }
                    newSprite.CollisionMasks.Add(newSprite.NewMaskEntry());
                    Rectangle bmpRect = new Rectangle(n.Bounds.X, n.Bounds.Y, n.Bounds.Width, n.Bounds.Height);
                    System.Drawing.Imaging.PixelFormat format = atlasBitmap.PixelFormat;
                    Bitmap cloneBitmap = atlasBitmap.Clone(bmpRect, format);
                    int width = ((n.Bounds.Width + 7) / 8) * 8;
                    BitArray maskingBitArray = new BitArray(width * n.Bounds.Height);
                    for (int y = 0; y < n.Bounds.Height; y++)
                    {
                        for (int x = 0; x < n.Bounds.Width; x++)
                        {
                            Color pixelColor = cloneBitmap.GetPixel(x, y);
                            maskingBitArray[y * width + x] = (pixelColor.A > 0);
                        }
                    }
                    BitArray tempBitArray = new BitArray(width * n.Bounds.Height);
                    for (int i = 0; i < maskingBitArray.Length; i += 8)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            tempBitArray[j + i] = maskingBitArray[-(j - 7) + i];
                        }
                    }
                    int numBytes;
                    numBytes = maskingBitArray.Length / 8;
                    byte[] bytes = new byte[numBytes];
                    tempBitArray.CopyTo(bytes, 0);
                    for (int i = 0; i < bytes.Length; i++)
                        newSprite.CollisionMasks[0].Data[i] = bytes[i];
                    newSprite.Textures.Add(texentry);
                    Data.Sprites.Add(newSprite);
                    continue;
                }
                if (frame > sprite.Textures.Count - 1)
                {
                    while (frame > sprite.Textures.Count - 1)
                    {
                        sprite.Textures.Add(texentry);
                    }
                    continue;
                }
                sprite.Textures[frame] = texentry;
                sprite.GMS2PlaybackSpeedType = (AnimSpeedType)playback;
                sprite.GMS2PlaybackSpeed = animSpd;
                switch (offresult)
                {
                    case ("Top Left"):
                        sprite.OriginX = 0;
                        sprite.OriginY = 0;
                    break;
                    case ("Top Center"):
                        sprite.OriginX = (int)(sprite.Width/2);
                        sprite.OriginY = 0;
                    break;
                    case ("Top Right"):
                        sprite.OriginX = (int)(sprite.Width);
                        sprite.OriginY = 0;
                    break;
                    case ("Center Left"):
                        sprite.OriginX = 0;
                        sprite.OriginY = (int)(sprite.Height/2);
                    break;
                    case ("Center"):
                        sprite.OriginX = (int)(sprite.Width/2);
                        sprite.OriginY = (int)(sprite.Height/2);
                    break;
                    case ("Center Right"):
                        sprite.OriginX = (int)(sprite.Width);
                        sprite.OriginY = (int)(sprite.Height/2);
                    break;
                    case ("Bottom Left"):
                        sprite.OriginX = 0;
                        sprite.OriginY = (int)(sprite.Height);
                    break;
                    case ("Bottom Center"):
                        sprite.OriginX = (int)(sprite.Width/2);
                        sprite.OriginY = (int)(sprite.Height);
                    break;
                    case ("Bottom Right"):
                        sprite.OriginX = (int)(sprite.Width);
                        sprite.OriginY = (int)(sprite.Height);
                    break;
                }
            }
        }
    }
    // Increment atlas
    atlasCount++;
}

HideProgressBar();
ScriptMessage("Import Complete!");

void setTextureTargetBounds(UndertaleTexturePageItem tex, string textureName, Node n)
{
    tex.TargetX = 0;
    tex.TargetY = 0;
    tex.TargetWidth = (ushort)n.Bounds.Width;
    tex.TargetHeight = (ushort)n.Bounds.Height;
}

public class TextureInfo
{
    public string Source;
    public int Width;
    public int Height;
}

public enum SpriteType
{
    Sprite,
    Background,
    Font,
    Unknown
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
            img.Save(atlasName, System.Drawing.Imaging.ImageFormat.Png);
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
        foreach (Node n in _Atlas.Nodes)
        {
            if (n.Texture != null)
            {
                Image sourceImg = Image.FromFile(n.Texture.Source);
                g.DrawImage(sourceImg, n.Bounds);
            }
        }
        // DPI FIX START
        Bitmap ResolutionFix = new Bitmap(img);
        ResolutionFix.SetResolution(96.0F, 96.0F);
        Image img2 = ResolutionFix;
        return img2;
        // DPI FIX END
    }
}

SpriteType GetSpriteType(string path)
{
    string folderPath = Path.GetDirectoryName(path);
    string folderName = new DirectoryInfo(folderPath).Name;
    string lowerName = folderName.ToLower();

    if (lowerName == "backgrounds" || lowerName == "background")
    {
        return SpriteType.Background;
    }
    else if (lowerName == "fonts" || lowerName == "font")
    {
        return SpriteType.Font;
    }
    else if (lowerName == "sprites" || lowerName == "sprite")
    {
        return SpriteType.Sprite;
    }
    return SpriteType.Unknown;
}

string CheckValidity()
{
    bool recursiveCheck = ScriptQuestion(@"This script imports all sprites in all subdirectories recursively.
If an image file is in a folder named ""Backgrounds"", then the image will be imported as a background.
Otherwise, the image will be imported as a sprite, and allow you to select it's origin point.
Do you want to continue?");
    if (!recursiveCheck)
        throw new ScriptException("Script cancelled.");

    // Get import folder
    string importFolder = PromptChooseDirectory();
    if (importFolder == null)
        throw new ScriptException("The import folder was not set.");

    //Stop the script if there's missing sprite entries or w/e.
    bool hadMessage = false;
    string[] dirFiles = Directory.GetFiles(importFolder, "*.png", SearchOption.AllDirectories);
    foreach (string file in dirFiles)
    {
        string FileNameWithExtension = Path.GetFileName(file);
        string stripped = Path.GetFileNameWithoutExtension(file);
        int lastUnderscore = stripped.LastIndexOf('_');
        string spriteName = "";

        SpriteType spriteType = GetSpriteType(file);

        if ((spriteType != SpriteType.Sprite) && (spriteType != SpriteType.Background))
        {
            if (!hadMessage)
            {
                hadMessage = true;
                importAsSprite = ScriptQuestion(FileNameWithExtension + @" is in an incorrectly-named folder (valid names being ""Sprites"" and ""Backgrounds""). Would you like to import these images as sprites?
Pressing ""No"" will cause the program to ignore these images.");
            }

            if (!importAsSprite)
            {
                continue;
            }
            else
            {
                spriteType = SpriteType.Sprite;
            }
        }

        // Check for duplicate filenames
        string[] dupFiles = Directory.GetFiles(importFolder, FileNameWithExtension, SearchOption.AllDirectories);
        if (dupFiles.Length > 1)
            throw new ScriptException("Duplicate file detected. There are " + dupFiles.Length + " files named: " + FileNameWithExtension);

        // Sprites can have multiple frames! Do some sprite-specific checking.
        if (spriteType == SpriteType.Sprite)
        {
            try
            {
                spriteName = stripped.Substring(0, lastUnderscore);
            }
            catch
            {
                throw new ScriptException("Getting the sprite name of " + FileNameWithExtension + " failed.");
            }
            Int32 validFrameNumber = 0;
            try
            {
                validFrameNumber = Int32.Parse(stripped.Substring(lastUnderscore + 1));
            }
            catch
            {
                throw new ScriptException("The index of " + FileNameWithExtension + " could not be determined.");
            }
            int frame = 0;
            try
            {
                frame = Int32.Parse(stripped.Substring(lastUnderscore + 1));
            }
            catch
            {
                throw new ScriptException(FileNameWithExtension + " is using letters instead of numbers. The script has stopped for your own protection.");
            }
            int prevframe = 0;
            if (frame != 0)
            {
                prevframe = (frame - 1);
            }
            if (frame < 0)
            {
                throw new ScriptException(spriteName + " is using an invalid numbering scheme. The script has stopped for your own protection.");
            }
            var prevFrameName = spriteName + "_" + prevframe.ToString() + ".png";
            string[] previousFrameFiles = Directory.GetFiles(importFolder, prevFrameName, SearchOption.AllDirectories);
            if (previousFrameFiles.Length < 1)
                throw new ScriptException(spriteName + " is missing one or more indexes. The detected missing index is: " + prevFrameName);
        }
    }
    return importFolder;
}

public void OffsetResult()
{
    Form form = new Form()
    {
        Size = new Size(300, 160),
        MinimumSize = new Size(300, 160),
        MaximumSize = new Size(300, 160),
        MaximizeBox = false,
        MinimizeBox = false,
        Text = "Select Sprite Parameters",
        StartPosition = FormStartPosition.CenterScreen
    };
    
    Label label1 = new Label();
    label1.Location = new Point(5, 10);
    label1.Text = "Animation Speed:";
    label1.Size = new Size(110, 30);
    form.Controls.Add(label1);

    TextBox textBox = new System.Windows.Forms.TextBox();
    textBox.AcceptsReturn = false;
    textBox.AcceptsTab = false;
    textBox.AutoSize = true;
    textBox.Multiline = false;
    textBox.Text = "1";
    textBox.Name = "Animation Speed";
    textBox.Location = new Point(label1.Width+5,10);
    textBox.Size = new Size(30, 30);
    textBox.Anchor = AnchorStyles.Right;
    form.Controls.Add(textBox);

    Label label2 = new Label();
    label2.Location = new Point(5, 15+textBox.Height);
    label2.Text = "Playback Type:";
    label2.Size = new Size(110, 30);
    form.Controls.Add(label2);

    ComboBox comboBox = new ComboBox();
    comboBox.Name = "Playback Type";
    comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
    comboBox.Location = new Point(label2.Width+5,15+textBox.Height);
    comboBox.Size = new Size(160, 30);
    comboBox.Anchor = AnchorStyles.Right;
    foreach (string play in playbacks)
        comboBox.Items.Add(play);
    int defaultSelection = comboBox.Items.IndexOf("Frames Per Second");
    comboBox.SelectedIndex = defaultSelection == -1 ? 0 : defaultSelection;
    form.Controls.Add(comboBox);

    Label label3 = new Label();
    label3.Location = new Point(5, 20+textBox.Height + comboBox.Height);
    label3.Text = "Origin Position:";
    label3.Size = new Size(110, 30);
    form.Controls.Add(label3);

    ComboBox comboBox2 = new ComboBox();
    comboBox2.Name = "Origin Position";
    comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
    comboBox2.Location = new Point(label2.Width+5,20+textBox.Height + comboBox.Height);
    comboBox2.Size = new Size(160, 30);
    comboBox2.Anchor = AnchorStyles.Right;
    foreach (string off in offsets)
        comboBox2.Items.Add(off);
    int defaultSelection2 = comboBox2.Items.IndexOf("Top Left");
    comboBox2.SelectedIndex = defaultSelection2 == -1 ? 0 : defaultSelection;
    form.Controls.Add(comboBox2);
    
    int bottomY = form.Size.Height - 30;
    
    Button okBtn = new Button();
    okBtn.Text = "&Confirm";
    okBtn.Size = new Size(90, 30);
    okBtn.Location = new Point(5, 20+textBox.Height + comboBox.Height + comboBox2.Height);
    okBtn.Anchor = AnchorStyles.Left;
    form.Controls.Add(okBtn);

    okBtn.Click += (o, e) =>
    {
        if (float.TryParse(textBox.Text, out float j))
        {
            animSpd = j;
            offresult = offsets[comboBox2.SelectedIndex];
            playback = comboBox.SelectedIndex;
            form.Close();
        }
        else
        {
            MessageBox.Show("Please use a number in the animation speed.");
        }
    };
    form.ShowDialog();
}
