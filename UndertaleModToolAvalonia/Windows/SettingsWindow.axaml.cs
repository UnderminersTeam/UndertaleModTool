using System;
using Avalonia.Controls;

namespace UndertaleModToolAvalonia;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        Closing += async (_, _) =>
        {
            if (DataContext is SettingsViewModel vm)
            {
                if (vm.MainVM.Settings.Save() is Exception ex)
                {
                    await vm.MainVM.View!.MessageDialog($"Error when saving settings file:\n{ex.Message}");
                }
            }
        };
    }
}
