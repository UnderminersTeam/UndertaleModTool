// Texture packer by Samuel Roy
// Uses code from https://github.com/mfascia/TexturePacker

using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib.Util;

//EnsureDataLoaded();

string importFolder = PromptChooseDirectory("Import From Where");
if (importFolder == null)
	throw new System.Exception("The import folder was not set.");

//Stop the script if there's missing sprite entries or w/e.
string[] dirFiles = Directory.GetFiles(importFolder);
foreach (string file in dirFiles) 
{
	string stripped = Path.GetFileNameWithoutExtension(file);
	int lastUnderscore = stripped.LastIndexOf('_');
	string spriteName = stripped.Substring(0, lastUnderscore);
	Int32 validFrameNumber = 0;
	try
	{
		validFrameNumber = Int32.Parse(stripped.Substring(lastUnderscore + 1));
	}
	catch
	{
	    ScriptError(spriteName + " is using letters instead of numbers. The script has stopped for your own protection.", "Error");
		return;
	}
	int frame = Int32.Parse(stripped.Substring(lastUnderscore + 1));
	int prevframe = 0;
	if (frame != 0)
	{
		prevframe = (frame - 1);
	}
	if (frame < 0)
	{
	    ScriptError(spriteName + " is using an invalid numbering scheme. The script has stopped for your own protection.", "Error");
		return;
	}
	//ScriptError(importFolder + spriteName + "_" + prevframe.ToString() + ".png", "TEST");
	if (!(File.Exists(importFolder + spriteName + "_" + prevframe.ToString() + ".png")))
	{
	    ScriptError(spriteName + " is missing one or more indexes. The script has stopped for your own protection.", "Error");
		return;
	}	
}

System.IO.Directory.CreateDirectory("Packager");
string sourcePath = importFolder;
string searchPattern = "*.png";
string outName = "Packager/atlas.txt";
int textureSize = 2048;
int border = 0;
bool debug = false;
Packer packer = new Packer();
packer.Process(sourcePath, searchPattern, textureSize, border, debug);
packer.SaveAtlasses(outName);

string prefix = outName.Replace(Path.GetExtension(outName), "");
int atlasCount = 0;
foreach (Atlas atlas in packer.Atlasses) 
{
	string atlasName = String.Format(prefix + "{0:000}" + ".png", atlasCount);
	Bitmap atlasBitmap = new Bitmap(atlasName);
	UndertaleEmbeddedTexture texture = new UndertaleEmbeddedTexture();
	texture.TextureData.TextureBlob = File.ReadAllBytes(atlasName);
	Data.EmbeddedTextures.Add(texture);
	foreach (Node n in atlas.Nodes) 
	{
		if (n.Texture != null) 
		{
			UndertaleTexturePageItem texturePageItem = new UndertaleTexturePageItem();
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

			string stripped = Path.GetFileNameWithoutExtension(n.Texture.Source);
			int lastUnderscore = stripped.LastIndexOf('_');
			string spriteName = stripped.Substring(0, lastUnderscore);
			int frame = Int32.Parse(stripped.Substring(lastUnderscore + 1));

			UndertaleSprite sprite = Data.Sprites.ByName(spriteName);
			UndertaleSprite.TextureEntry texentry = new UndertaleSprite.TextureEntry();
			texentry.Texture = texturePageItem;
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
				newSprite.OriginX = 0; 
				newSprite.OriginY = 0;
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
						tempBitArray[j + i] = maskingBitArray[-(j-7) + i];
					}
				}
				int numBytes;
				numBytes = maskingBitArray.Length / 8;
				byte[] bytes = new byte[numBytes];
				tempBitArray.CopyTo(bytes, 0);
				for (int i = 0; i < bytes.Length; i++) 
				{
					newSprite.CollisionMasks[0].Data[i] = bytes[i];
				}
				newSprite.Textures.Add(texentry);
				Data.Sprites.Add(newSprite);
				continue;
			}
			if (frame >= sprite.Textures.Count) 
			{
				sprite.Textures.Add(texentry);
				continue;
			}
			sprite.Textures[frame] = texentry;
		}
	}
	atlasCount++;
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
				} else
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
				} else 
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
			} else
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