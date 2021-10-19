using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib.Util;

EnsureDataLoaded();

string res = "";
string x = "";
List<string> currentList = new List<string>();

for (int i = 0; i < Data.GeneralInfo.RoomOrder.Count; i++)
{
    x = Data.GeneralInfo.RoomOrder[i].ToString();
    string stringBeforeChar = x.Substring(0, x.IndexOf(" "));
    res += (stringBeforeChar + "\n");
    currentList.Add(stringBeforeChar);
}
Reorganize<UndertaleRoom>(Data.Rooms, currentList);
return res;

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
            throw new ScriptException("Missing asset with name \"" + order[i] + "\"");
        }
        addOrder.Add(asset);
    }
    
    foreach (T asset in addOrder)
        list.Remove(asset);
    foreach (T asset in addOrder)
        list.Insert(0, asset);
}
