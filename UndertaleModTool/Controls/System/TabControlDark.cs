using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UndertaleModTool
{
    /// <summary>
    /// A standard tab control which compatible with the dark mode.
    /// </summary>
    public partial class TabControlDark : TabControl
    {
        /// <summary>Initializes a new instance of the tab control.</summary>
        public TabControlDark()
        {
            Loaded += TabControlDark_Loaded;
        }

        private void TabControlDark_Loaded(object sender, RoutedEventArgs e)
        {
            SetDarkMode(Settings.Instance.EnableDarkMode);
        }

        /// <inheritdoc/>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TabItemDark();
        }

        public void SetDarkMode(bool enable)
        {
            foreach (var item in MainWindow.FindVisualChildren<TabItemDark>(this))
                item.SetDarkMode(enable);
        }
    }

    /// <summary>
    /// A standard tab item which compatible with the dark mode.
    /// </summary>
    public partial class TabItemDark : TabItem
    {
        private static readonly SolidColorBrush itemHighlightDarkBrush = new(Color.FromArgb(255, 48, 48, 60));
        private static readonly Brush itemInactiveBrush = new LinearGradientBrush(
                                                            new GradientStopCollection()
                                                            {
                                                                new GradientStop(Color.FromArgb(255, 240, 240, 240), 0),
                                                                new GradientStop(Color.FromArgb(255, 229, 229, 229), 1)
                                                            }, new(0, 0), new(1, 0)
                                                          );
        private static readonly Brush itemInactiveDarkBrush = new LinearGradientBrush(
                                                                new GradientStopCollection()
                                                                {
                                                                    new GradientStop(Color.FromArgb(255, 15, 15, 15), 0),
                                                                    new GradientStop(Color.FromArgb(255, 26, 26, 26), 1)
                                                                }, new(0, 0), new(1, 0)
                                                              );
        private Border border;

        /// <summary>Initializes a new instance of the tab item.</summary>
        public TabItemDark()
        {
            SetResourceReference(ForegroundProperty, SystemColors.WindowTextBrushKey);

            Loaded += TabItemDark_Loaded;
        }

        private void TabItemDark_Loaded(object sender, RoutedEventArgs e)
        {
            border = MainWindow.FindVisualChild<Border>(this);
            if (Environment.OSVersion.Version.Major >= 10)
            {
                Border innerBd = MainWindow.FindVisualChild<Border>(this, "innerBorder");
                innerBd?.SetResourceReference(BackgroundProperty, SystemColors.WindowBrushKey);
            }

            SetDarkMode(Settings.Instance.EnableDarkMode);
        }

        public void SetDarkMode(bool enable)
        {
            Background = enable ? itemInactiveDarkBrush : itemInactiveBrush;
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == IsMouseOverProperty
                && Settings.Instance.EnableDarkMode)
            {
                if ((bool)e.NewValue)
                    border?.SetValue(BackgroundProperty, itemHighlightDarkBrush);
                else
                    border?.ClearValue(BackgroundProperty);
            }

            base.OnPropertyChanged(e);
        }
    }
}
