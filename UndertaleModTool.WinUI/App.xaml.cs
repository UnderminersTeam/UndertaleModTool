using Microsoft.UI.Xaml;

namespace UndertaleModTool_WinUI;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        if (TryRunSmokeTest(args.Arguments))
            return;

        string[] parsedArgs = StartupArguments.Parse(args.Arguments);
        string? startupDataFilePath = Program.StartupDataFilePath ?? StartupArguments.FindSupportedDataFilePath(parsedArgs);
        _window = new MainWindow(startupDataFilePath);
        MainWindow = _window;
        _window.Activate();
    }

    private static bool TryRunSmokeTest(string arguments)
    {
        string[] parsedArgs = StartupArguments.Parse(arguments);
        int referenceSmokeTestIndex = Array.IndexOf(parsedArgs, "--reference-smoke");
        if (referenceSmokeTestIndex >= 0)
        {
            string? dataFilePath = StartupArguments.GetSupportedDataFileOptionValue(parsedArgs, "--reference-smoke");
            int referenceExitCode = dataFilePath is not null
                ? MainPage.RunReferenceSmokeTest(dataFilePath, StartupArguments.GetOptionValue(parsedArgs, "--reference-smoke-output"))
                : 2;
            Environment.Exit(referenceExitCode);
            return true;
        }

        int runtimeSmokeTestIndex = Array.IndexOf(parsedArgs, "--runtime-smoke");
        if (runtimeSmokeTestIndex >= 0)
        {
            string? dataFilePath = StartupArguments.GetSupportedDataFileOptionValue(parsedArgs, "--runtime-smoke");
            int runtimeExitCode = dataFilePath is not null
                ? MainPage.RunRuntimeSmokeTest(dataFilePath, StartupArguments.GetOptionValue(parsedArgs, "--runtime-smoke-output"))
                : 2;
            Environment.Exit(runtimeExitCode);
            return true;
        }

        int perfSmokeTestIndex = Array.IndexOf(parsedArgs, "--perf-smoke");
        if (perfSmokeTestIndex >= 0)
        {
            string? dataFilePath = StartupArguments.GetSupportedDataFileOptionValue(parsedArgs, "--perf-smoke");
            int perfExitCode = dataFilePath is not null
                ? MainPage.RunPerfSmokeTest(dataFilePath, StartupArguments.GetOptionValue(parsedArgs, "--perf-smoke-output"))
                : 2;
            Environment.Exit(perfExitCode);
            return true;
        }

        int smokeTestIndex = Array.IndexOf(parsedArgs, "--smoke-test");
        if (smokeTestIndex < 0)
            return false;

        string? smokeDataFilePath = StartupArguments.GetSupportedDataFileOptionValue(parsedArgs, "--smoke-test");
        int exitCode = smokeDataFilePath is not null
            ? MainPage.RunSmokeTest(smokeDataFilePath)
            : 2;
        Environment.Exit(exitCode);
        return true;
    }
}
