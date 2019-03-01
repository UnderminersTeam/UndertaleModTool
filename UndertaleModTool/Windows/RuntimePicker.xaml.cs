using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using UndertaleModLib;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy RuntimePicker.xaml
    /// </summary>
    public partial class RuntimePicker : Window
    {
        public class Runtime
        {
            public string Version { get; set; }
            public string Path { get; set; }
            public string DebuggerPath { get; set; }
        }

        public ObservableCollection<Runtime> Runtimes { get; private set; } = new ObservableCollection<Runtime>();
        public Runtime Selected { get; private set; } = null;

        public RuntimePicker()
        {
            this.DataContext = this;
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Selected = Picker.SelectedItem as Runtime;
            Close();
        }

        public void DiscoverRuntimes(string dataFilePath, UndertaleData data)
        {
            Runtimes.Clear();
            DiscoverGameExe(dataFilePath, data);
            DiscoverGMS2();
            DiscoverGMS1();
        }

        private void DiscoverGameExe(string dataFilePath, UndertaleData data)
        {
            string gameExeName = data?.GeneralInfo?.Filename?.Content;
            if (gameExeName == null)
                return;

            string gameExePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(dataFilePath), gameExeName + ".exe");
            if (!File.Exists(gameExePath))
                return;

            Runtimes.Add(new Runtime() { Version = "Game EXE", Path = gameExePath });
        }

        private void DiscoverGMS1()
        {
            string studioRunner = System.IO.Path.Combine(Environment.ExpandEnvironmentVariables(SettingsWindow.GameMakerStudioPath), "Runner.exe");
            if (!File.Exists(studioRunner))
                return;

            string studioDebugger = System.IO.Path.Combine(Environment.ExpandEnvironmentVariables(SettingsWindow.GameMakerStudioPath), @"GMDebug\GMDebug.exe");
            if (!File.Exists(studioDebugger))
                studioDebugger = null;

            Runtimes.Add(new Runtime() { Version = "1.4.xxx", Path = studioRunner, DebuggerPath = studioDebugger });
        }

        private void DiscoverGMS2()
        {
            string runtimesPath = Environment.ExpandEnvironmentVariables(SettingsWindow.GameMakerStudio2RuntimesPath);
            if (!Directory.Exists(runtimesPath))
                return;

            Regex runtimePattern = new Regex(@"^runtime-(.*)$");
            foreach(var runtimePath in Directory.EnumerateDirectories(runtimesPath))
            {
                Match m = runtimePattern.Match(System.IO.Path.GetFileName(runtimePath));
                if (!m.Success)
                    continue;

                string runtimeRunner = System.IO.Path.Combine(runtimePath, @"windows\Runner.exe");
                if (!File.Exists(runtimeRunner))
                    continue;

                Runtimes.Add(new Runtime() { Version = m.Groups[1].Value, Path = runtimeRunner });
            }
        }

        public Runtime Pick(string dataFilePath, UndertaleData data)
        {
            DiscoverRuntimes(dataFilePath, data);
            if (Runtimes.Count == 0)
            {
                MessageBox.Show("Unable to find game EXE or any installed Studio runtime", "Run error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            else if (Runtimes.Count == 1)
            {
                return Runtimes[0];
            }
            else
            {
                ShowDialog();
                return Selected;
            }
        }
    }
}
