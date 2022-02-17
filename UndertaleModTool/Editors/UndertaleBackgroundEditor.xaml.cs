using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleBackgroundEditor.xaml
    /// </summary>
    public partial class UndertaleBackgroundEditor : DataUserControl
    {
        public UndertaleBackgroundEditor()
        {
            InitializeComponent();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as DataGrid).SelectedItem is UndertaleBackground.TileID tileID)
            {
                UndertaleBackground bg = DataContext as UndertaleBackground;
                uint x = tileID.ID % bg.GMS2TileColumns;
                uint y = tileID.ID / bg.GMS2TileColumns;

                Canvas.SetLeft(TileRectangle, ((x + 1) * bg.GMS2OutputBorderX) + (x * (bg.GMS2TileWidth + bg.GMS2OutputBorderX)));
                Canvas.SetTop(TileRectangle, ((y + 1) * bg.GMS2OutputBorderY) + (y * (bg.GMS2TileHeight + bg.GMS2OutputBorderY)));
            }
        }

        private void BGTexture_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!TileRectangle.IsVisible) // MainWindow.IsGMS2
                return;

            Point pos = e.GetPosition((IInputElement)sender);
            UndertaleBackground bg = DataContext as UndertaleBackground;
            int x = (int)((int)pos.X / (bg.GMS2TileWidth + (2 * bg.GMS2OutputBorderX)));
            int y = (int)((int)pos.Y / (bg.GMS2TileHeight + (2 * bg.GMS2OutputBorderY)));
            int tileID = (int)((bg.GMS2TileColumns * y) + x);
            if (tileID > bg.GMS2TileCount - 1)
                return;

            e.Handled = true;

            int tileIndex = bg.GMS2TileIds.FindIndex(x => x.ID == tileID);

            ScrollViewer tileListViewer = MainWindow.FindVisualChild<ScrollViewer>(TileIdList);
            tileListViewer.ScrollToVerticalOffset(tileIndex + 1 - (tileListViewer.ViewportHeight / 2)); // DataGrid offset is logical
            tileListViewer.UpdateLayout();

            ScrollViewer dataEditorViewer = (Application.Current.MainWindow as MainWindow).DataEditor.Parent as ScrollViewer;
            double initOffset = dataEditorViewer.VerticalOffset;

            TileIdList.SelectedIndex = tileIndex;
            (TileIdList.ItemContainerGenerator.ContainerFromIndex(tileIndex) as DataGridRow)?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

            dataEditorViewer.UpdateLayout();
            dataEditorViewer.ScrollToVerticalOffset(initOffset);
        }

        private void DataUserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsLoaded)
                MainWindow.FindVisualChild<ScrollViewer>(TileIdList).ScrollToVerticalOffset(0);
        }
    }
}
