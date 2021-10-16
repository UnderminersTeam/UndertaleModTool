using System;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModTool;

DoLongErrorMessages(false);
EnsureDataLoaded();

int progress = 0;
int exportTotal = Data.Rooms.Count;

List<string> outputsList = new List<string>();

System.Windows.Controls.ItemsControl RoomGraphics;

string exportedTexturesFolder = PromptChooseDirectory("Choose an export folder");
if (exportedTexturesFolder == null) {
    throw new System.Exception("The export folder was not set, stopping script.");
}

DirectoryInfo dir = new DirectoryInfo(exportedTexturesFolder);

TextureWorker worker = new TextureWorker();

await DumpRooms();

worker.Cleanup();
HideProgressBar();

void UpdateProgress() {
    UpdateProgressBar(null, "Rooms Exported", progress += 1, exportTotal);
}

async Task DumpRooms() {
    for (int i = 0; i < Data.Rooms.Count; i++) {
        // Change room here
        UndertaleModTool.MainWindow window = (UndertaleModTool.MainWindow)(Application.Current.MainWindow);
        window.Highlighted = (Data.Rooms[i]);
        window.ChangeSelection(Highlighted);
        await Task.Delay(TimeSpan.FromSeconds(0.1));
        DumpRoom(Data.Rooms[i]);
    }
}

void DumpRoom(UndertaleRoom room) {
    using (var file = File.OpenWrite(exportedTexturesFolder + System.IO.Path.DirectorySeparatorChar + room.Name.Content + ".png")) {
        SaveImagePNG(file);
    }
    UpdateProgress();
}

public void SaveImagePNG(Stream outfile) {
    try {
        Window window = Application.Current.MainWindow;
        System.Windows.Controls.ItemsControl RoomGraphics = FindChild<System.Windows.Controls.ItemsControl>(window, "RoomGraphics");
        var target = new RenderTargetBitmap((int)RoomGraphics.RenderSize.Width, (int)RoomGraphics.RenderSize.Height, 96, 96, PixelFormats.Pbgra32);
        target.Render(RoomGraphics);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(target));
        encoder.Save(outfile);
    } catch (Exception e) {
        ScriptMessage(e.ToString());
    }
}

// Copied this from Stack overflow ~Sam
public static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject {    
    // Confirm parent and childName are valid. 
    if (parent == null){
      return null;
    }

    T foundChild = null;

    int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
    for (int i = 0; i < childrenCount; i++) {
        var child = VisualTreeHelper.GetChild(parent, i);
        // If the child is not of the request child type child
        T childType = child as T;
        if (childType == null) {
            // recursively drill down the tree
            foundChild = FindChild<T>(child, childName);

            // If the child is found, break so we do not overwrite the found child. 
            if (foundChild != null) {
                break;
            }
        } else if (!string.IsNullOrEmpty(childName)) {
            var frameworkElement = child as FrameworkElement;
            // If the child's name is set for search
            if (frameworkElement != null && frameworkElement.Name == childName) {
                // if the child's name is of the request name
                foundChild = (T)child;
                break;
            }
        } else {
            // child element found.
            foundChild = (T)child;
            break;
        }
    }
    return foundChild;
}