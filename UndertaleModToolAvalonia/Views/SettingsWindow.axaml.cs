using Avalonia.Controls;

namespace UndertaleModToolAvalonia.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        Closing += (_, __) =>
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.MainVM.Settings?.Save();
            }
        };
    }
}