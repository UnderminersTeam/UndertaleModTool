using System;

namespace UndertaleModTool
{
    /// <summary>
    /// Interaction logic for UndertaleSequenceEditor.xaml
    /// </summary>
    public partial class UndertaleSequenceEditor : DataUserControl
    {
        public bool PartOfSprite { get; set; } = false;

        public UndertaleSequenceEditor()
        {
            InitializeComponent();
        }
    }
}
