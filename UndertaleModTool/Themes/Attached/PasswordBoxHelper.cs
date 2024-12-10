using System;
using System.Windows;
using System.Windows.Controls;

namespace Theme.WPF.Themes.Attached
{
    public class PasswordBoxHelper
    {
        public static readonly DependencyProperty ListenToLengthProperty =
            DependencyProperty.RegisterAttached(
                "ListenToLength",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(false, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox box)
            {
                box.PasswordChanged -= BoxOnPasswordChanged;
                if (e.NewValue != null && (bool) e.NewValue)
                {
                    box.PasswordChanged += BoxOnPasswordChanged;
                }
            }
            else
            {
                throw new Exception("DependencyObject is not a password box. It is '" + (d == null ? "null" : d.GetType().Name) + '\'');
            }
        }

        public static readonly DependencyProperty InputLengthProperty =
            DependencyProperty.RegisterAttached(
                "InputLength",
                typeof(int),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(0));

        public static bool GetListenToLength(PasswordBox box)
        {
            return (bool) box.GetValue(ListenToLengthProperty);
        }

        public static void SetListenToLength(PasswordBox box, bool value)
        {
            box.SetValue(ListenToLengthProperty, value);
        }

        public static int GetInputLength(PasswordBox box)
        {
            return (int) box.GetValue(InputLengthProperty);
        }

        public static void SetInputLength(PasswordBox box, int value)
        {
            box.SetValue(InputLengthProperty, value);
        }

        private static void BoxOnPasswordChanged(object sender, RoutedEventArgs e)
        {
            SetInputLength((PasswordBox) sender, ((PasswordBox) sender).SecurePassword.Length);
        }
    }
}