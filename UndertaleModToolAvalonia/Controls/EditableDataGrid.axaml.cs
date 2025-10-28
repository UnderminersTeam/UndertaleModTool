using System;
using System.Collections;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace UndertaleModToolAvalonia;

public partial class EditableDataGrid : UserControl
{
    public static readonly StyledProperty<IList> ItemsSourceProperty = AvaloniaProperty.Register<EditableDataGrid, IList>(
        nameof(ItemsSource));
    public IList ItemsSource
    {
        get { return GetValue(ItemsSourceProperty); }
        set { SetValue(ItemsSourceProperty, value); }
    }

    public static readonly StyledProperty<Delegate?> ItemFactoryProperty = AvaloniaProperty.Register<EditableDataGrid, Delegate?>(
        nameof(ItemFactory));
    public Delegate? ItemFactory
    {
        get { return GetValue(ItemFactoryProperty); }
        set { SetValue(ItemFactoryProperty, value); }
    }

    public static readonly StyledProperty<Action<object?>?> SelectionChangedProperty = AvaloniaProperty.Register<EditableDataGrid, Action<object?>?>(
        nameof(SelectionChanged));
    public Action<object?>? SelectionChanged
    {
        get { return GetValue(SelectionChangedProperty); }
        set { SetValue(SelectionChangedProperty, value); }
    }

    public static readonly StyledProperty<DataGridHeadersVisibility> HeadersVisibilityProperty = AvaloniaProperty.Register<EditableDataGrid, DataGridHeadersVisibility>(
        nameof(HeadersVisibility), DataGridHeadersVisibility.All);
    public DataGridHeadersVisibility HeadersVisibility
    {
        get { return GetValue(HeadersVisibilityProperty); }
        set { SetValue(HeadersVisibilityProperty, value); }
    }

    public ObservableCollection<DataGridColumn> Columns
    {
        get => DataGridControl.Columns;
        set
        {
            DataGridControl.Columns.Clear();
            foreach (DataGridColumn column in value)
            {
                DataGridControl.Columns.Add(column);
            }
        }
    }

    public EditableDataGrid()
    {
        InitializeComponent();

        DataGridControl.SelectionChanged += (object? sender, SelectionChangedEventArgs e) =>
        {
            // HACK: Hack to make it so a temporary deselection when moving items doesn't stop the repeat button.
            Dispatcher.UIThread.Post(() =>
            {
                RemoveButton.IsEnabled = (DataGridControl.SelectedIndex != -1);
                MoveUpButton.IsEnabled = (DataGridControl.SelectedIndex > 0);
                MoveDownButton.IsEnabled = (DataGridControl.SelectedIndex < ItemsSource.Count - 1);
            });

            SelectionChanged?.Invoke(DataGridControl.SelectedItem);
        };
        DataGridControl.GotFocus += (object? sender, GotFocusEventArgs e) =>
        {
            if (e.Source is Control control)
            {
                DataGridRow? row = control.FindLogicalAncestorOfType<DataGridRow>();
                if (row is not null)
                    row.IsSelected = true;
            }
        };
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemFactoryProperty)
        {
            AddButton.IsVisible = ItemFactory is not null;
            RemoveButton.IsVisible = ItemFactory is not null;
        }
    }

    public void Add()
    {
        object item;
        if (ItemFactory is Func<object> itemFactory)
        {
            item = itemFactory();
        }
        else if (ItemFactory is Func<int, object> itemFactoryWithIndex)
        {
            item = itemFactoryWithIndex(ItemsSource.Count);
        }
        else
        {
            throw new InvalidOperationException();
        }

        ItemsSource.Add(item);
        DataGridControl.SelectedIndex = ItemsSource.Count - 1;
    }

    public void Remove()
    {
        if (DataGridControl.SelectedItem is not null)
        {
            int index = DataGridControl.SelectedIndex;
            ItemsSource.RemoveAt(index);

            if (index == ItemsSource.Count)
                DataGridControl.SelectedIndex = index - 1;
            else
                DataGridControl.SelectedIndex = index;
        }
    }

    public void MoveUp()
    {
        if (DataGridControl.SelectedItem is not null && DataGridControl.SelectedIndex > 0)
        {
            int index = DataGridControl.SelectedIndex;
            object? item = ItemsSource[index];
            ItemsSource[index] = ItemsSource[index - 1];
            ItemsSource[index - 1] = item;
            DataGridControl.SelectedIndex = index - 1;
        }
    }

    public void MoveDown()
    {
        if (DataGridControl.SelectedItem is not null && DataGridControl.SelectedIndex < ItemsSource.Count - 1)
        {
            int index = DataGridControl.SelectedIndex;
            object? item = ItemsSource[index];
            ItemsSource[index] = ItemsSource[index + 1];
            ItemsSource[index + 1] = item;
            DataGridControl.SelectedIndex = index + 1;
        }
    }
}