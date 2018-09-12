using System;
using System.Collections.Generic;
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
    /// Logika interakcji dla klasy UndertaleGameObjectEditor.xaml
    /// </summary>
    public partial class UndertaleGameObjectEditor : UserControl
    {
        public UndertaleGameObjectEditor()
        {
            InitializeComponent();
        }

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            UndertaleGameObject.Event obj = new UndertaleGameObject.Event();
            obj.Actions.Add(new UndertaleGameObject.EventAction());
            e.NewItem = obj;
        }
    }
}
