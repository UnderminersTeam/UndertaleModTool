using Avalonia.Controls;
using UndertaleModToolAvalonia.Controls;

namespace UndertaleModToolAvalonia.Views;

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

                async void ShowSaveChangesBeforeQuittingDialog()
                {
                    var result = await vm.ShowMessageDialog("Save changes before quitting?", ok: false, yes: true, no: true, cancel: true);
                    if (result == MessageWindow.Result.Yes)
                    {
                        if (await vm.FileSave())
                        {
                            Close();
                        }
                    }
                    else if (result == MessageWindow.Result.No)
                    {
                        Close();
                    }
                }

                ShowSaveChangesBeforeQuittingDialog();
            }
        }

        base.OnClosing(e);
    }
}
