// Exports every room as a PNG image.
// Version for the cross-platform UndertaleModTool: it renders rooms with the app's own room renderer
// (the same one as "Save as image" in the room editor), instead of driving the WPF room editor.
using System;
using System.IO;
using System.Threading.Tasks;
using UndertaleModToolAvalonia;

EnsureDataLoaded();

string exportFolder = PromptChooseDirectory();
if (exportFolder is null)
    throw new ScriptCancelledException("The export folder was not set, stopping script.");

SetProgressBar(null, "Rooms exported", 0, Data.Rooms.Count);
StartProgressBarUpdater();

int skippedCount = 0;

// Rooms are rendered on the UI thread, because the room renderer shares the app's image cache with the open editors,
// and that cache is not thread-safe. A short pause after each room lets the progress window redraw.
foreach (UndertaleRoom room in Data.Rooms)
{
    if (IsAppClosed)
        break;

    bool hasNoImage = room is null || room.Width == 0 || room.Height == 0;
    if (hasNoImage)
    {
        skippedCount++;
        IncrementProgressParallel();
        continue;
    }

    string roomName = room.Name?.Content ?? $"room_{Data.Rooms.IndexOf(room)}";
    try
    {
        using FileStream file = File.Create(Path.Join(exportFolder, roomName + ".png"));
        await ImportExport.ExportRoomAsPNG(room, file);
    }
    catch (Exception e)
    {
        throw new ScriptException($"An error occurred while exporting room \"{roomName}\".\n{e}");
    }

    IncrementProgressParallel();
    await Task.Delay(1);
}

await StopProgressBarUpdater();
HideProgressBar();

string skippedText = skippedCount > 0 ? $"\n{skippedCount} empty or zero-sized rooms were skipped." : "";
ScriptMessage("Exported successfully." + skippedText);
