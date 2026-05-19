using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
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
using UndertaleModTool.Localization;

namespace UndertaleModTool
{
    public partial class SettingsWindow : Window
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

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

        public static bool ShowNullEntriesInResourceTree
        {
            get => Settings.Instance.ShowNullEntriesInResourceTree;
            set
            {
                Settings.Instance.ShowNullEntriesInResourceTree = value;
                Settings.Save();

                mainWindow.UpdateTree();
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

                if (!value && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (mainWindow.ShowQuestion(LocalizationSource.GetString("Settings_RemoveFileAssociations"), MessageBoxImage.Question, LocalizationSource.GetString("Settings_FileAssociations")) == MessageBoxResult.Yes)
                    {
                        try
                        {
                            FileAssociations.RemoveAssociations();
                        }
                        catch (Exception ex)
                        {
                            mainWindow.ScriptError(ex.ToString(), LocalizationSource.GetString("Settings_UnassociationFailed"), false);
                        }
                    }
                }
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

        public static string TransparencyGridColor1
        {
            get => Settings.Instance.TransparencyGridColor1;
            set
            {
                try
                {
                    MainWindow.SetTransparencyGridColors(value, TransparencyGridColor2);

                    Settings.Instance.TransparencyGridColor1 = value;
                    Settings.Save();
                }
                catch (FormatException) { }
            }
        }

        public static string TransparencyGridColor2
        {
            get => Settings.Instance.TransparencyGridColor2;
            set
            {
                try
                {
                    MainWindow.SetTransparencyGridColors(TransparencyGridColor1, value);

                    Settings.Instance.TransparencyGridColor2 = value;
                    Settings.Save();
                }
                catch (FormatException) { }
            }
        }

        public static bool EnableDarkMode
        {
            get => Settings.Instance.EnableDarkMode;
            set
            {
                Settings.Instance.EnableDarkMode = value;
                Settings.Save();

                MainWindow.SetDarkMode(value);

                if (value)
                    mainWindow.ShowWarning(LocalizationSource.GetString("Settings_DarkModeMsgBoxWarning"));
            }
        }

        public static bool ShowDebuggerOption
        {
            get => Settings.Instance.ShowDebuggerOption;
            set
            {
                Settings.Instance.ShowDebuggerOption = value;
                Settings.Save();

                mainWindow.RunGMSDebuggerItem.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public static bool RememberWindowPlacements
        {
            get => Settings.Instance.RememberWindowPlacements;
            set
            {
                Settings.Instance.RememberWindowPlacements = value;
                Settings.Save();
            }
        }

        public static bool RecompileAllCodeSourcesOnProjectSave
        {
            get => Settings.Instance.RecompileAllCodeSourcesOnProjectSave;
            set
            {
                Settings.Instance.RecompileAllCodeSourcesOnProjectSave = value;
                Settings.Save();
            }
        }

        public static DecompilerSettings DecompilerSettings => Settings.Instance.DecompilerSettings;

        public static string InstanceIdPrefix
        {
            get => Settings.Instance.InstanceIdPrefix;
            set
            {
                Settings.Instance.InstanceIdPrefix = value;
                Settings.Save();
            }
        }

        public bool UpdateButtonEnabled
        {
            get => UpdateAppButton.IsEnabled;
            set => UpdateAppButton.IsEnabled = value;
        }

#if DEBUG
        public static Visibility UpdaterButtonVisibility => Visibility.Visible;
#else
        public static Visibility UpdaterButtonVisibility => Visibility.Hidden;
#endif

        public SettingsWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            Settings.Load();

            string lang = Settings.Instance.Language ?? "en";
            for (int i = 0; i < LanguageComboBox.Items.Count; i++)
            {
                if ((LanguageComboBox.Items[i] as ComboBoxItem)?.Tag as string == lang)
                {
                    LanguageComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem item)
            {
                string lang = item.Tag as string;
                if (lang != null && lang != Settings.Instance.Language)
                {
                    Settings.Instance.Language = lang;
                    Settings.Save();

                    var culture = new CultureInfo(lang);
                    LocalizationSource.Instance.CurrentCulture = culture;
                }
            }
        }

        private void AppDataButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.OpenFolder(Settings.AppDataFolder);
        }

        private void UpdateAppButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Owner).UpdateApp(this);
        }

        private void GMLSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            GMLSettingsWindow settings = new(Settings.Instance);
            settings.Owner = this;
            settings.ShowDialog();
            Settings.Save();
        }
    }
}
