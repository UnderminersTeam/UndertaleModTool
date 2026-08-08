using Avalonia.Controls;

namespace UndertaleModToolAvalonia;

public partial class UndertaleSpriteView : UserControl
{
    public UndertaleSpriteView()
    {
        InitializeComponent();

        DataContextChanged += (_, __) =>
        {
            if (DataContext is UndertaleSpriteViewModel vm)
            {
                vm.TexturesSelectedChanged(TexturesDataGrid.DataGridControl.SelectedItem);
                vm.CollisionMasksSelectedChanged(CollisionMasksDataGrid.DataGridControl.SelectedItem);
            }
        };
    }
}