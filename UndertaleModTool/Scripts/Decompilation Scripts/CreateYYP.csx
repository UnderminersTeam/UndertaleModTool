// Made with the help of QuantumV and The United Modders Of Pizza Tower Team
using System.Text;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UndertaleModLib.Util;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

EnsureDataLoaded();

string yypFolder = GetFolder(FilePath) + "Decompiled" + Path.DirectorySeparatorChar;

if (!Directory.Exists(yypFolder))
{
    Directory.CreateDirectory(yypFolder);
}

string jsonFilePath = GetFolder(FilePath) + "Decompiled" + Path.DirectorySeparatorChar + Data.GeneralInfo.FileName.Content + ".yyp";
// type hell
Tuple<string, IEnumerable<UndertaleNamedResource>>[] foldersPaths =
{
    Tuple.Create("sprites", (IEnumerable<UndertaleNamedResource>)Data.Sprites),
    Tuple.Create("scripts", (IEnumerable<UndertaleNamedResource>)Data.Scripts),
    Tuple.Create("objects", (IEnumerable<UndertaleNamedResource>)Data.GameObjects),
    Tuple.Create("rooms", (IEnumerable<UndertaleNamedResource>)Data.Rooms),
    Tuple.Create("tilesets", (IEnumerable<UndertaleNamedResource>)Data.Backgrounds),
    Tuple.Create("sprites", (IEnumerable<UndertaleNamedResource>)Data.Backgrounds),
    Tuple.Create("sounds", (IEnumerable<UndertaleNamedResource>)Data.Sounds),
    Tuple.Create("shaders", (IEnumerable<UndertaleNamedResource>)Data.Shaders),
    Tuple.Create("fonts", (IEnumerable<UndertaleNamedResource>)Data.Fonts),
    Tuple.Create("extensions", (IEnumerable<UndertaleNamedResource>)Data.Extensions),
};
var resources = new List<object>();
var folders = new List<object>();

List<UndertaleCode> scriptCode = new List<UndertaleCode>();
foreach (UndertaleScript script in Data.Scripts)
{
    if (script.Code == null) continue;
    scriptCode.Add(script.Code);
}
bool hasGlobalInit = Data.GlobalInitScripts.ToList().Exists(
    (UndertaleGlobalInit gi) => scriptCode.Contains(gi.Code)
);

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}
#region yyp info
string projectname = Data.GeneralInfo.FileName.Content;

#endregion
var folderData = new[]
{
    new { name = "Sprites", folderPath = "folders/Sprites.yy", order = 1 },
    new { name = "Tile Sets", folderPath = "folders/Tile Sets.yy", order = 2 },
    new { name = "Sounds", folderPath = "folders/Sounds.yy", order = 3 },
    new { name = "Scripts", folderPath = "folders/Scripts.yy", order = 5 },
    new { name = "Shaders", folderPath = "folders/Shaders.yy", order = 6 },
    new { name = "Paths", folderPath = "folders/Paths.yy", order = 4 },
    new { name = "Fonts", folderPath = "folders/Fonts.yy", order = 7 },
    new { name = "Timelines", folderPath = "folders/Timelines.yy", order = 8 },
    new { name = "Objects", folderPath = "folders/Objects.yy", order = 9 },
    new { name = "Rooms", folderPath = "folders/Rooms.yy", order = 10 },
    new { name = "Animation Curves", folderPath = "folders/Animation Curves.yy", order = 12 },
    new { name = "Notes", folderPath = "folders/Notes.yy", order = 13 },
    new { name = "Sequences", folderPath = "folders/Sequences.yy", order = 11 },
    new { name = "Extensions", folderPath = "folders/Extensions.yy", order = 14 }
};

foreach (var folderPathF in foldersPaths)
{
    try
    {
        string folderPath = GetFolder(FilePath) + "Decompiled" + Path.DirectorySeparatorChar + folderPathF + Path.DirectorySeparatorChar;

        foreach (var element in folderPathF.Item2)
        {
            if (element is UndertaleScript && ((UndertaleScript)element)?.Code?.ParentEntry != null) continue;
            string elementName = element.Name.Content;

            if (folderPathF.Item1 == "sprites" && folderPathF.Item2 == Data.Backgrounds) {
                elementName = "tilespr_" + elementName;
            }

            var resource = new
            {
                id = new
                {
                    name = elementName,
                    path = $"{folderPathF.Item1}/{elementName}/{elementName}.yy"
                },
                order = resources.Count
            };

            resources.Add(resource);
        }

        if (folderPathF.Item2 == Data.Scripts && hasGlobalInit)
        {
            var resource = new
            {
                id = new
                {
                    name = "___global_init",
                    path = $"{folderPathF.Item1}/___global_init/___global_init.yy"
                },
                order = resources.Count
            };

            resources.Add(resource);
        }
    }
    catch (Exception ex)
    {
        continue;
    }


}

foreach(var folder in folderData)
{
    folders.Add(folder);
}

public class AssetReference
{
    public string name { get; set; }
    public string path { get; set; }
}

class GMTextureGroup
{
    public string resourceType = "GMTextureGroup";
    public string resourceVersion = "1.3";
    public string name;
    public bool autocrop = true;
    public uint border = 2;
    public string compressFormat = "bz2";
    public string directory = "";
    public AssetReference groupParent;
    public bool isScaled = true;
    public string loadType = "default";
    public uint mipsToGenerate = 0;
    public int targets = -1;
}

var textureGroups = new List<GMTextureGroup>();
foreach (UndertaleTextureGroupInfo group in Data.TextureGroupInfo)
{
    // this texture page is created by gamemaker, so don't try adding it again
    if (group.Name.Content == "__YY__0fallbacktexture.png_YYG_AUTO_GEN_TEX_GROUP_NAME_") continue;

    string compress = "png";
    if (group.TexturePages.Count > 0)
    {
        UndertaleEmbeddedTexture firstPage = group.TexturePages[0].Resource;
        if (firstPage.TextureData.FormatBZ2)
            compress = "bz2";
        else if (firstPage.TextureData.FormatQOI)
            compress = "qoi";
    }
    textureGroups.Add(new GMTextureGroup
    {
        name = group.Name.Content,
        directory = group?.Directory?.Content ?? "",
        loadType = group.LoadType switch {
            UndertaleTextureGroupInfo.TextureGroupLoadType.InFile => "default",
            // i have no idea
            UndertaleTextureGroupInfo.TextureGroupLoadType.SeparateGroup => "dynamicpages",
            UndertaleTextureGroupInfo.TextureGroupLoadType.SeparateTextures => "dynamicpages",
        },
        compressFormat = compress,
    }
    );
}


var json = new
{
    resourceType = "GMProject",
    resourceVersion = "1.6",
    name = projectname,
    resources = resources,
    Options = new[]
    {
        new
        {
            name = "Main",
            path = "options/main/options_main.yy"
        },
        new
        {
            name = "Windows",
            path = "options/windows/options_windows.yy"
        }
    },
    defaultScriptType = 1,
    isEcma = false,
    Folders = folders,
    configs = new
    {
        name = "Default",
        children = new object[] { }
    },
    IncludedFiles = new object[] { },
    MetaData = new
    {
        IDEVersion = "2022.0.0.19"
    },
    TextureGroups = textureGroups
};

string jsonString = JsonConvert.SerializeObject(json, Formatting.Indented);
File.WriteAllText(jsonFilePath, jsonString);