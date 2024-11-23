#pragma warning disable CA1416 // Validate platform compatibility

using ICSharpCode.AvalonEdit;
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
    /// Interaction logic for UndertaleShaderEditor.xaml
    /// </summary>
    public partial class UndertaleShaderEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public UndertaleShaderEditor()
        {
            InitializeComponent();
        }

        private void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            var editor = sender as TextEditor;
            if (editor is null)
            {
                mainWindow.ShowError("Cannot load the code of one of the shader properties - the editor is not found?");
                return;
            }

            var srcString = editor.DataContext as UndertaleString;
            if (srcString is null)
            {
                mainWindow.ShowError("Cannot load the code of one of the shader properties - the source string object is null.");
                return;
            }

            editor.Text = srcString.Content;
        }

        private void TextEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            var editor = sender as TextEditor;
            if (editor is null)
            {
                mainWindow.ShowError("The changes weren't saved - the editor is not found?");
                return;
            }

            var srcString = editor.DataContext as UndertaleString;
            if (srcString is null)
            {
                mainWindow.ShowError("The changes weren't saved - the source string object is null.");
                return;
            }

            srcString.Content = editor.Text;
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
