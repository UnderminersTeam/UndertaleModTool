using System.Text;
using System;
using System.IO;

EnsureDataLoaded();

string outputPath = PromptSaveFile(".txt", "TXT files (*.txt)|*.txt|All files (*.*)|*.*");
if (string.IsNullOrWhiteSpace(outputPath))
{
    return;
}

void WriteAssetNames<T>(StreamWriter writer, IList<T> assets) where T : UndertaleNamedResource
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
