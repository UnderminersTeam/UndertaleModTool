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
using ImageMagick;


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

ScriptMessage("Grabs a picture of the background in every room in the game.");


string exportedBackgroundsFolder = PromptChooseDirectory();
if (exportedBackgroundsFolder == null)
    throw new ScriptException("The export folder was not set, stopping script.");

DirectoryInfo dir = new DirectoryInfo(exportedBackgroundsFolder);

mainWindow.LastOpenedObject = mainWindow.Selected;

SetProgressBar(null, "Backgrounds Exported", 0, roomCount);
StartProgressBarUpdater();

await DumpRooms();

await StopProgressBarUpdater();
HideProgressBar();

GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce; // force full garbage collection
GC.Collect();

ScriptMessage("Exported successfully.");

var scale = 3;

async Task DumpRooms()
{
  for (int i = 0; i < roomCount; i++)
  {
    if (IsAppClosed)
      break;

    UndertaleRoom room = Data.Rooms[i];
    var savedGameObjects = room.GameObjects;
    room.GameObjects = [];

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
    ResizeDumpedRoom(room.Name.Content); 
    room.GameObjects = savedGameObjects;

    scale = 1; 
  }

  mainWindow.RoomRendererEnabled = false;
}


void DumpRoom(string roomName, bool last)
{
  var path = exportedBackgroundsFolder + System.IO.Path.DirectorySeparatorChar + "!" + roomName + ".png";
  using (var file = File.OpenWrite(path))
  {
    try
    {
      roomRenderer.SaveImagePNG(file, false, last);
    }
    catch (Exception e)
    {
      throw new ScriptException($"An error occurred while exporting room \"{roomName}\".\n{e}");
    }
  }

  
}

void ResizeDumpedRoom(string roomName)
{
  var path = exportedBackgroundsFolder + System.IO.Path.DirectorySeparatorChar + "!" + roomName + ".png";
  try
  {
    using var collection = new MagickImageCollection(path);
    collection.Coalesce();
    var info = new MagickImageInfo();
    info.Read(path);
    foreach (var image in collection)
    {
      image.Resize(info.Width * 3, info.Height * 3);
    }
    collection.Write(path);
    IncrementProgress();
  }
  catch (Exception e)
  {
    throw new ScriptException($"An error occurred while exporting room \"{roomName}\".\n{e}");
  }
}
