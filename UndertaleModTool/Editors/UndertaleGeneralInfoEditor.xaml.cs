using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// Logika interakcji dla klasy UndertaleGeneralInfoEditor.xaml
    /// </summary>
    public partial class UndertaleGeneralInfoEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public UndertaleGeneralInfoEditor()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Selects the given room inside the RoomOrderList.
        /// </summary>
        /// <param name="rooms">The list of rooms to select.</param>
        /// <param name="current">The room to make currenly selected and focused</param>
        private void SelectItems(IList<UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>> rooms, object current)
        {
            RoomListGrid.ScrollIntoView(current); // This works with a virtualized DataGrid
            RoomListGrid.SelectedItems.Clear();
            foreach (object room in rooms)
            {
                RoomListGrid.SelectedItems.Add(room);
            }
            RoomListGrid.CurrentItem= current;
            RoomListGrid.UpdateLayout();

            // Set focus to the selected Element
            DataGridRow row = RoomListGrid.ItemContainerGenerator.ContainerFromItem(current) as DataGridRow;
            DataGridCellsPresenter presenter = MainWindow.FindVisualChild<DataGridCellsPresenter>(row) as DataGridCellsPresenter;
            DataGridCell cell = presenter.ItemContainerGenerator.ContainerFromIndex(0) as DataGridCell;
            cell.Focus();
        }

        /// <summary>
        /// Moves the given room up and down the RoomOrderList./>.
        /// </summary>
        /// <param name="rooms">The list of rooms to move.</param>
        /// <param name="dist">Distance to move them. Positive - down, negative - up.</param>
        private void MoveItem(IList<UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>> rooms, int dist)
        {
            if (rooms.Count == 0)
                return;

            IList<UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>> roomOrder = (this.DataContext as GeneralInfoEditor).GeneralInfo.RoomOrder;
            
            List<int> indexes = rooms.Select(room => roomOrder.IndexOf(room)).ToList();
            if (indexes.Contains(-1))
            {
                mainWindow.ShowError("Can't change room position - room not found in room order.");
                return;
            }

            dist = Math.Clamp(dist, -indexes.Min(), roomOrder.Count - 1 - indexes.Max());
            if (dist == 0)
                return;
            
            // If we move the rooms in the wrong order we could move a room twice
            indexes.Sort();
            if (dist > 0)
                indexes.Reverse();
            object current = RoomListGrid.CurrentItem; // In case changing the order changes CurrentItem
            foreach(int index in indexes)
                (roomOrder[index + dist], roomOrder[index]) = (roomOrder[index], roomOrder[index + dist]);

            SelectItems(rooms, current);
        }

        private void RoomListGrid_KeyDown(object sender, KeyEventArgs e)
        {
            IList<UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>> roomOrder = (this.DataContext as GeneralInfoEditor).GeneralInfo.RoomOrder;

            List<UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>> selected = RoomListGrid.SelectedItems.Cast<UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>>().ToList();
            if (selected.Count == 0)
                return;

            switch (e.Key)
            {
                case Key.OemMinus:
                    MoveItem(selected, -1);
                    break;
                case Key.OemPlus:
                    MoveItem(selected, 1);
                    break;
                 case Key.N: // Insert new blank room after selected rooms
                    int index = selected.Select(room => roomOrder.IndexOf(room)).Max();
                    UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM> newRoom = new();
                    roomOrder.Insert(index + 1, newRoom);
                    SelectItems(new List<UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>> { newRoom }, newRoom);
                    break;
            }
        }

        private void SyncRoomList_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow.ShowQuestion("Sync room order with room list?\n\nThis will undo all changes made to the room order.", MessageBoxImage.Warning, "Confirmation") == MessageBoxResult.Yes)
            {
                IList<UndertaleRoom> rooms = mainWindow.Data.Rooms;
                IList<UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>> roomOrder = (this.DataContext as GeneralInfoEditor).GeneralInfo.RoomOrder;
                roomOrder.Clear();
                foreach (var room in rooms)
                    roomOrder.Add(new UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>() { Resource = room });
            }
        }

        private void DebuggerCheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not CheckBox checkBox)
                return;

            if (checkBox.IsChecked != true)
                return;

            e.Handled = true;
            var result = mainWindow.ShowQuestion("Are you sure that you want to enable GMS debugger?\n" +
                                                 "If you want to enable a debug mode in some game, then you need to use one of the scripts.");
            if (result == MessageBoxResult.Yes)
                checkBox.IsChecked = false;
        }

    }

    public class TimestampDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ulong timestamp)
                return "(error)";
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds((long)timestamp);
            if (parameter is string par && par == "GMT")
                return "GMT+0: " + dateTimeOffset.UtcDateTime.ToString();
            else
                return dateTimeOffset.LocalDateTime.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string dateTimeStr)
                return new ValidationResult(false, "The value is not a string.");
            if (!DateTime.TryParse(dateTimeStr, out DateTime dateTime))
                return new ValidationResult(false, "Invalid date time format.");

            return (ulong)(new DateTimeOffset(dateTime).ToUnixTimeSeconds());
        }
    }
}
