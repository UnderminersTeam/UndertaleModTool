using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace UndertaleModToolAvalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        UpdateScriptsMenu();
    }

    void NativeMenuItem_Click_ReloadScriptsList(object? sender, EventArgs e) => UpdateScriptsMenu();

    async void UpdateScriptsMenu()
    {
        NativeMenuItem? scripts = NativeMenu.GetMenu(this)?.Items
            .OfType<NativeMenuItem>()
            .First(x => x.Header == "_Scripts");

        if (scripts is not null && scripts.Menu is not null)
        {
            while (scripts.Menu.Items.Count > 3)
            {
                scripts.Menu.Items.RemoveAt(scripts.Menu.Items.Count - 1);
            }

            string scriptsDir = Path.Join(AppContext.BaseDirectory, "Scripts");

            if (Directory.Exists(scriptsDir))
            {
                scripts.Menu.Items.Add(new NativeMenuItem("Loading...") { IsEnabled = false });

                var items = await Task.Run(() => GetScriptMenuItems(scriptsDir));

                scripts.Menu.Items.RemoveAt(3);

                foreach (var menu in items)
                {
                    scripts.Menu.Add(menu);
                }
            }
        }
    }

    IEnumerable<NativeMenuItemBase> GetScriptMenuItems(string dir)
    {
        foreach (string file in Directory.EnumerateDirectories(dir))
        {
            NativeMenuItem item = new(Path.GetFileName(file).Replace("_", "__"));

            item.Menu = [.. GetScriptMenuItems(file)];

            if (item.Menu.Items.Count > 0)
            {
                yield return item;
            }
        }

        foreach (string file in Directory.EnumerateFiles(dir))
        {
            if (Path.GetExtension(file) == ".csx")
            {
                NativeMenuItem item = new(Path.GetFileName(file).Replace("_", "__"));
                item.Click += (source, e) =>
                {
                    if (DataContext is MainViewModel vm)
                    {
                        vm.ScriptsRunScript(file);
                    }
                };
                yield return item;
            }
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!e.IsProgrammatic)
        {
            if (DataContext is MainViewModel vm && vm.Data is not null)
            {
                e.Cancel = true;

                async void AskSaveBeforeClose()
                {
                    if (await vm.AskProjectSave("There are assets marked to be exported in the current project. Save project before quitting?")
                        && await vm.AskFileSave("Save data file before quitting?"))
                        Close();
                }

                AskSaveBeforeClose();
            }
        }

        base.OnClosing(e);
    }
}
