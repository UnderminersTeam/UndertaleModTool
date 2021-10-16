using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib.Util;

DoLongErrorMessages(false);

string importFolderA = PromptChooseDirectory("Import From Where");
if (importFolderA == null) {
    throw new System.Exception("The import folder was not set.");
}

string importFolderB = PromptChooseDirectory("Merge With Where");
if (importFolderB == null) {
    throw new System.Exception("The import folder was not set.");
}

string exportFolder = PromptChooseDirectory("Export To Where");
if (exportFolder == null) {
    throw new System.Exception("The export folder was not set.");
}

string searchPattern = "*.png";

DirectoryInfo textureDirectoryA = new DirectoryInfo(importFolderA);
DirectoryInfo textureDirectoryB = new DirectoryInfo(importFolderB);
FileInfo[] filesA = textureDirectoryA.GetFiles(searchPattern, SearchOption.AllDirectories);

foreach (FileInfo fileA in filesA) {
    FileInfo[] fileMatch = textureDirectoryB.GetFiles(fileA.Name);
    if (fileMatch == null) {
        continue;
    }
    FileInfo fileB = fileMatch[0];
    Bitmap bitmapA = new Bitmap(System.IO.Path.Combine(importFolderA, fileA.Name));
    Bitmap bitmapB = new Bitmap(System.IO.Path.Combine(importFolderB, fileA.Name));
    int width = bitmapA.Size.Width + bitmapB.Size.Width;
    int height = bitmapA.Size.Width;
    if (bitmapB.Size.Width > height) {
        height = bitmapB.Size.Width;
    }
    
    Bitmap outputBitmap = new Bitmap(width, height);
    
    outputBitmap = SuperimposeOntoBitmap(bitmapA, outputBitmap, 0);
    outputBitmap = SuperimposeOntoBitmap(bitmapB, outputBitmap, bitmapA.Size.Width);
    
    outputBitmap.Save(System.IO.Path.Combine(exportFolder, fileA.Name), ImageFormat.Png);
}

Bitmap SuperimposeOntoBitmap(Bitmap bitmapToAdd, Bitmap baseBitmap, int x) {
    Graphics g = Graphics.FromImage(baseBitmap);
    g.CompositingMode = CompositingMode.SourceOver;
    g.DrawImage(bitmapToAdd, new System.Drawing.Point(x, 0));
    return baseBitmap;
}
