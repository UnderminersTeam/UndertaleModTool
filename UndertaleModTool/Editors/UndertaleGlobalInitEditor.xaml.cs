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
    /// Interaction logic for UndertaleGlobalInitEditor.xaml
    /// </summary>
    public partial class UndertaleGlobalInitEditor : DataUserControl
    {
        public UndertaleGlobalInitEditor()
        {
            InitializeComponent();
        }
        private void UndertaleObjectReference_Loaded(object sender, RoutedEventArgs e)
        {
            var objRef = sender as UndertaleObjectReference;

            objRef.ClearRemoveClickHandler();
            objRef.RemoveButton.Click += Remove_Click_Override;
            objRef.RemoveButton.ToolTip = "Remove script";
            objRef.RemoveButton.IsEnabled = true;
        }
        private void Remove_Click_Override(object sender, RoutedEventArgs e)
        {
            var btn = (ButtonDark)sender;
            var objRef = (UndertaleObjectReference)((Grid)btn.Parent).Parent;

            var data = (GlobalInitEditor)DataContext;
            var globalInits = data.GlobalInits;
            if (btn.DataContext is not UndertaleGlobalInit)
            {
                return;
            }
            globalInits.Remove((UndertaleGlobalInit)btn.DataContext);
        }
    }
}
