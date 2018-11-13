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
using System.Windows.Navigation;
using System.Windows.Shapes;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleFontEditor.xaml
    /// </summary>
    public partial class UndertaleFontEditor : UserControl
    {
        public UndertaleFontEditor()
        {
            InitializeComponent();
        }

        private void Button_Sort_Click(object sender, RoutedEventArgs e)
        {
            UndertaleFont font = this.DataContext as UndertaleFont;

            // There is no way to sort an ObservableCollection in place so we have to do this
            var copy = font.Glyphs.ToList();
            copy.Sort((x, y) => x.Character.CompareTo(y.Character));
            font.Glyphs.Clear();
            foreach (var glyph in copy)
                font.Glyphs.Add(glyph);
        }
    }
}
