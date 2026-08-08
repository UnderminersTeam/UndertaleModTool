using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace UndertaleModToolAvalonia;

/// <summary>
/// Automatically sets Grid.Row and Grid.Column on every child of the attached Grid, in order, according to ColumnDefinitions.
/// If RowDefinitions is empty, set it to a list of Autos that matches the amount of rows.
/// </summary>
public class AutoGridBehavior : AvaloniaObject
{
    public static readonly AttachedProperty<bool> EnableProperty = AvaloniaProperty.RegisterAttached<AutoGridBehavior, Grid, bool>(
        "Enable", false, false, BindingMode.OneTime);

    static AutoGridBehavior()
    {
        EnableProperty.Changed.AddClassHandler<Grid>(HandleEnableChanged);
    }

    private static void HandleEnableChanged(Grid grid, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is not null && (bool)args.NewValue)
        {
            grid.Initialized += (object? sender, EventArgs _) =>
            {
                int totalColumns = grid.ColumnDefinitions.Count;

                int currColumn = 0;
                int currRow = 0;

                for (int i = 0; i < grid.Children.Count; i++)
                {
                    Grid.SetColumn(grid.Children[i], currColumn);
                    Grid.SetRow(grid.Children[i], currRow);

                    currColumn++;
                    if (currColumn >= totalColumns)
                    {
                        currColumn = 0;
                        currRow++;
                    }
                }

                if (grid.RowDefinitions.Count == 0)
                {
                    if (currColumn != 0)
                        currRow++;

                    for (int i = 0; i < currRow; i++)
                        grid.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));
                }
            };
        }
    }

    public static void SetEnable(AvaloniaObject element, bool enableValue)
    {
        element.SetValue(EnableProperty, enableValue);
    }

    public static bool GetEnable(AvaloniaObject element)
    {
        return element.GetValue(EnableProperty);
    }
}