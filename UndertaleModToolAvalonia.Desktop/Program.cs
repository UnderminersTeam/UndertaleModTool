using System;
using System.IO;
using Avalonia;
using SDL3;

namespace UndertaleModToolAvalonia.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnMainWindowClose);
        }
        catch (Exception e)
        {
            string localAppData = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UndertaleModToolAvalonia");
            Directory.CreateDirectory(localAppData);

            File.WriteAllText(Path.Join(localAppData, "CrashLog.txt"), e.ToString());

            // TODO: Figure out a way to actually stop the UI and other threads.
            SDL.ShowSimpleMessageBox(SDL3.SDL.MessageBoxFlags.Error, "UndertaleModToolAvalonia", $"{e}", 0);
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // NOTE: Software rendering on Linux to avoid crashes, I don't know why that's happening.
            .With(new X11PlatformOptions() { RenderingMode = [X11RenderingMode.Software] })
            .LogToTrace();
}
