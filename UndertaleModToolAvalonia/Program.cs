using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using SDL3;

namespace UndertaleModToolAvalonia;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (source, e) =>
        {
            HandleException((e.ExceptionObject as Exception)!);
        };

        TaskScheduler.UnobservedTaskException += (source, e) =>
        {
            HandleException(e.Exception);
        };

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnMainWindowClose);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            throw;
        }
    }

    public static void HandleException(Exception ex)
    {
        string localAppData = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UndertaleModToolAvalonia");
        Directory.CreateDirectory(localAppData);

        File.WriteAllText(Path.Join(localAppData, "CrashLog.txt"), ex.ToString());

        SDL.ShowSimpleMessageBox(SDL3.SDL.MessageBoxFlags.Error,
            "UndertaleModToolAvalonia " + App.VersionString, ex.ToString(), 0);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new SkiaOptions() { MaxGpuResourceSizeBytes = null })
            .LogToTrace();
}
