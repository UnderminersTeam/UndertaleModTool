using System;
using System.Collections;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace UndertaleModToolAvalonia.Controls;

public partial class EditableDataGrid : UserControl
{
    public static readonly StyledProperty<IList> ItemsSourceProperty = AvaloniaProperty.Register<EditableDataGrid, IList>(
        nameof(ItemsSource));
    public IList ItemsSource
    {
        get { return GetValue(ItemsSourceProperty); }
        set { SetValue(ItemsSourceProperty, value); }
    }

    public static readonly StyledProperty<Func<object>?> ItemFactoryProperty = AvaloniaProperty.Register<EditableDataGrid, Func<object>?>(
        nameof(ItemFactory));
    public Func<object>? ItemFactory
    {
        get { return GetValue(ItemFactoryProperty); }
        set { SetValue(ItemFactoryProperty, value); }
    }

    public ObservableCollection<DataGridColumn> Columns {
        get => dataGrid.Columns;
        set
        {
            dataGrid.Columns.Clear();
            foreach (DataGridColumn column in value)
            {
                dataGrid.Columns.Add(column);
            }
        }
    }

    readonly DataGrid dataGrid;

    public EditableDataGrid()
    {
        InitializeComponent();

        dataGrid = this.Find<DataGrid>("DataGrid")!;
        dataGrid.SelectionChanged += (object? sender, SelectionChangedEventArgs e) =>
        {
            // Hack to make it so a temporary deselection when moving items doesn't stop the repeat button.
            Dispatcher.UIThread.Post(() =>
            {
                this.Find<InputElement>("RemoveButton")!.IsEnabled = (dataGrid.SelectedIndex != -1);
                this.Find<InputElement>("MoveUpButton")!.IsEnabled = (dataGrid.SelectedIndex > 0);
                this.Find<InputElement>("MoveDownButton")!.IsEnabled = (dataGrid.SelectedIndex < ItemsSource.Count - 1);
            });
        };
        dataGrid.GotFocus += (object? sender, GotFocusEventArgs e) =>
        {
            if (e.Source is Control control)
            {
                DataGridRow? row = control.FindLogicalAncestorOfType<DataGridRow>();
                if (row is not null)
                    row.IsSelected = true;
            }
        };
    }

    public bool CanAdd => ItemFactory is not null;

    public void Add()
    {
        if (ItemFactory is not null)
        {
            ItemsSource.Add(ItemFactory());
            dataGrid.SelectedIndex = ItemsSource.Count - 1;
        }
    }

    public void Remove()
    {
        if (dataGrid.SelectedItem is not null)
        {
            int index = dataGrid.SelectedIndex;
            ItemsSource.RemoveAt(index);

            if (index == ItemsSource.Count)
                dataGrid.SelectedIndex = index - 1;
            else
                dataGrid.SelectedIndex = index;
        }
    }

    public void MoveUp()
    {
        if (dataGrid.SelectedItem is not null && dataGrid.SelectedIndex > 0)
        {
            int index = dataGrid.SelectedIndex;
            object? item = ItemsSource[index];
            ItemsSource[index] = ItemsSource[index - 1];
            ItemsSource[index - 1] = item;
            dataGrid.SelectedIndex = index - 1;
        }
    }

    public void MoveDown()
    {
        if (dataGrid.SelectedItem is not null && dataGrid.SelectedIndex < ItemsSource.Count - 1)
        {
            int index = dataGrid.SelectedIndex;
            object? item = ItemsSource[index];
            ItemsSource[index] = ItemsSource[index + 1];
            ItemsSource[index + 1] = item;
            dataGrid.SelectedIndex = index + 1;
        }
    }
}