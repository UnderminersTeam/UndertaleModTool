using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Theme.WPF.Themes.Attached
{
    public static class HorizontalScrolling
    {
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
        private static extern bool SystemParametersInfo(int nAction, int nParam, ref int value, int ignore);

        private static bool hasCachedScrollChars;
        private static int scrollChars;

        public static int ScrollChars
        {
            [SecurityCritical]
            get
            {
                if (hasCachedScrollChars)
                    return scrollChars;

                if (!SystemParametersInfo(108, 0, ref scrollChars, 0))
                    throw new Win32Exception();

                hasCachedScrollChars = true;
                return scrollChars;
            }
        }


        public static readonly DependencyProperty UseHorizontalScrollingProperty = DependencyProperty.RegisterAttached("UseHorizontalScrolling", typeof(bool), typeof(HorizontalScrolling), new PropertyMetadata(false, OnUseHorizontalScrollWheelPropertyChanged));
        public static readonly DependencyProperty IsRequireShiftForHorizontalScrollProperty = DependencyProperty.RegisterAttached("IsRequireShiftForHorizontalScroll", typeof(bool), typeof(HorizontalScrolling), new PropertyMetadata(true));
        public static readonly DependencyProperty ForceHorizontalScrollingProperty = DependencyProperty.RegisterAttached("ForceHorizontalScrolling", typeof(bool), typeof(HorizontalScrolling), new PropertyMetadata(false));
        public static readonly DependencyProperty HorizontalScrollingAmountProperty = DependencyProperty.RegisterAttached("HorizontalScrollingAmount", typeof(int), typeof(HorizontalScrolling), new PropertyMetadata(ScrollChars));

        public static void SetUseHorizontalScrolling(DependencyObject element, bool value) => element.SetValue(UseHorizontalScrollingProperty, value);
        public static bool GetUseHorizontalScrolling(DependencyObject element) => (bool) element.GetValue(UseHorizontalScrollingProperty);

        public static void SetIsRequireShiftForHorizontalScroll(DependencyObject element, bool value) => element.SetValue(IsRequireShiftForHorizontalScrollProperty, value);
        public static bool GetIsRequireShiftForHorizontalScroll(DependencyObject element) => (bool) element.GetValue(IsRequireShiftForHorizontalScrollProperty);

        public static bool GetForceHorizontalScrolling(DependencyObject d) => (bool) d.GetValue(ForceHorizontalScrollingProperty);
        public static void SetForceHorizontalScrolling(DependencyObject d, bool value) => d.SetValue(ForceHorizontalScrollingProperty, value);

        public static int GetHorizontalScrollingAmount(DependencyObject d) => (int) d.GetValue(HorizontalScrollingAmountProperty);
        public static void SetHorizontalScrollingAmount(DependencyObject d, int value) => d.SetValue(HorizontalScrollingAmountProperty, value);

        private static void OnUseHorizontalScrollWheelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                element.PreviewMouseWheel -= OnPreviewMouseWheel;
                if ((bool) e.NewValue)
                {
                    element.PreviewMouseWheel += OnPreviewMouseWheel;
                }
            }
            else
            {
                throw new Exception("Attached property must be used with UIElement");
            }
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is UIElement element && e.Delta != 0)
            {
                ScrollViewer scroller = FindVisualChild<ScrollViewer>(element);
                if (scroller == null)
                {
                    return;
                }

                if (GetIsRequireShiftForHorizontalScroll(element) && scroller.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled)
                {
                    return;
                }

                int amount = GetHorizontalScrollingAmount(element);
                if (amount < 1)
                {
                    amount = 3;
                }

                if (Keyboard.Modifiers == ModifierKeys.Shift || Mouse.MiddleButton == MouseButtonState.Pressed || GetForceHorizontalScrolling(element))
                {
                    int count = (e.Delta / 120) * amount;
                    if (e.Delta < 0)
                    {
                        for (int i = -count; i > 0; i--)
                        {
                            scroller.LineRight();
                        }
                    }
                    else
                    {
                        for (int i = 0; i < count; i++)
                        {
                            scroller.LineLeft();
                        }
                    }

                    e.Handled = true;
                }
            }
        }

        // https://github.com/AngryCarrot789/SharpPad/blob/master/SharpPad/Utils/Visuals/VisualTreeUtils.cs

        public static T FindVisualChild<T>(DependencyObject obj, bool includeSelf = true) where T : class
        {
            if (obj == null)
                return null;
            if (includeSelf && obj is T t)
                return t;
            return FindVisualChildInternal<T>(obj);
        }

        private static T FindVisualChildInternal<T>(DependencyObject obj) where T : class
        {
            int count, i;
            if (obj is ContentControl)
            {
                DependencyObject child = ((ContentControl) obj).Content as DependencyObject;
                if (child is T t)
                {
                    return t;
                }
                else
                {
                    return child != null ? FindVisualChildInternal<T>(child) : null;
                }
            }
            else if ((obj is Visual || obj is Visual3D) && (count = VisualTreeHelper.GetChildrenCount(obj)) > 0)
            {
                for (i = 0; i < count;)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(obj, i++);
                    if (child is T t)
                    {
                        return t;
                    }
                }

                for (i = 0; i < count;)
                {
                    T child = FindVisualChildInternal<T>(VisualTreeHelper.GetChild(obj, i++));
                    if (child != null)
                    {
                        return child;
                    }
                }
            }

            return null;
        }
    }
}