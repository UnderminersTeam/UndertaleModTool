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
    /// Interaction logic for UndertaleExtensionFileEditor.xaml
    /// </summary>
    public partial class UndertaleExtensionFileEditor : DataUserControl
    {
        public UndertaleExtensionFileEditor()
        {
            InitializeComponent();
        }

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            var itemList = (sender as DataGrid).ItemsSource as IList<UndertaleExtensionFunction>;
            int lastItem = itemList.Count;

            UndertaleExtensionFunction obj = new UndertaleExtensionFunction()
            {
                Name = (Application.Current.MainWindow as MainWindow).Data.Strings.MakeString($"new_extension_function_{lastItem}"),
                ExtName = (Application.Current.MainWindow as MainWindow).Data.Strings.MakeString($"new_extension_function_{lastItem}_ext"),
                RetType = UndertaleExtensionVarType.Double,
                Arguments = new UndertaleSimpleList<UndertaleExtensionFunctionArg>(),
                Kind = 11, // ???
                ID = (Application.Current.MainWindow as MainWindow).Data.ExtensionFindLastId()
            };

            e.NewItem = obj;
        }
    }
}
