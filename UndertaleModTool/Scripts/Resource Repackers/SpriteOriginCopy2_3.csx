using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using UndertaleModLib;
using UndertaleModLib.Scripting;
using System.Security.Cryptography;
using UndertaleModLib.Models;
using UndertaleModLib.Decompiler;

EnsureDataLoaded();

if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1 & 2")
{
    ScriptError("Maybe script will not work? Make sure to backup files");
}
else if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1&2")
{
    ScriptError("Maybe script will not work? Make sure to backup files");
}

ScriptMessage("Select the file to copy origins from");

UndertaleData DonorData;
string DonorDataPath = PromptLoadFile(null, null);
if (DonorDataPath == null)
    throw new ScriptException("The donor data path was not set.");

using (var stream = new FileStream(DonorDataPath, FileMode.Open, FileAccess.Read))
    DonorData = UndertaleIO.Read(stream, warning => ScriptMessage("A warning occured while trying to load " + DonorDataPath + ":\n" + warning));
var DonorDataEmbeddedTexturesCount = DonorData.EmbeddedTextures.Count;
DonorData.BuiltinList = new BuiltinList(DonorData);
AssetTypeResolver.InitializeTypes(DonorData);

foreach (UndertaleSprite sprite in Data.Sprites)
{
    if (!(DonorData.Sprites.ByName(sprite.Name.Content) is null))
    {
    sprite.OriginXWrapper = DonorData.Sprites.ByName(sprite.Name.Content).OriginXWrapper;
    sprite.OriginYWrapper = DonorData.Sprites.ByName(sprite.Name.Content).OriginYWrapper;
    }
}
