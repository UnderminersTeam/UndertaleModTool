// Exports every room as a PNG image.
// Version for the cross-platform UndertaleModTool: it renders rooms with ExportRoomAsPNG
// (the same renderer as "Save as image" in the room editor), instead of driving the WPF room editor.
using System;
using System.IO;
using System.Threading.Tasks;

EnsureDataLoaded();

string exportFolder = PromptChooseDirectory();
if (exportFolder is null)
    throw new ScriptCancelledException("The export folder was not set, stopping script.");

SetProgressBar(null, "Rooms exported", 0, Data.Rooms.Count);
StartProgressBarUpdater();

int skippedCount = 0;

// The loop runs off the UI thread so the progress window stays responsive.
await Task.Run(() =>
{
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
            ExportRoomAsPNG(room, Path.Join(exportFolder, roomName + ".png"));
        }
        catch (Exception e)
        {
            throw new ScriptException($"An error occurred while exporting room \"{roomName}\".\n{e}");
        }

        IncrementProgressParallel();
    }
});

await StopProgressBarUpdater();
HideProgressBar();

string skippedText = skippedCount > 0 ? $"\n{skippedCount} empty or zero-sized rooms were skipped." : "";
ScriptMessage("Exported successfully." + skippedText);
