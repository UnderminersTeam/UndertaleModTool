using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace UndertaleModTool
{
    /// <summary>
    /// A standard menu item which compatible with the dark mode.
    /// </summary>
    public partial class MenuItemDark : MenuItem
    {
        /// <summary>Initializes a new instance of the menu item.</summary>
        public MenuItemDark()
        {
            Loaded += MenuItemDark_Loaded;
        }

        private void MenuItemDark_Loaded(object sender, RoutedEventArgs e)
        {
            Popup popup = MainWindow.FindVisualChild<Popup>(this);
            var content = popup?.Child as Border;
            if (content is null)
                return;
            popup.Tag = "PopupDark";
            content.Tag = "Dark";

            // Change text color of dropdown items
            content.SetResourceReference(BackgroundProperty, SystemColors.MenuBrushKey);
        }
    }
}
