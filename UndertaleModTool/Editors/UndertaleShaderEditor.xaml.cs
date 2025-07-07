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
using UndertaleModLib;
using UndertaleModLib.Models;
using WpfAnimatedGif;

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

            ((Image)mainWindow.FindName("Flowey")).Opacity = 0;
            ((Image)mainWindow.FindName("FloweyLeave")).Opacity = 0;
            ((Image)mainWindow.FindName("FloweyBubble")).Opacity = 0;

            ((Label)this.FindName("ShadersObjectLabel")).Content = ((Label)mainWindow.FindName("ObjectLabel")).Content;
        }
        private void UndertaleShadersEditor_Unloaded(object sender, RoutedEventArgs e)
        {
            var floweranim = ((Image)mainWindow.FindName("Flowey"));
            //floweranim.Opacity = 1;

            var controller = ImageBehavior.GetAnimationController(floweranim);
            controller.Pause();
            controller.GotoFrame(controller.FrameCount - 5);
            controller.Play();

            ((Image)mainWindow.FindName("FloweyLeave")).Opacity = 0;
        }
        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UndertaleShader code = this.DataContext as UndertaleShader;

            int foundIndex = code is UndertaleResource res ? mainWindow.Data.IndexOf(res, false) : -1;
            string idString;

            if (foundIndex == -1)
                idString = "None";
            else if (foundIndex == -2)
                idString = "N/A";
            else
                idString = Convert.ToString(foundIndex);

            ((Label)this.FindName("ShadersObjectLabel")).Content = idString;

            //((Image)mainWindow.FindName("FloweyBubble")).Opacity = 0;
            //((Image)mainWindow.FindName("Flowey")).Opacity = 0;
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

        private void TextEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var editor = sender as TextEditor;
            if (editor is null)
            {
                return;
            }

            var srcString = e.NewValue as UndertaleString;
            if (srcString is null)
            {
                return;
            }

            editor.Text = srcString.Content;
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
