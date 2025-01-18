using System.Windows;
using System.Windows.Controls;

namespace UndertaleModTool
{
    public class NullConditionalDataTemplateSelector : DataTemplateSelector
    {
        // <summary>
        // Selects a DataTemplate based on whether the item is null or not.
        // </summary>
        // <remarks>
        // Used by <see cref="ResourceListTreeViewItem"/> to select the appropriate template for an item.
        // </remarks>

        // <summary>
        // The template to use if the item is not null.
        // </summary>
        public DataTemplate NonNullTemplate { get; set; }

        // <summary>
        // The template to use if the item is null.
        // </summary>
        public DataTemplate NullTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is null)
                return NullTemplate;
            return NonNullTemplate;
        }
    }
}