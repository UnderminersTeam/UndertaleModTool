using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UndertaleModTool
{
    /// <summary>
    /// Interaction logic for MessageDialog.xaml
    /// </summary>
    public partial class MessageDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Text { get; set; }
        public MessageBoxButton Buttons { get; set; } = MessageBoxButton.OK;
        public ImageSource TextIcon { get; set; }

        public Visibility OkButtonVisibility => (Buttons == MessageBoxButton.OK || Buttons == MessageBoxButton.OKCancel) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility YesNoButtonsVisibility => (Buttons == MessageBoxButton.YesNo || Buttons == MessageBoxButton.YesNoCancel) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CancelButtonVisibility => (Buttons == MessageBoxButton.OKCancel || Buttons == MessageBoxButton.YesNoCancel) ? Visibility.Visible : Visibility.Collapsed;

        public MessageBoxResult Result = MessageBoxResult.None;

        public MessageDialog()
        {
            InitializeComponent();
        }

        public void SetIcon(MessageBoxImage image)
        {
            // TODO: Make custom icons so they don't look terrible
            SetIcon(image switch
            {
                MessageBoxImage.Information => SystemIcons.Information,
                MessageBoxImage.Question => SystemIcons.Question,
                MessageBoxImage.Warning => SystemIcons.Warning,
                MessageBoxImage.Error => SystemIcons.Error,
                _ => throw new NotImplementedException(),
            });
        }

        public void SetIcon(Icon icon)
        {
            TextIcon = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            Close();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            Close();
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (Result == MessageBoxResult.None)
            {
                if (Buttons == MessageBoxButton.YesNo)
                {
                    e.Cancel = true;
                }
                else if (Buttons == MessageBoxButton.OKCancel || Buttons == MessageBoxButton.YesNoCancel)
                {
                    Result = MessageBoxResult.Cancel;
                }
            }
        }
    }
}
