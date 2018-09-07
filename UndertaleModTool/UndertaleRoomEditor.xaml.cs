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
        
        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UndertaleObject clickedObj = (sender as Rectangle).DataContext as UndertaleObject;
            UndertaleObject selectedObj = ObjectEditor.Content as UndertaleObject;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (selectedObj is UndertaleRoom.GameObject || selectedObj is UndertaleRoom.Tile)
                {
                    var mousePos = e.GetPosition(RoomGraphics);

                    if (selectedObj is UndertaleRoom.GameObject)
                    {
                        (selectedObj as UndertaleRoom.GameObject).X = (int)mousePos.X;
                        (selectedObj as UndertaleRoom.GameObject).Y = (int)mousePos.Y;
                    }
                    else if (selectedObj is UndertaleRoom.Tile)
                    {
                        (selectedObj as UndertaleRoom.Tile).X = (int)mousePos.X;
                        (selectedObj as UndertaleRoom.Tile).Y = (int)mousePos.Y;
                    }
                }
            }
            else
            {
                SelectObject(clickedObj);
            }
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

            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null && sourceItem is UndertaleGameObject ? DragDropEffects.Link : DragDropEffects.None;
            e.Handled = true;
        }

        private void Canvas_Drop(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[e.Data.GetFormats().Length - 1]) as UndertaleObject;
            
            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null && sourceItem is UndertaleGameObject ? DragDropEffects.Link : DragDropEffects.None;
            if (e.Effects == DragDropEffects.Link)
            {
                UndertaleGameObject droppedObject = sourceItem as UndertaleGameObject;
                var mousePos = e.GetPosition(RoomGraphics);

                UndertaleRoom room = this.DataContext as UndertaleRoom;
                var obj = new UndertaleRoom.GameObject();
                obj.X = (int)mousePos.X;
                obj.Y = (int)mousePos.Y;
                obj.ObjDefIndex = droppedObject;
                obj.InstanceID = ++(Application.Current.MainWindow as MainWindow).Data.GeneralInfo.LastObj; // TODO: kinda ugly...
                obj.CreationCodeID = -1;
                obj.ScaleX = 1;
                obj.ScaleY = 1;
                obj.ArgbTint = 0xFFFFFFFF;
                obj.Rotation = 0;
                obj.Unknown = -1;
                room.GameObjects.Add(obj);

                SelectObject(obj);
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
                    bg.BgDefIndex = null;
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
    }
}
