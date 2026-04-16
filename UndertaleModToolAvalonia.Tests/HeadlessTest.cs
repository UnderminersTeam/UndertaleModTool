using System.Diagnostics;
using System.IO;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.DependencyInjection;

namespace UndertaleModToolAvalonia.Tests;

public class HeadlessTest
{
    [AvaloniaFact]
    public async void Save_New_Data()
    {
        MainViewModel vm = App.Services.GetRequiredService<MainViewModel>();

        var mainWindow = new MainWindow
        {
            DataContext = vm,
        };
        mainWindow.Show();

        await vm.NewData();

        Assert.NotNull(vm.Data);

        using MemoryStream stream = new();

        await vm.SaveData(stream);
        Debug.WriteLine(stream.Length);

        Assert.Equal(2200, stream.Length);
    }
}
