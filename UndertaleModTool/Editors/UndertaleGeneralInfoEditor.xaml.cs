using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Logika interakcji dla klasy UndertaleGeneralInfoEditor.xaml
    /// </summary>
    public partial class UndertaleGeneralInfoEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public UndertaleGeneralInfoEditor()
        {
            InitializeComponent();
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
