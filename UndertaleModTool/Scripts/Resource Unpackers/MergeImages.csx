using System;
using System.IO;
using SkiaSharp;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib.Util;

string importFolderA = PromptChooseDirectory();
if (importFolderA is null)
    throw new ScriptException("The import folder was not set.");

string importFolderB = PromptChooseDirectory();
if (importFolderB is null)
    throw new ScriptException("The import folder was not set.");

string exportFolder = PromptChooseDirectory();
if (exportFolder is null)
    throw new ScriptException("The export folder was not set.");

string searchPattern = "*.png";

DirectoryInfo textureDirectoryA = new DirectoryInfo(importFolderA);
DirectoryInfo textureDirectoryB = new DirectoryInfo(importFolderB);
FileInfo[] filesA = textureDirectoryA.GetFiles(searchPattern, SearchOption.AllDirectories);

foreach (FileInfo fileA in filesA)
{
    FileInfo[] fileMatch = textureDirectoryB.GetFiles(fileA.Name);
    if (fileMatch is null)
        continue;
    FileInfo fileB = fileMatch[0];
    using SKBitmap bitmapA = SKBitmap.Decode(Path.Combine(importFolderA, fileA.Name));
    using SKBitmap bitmapB = SKBitmap.Decode(Path.Combine(importFolderB, fileA.Name));
    int width = bitmapA.Width + bitmapB.Width;
    int height = bitmapA.Height;
    if (bitmapB.Width > height)
        height = bitmapB.Width;

    using SKBitmap outputBitmap = new(width, height);
    using (SKCanvas g = new(outputBitmap))
    {
        g.DrawBitmap(bitmapA, 0, 0);
        g.DrawBitmap(bitmapB, bitmapA.Width, 0);
    }

    TextureWorker.SaveImageToFile(Path.Combine(exportFolder, fileA.Name), outputBitmap);
}
