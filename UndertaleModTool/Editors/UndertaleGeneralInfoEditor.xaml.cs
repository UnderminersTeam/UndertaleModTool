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
        /// <param name="room">the room to select.</param>
        private void SelectItem(object room)
        {
            RoomListGrid.ScrollIntoView(room); // This works with a virtualized DataGrid
            RoomListGrid.SelectedItem = room;
            RoomListGrid.CurrentItem= room;
            RoomListGrid.UpdateLayout();

            // Set focus to the selected Element
            DataGridRow row = RoomListGrid.ItemContainerGenerator.ContainerFromItem(room) as DataGridRow;
            DataGridCellsPresenter presenter = MainWindow.FindVisualChild<DataGridCellsPresenter>(row) as DataGridCellsPresenter;
            DataGridCell cell = presenter.ItemContainerGenerator.ContainerFromIndex(0) as DataGridCell;
            cell.Focus();
        }

        /// <summary>
        /// Moves the given room up and down the RoomOrderList./>.
        /// </summary>
        /// <param name="room">The room to move.</param>
        /// <param name="dist">Distance to move it. Positive - down, negative - up.</param>
        private void MoveItem(UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM> room, int dist)
        {
            IList<UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>> roomOrder = (this.DataContext as GeneralInfoEditor).GeneralInfo.RoomOrder;

            int index = roomOrder.IndexOf(room);
            if (index == -1)
            {
                mainWindow.ShowError("Can't change room position - room not found in room order.");
                return;
            }

            int newIndex = Math.Clamp(index + dist, 0 , roomOrder.Count - 1);
            if (newIndex != index)
            {
                // Tuple syntax for swapping values. Looks nicer and isn't any slower.
                // See https://www.reddit.com/r/ProgrammerTIL/comments/8ssiqb/comment/e12301f/
                (roomOrder[newIndex], roomOrder[index]) = (roomOrder[index], roomOrder[newIndex]);
            }

            SelectItem(roomOrder[newIndex]);
        }

        private void RoomListGrid_KeyDown(object sender, KeyEventArgs e)
        {
            IList<UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>> roomOrder = (this.DataContext as GeneralInfoEditor).GeneralInfo.RoomOrder;

            object selected = RoomListGrid.SelectedItem;
            if (selected == null)
                return;
            UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM> room = selected as UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>;
            
            switch(e.Key)
            {
                case Key.OemMinus:
                    MoveItem(room, -1);
                    break;
                case Key.OemPlus:
                    MoveItem(room, 1);
                    break;
                 case Key.N: // Insert new blank room
                    int index = roomOrder.IndexOf(room);
                    UndertaleResourceById < UndertaleRoom, UndertaleChunkROOM > newRoom = new();
                    roomOrder.Insert(index + 1, newRoom);
                    SelectItem(newRoom);
                    break;
            }
        }

        private void SyncRoomList_Click(object sender, RoutedEventArgs e)
        {
            IList<UndertaleRoom> rooms = mainWindow.Data.Rooms;
            IList<UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>> roomOrder = (this.DataContext as GeneralInfoEditor).GeneralInfo.RoomOrder;
            roomOrder.Clear();
            foreach(var room in rooms)
                roomOrder.Add(new UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>() { Resource = room });
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
