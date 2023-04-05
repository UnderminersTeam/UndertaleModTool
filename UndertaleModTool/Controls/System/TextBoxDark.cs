using System.Windows.Controls;

namespace UndertaleModTool
{
    /// <summary>
    /// A standard text box which compatible with the dark mode.
    /// </summary>
    public partial class TextBoxDark : TextBox
    {
        /// <summary>Initializes a new instance of the text box.</summary>
        public TextBoxDark()
        {
            SetResourceReference(ContextMenuProperty, "textBoxContextMenu");
        }
    }
}
