using System.Configuration;
using System.Windows;
using System.Windows.Media;

namespace UndertaleModTool
{
    /// <summary>
    /// A standard button which compatible with the dark mode.
    /// </summary>
    public partial class Button : System.Windows.Controls.Button
    {
        // Setting "Foreground" implicitly breaks internal "IsEnabled" style trigger,
        // so this should be handled manually.
        private static readonly SolidColorBrush disabledTextBrush = new(Color.FromArgb(255, 131, 131, 131));

        /// <summary>Initializes a new instance of the button.</summary>
        public Button()
        {
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
