using System;

namespace Theme.WPF.Themes
{
    public enum ThemeType
    {
        None,
        SoftDark,
        RedBlackTheme,
        DeepDark,
        GreyTheme,
        DarkGreyTheme,
        LightTheme,
    }

    public static class ThemeTypeExtension
    {
        public static string GetName(this ThemeType type)
        {
            switch (type)
            {
                case ThemeType.None: return null;
                case ThemeType.SoftDark: return "SoftDark";
                case ThemeType.RedBlackTheme: return "RedBlackTheme";
                case ThemeType.DeepDark: return "DeepDark";
                case ThemeType.GreyTheme: return "GreyTheme";
                case ThemeType.DarkGreyTheme: return "DarkGreyTheme";
                case ThemeType.LightTheme: return "LightTheme";
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}