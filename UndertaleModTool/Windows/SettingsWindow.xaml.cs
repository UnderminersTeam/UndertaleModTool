﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
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

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        private static float darkCount = 0;

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

                // Refresh the tree for the change to take effect
                mainWindow.UpdateTree();
            }
        }

        public static bool PlaySaveSound
        {
            get => Settings.Instance.PlaySaveSound;
            set
            {
                Settings.Instance.PlaySaveSound = value;
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

                if (!value && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Prompt user if they want to unassociate
                    if (mainWindow.ShowQuestion("Remove current file associations, if they exist?", MessageBoxImage.Question, "File associations") == MessageBoxResult.Yes)
                    {
                        try
                        {
                            FileAssociations.RemoveAssociations();
                        }
                        catch (Exception ex)
                        {
                            mainWindow.ScriptError(ex.ToString(), "Unassociation failed", false);
                        }
                    }
                }
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
        public static bool CustomProfileName
        {
            get => Settings.Instance.CustomProfileName;
            set
            {
                Settings.Instance.CustomProfileName = value;
                Settings.Save();
            }
        }
        public static bool RememberProfileName
        {
            get => Settings.Instance.RememberProfileName;
            set
            {
                Settings.Instance.RememberProfileName = value;
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
                if (value == true)
                {
                    darkCount += 1;

                    if (darkCount == 2)
                    {
                        Stream str = Properties.Resource1.snd_wngdng2;
                        SoundPlayer player = new SoundPlayer(str);
                        player.Play();
                    }
                    if (darkCount == 3)
                    {
                        Stream str = Properties.Resource1.snd_wngdng3;
                        SoundPlayer player = new SoundPlayer(str);
                        player.Play();
                    }
                    if (darkCount == 4)
                    {
                        Stream str = Properties.Resource1.snd_wngdng4;
                        SoundPlayer player = new SoundPlayer(str);
                        player.Play();
                    }
                    if (darkCount == 5)
                    {
                        Stream str = Properties.Resource1.snd_wngdng5;
                        SoundPlayer player = new SoundPlayer(str);
                        player.Play();
                    }
                    if (darkCount == 6)
                    {
                        mainWindow.ShowMessage("As you click on the box one last time... you feel\nthe tool turn darker yet darker...", "???");
                        Settings.Instance.EnableDarkerMode = true;
                        Stream str = Properties.Resource1.snd_mysterygo;
                        SoundPlayer player = new SoundPlayer(str);
                        player.Play();
                    }

                    if (darkCount > 6)
                        darkCount = 0;
                }
                else if (Settings.Instance.EnableDarkerMode)
                {
                    Settings.Instance.EnableDarkerMode = true;
                    Stream str = Properties.Resource1.snd_him_quick;
                    SoundPlayer player = new SoundPlayer(str);
                    player.Play();
                    Settings.Instance.EnableDarkerMode = false;
                }

                Settings.Instance.EnableDarkMode = value;
                Settings.Save();

                MainWindow.SetDarkMode(value);

                if (value && darkCount < 3)
                    mainWindow.ShowWarning("The message boxes (like this one) aren't compatible with the dark mode.\n" +
                                           "This will be fixed in future versions.");
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
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }

        private void AppDataButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.OpenFolder(Settings.AppDataFolder);
        }

        private void UpdateAppButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Owner).UpdateApp(this);
        }
        private void ProfileButtonExport_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Owner).ExportProfileFolder();
        }
        private void ProfileButtonImport_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Owner).ImportProfileFolder();
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
