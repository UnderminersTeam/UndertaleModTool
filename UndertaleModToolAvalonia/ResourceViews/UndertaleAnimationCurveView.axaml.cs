using Avalonia.Controls;

namespace UndertaleModToolAvalonia;

public partial class UndertaleAnimationCurveView : UserControl
{
    public UndertaleAnimationCurveView()
    {
        InitializeComponent();

        DataContextChanged += (_, __) =>
        {
            if (DataContext is UndertaleAnimationCurveViewModel vm)
            {
                vm.ChannelSelectedChanged(ChannelsDataGrid.DataGridControl.SelectedItem);
            }
        };
    }
}