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
    /// Interaction logic for UndertaleExtensionEditor.xaml
    /// </summary>
    public partial class UndertaleExtensionEditor : UserControl
    {
        public int MyIndex { get => (Application.Current.MainWindow as MainWindow).Data.Extensions.IndexOf((UndertaleExtension)this.DataContext); }
        public byte[] ProductIdData { get => (((Application.Current.MainWindow as MainWindow).Data.GeneralInfo.Major >= 2) ? (Application.Current.MainWindow as MainWindow).Data.FORM.EXTN.productIdData[MyIndex] : null); set => (Application.Current.MainWindow as MainWindow).Data.FORM.EXTN.productIdData[MyIndex] = value; }

        public UndertaleExtensionEditor()
        {
            InitializeComponent();
        }

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            int lastItem = ((sender as DataGrid).ItemsSource as IList<UndertaleExtensionFile>).Count;

            UndertaleExtensionFile obj = new UndertaleExtensionFile()
            {
                Kind = UndertaleExtensionKind.DLL,
                Filename = (Application.Current.MainWindow as MainWindow).Data.Strings.MakeString($"NewExtensionFile{lastItem}.dll"),
                Functions = new UndertalePointerList<UndertaleExtensionFunction>()
            };

            e.NewItem = obj;
        }
    }
}
