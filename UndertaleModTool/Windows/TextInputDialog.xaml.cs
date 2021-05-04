using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UndertaleModTool
{
    /// <summary>
    /// Interaction logic for TextInputDialog.xaml
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public partial class TextInputDialog : Window
    {

        public Visibility CancelButtonVisibility { get => PreventClose ? Visibility.Hidden : Visibility.Visible; }
        public string Message { get; set; } // text in the label
        public string MessageTitle { get; set; } // label of the window
        public string ButtonTitle { get; set; } // text inside the button
        public string CancelButtonTitle { get; set; }
        public string InputText { get; set; } // text in the textbox.
        public bool PreventClose { get; set; } // should the dialog prevent itself from closing?
        public bool IsMultiline { get; set; }

        public TextInputDialog(string titleText, string labelText, string defaultInputBoxText, string cancelButtonText, string submitButtonText, bool isMultiline, bool preventClose)
        {
            IsMultiline = isMultiline;
            PreventClose = preventClose;
            MessageTitle = titleText;
            Message = labelText;
            ButtonTitle = submitButtonText;
            CancelButtonTitle = cancelButtonText;
            InputText = defaultInputBoxText;

            InitializeComponent();
            this.DataContext = this;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = this.PreventClose;
        }

        public void TryHide()
        {
            Dispatcher.Invoke(() =>
            {
                if (IsVisible)
                {
                    this.PreventClose = false;
                    Hide();
                }
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.PreventClose = false;
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
