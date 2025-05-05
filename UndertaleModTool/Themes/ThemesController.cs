using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace Theme.WPF.Themes
{
    public static class ThemesController
    {
        public static ThemeType CurrentTheme { get; set; } = ThemeType.LightTheme;

        static ResourceDictionary ColourDictionary = null;
        static ResourceDictionary ControlColours = null;
        static ResourceDictionary Controls = null;

        public static void SetTheme(ThemeType theme)
        {
            //Theme.WPF.Themes.ThemesController.SetTheme(Theme.WPF.Themes.ThemeType.None)
            string themeName = theme.GetName();
            //if (string.IsNullOrEmpty(themeName))
            //{
            //    return;
            //}

            Controls = new ResourceDictionary() { Source = new Uri("Themes/Controls.xaml", UriKind.Relative) };

            if (theme == ThemeType.None)
            {
                if (CurrentTheme != ThemeType.None)
                {
                    Application.Current.Resources.MergedDictionaries.RemoveAt(2);
                    Application.Current.Resources.MergedDictionaries.RemoveAt(1);
                    Application.Current.Resources.MergedDictionaries.RemoveAt(0);
                }
            }
            else
            {
                ColourDictionary = new ResourceDictionary() { Source = new Uri($"Themes/ColourDictionaries/{themeName}.xaml", UriKind.Relative) };
                ControlColours = new ResourceDictionary() { Source = new Uri("Themes/ControlColours.xaml", UriKind.Relative) };

                if (CurrentTheme == ThemeType.None)
                {
                    Application.Current.Resources.MergedDictionaries.Insert(0, ColourDictionary);
                }
                else
                {
                    Application.Current.Resources.MergedDictionaries.RemoveAt(2);
                    Application.Current.Resources.MergedDictionaries.RemoveAt(1);
                    Application.Current.Resources.MergedDictionaries[0] = ColourDictionary;
                }

                Application.Current.Resources.MergedDictionaries.Insert(1, ControlColours);
                Application.Current.Resources.MergedDictionaries.Insert(2, Controls);
            }

            CurrentTheme = theme;
        }

        public static object GetResource(object key)
        {
            return ColourDictionary[key];
        }

        public static SolidColorBrush GetBrush(string name)
        {
            return GetResource(name) is SolidColorBrush brush ? brush : new SolidColorBrush(Colors.White);
        }
    }
}