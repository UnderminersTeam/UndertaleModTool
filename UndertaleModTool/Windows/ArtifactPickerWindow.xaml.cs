using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UndertaleModTool.Windows
{
    /// <summary>
    /// Interaction logic for ArtifactPickerWindow.xaml
    /// </summary>
    public partial class ArtifactPickerWindow : Window
    {
        public int ArtifactId = 0;
        public ArtifactPickerWindow()
        {
            InitializeComponent();
        }

        public void ArtifactButton_Click(object sender, RoutedEventArgs e)
        {
            ArtifactId = (int) (sender as Button).Tag;
            DialogResult = true;
            Close();
        }

    }
}
