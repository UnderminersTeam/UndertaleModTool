using System;
using System.Collections.Generic;
using System.Globalization;
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
using UndertaleModTool.Editors.UndertaleFontEditor;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleFontEditor.xaml
    /// </summary>
    public partial class UndertaleFontEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public UndertaleFontEditor()
        {
            InitializeComponent();
        }

        private void Button_Sort_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UndertaleFont font)
                return;

            // There is no way to sort an ObservableCollection in place so we have to do this
            var copy = font.Glyphs.ToList();
            copy.Sort((x, y) => x.Character.CompareTo(y.Character));
            font.Glyphs.Clear();
            foreach (var glyph in copy)
                font.Glyphs.Add(glyph);

            mainWindow.ShowMessage("The glyphs were sorted successfully.");
        }
        private void Button_UpdateRange_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UndertaleFont font)
                return;

            var characters = font.Glyphs.Select(x => x.Character);
            font.RangeStart = characters.Min();
            font.RangeEnd = characters.Max();

            mainWindow.ShowMessage("The range was updated successfully.");
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not UndertaleFont font)
                return;

            var pos = e.GetPosition(sender as IInputElement);
            for (int i = 0; i < font.Glyphs.Count; i++)
            {
                var glyph = font.Glyphs[i];

                if (pos.X > glyph.SourceX && pos.X < glyph.SourceX + glyph.SourceWidth
                    && pos.Y > glyph.SourceY && pos.Y < glyph.SourceY + glyph.SourceHeight)
                {
                    GlyphsGrid.SelectedItem = glyph;

                    // "ScrollIntoView(glyph)" is noticeably slower
                    ScrollGlyphIntoView(i);
                    break;
                }
            }
        }
        private void ScrollGlyphIntoView(UndertaleFont.Glyph glyph)
        {
            if (DataContext is not UndertaleFont font)
                return;

            int index = font.Glyphs.IndexOf(glyph);
            if (index != -1)
                ScrollGlyphIntoView(index);
        }
        private void ScrollGlyphIntoView(int glyphIndex)
        {
            ScrollViewer glyphListViewer = MainWindow.FindVisualChild<ScrollViewer>(GlyphsGrid);
            if (glyphListViewer is null)
            {
                mainWindow.ShowError("Cannot find the glyphs table scroll viewer.");
                return;
            }
            glyphListViewer.ScrollToVerticalOffset(glyphIndex + 1 - (glyphListViewer.ViewportHeight / 2)); // DataGrid offset is logical
            glyphListViewer.UpdateLayout();

            ScrollViewer dataEditorViewer = mainWindow.DataEditor.Parent as ScrollViewer;
            if (dataEditorViewer is not null)
            {
                double initOffset = dataEditorViewer.VerticalOffset;

                dataEditorViewer.UpdateLayout();
                dataEditorViewer.ScrollToVerticalOffset(initOffset);
            }
        }

        private void EditRectangleButton_Click(object sender, RoutedEventArgs e)
        {
            if (GlyphsGrid.SelectedItem is not UndertaleFont.Glyph glyph)
            {
                mainWindow.ShowError("No glyph selected.");
                return;
            }

            if (DataContext is UndertaleFont font && font.Texture is null)
            {
                mainWindow.ShowError("The font has no texture.");
                return;
            }

            EditGlyphRectangleWindow dialog = null;
            try
            {
                dialog = new(DataContext as UndertaleFont, glyph);
                if (dialog.ShowDialog() == true)
                {
                    GlyphsGrid.SelectedItem = dialog.SelectedGlyph;
                    ScrollGlyphIntoView(dialog.SelectedGlyph);
                }
            }
            catch (Exception ex)
            {
                mainWindow.ShowError("An error occured in the glyph rectangle editor window.\n" +
                                     $"Please report this on GitHub.\n\n{ex}");
            }
            finally
            {
                dialog?.Close();
            }
        }
        private void CreateGlyphButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UndertaleFont font)
                return;

            int index = font.Glyphs.Count - 1;
            if (index >= 0)
            {
                if (font.Glyphs[index].SourceWidth == 0
                || font.Glyphs[index].SourceHeight == 0)
                {
                    mainWindow.ShowWarning("The last glyph has zero size.\n" +
                                           "You can use the button on the left to fix that.");
                    return;
                }
            }

            font.Glyphs.Add(new());
            index++;

            GlyphsGrid.SelectedIndex = index;
            ScrollGlyphIntoView(index);
        }

        private void EditKerningButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not UndertaleFont.Glyph glyph)
                return;

            GlyphsGrid.Visibility = Visibility.Collapsed;
            GlyphsGrid.IsEnabled = false;

            GlyphKerningGrid.SetBinding(DataGrid.ItemsSourceProperty,
                                        new Binding() { Source = glyph.Kerning });
            GlyphKerningBorder.Visibility = Visibility.Visible;
            GlyphKerningGrid.IsEnabled = true;

            char? ch = (char?)CharConverter.Instance.Convert(glyph.Character, null, null, null);
            ch ??= default;
            GlyphsLabel.Text = $"Kerning of glyph '{ch}' (code - {glyph.Character}):";
        }

        private void KerningBackButton_Click(object sender, RoutedEventArgs e)
        {
            BindingOperations.ClearBinding(GlyphKerningGrid, DataGrid.ItemsSourceProperty);
            GlyphKerningBorder.Visibility = Visibility.Collapsed;
            GlyphKerningGrid.IsEnabled = false;

            GlyphsGrid.Visibility = Visibility.Visible;
            GlyphsGrid.IsEnabled = true;

            GlyphsLabel.Text = "Glyphs:";
        }
        private void Command_GoBack(object sender, ExecutedRoutedEventArgs e)
        {
            if (GlyphKerningGrid.IsEnabled)
                KerningBackButton_Click(null, null);
        }
    }

    public class CharConverter : IValueConverter
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        public static readonly CharConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ushort charNum)
            {
                if (value is not short charNum1)
                    return "(error)";

                try
                {
                    charNum = (ushort)charNum1;
                }
                catch
                {
                    return "(error)";
                }
            }

            if (charNum == 0)
                return null;
            return System.Convert.ToChar(charNum);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string charStr || charStr.Length == 0)
                return new ValidationResult(false, null);

            uint charNum = charStr[0];
            if (charNum > ushort.MaxValue)
            {
                mainWindow.ShowError("The character code is greater than the maximum (65535)");
                return new ValidationResult(false, null);
            }

            return (ushort)charNum;
        }
    }
}
