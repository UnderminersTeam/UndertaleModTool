// ImportGraphicsWithParameters but it can also set animation speeds and import more types of files
// Edits made by CST1229
// Based off of ImportGraphics.csx by the UTMT team
// and ImportGraphicsWithParameters.csx by someone, I don't remember (AwfulNasty???)

// revision 2: fixed gif import not working unless the folder was named Sprites,
// fixed the default origin being Top Center instead of Top Left and
// reworded Is special type?'s boolean and the background import error message
// revision 3: added optional support for single-frame sprites if a frame number is not specified
// revision 4: added support for the texture handling refactor
// revision 5: handle breaking Magick.NET changes, disabled animation speed options in GMS1 games

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
using System.Text.RegularExpressions;
using UndertaleModLib.Util;
using UndertaleModLib.Models;
using System.Windows.Forms;
using ImageMagick;

EnsureDataLoaded();

static bool importAsSprite = true;
static bool importFrameless = false;

string[] offsets = { "Top Left", "Top Center", "Top Right", "Center Left", "Center", "Center Right", "Bottom Left", "Bottom Center", "Bottom Right" };

string[] playbacks = { "Frames Per Second", "Frames Per Game Frame" };

static List<MagickImage> imagesToCleanup = new();

float animSpd = 1;

bool isSpecial = false;
uint specialVer = 1;

string offresult;

int playback;

HashSet<string> spritesStartAt1 = new HashSet<string>();

string importFolder = CheckValidity();

string packDir = Path.Combine(ExePath, "Packager");
Directory.CreateDirectory(packDir);

try
{
    string sourcePath = importFolder;
    string outName = Path.Combine(packDir, "atlas.txt");
    int textureSize = 2048;
    int PaddingValue = 2;
    bool debug = false;
    Packer packer = new Packer();
    packer.Process(sourcePath, textureSize, PaddingValue, debug);
    packer.SaveAtlasses(outName);

    int lastTextPage = Data.EmbeddedTextures.Count - 1;
    int lastTextPageItem = Data.TexturePageItems.Count - 1;

    // Import everything into UTMT
    string prefix = outName.Replace(Path.GetExtension(outName), "");
    int atlasCount = 0;
    OffsetResult();
    foreach (Atlas atlas in packer.Atlasses)
    {
        string atlasName = Path.Combine(packDir, $"{prefix}{atlasCount:000}.png");
        using MagickImage atlasImage = TextureWorker.ReadBGRAImageFromFile(atlasName);
        IPixelCollection<byte> atlasPixels = atlasImage.GetPixels();

        UndertaleEmbeddedTexture texture = new();
        texture.Name = new UndertaleString($"Texture {++lastTextPage}");
        texture.TextureData.Image = GMImage.FromMagickImage(atlasImage).ConvertToPng(); // TODO: other formats?
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
                        // check if the frame number is a valid string or not
                        Int32.Parse(stripped.Substring(lastUnderscore + 1));
                        spriteName = stripped.Substring(0, lastUnderscore);
                        frame = Int32.Parse(stripped.Substring(lastUnderscore + 1));
                    }
                    catch (Exception e)
                    {
                        if (!importFrameless)
                        {
                            continue;
                        }
                        spriteName = stripped;
                        frame = 0;
                    }

                    if (spritesStartAt1.Contains(spriteName))
                    {
                        frame--;
                    }

                    // Create TextureEntry object
                    UndertaleSprite.TextureEntry texentry = new UndertaleSprite.TextureEntry();
                    texentry.Texture = texturePageItem;

                    // Set values for new sprites
                    UndertaleSprite sprite = Data.Sprites.ByName(spriteName);
                    if (sprite is null)
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
                        newSprite.IsSpecialType = isSpecial;
                        newSprite.SVersion = specialVer;
                        switch (offresult)
                        {
                            case ("Top Left"):
                                newSprite.OriginX = 0;
                                newSprite.OriginY = 0;
                                break;
                            case ("Top Center"):
                                newSprite.OriginX = (int)(newSprite.Width / 2);
                                newSprite.OriginY = 0;
                                break;
                            case ("Top Right"):
                                newSprite.OriginX = (int)(newSprite.Width);
                                newSprite.OriginY = 0;
                                break;
                            case ("Center Left"):
                                newSprite.OriginX = 0;
                                newSprite.OriginY = (int)(newSprite.Height / 2);
                                break;
                            case ("Center"):
                                newSprite.OriginX = (int)(newSprite.Width / 2);
                                newSprite.OriginY = (int)(newSprite.Height / 2);
                                break;
                            case ("Center Right"):
                                newSprite.OriginX = (int)(newSprite.Width);
                                newSprite.OriginY = (int)(newSprite.Height / 2);
                                break;
                            case ("Bottom Left"):
                                newSprite.OriginX = 0;
                                newSprite.OriginY = (int)(newSprite.Height);
                                break;
                            case ("Bottom Center"):
                                newSprite.OriginX = (int)(newSprite.Width / 2);
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

                        int width = ((n.Bounds.Width + 7) / 8) * 8;
                        BitArray maskingBitArray = new BitArray(width * n.Bounds.Height);
                        for (int y = 0; y < n.Bounds.Height; y++)
                        {
                            for (int x = 0; x < n.Bounds.Width; x++)
                            {
                                IMagickColor<byte> pixelColor = atlasPixels.GetPixel(x + n.Bounds.X, y + n.Bounds.Y).ToColor();
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
                        int numBytes = maskingBitArray.Length / 8;
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
                    sprite.IsSpecialType = isSpecial;
                    sprite.SVersion = specialVer;
                    switch (offresult)
                    {
                        case ("Top Left"):
                            sprite.OriginX = 0;
                            sprite.OriginY = 0;
                            break;
                        case ("Top Center"):
                            sprite.OriginX = (int)(sprite.Width / 2);
                            sprite.OriginY = 0;
                            break;
                        case ("Top Right"):
                            sprite.OriginX = (int)(sprite.Width);
                            sprite.OriginY = 0;
                            break;
                        case ("Center Left"):
                            sprite.OriginX = 0;
                            sprite.OriginY = (int)(sprite.Height / 2);
                            break;
                        case ("Center"):
                            sprite.OriginX = (int)(sprite.Width / 2);
                            sprite.OriginY = (int)(sprite.Height / 2);
                            break;
                        case ("Center Right"):
                            sprite.OriginX = (int)(sprite.Width);
                            sprite.OriginY = (int)(sprite.Height / 2);
                            break;
                        case ("Bottom Left"):
                            sprite.OriginX = 0;
                            sprite.OriginY = (int)(sprite.Height);
                            break;
                        case ("Bottom Center"):
                            sprite.OriginX = (int)(sprite.Width / 2);
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
}
finally
{
	foreach (MagickImage img in imagesToCleanup) {
		img.Dispose();
	}
}

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
	public MagickImage Image;
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
	public HashSet<string> Sources;

	public Packer()
	{
		SourceTextures = new List<TextureInfo>();
		Log = new StringWriter();
		Error = new StringWriter();
	}

	public void Process(string _SourceDir, int _AtlasSize, int _Padding, bool _DebugMode)
	{
		Padding = _Padding;
		AtlasSize = _AtlasSize;
		DebugMode = _DebugMode;
		//1: scan for all the textures we need to pack
		Sources = new HashSet<string>();
		ScanForTextures(_SourceDir);
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

            // 1: Save images
            using (MagickImage img = CreateAtlasImage(atlas))
                TextureWorker.SaveImageToFile(img, atlasName);

            // 2: save description in file
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

	private void ScanForTextures(string _Path)
	{
		DirectoryInfo di = new DirectoryInfo(_Path);
		FileInfo[] files = di.GetFiles("*", SearchOption.AllDirectories);
		foreach (FileInfo fi in files)
		{
			SpriteType spriteType = GetSpriteType(fi.FullName);
			string ext = Path.GetExtension(fi.FullName);

			bool isSprite = spriteType == SpriteType.Sprite || (spriteType == SpriteType.Unknown && importAsSprite);

			if (ext == ".gif")
			{
				// animated .gif
				string dirName = Path.GetDirectoryName(fi.FullName);
				string spriteName = Path.GetFileNameWithoutExtension(fi.FullName);

                MagickReadSettings settings = new()
                {
                    ColorSpace = ColorSpace.sRGB,
                };
                using MagickImageCollection gif = new(fi.FullName, settings);
				int frames = gif.Count;
                if (!isSprite && frames > 1)
				{
					throw new ScriptException(fi.FullName + " is a " + spriteType + ", but has more than 1 frame. Script has been stopped.");
				}

				for (int i = frames - 1; i >= 0; i--)
				{
					AddSource(
						(MagickImage)gif[i],
						Path.Join(
							dirName,
							isSprite ?
								(spriteName + "_" + i + ".png") : (spriteName + ".png")
						)
					);
					// don't auto-dispose
					gif.RemoveAt(i);
				}
			}
			else if (ext == ".png")
			{
				Match stripMatch = null;
				if (isSprite)
				{
					stripMatch = Regex.Match(Path.GetFileNameWithoutExtension(fi.Name), @"(.*)_strip(\d+)");
				}
				if (stripMatch is not null && stripMatch.Success)
				{
					string spriteName = stripMatch.Groups[1].Value;
					string frameCountStr = stripMatch.Groups[2].Value;

                    uint frames;
					try
					{
						frames = UInt32.Parse(frameCountStr);
					}
					catch
					{
						throw new ScriptException(fi.FullName + " has an invalid strip numbering scheme. Script has been stopped.");
					}
					if (frames <= 0)
					{
						throw new ScriptException(fi.FullName + " has 0 frames. Script has been stopped.");
					}

					if (!isSprite && frames > 0)
					{
						throw new ScriptException(fi.FullName + " is not a sprite, but has more than 1 frame. Script has been stopped.");
					}

                    MagickReadSettings settings = new()
                    {
                        ColorSpace = ColorSpace.sRGB,
                    };
                    using MagickImage img = new(fi.FullName, settings);
					if ((img.Width % frames) > 0)
					{
						throw new ScriptException(fi.FullName + " has a width not divisible by the number of frames. Script has been stopped.");
					}

					string dirName = Path.GetDirectoryName(fi.FullName);

                    uint frameWidth = (uint)img.Width / frames;
                    uint frameHeight = (uint)img.Height;
					for (uint i = 0; i < frames; i++)
					{
						AddSource(
							(MagickImage)img.Clone(
								(int)(frameWidth * i), 0, frameWidth, frameHeight
							),
							Path.Join(dirName,
								isSprite ?
									(spriteName + "_" + i + ".png") : (spriteName + ".png")
							)
						);
					}
				}
				else
                {
                    MagickReadSettings settings = new()
                    {
                        ColorSpace = ColorSpace.sRGB,
                    };
                    MagickImage img = new(fi.FullName);
					AddSource(img, fi.FullName);
				}
			}
		}
	}

	private void AddSource(MagickImage img, string fullName)
	{
        imagesToCleanup.Add(img);
        if (img.Width <= AtlasSize && img.Height <= AtlasSize)
		{
			TextureInfo ti = new TextureInfo();

			if (!Sources.Add(fullName))
			{
				throw new ScriptException(
					Path.GetFileNameWithoutExtension(fullName) +
					" as a frame already exists (possibly due to having multiple types of sprite images named the same). Script has been stopped."
				);
			}

			ti.Source = fullName;
			ti.Width = (int)img.Width;
			ti.Height = (int)img.Height;
            ti.Image = img;

            SourceTextures.Add(ti);

			Log.WriteLine("Added " + fullName);
		}
		else
		{
			Error.WriteLine(fullName + " is too large to fix in the atlas. Skipping!");
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
                using IMagickImage<byte> resizedSourceImg = TextureWorker.ResizeImage(n.Texture.Image, n.Bounds.Width, n.Bounds.Height);
                img.Composite(resizedSourceImg, n.Bounds.X, n.Bounds.Y, CompositeOperator.Copy);
			}
		}
        return img;
	}
}

public static SpriteType GetSpriteType(string path)
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
Otherwise, the image will be imported as a sprite, and allow you to select its origin point and animation speed (if applicable).
Accepted sprite formats: separate frames starting at 0 or 1 (sprite_N.png), GM-style strip (sprite_stripN.png), animated GIF (sprite.gif), optionally single image (sprite.png).
Accepted background formats: single image (bg.png), single-frame GIF (bg.gif).
Do you want to continue?");
	if (!recursiveCheck)
		throw new ScriptException("Script cancelled.");

	// Get import folder
	string importFolder = PromptChooseDirectory();
	if (importFolder == null)
		throw new ScriptException("The import folder was not set.");

	//Stop the script if there's missing sprite entries or w/e.
	bool hadMessage = false;
	bool hadFramelessMessage = false;
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
				// this is annoying
				/*importAsSprite = ScriptQuestion(FileNameWithExtension + @" is in an incorrectly-named folder (valid names being ""Sprites"" and ""Backgrounds""). Would you like to import these images as sprites?
Pressing ""No"" will cause the program to ignore these images.");*/
				importAsSprite = true;
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
			Match stripMatch = Regex.Match(stripped, @"(.*)_strip(\d+)");
			if (stripMatch.Success)
			{
				string frameCountStr = stripMatch.Groups[2].Value;

				int frames;
				try
				{
					frames = Int32.Parse(frameCountStr);
				}
				catch
				{
					throw new ScriptException(FileNameWithExtension + " has an invalid strip numbering scheme. Script has been stopped.");
				}
				if (frames <= 0)
				{
					throw new ScriptException(FileNameWithExtension + " has 0 frames. Script has been stopped.");
				}

				// Probably a valid strip, can continue
				continue;
			}

			try
			{
				spriteName = stripped.Substring(0, lastUnderscore);
				// check if the frame number is a valid string or not
		
	
																								  
	
							  
	  
	
				Int32.Parse(stripped.Substring(lastUnderscore + 1));
			}
			catch
			{
				if (!hadFramelessMessage)
				{
					importFrameless = ScriptQuestion(FileNameWithExtension + @" does not seem to have a frame number or count. Import this image as a single-frame sprite named " + stripped + @"?
Pressing ""No"" will cause the program to ignore these images.");
					hadFramelessMessage = true;
				}
				if (importFrameless)
				{
					spriteName = stripped;
				}
				else
				{
					continue;
				}
				// throw new ScriptException("Getting the sprite name of " + FileNameWithExtension + " failed.");
			}
			
			int frame = 0;
			// if the sprite doesn't have an underscore, don't bother trying to parse it since it'll be single-frame anyways
			if (spriteName != stripped)
			{
				Int32 validFrameNumber = 0;
				try
				{
					validFrameNumber = Int32.Parse(stripped.Substring(lastUnderscore + 1));
				}
				catch
				{
					if (!hadFramelessMessage)
					{
						importFrameless = ScriptQuestion(FileNameWithExtension + @" does not seem to have a frame number or count. Import this image as a single-frame sprite named " + stripped + @"?
	Pressing ""No"" will cause the program to ignore these images.");
						hadFramelessMessage = true;
					}
					if (importFrameless)
					{
						spriteName = stripped;
					}
					else
					{
						continue;
					}
					// throw new ScriptException("The index of " + FileNameWithExtension + " could not be determined.");
				}
				try
				{
					frame = Int32.Parse(stripped.Substring(lastUnderscore + 1));
				}
				catch
				{
					throw new ScriptException(FileNameWithExtension + " is using letters instead of numbers. The script has stopped for your own protection.");
				}
			}

			int prevframe = 0;
			if (frame > 0)
			{
				prevframe = (frame - 1);
			}
			else if (frame < 0)
			{
				throw new ScriptException(spriteName + " is using an invalid numbering scheme. The script has stopped for your own protection.");
			}
			else
			{
				continue;
			}
			string prevFrameName = spriteName + "_" + prevframe.ToString() + ".png";
			string[] previousFrameFiles = Directory.GetFiles(importFolder, prevFrameName, SearchOption.AllDirectories);
			if (previousFrameFiles.Length < 1)
			{
				if (frame == 1)
				{
					spritesStartAt1.Add(spriteName);
                }
				else
				{
					throw new ScriptException(spriteName + " is missing one or more indexes. The detected missing index is: " + prevFrameName);
				}
			}
		}
	}
	return importFolder;
}

public void OffsetResult()
{
	Form form = new Form()
	{
		Size = new Size(300, 200),
		MinimumSize = new Size(300, 200),
		MaximumSize = new Size(300, 200),
		MaximizeBox = false,
		MinimizeBox = false,
		Text = "Select Sprite Parameters",
		StartPosition = FormStartPosition.CenterScreen
	};

	ToolTip toolTip = new ToolTip();

	Label specialLabel = new Label();
	specialLabel.Location = new Point(5, 10);
	specialLabel.Text = "Special Version:";
	specialLabel.Size = new Size(110, 30);
	form.Controls.Add(specialLabel);

	CheckBox isSpecialBox = new System.Windows.Forms.CheckBox();
    isSpecialBox.Enabled = Data.IsGameMaker2();
    isSpecialBox.Location = new Point(specialLabel.Width + 5, 10);
	isSpecialBox.Size = new Size(20, 20);
	toolTip.SetToolTip(isSpecialBox, "Is special type? (required for setting animation speed)");
	form.Controls.Add(isSpecialBox);

	TextBox specialVerBox = new System.Windows.Forms.TextBox();
    specialVerBox.Enabled = Data.IsGameMaker2();
    specialVerBox.AcceptsReturn = false;
	specialVerBox.AcceptsTab = false;
	specialVerBox.AutoSize = true;
	specialVerBox.Multiline = false;
	specialVerBox.Text = "1";
	specialVerBox.Name = "Special Version";
	specialVerBox.Location = new Point(specialLabel.Width + 5 + 20, 10);
	specialVerBox.Size = new Size(30, 30);
	specialVerBox.Anchor = AnchorStyles.Right;
	form.Controls.Add(specialVerBox);

	Label label1 = new Label();
	label1.Location = new Point(5, specialVerBox.Height + 15);
	label1.Text = "Animation Speed:";
	label1.Size = new Size(110, 30);
	form.Controls.Add(label1);

	TextBox textBox = new System.Windows.Forms.TextBox();
    textBox.Enabled = Data.IsGameMaker2();
    textBox.AcceptsReturn = false;
	textBox.AcceptsTab = false;
	textBox.AutoSize = true;
	textBox.Multiline = false;
	textBox.Text = "1";
	textBox.Name = "Animation Speed";
	textBox.Location = new Point(label1.Width + 5, specialVerBox.Height + 15);
	textBox.Size = new Size(30, 30);
	textBox.Anchor = AnchorStyles.Right;
	form.Controls.Add(textBox);

	Label label2 = new Label();
	label2.Location = new Point(5, 20 + specialVerBox.Height + textBox.Height);
	label2.Text = "Playback Type:";
	label2.Size = new Size(110, 30);
	form.Controls.Add(label2);

	ComboBox comboBox = new ComboBox();
    comboBox.Enabled = Data.IsGameMaker2();
    comboBox.Name = "Playback Type";
	comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
	comboBox.Location = new Point(label2.Width + 5, 20 + specialVerBox.Height + textBox.Height);
	comboBox.Size = new Size(160, 30);
	comboBox.Anchor = AnchorStyles.Right;
	foreach (string play in playbacks)
		comboBox.Items.Add(play);
	int defaultSelection = comboBox.Items.IndexOf("Frames Per Game Frame");
	comboBox.SelectedIndex = defaultSelection == -1 ? 0 : defaultSelection;
	form.Controls.Add(comboBox);

	Label label3 = new Label();
	label3.Location = new Point(5, 25 + specialVerBox.Height + textBox.Height + comboBox.Height);
	label3.Text = "Origin Position:";
	label3.Size = new Size(110, 30);
	form.Controls.Add(label3);

	ComboBox comboBox2 = new ComboBox();
	comboBox2.Name = "Origin Position";
	comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
	comboBox2.Location = new Point(label2.Width + 5, 25 + specialVerBox.Height + textBox.Height + comboBox.Height);
	comboBox2.Size = new Size(160, 30);
	comboBox2.Anchor = AnchorStyles.Right;
	foreach (string off in offsets)
		comboBox2.Items.Add(off);
	int defaultSelection2 = comboBox2.Items.IndexOf("Top Left");
	comboBox2.SelectedIndex = defaultSelection2 == -1 ? 0 : defaultSelection2;
	form.Controls.Add(comboBox2);

	int bottomY = form.Size.Height - 30;

	Button okBtn = new Button();
	okBtn.Text = "&Confirm";
	okBtn.Size = new Size(90, 30);
	okBtn.Location = new Point(5, 35 + specialVerBox.Height + textBox.Height + comboBox.Height + comboBox2.Height);
	okBtn.Anchor = AnchorStyles.Left;
	form.Controls.Add(okBtn);

	EventHandler updateFramesActive = (o, e) =>
	{
		specialVerBox.Enabled = isSpecialBox.Checked;
		textBox.Enabled = isSpecialBox.Checked;
	};

	isSpecialBox.CheckedChanged += updateFramesActive;
	updateFramesActive(null, null);

	okBtn.Click += (o, e) =>
	{
		if (float.TryParse(textBox.Text, out float j))
		{
			if (uint.TryParse(specialVerBox.Text, out uint k))
			{
				isSpecial = isSpecialBox.Checked;
				specialVer = k;
				animSpd = j;
				offresult = offsets[comboBox2.SelectedIndex];
				playback = comboBox.SelectedIndex;
				form.Close();
			}
			else
			{
				MessageBox.Show("Please use a number in the special version.");
			}
		}
		else
		{
			MessageBox.Show("Please use a number in the animation speed.");
		}
	};
	form.ShowDialog();
}
