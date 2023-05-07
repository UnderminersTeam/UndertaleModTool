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
    public partial class UndertaleExtensionEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public int MyIndex { get => mainWindow.Data.Extensions.IndexOf((UndertaleExtension)this.DataContext); }
        // God this is so ugly, if there's a better way, please, put in a pull request
        public byte[] ProductIdData { get => ((((mainWindow.Data?.GeneralInfo?.Major ?? 0) >= 2) || (((mainWindow.Data?.GeneralInfo?.Major ?? 0) == 1) && (((mainWindow.Data?.GeneralInfo?.Build ?? 0) >= 1773) || ((mainWindow.Data?.GeneralInfo?.Build ?? 0) == 1539)))) ? mainWindow.Data.FORM.EXTN.productIdData[MyIndex] : null); set => mainWindow.Data.FORM.EXTN.productIdData[MyIndex] = value; }

        public UndertaleExtensionEditor()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int lastItem = (mainWindow.Data.Extensions[MyIndex]).Files.Count;

            UndertaleExtensionFile obj = new UndertaleExtensionFile()
            {
                Kind = UndertaleExtensionKind.Dll,
                Filename = mainWindow.Data.Strings.MakeString($"NewExtensionFile{lastItem}.dll"),
                Functions = new UndertalePointerList<UndertaleExtensionFunction>()
            };

            (mainWindow.Data.Extensions[MyIndex]).Files.Add(obj);
        }
    }
}
