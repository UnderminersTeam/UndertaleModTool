#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.Collections.Generic;
using log4net;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.Win32;
using UndertaleModLib;

namespace UndertaleModTool
{
    public static class Program
    {
        public static string GetExecutableDirectory()
        {
            return Path.GetDirectoryName(Environment.ProcessPath);
        }

        // https://stackoverflow.com/questions/1025843/merging-dlls-into-a-single-exe-with-wpf
        [STAThread]
        public static void Main()
        {
            try
            {
                AppDomain currentDomain = default(AppDomain);
                currentDomain = AppDomain.CurrentDomain;
                // Handler for unhandled exceptions.
                currentDomain.UnhandledException += GlobalUnhandledExceptionHandler;
                // Handler for exceptions in threads behind forms.
                System.Windows.Forms.Application.ThreadException += GlobalThreadExceptionHandler;
                App.Main();
            }
            catch (Exception e)
            {
                File.WriteAllText(Path.Combine(GetExecutableDirectory(), "crash.txt"), e.ToString());
                MessageBox.Show(e.ToString());
                EmergencySaveFile();
            }
        }
        private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = default(Exception);
            ex = (Exception)e.ExceptionObject;
            ILog log = LogManager.GetLogger(typeof(Program));
            log.Error(ex.Message + "\n" + ex.StackTrace);
            File.WriteAllText(Path.Combine(GetExecutableDirectory(), "crash2.txt"), (ex.ToString() + "\n" + ex.Message + "\n" + ex.StackTrace));
        }

        private static void GlobalThreadExceptionHandler(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Exception ex = default(Exception);
            ex = e.Exception;
            ILog log = LogManager.GetLogger(typeof(Program)); //Log4NET
            log.Error(ex.Message + "\n" + ex.StackTrace);
            File.WriteAllText(Path.Combine(GetExecutableDirectory(), "crash3.txt"), (ex.Message + "\n" + ex.StackTrace));
        }

        private static async Task EmergencySaveFile()
        {
            var data = MainWindow.LastData;
            if (data is null || data.UnsupportedBytecodeVersion) return;

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = "win";
            dlg.Filter = "GameMaker data files (.win, .unx, .ios, .droid, audiogroup*.dat)|*.win;*.unx;*.ios;*.droid;audiogroup*.dat|All files|*";
            dlg.FileName = MainWindow.LastFilePath;
            dlg.Title = "Emergency-save the current file (separate file recommended):";

            if (dlg.ShowDialog() != true) return;

            var filename = dlg.FileName;

            bool SaveSucceeded = false;
            try {
                using (var stream = new FileStream(filename + "temp", FileMode.Create, FileAccess.Write))
                {
                    UndertaleIO.Write(stream, data, message => {});
                    SaveSucceeded = true;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("An error occurred while trying to save:\n" + e.Message, "Save error");
            }
            // Don't make any changes unless the save succeeds.
            try
            {
                if (SaveSucceeded)
                {
                    // It saved successfully!
                    // If we're overwriting a previously existing data file, we're going to overwrite it now.
                    // Then, we're renaming it back to the proper (non-temp) file name.
                    File.Move(filename + "temp", filename, true);
                    MessageBox.Show("Emergency save successful.");
                }
                else
                {
                    // Leave the temporary file for possible recovery; so do nothing
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("An error occurred while trying to save:\n" + exc.Message, "Save error");
            }
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
