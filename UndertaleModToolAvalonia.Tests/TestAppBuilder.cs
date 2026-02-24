using Avalonia;
using Avalonia.Headless;
using UndertaleModToolAvalonia.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace UndertaleModToolAvalonia.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
