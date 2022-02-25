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
    /// Logika interakcji dla klasy UndertaleCodeLocalsEditor.xaml
    /// </summary>
    public partial class UndertaleCodeLocalsEditor : DataUserControl
    {
        public UndertaleCodeLocalsEditor()
        {
            InitializeComponent();
        }

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            UndertaleCodeLocals.LocalVar obj = new UndertaleCodeLocals.LocalVar();
            obj.Index = (uint)((sender as DataGrid).ItemsSource as IList<UndertaleCodeLocals.LocalVar>).Count;
            e.NewItem = obj;
        }
    }
}
