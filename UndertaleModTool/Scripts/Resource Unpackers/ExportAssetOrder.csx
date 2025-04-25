// Exports the names of assets in a data file in order.
// Made by Grossley and colinator27.

using System.Text;
using System;
using System.IO;

EnsureDataLoaded();

// Get the path, and check for overwriting
string outputPath = Path.Combine(Path.GetDirectoryName(FilePath) + Path.DirectorySeparatorChar, "asset_names.txt");
if (File.Exists(outputPath))
{
    bool overwriteCheck = ScriptQuestion(@"An 'asset_names.txt' file already exists. 
Would you like to overwrite it?");
    if (overwriteCheck)
        File.Delete(outputPath);
    else
    {
        ScriptError("An 'asset_names.txt' file already exists. Please remove it and try again.", "Error: Export already exists.");
        return;
    }
}

void WriteAssetNames(StreamWriter writer, IList<UndertaleNamedResource> assets)
{
    if (assets.Count == 0)
        return;
    foreach (var asset in assets)
    {
        if (asset is not null)
            writer.WriteLine(asset.Name?.Content ?? assets.IndexOf(asset).ToString());
        else
            writer.WriteLine("(null)");
    }
}

using (StreamWriter writer = new StreamWriter(outputPath))
{
    // Write Sounds.
    writer.WriteLine("@@sounds@@");
    WriteAssetNames(writer, Data.Sounds);

    // Write Sprites.
    writer.WriteLine("@@sprites@@");
    WriteAssetNames(writer, Data.Sprites);
    
    // Write Backgrounds.
    writer.WriteLine("@@backgrounds@@");
    WriteAssetNames(writer, Data.Backgrounds);
    
    // Write Paths.
    writer.WriteLine("@@paths@@");
    WriteAssetNames(writer, Data.Paths);
    
    // Write Scripts.
    writer.WriteLine("@@scripts@@");
    WriteAssetNames(writer, Data.Scripts);
    
    // Write Fonts.
    writer.WriteLine("@@fonts@@");
    WriteAssetNames(writer, Data.Fonts);

    // Write Objects.
    writer.WriteLine("@@objects@@");
    WriteAssetNames(writer, Data.GameObjects);
    
    // Write Timelines.
    writer.WriteLine("@@timelines@@");
    WriteAssetNames(writer, Data.Timelines);

    // Write Rooms.
    writer.WriteLine("@@rooms@@");
    WriteAssetNames(writer, Data.Rooms);

    // Write Shaders.
    writer.WriteLine("@@shaders@@");
    WriteAssetNames(writer, Data.Shaders);

    // Write Extensions.
    writer.WriteLine("@@extensions@@");
    WriteAssetNames(writer, Data.Extensions);

    // TODO: Perhaps detect GMS2.3, export those asset names as well.
}
