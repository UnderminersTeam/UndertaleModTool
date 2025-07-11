using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Controls;
using UndertaleModLib.Util;
using System.Linq;

EnsureDataLoaded();

string fntFolder = PromptChooseDirectory();
if (fntFolder is null)
    return;

class FontEntry
{
    public FontEntry(UndertaleFont font, bool @checked)
    {
        Font = font;
        FontNameSafe = font.Name?.Content?.Replace("_", "__");
        Checked = @checked;
    }

    public UndertaleFont Font { get; }
    public string FontNameSafe { get; }
    public bool Checked { get; set; }
}

public UndertaleFont[] SelectFonts()
{
    double minWidth = 420;
    double minHeight = 430;
    Window window = new()
    {
        MinWidth = minWidth,
        MinHeight = minHeight,
        Width = minWidth,
        Height = minHeight,
        Title = "Select fonts to export"
    };
    window.IsVisibleChanged += (_, _) =>
    {
        // There is no check for `IsVisible` or `IsLoaded`,
        // because, apparently, it works differently for programmatically created window

        if (Settings.Instance.EnableDarkMode)
            MainWindow.SetDarkTitleBarForWindow(window, true, false);
    };

    FontEntry[] fonts = Data.Fonts?.Select(x => new FontEntry(x, true)).ToArray();

    Grid contentGrid = new();
    contentGrid.RowDefinitions.Add(new() { Height = new(1, GridUnitType.Star), MinHeight = 300 });
    contentGrid.RowDefinitions.Add(new() { Height = GridLength.Auto });
    contentGrid.RowDefinitions.Add(new() { Height = GridLength.Auto });

    ListBox fontListBox = new()
    {
        ItemsSource = fonts,
        Margin = new(10, 10, 10, 0),
        MinWidth = 380,
        SelectionMode = SelectionMode.Multiple
    };
    var appResources = Application.Current.Resources;
    var bgBrush = (appResources?[SystemColors.ControlBrushKey]) as SolidColorBrush;
    if (bgBrush is not null)
        fontListBox.Background = bgBrush;

    Style itemContStyle = new(typeof(ListBoxItem));
    itemContStyle.Setters.Add(new Setter(ListBoxItem.IsSelectedProperty, new Binding("Checked") { Mode = BindingMode.TwoWay }));
    fontListBox.ItemContainerStyle = itemContStyle;

    // Microsoft recommends to use `XAMLReader.Load()`,
    // but it sucks in two ways:
    // 1) It's difficult to modify, as there is no syntax highlighting, error checking, and auto-indentation.
    // 2) In order to add the event listeners, you have to add them later, by accessing each element by name.
    //
    // When `FrameworkElementFactory` will be removed from C#, then any AI assistant (e.g. ChatGPT, DeepSeek)
    // can generate XAML code from this C# code easily, with some minor tweaks (I've checked it).
    // I mean even for the whole window.
    DataTemplate fontTemplate = new();
    FrameworkElementFactory templateFactory = new(typeof(CheckBox));
    templateFactory.SetValue(CheckBox.IsCheckedProperty, new Binding("Checked") { Mode = BindingMode.TwoWay });
    templateFactory.SetValue(CheckBox.ContentProperty, new Binding("FontNameSafe") { Mode = BindingMode.OneTime });
    fontTemplate.VisualTree = templateFactory;
    fontListBox.ItemTemplate = fontTemplate;

    StackPanel selectStackPanel = new()
    {
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(10, 7, 0, 0)
    };
    ButtonDark selectAllButton = new()
    {
        Content = "Select all",
        Height = 26,
        Width = 63
    };
    selectAllButton.Click += (_, _) => fontListBox.SelectAll();
    ButtonDark deselectAllButton = new()
    {
        Content = "Deselect all",
        Margin = new Thickness(5, 0, 0, 0),
        Height = 26,
        Width = 71
    };
    deselectAllButton.Click += (_, _) => fontListBox.UnselectAll();
    selectStackPanel.Children.Add(selectAllButton);
    selectStackPanel.Children.Add(deselectAllButton);

    ButtonDark okButton = new()
    {
        Content = "OK",
        Margin = new Thickness(0, 16, 0, 16),
        Width = 80,
        Height = 32,
        FontSize = 16
    };
    okButton.Click += (_, _) =>
    {
        var checkedFonts = fonts.Where(f => f.Checked)
                                .Select(f => f.Font)
                                .ToArray();
        if (checkedFonts.Length == 0)
        {
            window.ShowError("No fonts are selected.");
            return;
        }

        window.Tag = checkedFonts;
        window.DialogResult = true;
        window.Close();
    };

    contentGrid.Children.Add(fontListBox);
    contentGrid.Children.Add(selectStackPanel);
    contentGrid.Children.Add(okButton);
    Grid.SetRow(fontListBox, 0);
    Grid.SetRow(selectStackPanel, 1);
    Grid.SetRow(okButton, 2);

    window.Content = contentGrid;

    if (window.ShowDialog() == true)
    {
        var selectedFonts = window.Tag as UndertaleFont[];
        if (selectedFonts is null)
            return null; // Shouldn't happen

        return selectedFonts;
    }

    return null;
}

var selectedFonts = SelectFonts();
if (selectedFonts is null)
    return;

SetProgressBar(null, "Fonts", 0, selectedFonts.Length);
StartProgressBarUpdater();

TextureWorker worker = null;
using (worker = new())
{
    await DumpFonts();
}

await StopProgressBarUpdater();
HideProgressBar();

async Task DumpFonts()
{
    await Task.Run(() => Parallel.ForEach(selectedFonts, DumpFont));
}

void DumpFont(UndertaleFont font)
{
    if (font is not null)
    {
        worker.ExportAsPNG(font.Texture, Path.Combine(fntFolder, $"{font.Name.Content}.png"));
        using (StreamWriter writer = new(Path.Combine(fntFolder, $"glyphs_{font.Name.Content}.csv")))
        {
            writer.WriteLine($"{font.DisplayName};{font.EmSize};{font.Bold};{font.Italic};{font.Charset};{font.AntiAliasing};{font.ScaleX};{font.ScaleY}");

            foreach (var g in font.Glyphs)
            {
                writer.WriteLine($"{g.Character};{g.SourceX};{g.SourceY};{g.SourceWidth};{g.SourceHeight};{g.Shift};{g.Offset}");
            }
        }
    }

    IncrementProgressParallel();
}
