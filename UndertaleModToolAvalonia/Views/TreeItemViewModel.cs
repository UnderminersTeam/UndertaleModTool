using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged.SourceGenerator;

namespace UndertaleModToolAvalonia.Views;

public partial class TreeItemViewModel
{
    public ObservableCollection<TreeItemViewModel> TreeSource { get; set; }
    public int Level { get; set; }
    public object? Value { get; set; }
    public object? Header { get; set; }
    public object? Tag { get; set; }

    [Notify]
    private object? _Source { get; set; }

    [Notify]
    private bool _IsExpanded = false;
    public string ExpanderIcon => IsExpanded ? "-" : "+";
    public bool IsExpanderVisible => Source is IList { Count: > 0 };

    private object? internalSource = null;
    private List<TreeItemViewModel> children = [];

    public TreeItemViewModel(ObservableCollection<TreeItemViewModel> treeSource, int level = 0,
        object? value = null, object? header = null, object? tag = null, object? source = null)
    {
        TreeSource = treeSource;
        Level = level;
        Value = value;
        Header = header ?? value;
        Tag = tag;
        Source = source;
    }

    public void RemoveChildren()
    {
        if (internalSource is not null)
        {
            RemoveList(children, 0);
        }
    }

    public void AddChildren()
    {
        if (internalSource is IList _sourceList)
        {
            AddList(_sourceList, 0);
        }
    }

    public void UpdateSource()
    {
        if (internalSource != Source)
        {
            if (internalSource is not null)
            {
                if (internalSource is INotifyCollectionChanged _sourceNotifyCollectionChanged)
                    _sourceNotifyCollectionChanged.CollectionChanged -= Source_CollectionChanged;

                if (IsExpanded)
                    RemoveChildren();
            }

            internalSource = Source;

            if (internalSource is not null)
            {
                if (internalSource is INotifyCollectionChanged _sourceNotifyCollectionChanged)
                    _sourceNotifyCollectionChanged.CollectionChanged += Source_CollectionChanged;

                if (IsExpanded)
                    AddChildren();
            }
        }

        foreach (var child in children)
        {
            child.UpdateSource();
        }
    }

    public void ExpandCollapse()
    {
        IsExpanded = !IsExpanded;

        if (IsExpanded)
        {
            AddChildren();
        }
        else
        {
            RemoveChildren();
        }
    }

    void RemoveList(IList list, int startingIndex)
    {
        int index = TreeSource.IndexOf(this) + 1;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            object? obj = list[i];

            if (obj is TreeItemViewModel treeItem)
            {
                if (treeItem.Source is INotifyCollectionChanged sourceNotifyCollectionChanged)
                    sourceNotifyCollectionChanged.CollectionChanged -= treeItem.Source_CollectionChanged;

                if (treeItem.IsExpanded)
                    treeItem.RemoveChildren();
            }
        }

        for (int i = list.Count - 1; i >= 0; i--)
        {
            object? obj = list[i];

            TreeSource.RemoveAt(index + i + startingIndex);
            children.RemoveAt(i + startingIndex);
        }
    }

    void AddList(IList list, int startingIndex)
    {
        int index = TreeSource.IndexOf(this) + 1;

        for (int i = 0; i < list.Count; i++)
        {
            object? obj = list[i];

            TreeItemViewModel item;
            if (obj is TreeItemViewModel _item)
            {
                item = _item;
                item.Level = Level + 1;
            }
            else
            {
                item = new(TreeSource, Level + 1, obj);
            }

            TreeSource.Insert(index + i + startingIndex, item);
            children.Insert(i + startingIndex, item);
        }

        foreach (TreeItemViewModel treeItem in children)
        {
            treeItem.UpdateSource();

            if (treeItem.Source is INotifyCollectionChanged sourceNotifyCollectionChanged)
                sourceNotifyCollectionChanged.CollectionChanged += treeItem.Source_CollectionChanged;

            if (treeItem.IsExpanded)
                treeItem.AddChildren();
        }
    }

    private void Source_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // TODO: Check if this works when moving
        if (IsExpanded)
        {
            if (e.OldItems is not null)
            {
                RemoveList(e.OldItems, e.OldStartingIndex);
            }
            if (e.NewItems is not null)
            {
                AddList(e.NewItems, e.NewStartingIndex);
            }
        }

        // TODO: Check if this is the correct way of doing it, I just copied from the generated code lol
        this.OnPropertyChanged(global::PropertyChanged.SourceGenerator.Internal.EventArgsCache.PropertyChanged_IsExpanderVisible);
    }
}