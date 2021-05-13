using System;
using System.Collections.Generic;
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
            return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        }

        // https://stackoverflow.com/questions/1025843/merging-dlls-into-a-single-exe-with-wpf
        [STAThreadAttribute]
        public static void Main()
        {
            try
            {
                App.Main();
            }
            catch (Exception e)
            {
                File.WriteAllText("crash.txt", e.ToString());
                MessageBox.Show(e.ToString());
            }
        }
    }
}
