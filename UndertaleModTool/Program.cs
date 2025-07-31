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
                if (Application.Current.MainWindow is MainWindow mw) {
                    mw.DoSaveDialog(false, true);
                }
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
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
