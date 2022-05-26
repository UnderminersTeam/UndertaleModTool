using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace UndertaleModTool
{
    // Wraps OwnedMessageBox.Show, automatically gives it a window owner (the main window)
    // This is my p100 fix for not having to do Find & Replace on all the calls
    public class OwnedMessageBox
    {
        public static MessageBoxResult Show(string sText)
        {
            return MessageBox.Show(Application.Current?.MainWindow, sText);
        }

        // This one is unused in all of UMT, but I'll leave it here so it's easy to re-enable
        /*
        public static MessageBoxResult Show(string sText, string sCaption)
        {
            return MessageBox.Show(Application.Current?.MainWindow, sText, sCaption);
        }
        */

        public static MessageBoxResult Show(string sText, string sCaption, MessageBoxButton mbbButtons)
        {
            return MessageBox.Show(Application.Current?.MainWindow, sText, sCaption, mbbButtons);
        }

        public static MessageBoxResult Show(string sText, string sCaption, MessageBoxButton mbbButtons, MessageBoxImage mbiIcon)
        {
            return MessageBox.Show(Application.Current?.MainWindow, sText, sCaption, mbbButtons, mbiIcon);
        }

        public static MessageBoxResult Show(string sText, string sCaption, MessageBoxButton mbbButtons, MessageBoxImage mbiIcon, MessageBoxResult mbrDefaultResult)
        {
            return MessageBox.Show(Application.Current?.MainWindow, sText, sCaption, mbbButtons, mbiIcon, mbrDefaultResult);
        }

        // This one is unused in all of UMT, but I'll leave it here so it's easy to re-enable
        /*
        public static MessageBoxResult Show(string sText, string sCaption, MessageBoxButton mbbButtons, MessageBoxImage mbiIcon, MessageBoxResult mbrDefaultResult, MessageBoxOptions mboOptions)
        {
            return MessageBox.Show(Application.Current?.MainWindow, sText, sCaption, mbbButtons, mbiIcon, mbrDefaultResult, mboOptions);
        }
        */
    }
}
