using System.Windows;
using System.Windows.Media;

namespace UndertaleModTool
{
    /// <summary>
    /// A standard combo box which compatible with the dark mode.
    /// </summary>
    public partial class ComboBox : System.Windows.Controls.ComboBox
    {
        // Setting "Foreground" implicitly breaks internal "IsEnabled" style trigger,
        // so this has to be handled manually.
        private static readonly SolidColorBrush disabledTextBrush = new(Color.FromArgb(255, 131, 131, 131));

        /// <summary>Initializes a new instance of the combo box.</summary>
        public ComboBox()
        {
            // Even though this will be called again in "OnPropertyChanged()", it's required.
            SetResourceReference(ForegroundProperty, "CustomTextBrush");
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
