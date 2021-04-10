using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

int progress = 0;
string texFolder = GetFolder(FilePath) + "Export_Tilesets" + Path.DirectorySeparatorChar;
TextureWorker worker = new TextureWorker();

if (Directory.Exists(texFolder))
{
    ScriptError("A texture export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(texFolder);

UpdateProgress();
await DumpTilesets();
worker.Cleanup();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + texFolder);

void UpdateProgress()
{
    UpdateProgressBar(null, "Tilesets", progress++, Data.Backgrounds.Count);
}

string GetFolder(string path) 
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}


async Task DumpTilesets()
{
    await Task.Run(() => Parallel.ForEach(Data.Backgrounds, DumpTileset));
}

void DumpTileset(UndertaleBackground tileset) 
{
	worker.ExportAsPNG(tileset.Texture, texFolder + tileset.Name.Content + ".png");
    UpdateProgress();
}