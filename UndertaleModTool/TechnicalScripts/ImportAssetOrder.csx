// Takes an existing text file exported from ExportAssetOrder, and uses it to reorganize the assets in the current data file.
// Made by colinator27.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib.Util;

string assetNamePath = PromptLoadFile("Choose asset name text file", "Text files (.txt)|*.txt|All files|*");
if (assetNamePath == null)
    throw new Exception("The asset name text file was not chosen");

string[] lines = File.ReadAllLines(assetNamePath);

void Reorganize<T>(IList<T> list, List<string> order) where T : UndertaleNamedResource, new()
{
    Dictionary<string, T> temp = new Dictionary<string, T>();
    for (int i = 0; i < list.Count; i++)
    {
        T asset = list[i];
        string assetName = asset.Name?.Content;
        if (order.Contains(assetName))
        {
            temp[assetName] = asset;
        }
    }
    
    List<T> addOrder = new List<T>();
    for (int i = order.Count - 1; i >= 0; i--)
    {
        T asset;
        try
        {
            asset = temp[order[i]];
        } catch (Exception e)
        {
            throw new Exception("Missing asset with name \"" + order[i] + "\"");
        }
        addOrder.Add(asset);
    }
    
    foreach (T asset in addOrder)
        list.Remove(asset);
    foreach (T asset in addOrder)
        list.Insert(0, asset);
}

string currentType;
List<string> currentList = new List<string>();

void SubmitList()
{
    if (currentList.Count == 0)
        return;
    
    switch (currentType)
    {
        case "sounds":
            Reorganize<UndertaleSound>(Data.Sounds, currentList);
            break;
        case "sprites":
            Reorganize<UndertaleSprite>(Data.Sprites, currentList);
            break;
        case "backgrounds":
            Reorganize<UndertaleBackground>(Data.Backgrounds, currentList);
            break;
        case "paths":
            Reorganize<UndertalePath>(Data.Paths, currentList);
            break;
        case "scripts":
            Reorganize<UndertaleScript>(Data.Scripts, currentList);
            break;
        case "fonts":
            Reorganize<UndertaleFont>(Data.Fonts, currentList);
            break;
        case "objects":
            Reorganize<UndertaleGameObject>(Data.GameObjects, currentList);
            break;
        case "timelines":
            Reorganize<UndertaleTimeline>(Data.Timelines, currentList);
            break;
        case "rooms":
            Reorganize<UndertaleRoom>(Data.Rooms, currentList);
            break;
        case "shaders":
            Reorganize<UndertaleShader>(Data.Shaders, currentList);
            break;
        case "extensions":
            Reorganize<UndertaleExtension>(Data.Extensions, currentList);
            break;
    }
}

foreach (string line in lines)
{
    if (line.StartsWith("@@") && line.EndsWith("@@"))
    {
        SubmitList();
        currentType = line.Substring(2, line.Length - 4).ToLower();
        currentList.Clear();
    } else
        currentList.Add(line.Trim());
}
SubmitList();