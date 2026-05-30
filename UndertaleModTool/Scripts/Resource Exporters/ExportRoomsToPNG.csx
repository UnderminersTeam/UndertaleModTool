using System;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Runtime;

using UndertaleModLib;
using UndertaleModLib.Util;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;


EnsureDataLoaded();

UndertaleModTool.MainWindow mainWindow = Application.Current.MainWindow as UndertaleModTool.MainWindow;

ContentControl dataEditor = mainWindow.FindName("DataEditor") as ContentControl;
if (dataEditor is null)
    throw new ScriptException("Can't find \"DataEditor\" control.");

DependencyObject dataEditorChild = VisualTreeHelper.GetChild(dataEditor, 0);
if (dataEditorChild is null)
    throw new ScriptException("Can't find \"DataEditor\" child control.");

UndertaleRoomRenderer roomRenderer;
int roomCount = Data.Rooms.Count;

var builder = CreateScriptOptionsBuilder()
    .AddDirectory("folder", "Output Folder:")
    .AddText("patterns", "Names (one per line, leave empty for all):", multiline: true)
    .AddRadio("filterMode", "Filter mode:", "Exact", "Regex", "Wildcard")
    .AddBool("caseSensitive", "Case-sensitive", defaultValue: true)
    .AddBool("grid", "Draw room grid")
    .AddBool("memoryEconomy", "Use the memory economy mode (uses less RAM, but slower)");

var result = ShowScriptOptionsDialog("Export Rooms To PNG", builder);
if (result is null) return;

string exportedTexturesFolder = result["folder"] as string;

if (!Directory.Exists(exportedTexturesFolder))
{
    ScriptError("The specified output folder does not exist.");
    return;
}

string rawPatterns = result["patterns"] as string;
bool exportAll = string.IsNullOrWhiteSpace(rawPatterns);
string[] patterns = rawPatterns.Split("\n");
NameFilterMode filterMode = Enum.Parse<NameFilterMode>(result["filterMode"] as string);
bool caseSensitive = result["caseSensitive"] as bool? == true;

bool displayGrid = result["grid"] as bool? == true;

if (mainWindow.IsGMS2 == Visibility.Visible && result["memoryEconomy"] as bool? != true)
    TileLayerTemplateSelector.ForcedMode = 1;

DirectoryInfo dir = new DirectoryInfo(exportedTexturesFolder);

mainWindow.LastOpenedObject = mainWindow.Selected;

SetProgressBar(null, "Rooms Exported", 0, roomCount);
StartProgressBarUpdater();

await DumpRooms();

await StopProgressBarUpdater();
HideProgressBar();

GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
GC.Collect();

ScriptMessage("Exported successfully.");


async Task DumpRooms()
{
    for (int i = 0; i < roomCount; i++) {
        if (IsAppClosed)
            break;

        UndertaleRoom room = Data.Rooms[i];

        if (!exportAll)
        {
            bool match = false;
            foreach (string pattern in patterns)
            {
                if (NameFilter.IsMatch(room.Name.Content, pattern, filterMode, caseSensitive))
                {
                    match = true;
                    break;
                }
            }
            if (!match) { IncrementProgress(); continue; }
        }

        mainWindow.CurrentTab.CurrentObject = room; 

        if (roomRenderer is null)
        {
            await Task.Delay(150);
            mainWindow.RoomRendererEnabled = true;
            await Task.Delay(150);

            DependencyObject obj = VisualTreeHelper.GetChild(dataEditorChild, 0);
            if (obj is UndertaleRoomRenderer)
                roomRenderer = obj as UndertaleRoomRenderer;
            else
                throw new ScriptException("Can't find the room renderer object, try again.");
        }

        DumpRoom(room.Name.Content, (i == roomCount - 1));
    }

    mainWindow.RoomRendererEnabled = false;
}

void DumpRoom(string roomName, bool last)
{
    using (var file = File.OpenWrite(exportedTexturesFolder + System.IO.Path.DirectorySeparatorChar + roomName + ".png"))
    {
        try
        {
            roomRenderer.SaveImagePNG(file, displayGrid, last);
        }
        catch (Exception e)
        {
            throw new ScriptException($"An error occurred while exporting room \"{roomName}\".\n{e}");
        }
    }

    IncrementProgress();
}
