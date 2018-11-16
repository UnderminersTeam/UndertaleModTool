using System;
using System.Collections.Generic;
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
    public partial class UndertaleGeneralInfoEditor : UserControl
    {
        public UndertaleGeneralInfoEditor()
        {
            InitializeComponent();
        }

        private void SyncRoomList_Click(object sender, RoutedEventArgs e)
        {
            IList<UndertaleRoom> rooms = (Application.Current.MainWindow as MainWindow).Data.Rooms;
            IList<UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>> roomOrder = (this.DataContext as GeneralInfoEditor).GeneralInfo.RoomOrder;
            roomOrder.Clear();
            foreach(var room in rooms)
                roomOrder.Add(new UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>() { Resource = room });
        }
    }
}
