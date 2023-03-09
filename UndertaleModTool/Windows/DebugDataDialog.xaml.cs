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
using System.Windows.Shapes;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy DebugDataDialog.xaml
    /// </summary>
    public partial class DebugDataDialog : Window
    {
        public enum DebugDataMode
        {
            FullAssembler,
            PartialAssembler,
            Decompiled,
            NoDebug
        }

        public DebugDataMode Result { get; private set; } = DebugDataMode.NoDebug;

        public DebugDataDialog()
        {
            InitializeComponent();
        }
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            Result = DebugDataMode.Decompiled;
            Close();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            Result = DebugDataMode.PartialAssembler;
            Close();
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            Result = DebugDataMode.FullAssembler;
            Close();
        }

        private void Button4_Click(object sender, RoutedEventArgs e)
        {
            Result = DebugDataMode.NoDebug;
            Close();
        }
    }
}
