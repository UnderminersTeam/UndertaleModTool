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
        public static string GraphVizPath
        {
            get => ConfigurationManager.AppSettings["graphVizLocation"];
            set
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["graphVizLocation"].Value = value;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
        }

        public static string GameMakerStudioPath
        {
            get => ConfigurationManager.AppSettings["GameMakerStudioPath"];
            set
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["GameMakerStudioPath"].Value = value;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
        }

        public static string GameMakerStudio2RuntimesPath
        {
            get => ConfigurationManager.AppSettings["GameMakerStudio2RuntimesPath"];
            set
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["GameMakerStudio2RuntimesPath"].Value = value;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
        }

        public static string AssetOrderSwappingEnabled
        {
            get => ConfigurationManager.AppSettings["AssetOrderSwappingEnabled"];
            set
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["AssetOrderSwappingEnabled"].Value = value;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
        }

        public static string ProfileModeEnabled
        {
            get => ConfigurationManager.AppSettings["ProfileModeEnabled"];
            set
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["ProfileModeEnabled"].Value = value;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                if ((Application.Current.MainWindow as MainWindow).Data != null)
                {
                    (Application.Current.MainWindow as MainWindow).Data.ToolInfo.ProfileMode = (ProfileModeEnabled == "True");
                }
                else if ((Application.Current.MainWindow as MainWindow).Data.GMS2_3)
                {
	                config.AppSettings.Settings["ProfileModeEnabled"].Value = "False";
                    (Application.Current.MainWindow as MainWindow).Data.ToolInfo.ProfileMode = false;
                }
                else
                {
	                config.AppSettings.Settings["ProfileModeEnabled"].Value = "False";
                }
            }
        }

        public static string ProfileMessageShown
        {
            get => ConfigurationManager.AppSettings["ProfileMessageShown"];
            set
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["ProfileMessageShown"].Value = value;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
        }

        public static string DeleteOldProfileOnSave
        {
            get => ConfigurationManager.AppSettings["DeleteOldProfileOnSave"];
            set
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["DeleteOldProfileOnSave"].Value = value;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
        }

        public SettingsWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }
    }
}
