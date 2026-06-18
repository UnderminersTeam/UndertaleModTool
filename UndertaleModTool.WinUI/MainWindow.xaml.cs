using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Windows.UI;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UndertaleModTool_WinUI;

/// <summary>
/// The application window. This hosts a Frame that displays pages. Add your
/// UI and logic to MainPage.xaml / MainPage.xaml.cs instead of here so you
/// can use Page features such as navigation events and the Loaded lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
    private bool _isCloseConfirmed;

    public MainWindow(string? startupDataFilePath = null)
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        ConfigureTitleBarColors();

        AppWindow.SetIcon("Assets/AppIcon.ico");
        AppWindow.Resize(new SizeInt32(1720, 960));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
            presenter.Maximize();

        // Navigate the root frame to the main page on startup.
        RootFrame.Navigate(typeof(MainPage));
        OpenInitialDataFile(startupDataFilePath);
        AppWindow.Closing += AppWindow_Closing;
    }

    public void SetDocumentTitle(string title)
    {
        Title = title;
        AppTitleBar.Title = title;
    }

    private void ConfigureTitleBarColors()
    {
        AppWindowTitleBar titleBar = AppWindow.TitleBar;
        Color shell = Color.FromArgb(255, 25, 26, 29);
        Color shellHover = Color.FromArgb(255, 34, 36, 40);
        Color shellPressed = Color.FromArgb(255, 42, 45, 51);
        Color text = Color.FromArgb(255, 238, 240, 243);
        Color quietText = Color.FromArgb(255, 170, 176, 184);

        titleBar.BackgroundColor = shell;
        titleBar.ForegroundColor = text;
        titleBar.InactiveBackgroundColor = shell;
        titleBar.InactiveForegroundColor = quietText;
        titleBar.ButtonBackgroundColor = shell;
        titleBar.ButtonForegroundColor = text;
        titleBar.ButtonHoverBackgroundColor = shellHover;
        titleBar.ButtonHoverForegroundColor = text;
        titleBar.ButtonPressedBackgroundColor = shellPressed;
        titleBar.ButtonPressedForegroundColor = text;
        titleBar.ButtonInactiveBackgroundColor = shell;
        titleBar.ButtonInactiveForegroundColor = quietText;
    }

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_isCloseConfirmed)
            return;

        if (RootFrame.Content is not MainPage page)
            return;

        args.Cancel = true;
        if (!await page.TryCloseAsync())
            return;

        _isCloseConfirmed = true;
        Close();
    }

    private void OpenInitialDataFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || RootFrame.Content is not MainPage page)
            return;

        page.Loaded += async (_, _) => await page.OpenInitialDataFileAsync(path);
    }
}
