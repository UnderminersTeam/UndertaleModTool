using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib.Util;
using ImageMagick;

string importFolderA = PromptChooseDirectory();
if (importFolderA is null) 
    throw new ScriptException("The import folder was not set.");

string importFolderB = PromptChooseDirectory();
if (importFolderB is null)
    throw new ScriptException("The import folder was not set.");

string exportFolder = PromptChooseDirectory();
if (exportFolder is null) 
    throw new ScriptException("The export folder was not set.");

// Loop over all PNG files in folder A
DirectoryInfo textureDirectoryA = new DirectoryInfo(importFolderA);
FileInfo[] filesA = textureDirectoryA.GetFiles("*.png", SearchOption.AllDirectories);
foreach (FileInfo fileA in filesA) 
{
    // If there's no matching file found, abort
    if (!File.Exists(Path.Combine(importFolderB, fileA.Name)))
        continue;
    
    // Load both images, and calculate dimensions of resulting image
    using MagickImage imageA = TextureWorker.ReadBGRAImageFromFile(Path.Combine(importFolderA, fileA.Name));
    using MagickImage imageB = TextureWorker.ReadBGRAImageFromFile(Path.Combine(importFolderB, fileA.Name));
    uint width = imageA.Width + imageB.Width;
    uint height = Math.Max(imageA.Height, imageB.Height);

    // Make combined image, and composite both images onto it
    using MagickImage outputImage = new(MagickColor.FromRgba(0, 0, 0, 0), width, height);
    outputImage.Composite(imageA, 0, 0, CompositeOperator.Copy);
    outputImage.Composite(imageB, (int)imageA.Width, 0, CompositeOperator.Copy);

    // Save image to output folder
    TextureWorker.SaveImageToFile(outputImage, Path.Combine(exportFolder, fileA.Name));
}
