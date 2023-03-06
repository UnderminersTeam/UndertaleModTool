using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace UndertaleModTool
{
    /// <summary>
    /// A standard combo box which compatible with the dark mode.
    /// </summary>
    public partial class ComboBoxDark : System.Windows.Controls.ComboBox
    {
        // Setting "Foreground" implicitly breaks internal "IsEnabled" style trigger,
        // so this has to be handled manually.
        private static readonly SolidColorBrush disabledTextBrush = new(Color.FromArgb(255, 131, 131, 131));

        /// <summary>Initializes a new instance of the combo box.</summary>
        public ComboBoxDark()
        {
            // Even though this will be called again in "OnPropertyChanged()", it's required.
            SetResourceReference(ForegroundProperty, "CustomTextBrush");

            Loaded += ComboBox_Loaded;
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            Popup popup = MainWindow.FindVisualChild<Popup>(this);
            var content = MainWindow.FindVisualChild<Border>(popup?.Child);
            if (content is null)
                return;

            // Change text color of dropdown items
            content.SetResourceReference(ForegroundProperty, SystemColors.ControlTextBrushKey);
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == IsEnabledProperty)
            {
                if ((bool)e.NewValue)
                    SetResourceReference(ForegroundProperty, "CustomTextBrush");
                else
                    Foreground = disabledTextBrush;
            }

            base.OnPropertyChanged(e);
        }
    }
}
