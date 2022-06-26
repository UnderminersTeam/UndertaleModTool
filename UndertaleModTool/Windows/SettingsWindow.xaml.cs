using System;
using System.Collections.Generic;
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
    public partial class SettingsWindow : Window
    {
        public static string GameMakerStudioPath
        {
            get => Settings.Instance.GameMakerStudioPath;
            set
            {
                Settings.Instance.GameMakerStudioPath = value;
                Settings.Save();
            }
        }

        public static string GameMakerStudio2RuntimesPath
        {
            get => Settings.Instance.GameMakerStudio2RuntimesPath;
            set
            {
                Settings.Instance.GameMakerStudio2RuntimesPath = value;
                Settings.Save();
            }
        }

        public static bool AssetOrderSwappingEnabled
        {
            get => Settings.Instance.AssetOrderSwappingEnabled;
            set
            {
                Settings.Instance.AssetOrderSwappingEnabled = value;
                Settings.Save();
            }
        }

        public static bool ProfileModeEnabled
        {
            get => Settings.Instance.ProfileModeEnabled;
            set
            {
                Settings.Instance.ProfileModeEnabled = value;
                Settings.Save();
            }
        }

        public static bool Warn_About_GMS23
        {
            get => Settings.Instance.Warn_About_GMS23;
            set
            {
                Settings.Instance.Warn_About_GMS23 = value;
                Settings.Save();
            }
        }

        public static bool UseGMLCache
        {
            get => Settings.Instance.UseGMLCache;
            set
            {
                Settings.Instance.UseGMLCache = value;
                Settings.Save();
            }
        }

        public static bool ProfileMessageShown
        {
            get => Settings.Instance.ProfileMessageShown;
            set
            {
                Settings.Instance.ProfileMessageShown = value;
                Settings.Save();
            }
        }
        public static bool TempRunMessageShow
        {
            get => Settings.Instance.TempRunMessageShow;
            set
            {
                Settings.Instance.TempRunMessageShow = value;
                Settings.Save();
            }
        }

        public static bool AutomaticFileAssociation
        {
            get => Settings.Instance.AutomaticFileAssociation;
            set
            {
                Settings.Instance.AutomaticFileAssociation = value;
                Settings.Save();
            }
        }

        public static bool DeleteOldProfileOnSave
        {
            get => Settings.Instance.DeleteOldProfileOnSave;
            set
            {
                Settings.Instance.DeleteOldProfileOnSave = value;
                Settings.Save();
            }
        }
        public static bool WarnOnClose
        {
            get => Settings.Instance.WarnOnClose;
            set
            {
                Settings.Instance.WarnOnClose = value;
                Settings.Save();
            }
        }

        public static double GlobalGridWidth
        {
            get => Settings.Instance.GlobalGridWidth;
            set
            {
                Settings.Instance.GlobalGridWidth = value;
                Settings.Save();
            }
        }

        public static bool GridWidthEnabled
        {
            get => Settings.Instance.GridWidthEnabled;
            set
            {
                Settings.Instance.GridWidthEnabled = value;
                Settings.Save();
            }
        }

        public static double GlobalGridHeight
        {
            get => Settings.Instance.GlobalGridHeight;
            set
            {
                Settings.Instance.GlobalGridHeight = value;
                Settings.Save();
            }
        }

        public static bool GridHeightEnabled
        {
            get => Settings.Instance.GridHeightEnabled;
            set
            {
                Settings.Instance.GridHeightEnabled = value;
                Settings.Save();
            }
        }

        public static double GlobalGridThickness
        {
            get => Settings.Instance.GlobalGridThickness;
            set
            {
                Settings.Instance.GlobalGridThickness = value;
                Settings.Save();
            }
        }

        public static bool GridThicknessEnabled
        {
            get => Settings.Instance.GridThicknessEnabled;
            set
            {
                Settings.Instance.GridThicknessEnabled = value;
                Settings.Save();
            }
        }

        public bool UpdateButtonEnabled
        {
            get => UpdateAppButton.IsEnabled;
            set => UpdateAppButton.IsEnabled = value;
        }

        public SettingsWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            Settings.Load();
        }

        private void AppDataButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.OpenFolder(Settings.AppDataFolder);
        }

        private void UpdateAppButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Owner).UpdateApp(this);
        }
    }
}
