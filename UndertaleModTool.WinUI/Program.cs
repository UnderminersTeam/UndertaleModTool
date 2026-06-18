using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace UndertaleModTool_WinUI;

internal static class Program
{
    internal static string? StartupDataFilePath { get; private set; }

    [STAThread]
    private static void Main(string[] args)
    {
        int referenceSmokeTestIndex = Array.IndexOf(args, "--reference-smoke");
        if (referenceSmokeTestIndex >= 0)
        {
            string? dataFilePath = StartupArguments.GetSupportedDataFileOptionValue(args, "--reference-smoke");
            int exitCode = dataFilePath is not null
                ? MainPage.RunReferenceSmokeTest(dataFilePath, StartupArguments.GetOptionValue(args, "--reference-smoke-output"))
                : 2;
            Environment.Exit(exitCode);
            return;
        }

        int runtimeSmokeTestIndex = Array.IndexOf(args, "--runtime-smoke");
        if (runtimeSmokeTestIndex >= 0)
        {
            string? dataFilePath = StartupArguments.GetSupportedDataFileOptionValue(args, "--runtime-smoke");
            int exitCode = dataFilePath is not null
                ? MainPage.RunRuntimeSmokeTest(dataFilePath, StartupArguments.GetOptionValue(args, "--runtime-smoke-output"))
                : 2;
            Environment.Exit(exitCode);
            return;
        }

        int perfSmokeTestIndex = Array.IndexOf(args, "--perf-smoke");
        if (perfSmokeTestIndex >= 0)
        {
            string? dataFilePath = StartupArguments.GetSupportedDataFileOptionValue(args, "--perf-smoke");
            int exitCode = dataFilePath is not null
                ? MainPage.RunPerfSmokeTest(dataFilePath, StartupArguments.GetOptionValue(args, "--perf-smoke-output"))
                : 2;
            Environment.Exit(exitCode);
            return;
        }

        int smokeTestIndex = Array.IndexOf(args, "--smoke-test");
        if (smokeTestIndex >= 0)
        {
            string? dataFilePath = StartupArguments.GetSupportedDataFileOptionValue(args, "--smoke-test");
            int exitCode = dataFilePath is not null
                ? MainPage.RunSmokeTest(dataFilePath)
                : 2;
            Environment.Exit(exitCode);
            return;
        }

        StartupDataFilePath = StartupArguments.FindSupportedDataFilePath(args);

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            DispatcherQueueSynchronizationContext context = new(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _app = new App();
        });
    }

    private static App? _app;
}
