using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using UndertaleModLib.Scripting;
using Ookii.Dialogs.Wpf;

namespace UndertaleModTool
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public partial class ScriptOptionsDialog : Window
    {
        public string MessageTitle { get; set; }

        private readonly ScriptOptionsBuilder _builder;
        private readonly Dictionary<string, object> _controls = new();

        public ScriptOptionsDialog(string title, ScriptOptionsBuilder builder)
        {
            MessageTitle = title;
            _builder = builder;

            InitializeComponent();
            this.DataContext = this;
            BuildOptions();
        }

        private void BuildOptions()
        {
            foreach (ScriptOption option in _builder.Options)
            {
                FrameworkElement control = option.Type switch
                {
                    ScriptOptionType.Bool => BuildBoolOption(option),
                    ScriptOptionType.Text => BuildTextOption(option),
                    ScriptOptionType.Radio => BuildRadioOption(option),
                    ScriptOptionType.Directory => BuildDirectoryOption(option),
                    _ => null
                };

                if (control is not null)
                    OptionsPanel.Children.Add(control);
            }
        }

        private FrameworkElement BuildBoolOption(ScriptOption option)
        {
            CheckBox checkBox = new()
            {
                Content = option.Label,
                IsChecked = option.DefaultValue is bool b && b,
                Margin = new Thickness(0, 5, 0, 0)
            };
            _controls[option.Id] = checkBox;
            return checkBox;
        }

        private FrameworkElement BuildTextOption(ScriptOption option)
        {
            StackPanel panel = new() { Margin = new Thickness(0, 5, 0, 0) };
            panel.Children.Add(new Label { Content = option.Label, Padding = new Thickness(0) });
            TextBox textBox = new()
            {
                Text = option.DefaultValue as string ?? "",
                AcceptsReturn = option.IsMultiline,
                Height = option.IsMultiline ? 80 : double.NaN,
                VerticalScrollBarVisibility = option.IsMultiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled
            };
            _controls[option.Id] = textBox;
            panel.Children.Add(textBox);
            return panel;
        }

        private FrameworkElement BuildRadioOption(ScriptOption option)
        {
            StackPanel panel = new() { Margin = new Thickness(0, 5, 0, 0) };
            panel.Children.Add(new Label { Content = option.Label, Padding = new Thickness(0) });

            StackPanel radioPanel = new();
            string defaultChoice = option.DefaultValue as string ?? "";

            foreach (string choice in option.Choices ?? [])
            {
                RadioButton radio = new()
                {
                    Content = choice,
                    GroupName = option.Id,
                    IsChecked = choice == defaultChoice,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                radioPanel.Children.Add(radio);
            }

            _controls[option.Id] = radioPanel;
            panel.Children.Add(radioPanel);
            return panel;
        }

        private FrameworkElement BuildDirectoryOption(ScriptOption option)
        {
            StackPanel panel = new() { Margin = new Thickness(0, 5, 0, 0) };
            panel.Children.Add(new Label { Content = option.Label, Padding = new Thickness(0) });

            Grid grid = new();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBox textBox = new()
            {
                Text = option.DefaultValue as string ?? ""
            };

            ButtonDark browseButton = new()
            {
                Content = "...",
                Width = 30,
                Margin = new Thickness(5, 0, 0, 0)
            };

            browseButton.Click += (_, _) =>
            {
                VistaFolderBrowserDialog dialog = new();
                bool? result = dialog.ShowDialog(this);
                if (result == true)
                    textBox.Text = dialog.SelectedPath;
            };

            Grid.SetColumn(textBox, 0);
            Grid.SetColumn(browseButton, 1);
            grid.Children.Add(textBox);
            grid.Children.Add(browseButton);

            _controls[option.Id] = textBox;
            panel.Children.Add(grid);
            return panel;
        }

        public Dictionary<string, object> GetResults()
        {
            Dictionary<string, object> results = new();

            foreach (ScriptOption option in _builder.Options)
            {
                if (!_controls.TryGetValue(option.Id, out object control))
                    continue;

                results[option.Id] = option.Type switch
                {
                    ScriptOptionType.Bool => (control as CheckBox)?.IsChecked == true,
                    ScriptOptionType.Text or ScriptOptionType.Directory => (control as TextBox)?.Text ?? "",
                    ScriptOptionType.Radio => GetSelectedRadio(control as StackPanel),
                    _ => null
                };
            }

            return results;
        }

        private static string GetSelectedRadio(StackPanel panel)
        {
            if (panel is null)
                return "";

            foreach (object child in panel.Children)
            {
                if (child is RadioButton radio && radio.IsChecked == true)
                    return radio.Content as string ?? "";
            }
            return "";
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
