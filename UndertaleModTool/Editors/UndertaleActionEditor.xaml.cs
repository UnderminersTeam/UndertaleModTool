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
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleActionEditor.xaml
    /// </summary>
    public partial class UndertaleActionEditor : UserControl
    {
        public UndertaleActionEditor()
        {
            InitializeComponent();
        }

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            UndertaleAction.Argument obj = new UndertaleAction.Argument();
            obj.Index = (uint)((sender as DataGrid).ItemsSource as IList<UndertaleAction.Argument>).Count;
            e.NewItem = obj;
        }
    }
}
