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
    public partial class TextInputDialog : INotifyPropertyChanged
    {
        private string _Message; // text in the label
        private string _MessageTitle; // label of the window
        private string _ButtonTitle; // text inside the button
        private string _CancelButtonTitle;
        private string _InputText; // text in the textbox.
        private bool _PreventClose; // should the dialog prevent itself from closing?
        private bool _IsMultiline;

        public event PropertyChangedEventHandler PropertyChanged;

        public Visibility CancelButtonVisibility { get => PreventClose ? Visibility.Hidden : Visibility.Visible; }
        public string Message { get => _Message; set { _Message = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Message")); } }
        public string MessageTitle { get => _MessageTitle; set { _MessageTitle = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MessageTitle")); } }
        public string ButtonTitle { get => _ButtonTitle; set { _ButtonTitle = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ButtonTitle")); } }
        public string CancelButtonTitle { get => _CancelButtonTitle; set { _CancelButtonTitle = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CancelButtonTitle")); } }
        public string InputText { get => _InputText; set { _InputText = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("InputText")); } }
        public bool PreventClose { get => _PreventClose; set { _PreventClose = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PreventClose")); } }
        public bool IsMultiline { get => _IsMultiline; set { _IsMultiline = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsMultiline")); } }
        
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
