using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleRoomEditor.xaml
    /// </summary>
    public partial class UndertaleRoomEditor : UserControl
    {
        public static DependencyProperty PreviewPathProperty =
            DependencyProperty.Register("PreviewPath", typeof(UndertalePath),
                typeof(UndertaleRoomEditor),
                new FrameworkPropertyMetadata(null));

        public UndertalePath PreviewPath
        {
            get { return (UndertalePath)GetValue(PreviewPathProperty); }
            set { SetValue(PreviewPathProperty, value); }
        }

        public UndertaleRoomEditor()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // I can't bind it directly because then clicking on the headers makes WPF explode because it tries to attach the header as child of ObjectEditor
            // TODO: find some better workaround
            object sel = e.NewValue;
            if (sel is UndertaleObject)
            {
                ObjectEditor.Content = sel;
            }
        }

        private UndertaleObject movingObj;
        private double hotpointX, hotpointY;
        
        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UndertaleObject clickedObj = (sender as Rectangle).DataContext as UndertaleObject;
            UndertaleObject selectedObj = ObjectEditor.Content as UndertaleObject;

            SelectObject(clickedObj);

            var mousePos = e.GetPosition(RoomGraphics);
            if (selectedObj is UndertaleRoom.GameObject || selectedObj is UndertaleRoom.Tile)
            {
                movingObj = clickedObj;
                if (movingObj is UndertaleRoom.GameObject)
                {
                    hotpointX = mousePos.X - (movingObj as UndertaleRoom.GameObject).X;
                    hotpointY = mousePos.Y - (movingObj as UndertaleRoom.GameObject).Y;
                }
                else if (movingObj is UndertaleRoom.Tile)
                {
                    hotpointX = mousePos.X - (movingObj as UndertaleRoom.Tile).X;
                    hotpointY = mousePos.Y - (movingObj as UndertaleRoom.Tile).Y;
                }
            }
        }

        private void Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (movingObj != null)
            {
                var mousePos = e.GetPosition(RoomGraphics);

                int tgtX = (int)(mousePos.X - hotpointX);
                int tgtY = (int)(mousePos.Y - hotpointY);

                int gridSize = 5;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    gridSize = 20;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    gridSize = 10;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    gridSize = 1;

                // Snap to grid
                tgtX = ((tgtX + gridSize / 2) / gridSize) * gridSize;
                tgtY = ((tgtY + gridSize / 2) / gridSize) * gridSize;

                if (movingObj is UndertaleRoom.GameObject)
                {
                    (movingObj as UndertaleRoom.GameObject).X = tgtX;
                    (movingObj as UndertaleRoom.GameObject).Y = tgtY;
                }
                else if (movingObj is UndertaleRoom.Tile)
                {
                    (movingObj as UndertaleRoom.Tile).X = tgtX;
                    (movingObj as UndertaleRoom.Tile).Y = tgtY;
                }
            }
        }

        private void Rectangle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            movingObj = null;
        }

        private void SelectObject(UndertaleObject obj)
        {
            foreach (var child in RoomObjectsTree.Items)
            {
                var twi = (child as TreeViewItem).ItemContainerGenerator.ContainerFromItem(obj) as TreeViewItem;
                if (twi != null)
                    twi.Focus();
            }
        }

        private void Canvas_DragOver(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[e.Data.GetFormats().Length - 1]) as UndertaleObject; // TODO: make this more reliable

            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null && (sourceItem is UndertaleGameObject || sourceItem is UndertalePath) ? DragDropEffects.Link : DragDropEffects.None;
            e.Handled = true;
        }

        private void Canvas_Drop(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[e.Data.GetFormats().Length - 1]) as UndertaleObject;
            
            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null && (sourceItem is UndertaleGameObject || sourceItem is UndertalePath) ? DragDropEffects.Link : DragDropEffects.None;
            if (e.Effects == DragDropEffects.Link)
            {
                if (sourceItem is UndertaleGameObject)
                {
                    UndertaleGameObject droppedObject = sourceItem as UndertaleGameObject;
                    var mousePos = e.GetPosition(RoomGraphics);

                    UndertaleRoom room = this.DataContext as UndertaleRoom;
                    var obj = new UndertaleRoom.GameObject();
                    obj.X = (int)mousePos.X;
                    obj.Y = (int)mousePos.Y;
                    obj.ObjectDefinition = droppedObject;
                    obj.InstanceID = ++(Application.Current.MainWindow as MainWindow).Data.GeneralInfo.LastObj; // TODO: kinda ugly...
                    obj.CreationCode = null;
                    obj.ScaleX = 1;
                    obj.ScaleY = 1;
                    obj.Color = 0xFFFFFFFF;
                    obj.Rotation = 0;
                    obj.Unknown = -1;
                    room.GameObjects.Add(obj);

                    SelectObject(obj);
                }

                if (sourceItem is UndertalePath)
                {
                    PreviewPath = sourceItem as UndertalePath;
                }
            }
            e.Handled = true;
        }

        private void RoomObjectsTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                UndertaleRoom room = this.DataContext as UndertaleRoom;
                UndertaleObject selectedObj = ObjectEditor.Content as UndertaleObject;

                if (selectedObj is UndertaleRoom.Background)
                {
                    UndertaleRoom.Background bg = selectedObj as UndertaleRoom.Background;
                    bg.Enabled = false;
                    bg.BackgroundDefinition = null;
                    ObjectEditor.Content = null;
                }
                else if (selectedObj is UndertaleRoom.View)
                {
                    UndertaleRoom.View view = selectedObj as UndertaleRoom.View;
                    view.Enabled = false;
                    ObjectEditor.Content = null;
                }
                else if (selectedObj is UndertaleRoom.Tile)
                {
                    UndertaleRoom.Tile tile = selectedObj as UndertaleRoom.Tile;
                    room.Tiles.Remove(tile);
                    ObjectEditor.Content = null;
                }
                else if (selectedObj is UndertaleRoom.GameObject)
                {
                    UndertaleRoom.GameObject gameObj = selectedObj as UndertaleRoom.GameObject;
                    room.GameObjects.Remove(gameObj);
                    ObjectEditor.Content = null;
                }
            }
        }

        private void RoomObjectsTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            object sel = (sender as TreeView).SelectedItem;
            if (sel is UndertaleRoom.GameObject)
                (Application.Current.MainWindow as MainWindow).Selected = (sel as UndertaleRoom.GameObject).ObjectDefinition;
            if (sel is UndertaleRoom.Background)
                (Application.Current.MainWindow as MainWindow).Selected = (sel as UndertaleRoom.Background).BackgroundDefinition;
        }

        private UndertaleObject copied;

        public void Command_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            UndertaleObject selectedObj = ObjectEditor.Content as UndertaleObject;
            if (selectedObj != null)
            {
                Debug.WriteLine("Copy");
                /*Clipboard.Clear();
                Clipboard.SetDataObject(new DataObject(selectedObj));*/
                copied = selectedObj;
            }
        }

        public void Command_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            /*IDataObject data = Clipboard.GetDataObject();
            UndertaleObject obj = data.GetData(data.GetFormats()[0]) as UndertaleObject;
            if (obj != null)
            {
                Debug.WriteLine("Paste");
                Debug.WriteLine(obj);
            }*/
            if (copied != null)
            {
                UndertaleRoom room = this.DataContext as UndertaleRoom;
                if (copied is UndertaleRoom.GameObject)
                {
                    var other = copied as UndertaleRoom.GameObject;
                    var obj = new UndertaleRoom.GameObject();
                    obj.X = other.X;
                    obj.Y = other.Y;
                    obj.ObjectDefinition = other.ObjectDefinition;
                    obj.InstanceID = ++(Application.Current.MainWindow as MainWindow).Data.GeneralInfo.LastObj;
                    obj.CreationCode = other.CreationCode;
                    obj.ScaleX = other.ScaleX;
                    obj.ScaleY = other.ScaleY;
                    obj.Color = other.Color;
                    obj.Rotation = other.Rotation;
                    obj.Unknown = other.Unknown;
                    room.GameObjects.Add(obj);
                    SelectObject(obj);
                }
                if (copied is UndertaleRoom.Tile)
                {
                    var other = copied as UndertaleRoom.Tile;
                    var obj = new UndertaleRoom.Tile();
                    obj.X = other.X;
                    obj.Y = other.Y;
                    obj.BackgroundDefinition = other.BackgroundDefinition;
                    obj.SourceX = other.SourceX;
                    obj.SourceY = other.SourceY;
                    obj.Width = other.Width;
                    obj.Height = other.Height;
                    obj.TileDepth = other.TileDepth;
                    obj.InstanceID = ++(Application.Current.MainWindow as MainWindow).Data.GeneralInfo.LastTile;
                    obj.ScaleX = other.ScaleX;
                    obj.ScaleY = other.ScaleY;
                    obj.Color = other.Color;
                    room.Tiles.Add(obj);
                    SelectObject(obj);
                }
            }
        }
    }
}
