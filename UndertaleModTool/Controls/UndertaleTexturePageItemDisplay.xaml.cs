using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Threading;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleTexturePageItemDisplay.xaml
    /// </summary>
    public partial class UndertaleTexturePageItemDisplay : UserControl
    {
        public static readonly DependencyProperty DisplayBorderProperty =
            DependencyProperty.Register("DisplayBorder", typeof(bool),
                typeof(UndertaleTexturePageItemDisplay),
                new FrameworkPropertyMetadata(true,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (sender, e) =>
                    {
                        var inst = sender as UndertaleTexturePageItemDisplay;
                        if (inst is null)
                            return;
                        if (e.NewValue is not bool val)
                            return;

                        inst.RenderAreaBorder.BorderThickness = new Thickness(val ? 1 : 0);
                    }));
        public bool DisplayBorder
        {
            get { return (bool)GetValue(DisplayBorderProperty); }
            set { SetValue(DisplayBorderProperty, value); }
        }

        public UndertaleTexturePageItemDisplay()
        {
            InitializeComponent();
        }
    }
}
