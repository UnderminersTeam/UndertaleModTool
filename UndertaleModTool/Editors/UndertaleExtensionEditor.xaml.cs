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
        // God this is so ugly, if there's a better way, please, put in a pull request
        public byte[] ProductIdData { get => (((((Application.Current.MainWindow as MainWindow).Data?.GeneralInfo?.Major ?? 0) >= 2) || ((((Application.Current.MainWindow as MainWindow).Data?.GeneralInfo?.Major ?? 0) == 1) && ((((Application.Current.MainWindow as MainWindow).Data?.GeneralInfo?.Build ?? 0) >= 1773) || (((Application.Current.MainWindow as MainWindow).Data?.GeneralInfo?.Build ?? 0) == 1539)))) ? (Application.Current.MainWindow as MainWindow).Data.FORM.EXTN.productIdData[MyIndex] : null); set => (Application.Current.MainWindow as MainWindow).Data.FORM.EXTN.productIdData[MyIndex] = value; }

        public UndertaleExtensionEditor()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int lastItem = ((Application.Current.MainWindow as MainWindow).Data.Extensions[MyIndex]).Files.Count;

            UndertaleExtensionFile obj = new UndertaleExtensionFile()
            {
                Kind = UndertaleExtensionKind.DLL,
                Filename = (Application.Current.MainWindow as MainWindow).Data.Strings.MakeString($"NewExtensionFile{lastItem}.dll"),
                Functions = new UndertalePointerList<UndertaleExtensionFunction>()
            };

            ((Application.Current.MainWindow as MainWindow).Data.Extensions[MyIndex]).Files.Add(obj);
        }
    }
}
