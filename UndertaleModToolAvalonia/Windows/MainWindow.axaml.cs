using Avalonia.Controls;

namespace UndertaleModToolAvalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!e.IsProgrammatic)
        {
            if (DataContext is MainViewModel vm && vm.Data is not null)
            {
                e.Cancel = true;

                async void AskFileSaveBeforeClose()
                {
                    if (await vm.AskFileSave("Save data file before quitting?"))
                        Close();
                }

                AskFileSaveBeforeClose();
            }
        }

        base.OnClosing(e);
    }
}
