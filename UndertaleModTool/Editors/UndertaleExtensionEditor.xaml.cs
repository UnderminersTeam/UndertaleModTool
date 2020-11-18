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
    /// Interaction logic for UndertaleExtensionEditor.xaml
    /// </summary>
    public partial class UndertaleExtensionEditor : UserControl
    {
        public UndertaleExtensionEditor()
        {
            InitializeComponent();
        }

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            int lastItem = ((sender as DataGrid).ItemsSource as IList<UndertaleExtension.ExtensionFile>).Count;

            UndertaleExtension.ExtensionFile obj = new UndertaleExtension.ExtensionFile()
            {
                Kind = UndertaleExtension.ExtensionKind.DLL,
                Filename = new UndertaleString($"NewExtensionFile{lastItem}.dll")
                // TODO: no way to call Data.Strings.MakeString?
            };

            e.NewItem = obj;
        }
    }
}
