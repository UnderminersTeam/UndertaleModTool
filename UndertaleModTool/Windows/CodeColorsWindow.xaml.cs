using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
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
using System.Windows.Shapes;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy SettingsWindow.xaml
    /// </summary>
    public partial class CodeColorsWindow : Window
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        private async void ResetFunctionColor_Click(object sender, RoutedEventArgs e)
        {
            Settings.Instance.FunctionColor_0 = 255;
            Settings.Instance.FunctionColor_1 = 184;
            Settings.Instance.FunctionColor_2 = 113;
            Settings.Save();
        }
        public static byte FunctionColor_0
        {
            get => Settings.Instance.FunctionColor_0;
            set 
            {
                Settings.Instance.FunctionColor_0 = value;
                Settings.Save();
            }
        }
        public static byte FunctionColor_1
        {
            get => Settings.Instance.FunctionColor_1;
            set
            {
                Settings.Instance.FunctionColor_1 = value;
                Settings.Save();
            }
        }
        public static byte FunctionColor_2
        {
            get => Settings.Instance.FunctionColor_2;
            set
            {
                Settings.Instance.FunctionColor_2 = value;
                Settings.Save();
            }
        }
        public static SolidColorBrush FColor1 = new(Color.FromArgb(255, FunctionColor_0, FunctionColor_1, FunctionColor_2));
        private async void ResetGlobalColor_Click(object sender, RoutedEventArgs e)
        {
            Settings.Instance.GlobalColor_0 = 249;
            Settings.Instance.GlobalColor_1 = 123;
            Settings.Instance.GlobalColor_2 = 249;
            Settings.Save();
        }
        public static byte GlobalColor_0
        {
            get => Settings.Instance.GlobalColor_0;
            set
            {
                Settings.Instance.GlobalColor_0 = value;
                Settings.Save();
            }
        }
        public static byte GlobalColor_1
        {
            get => Settings.Instance.GlobalColor_1;
            set
            {
                Settings.Instance.GlobalColor_1 = value;
                Settings.Save();
            }
        }
        public static byte GlobalColor_2
        {
            get => Settings.Instance.GlobalColor_2;
            set
            {
                Settings.Instance.GlobalColor_2 = value;
                Settings.Save();
            }
        }
        private async void ResetConstantColor_Click(object sender, RoutedEventArgs e)
        {
            Settings.Instance.ConstantColor_0 = 255;
            Settings.Instance.ConstantColor_1 = 128;
            Settings.Instance.ConstantColor_2 = 128;
            Settings.Save();
        }
        public static byte ConstantColor_0
        {
            get => Settings.Instance.ConstantColor_0;
            set
            {
                Settings.Instance.ConstantColor_0 = value;
                Settings.Save();
            }
        }
        public static byte ConstantColor_1
        {
            get => Settings.Instance.ConstantColor_1;
            set
            {
                Settings.Instance.ConstantColor_1 = value;
                Settings.Save();
            }
        }
        public static byte ConstantColor_2
        {
            get => Settings.Instance.ConstantColor_2;
            set
            {
                Settings.Instance.ConstantColor_2 = value;
                Settings.Save();
            }
        }
        private async void ResetInstanceColor_Click(object sender, RoutedEventArgs e)
        {
            Settings.Instance.InstanceColor_0 = 88;
            Settings.Instance.InstanceColor_1 = 227;
            Settings.Instance.InstanceColor_2 = 90;
            Settings.Save();
        }
        public static byte InstanceColor_0
        {
            get => Settings.Instance.InstanceColor_0;
            set
            {
                Settings.Instance.InstanceColor_0 = value;
                Settings.Save();
            }
        }
        public static byte InstanceColor_1
        {
            get => Settings.Instance.InstanceColor_1;
            set
            {
                Settings.Instance.InstanceColor_1 = value;
                Settings.Save();
            }
        }
        public static byte InstanceColor_2
        {
            get => Settings.Instance.InstanceColor_2;
            set
            {
                Settings.Instance.InstanceColor_2 = value;
                Settings.Save();
            }
        }
        private async void ResetLocalColor_Click(object sender, RoutedEventArgs e)
        {
            Settings.Instance.LocalColor_0 = 255;
            Settings.Instance.LocalColor_1 = 248;
            Settings.Instance.LocalColor_2 = 153;
            Settings.Save();
        }
        public static byte LocalColor_0
        {
            get => Settings.Instance.LocalColor_0;
            set
            {
                Settings.Instance.LocalColor_0 = value;
                Settings.Save();
            }
        }
        public static byte LocalColor_1
        {
            get => Settings.Instance.LocalColor_1;
            set
            {
                Settings.Instance.LocalColor_1 = value;
                Settings.Save();
            }
        }
        public static byte LocalColor_2
        {
            get => Settings.Instance.LocalColor_2;
            set
            {
                Settings.Instance.LocalColor_2 = value;
                Settings.Save();
            }
        }

        public CodeColorsWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            Settings.Load();
        }
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }
    }
}
