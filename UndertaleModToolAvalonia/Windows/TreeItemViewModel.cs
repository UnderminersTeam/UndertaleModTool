using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using PropertyChanged.SourceGenerator;

namespace UndertaleModToolAvalonia;

public partial class TreeItemViewModel
{
    public DataTreeView DataTreeView;
    public int Level { get; set; }
    public object? Value { get; set; }
    public object? Header { get; set; }
    public object? Tag { get; set; }

    [Notify]
    private int _Index = 0;

    [Notify]
    private object? _Source;

    [Notify]
    private bool _IsExpanded = false;
    public string ExpanderIcon => IsExpanded ? "-" : "+";
    public bool IsExpanderVisible => Source is IList { Count: > 0 };

    private object? internalSource = null;
    private List<TreeItemViewModel> children = [];

    private Func<object?, bool>? filter;

    public TreeItemViewModel(DataTreeView dataTreeView, int level = 0,
        object? value = null, object? header = null, object? tag = null, object? source = null)
    {
        DataTreeView = dataTreeView;
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
        int index = DataTreeView.TreeSource.IndexOf(this) + 1;

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

            DataTreeView.TreeSource.RemoveAt(index + i + startingIndex);
            children.RemoveAt(i + startingIndex);
        }

        // Update previously existing indexes
        foreach (TreeItemViewModel treeItem in children)
        {
            if (treeItem.Index >= startingIndex)
            {
                treeItem.Index -= list.Count;
            }
        }
    }

    void AddList(IList list, int startingIndex)
    {
        // Update previously existing indexes
        foreach (TreeItemViewModel treeItem in children)
        {
            if (treeItem.Index >= startingIndex)
            {
                treeItem.Index += list.Count;
            }
        }

        int index = DataTreeView.TreeSource.IndexOf(this) + 1;

        for (int i = 0; i < list.Count; i++)
        {
            TreeItemViewModel item = MakeItem(list[i], i + startingIndex);

            DataTreeView.TreeSource.Insert(index + i + startingIndex, item);
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

        // TODO: Filter while adding to avoid immediate removal
        ApplyFilter();
    }

    public void SetFilter(Func<object?, bool>? filter)
    {
        this.filter = filter;

        ApplyFilter();
    }

    void ApplyFilter()
    {
        if (filter is null)
            return;
        if (!IsExpanded)
            return;

        int treeIndex = DataTreeView.TreeSource.IndexOf(this) + 1;

        // Remove items
        DataTreeView.DataListBox.SelectedItem = null;

        for (int i = children.Count - 1; i >= 0; i--)
        {
            if (filter(children[i].Value) == false)
            {
                DataTreeView.TreeSource.RemoveAt(treeIndex + i);
                children.RemoveAt(i);
            }
        }

        // Add items
        if (internalSource is IList _sourceList)
        {
            int childIndex = 0;

            for (int i = 0; i < _sourceList.Count; i++)
            {
                if (filter(_sourceList[i]) == true)
                {
                    TreeItemViewModel item = MakeItem(_sourceList[i], i);

                    bool shouldAdd = true;
                    while (childIndex < children.Count)
                    {
                        if (children[childIndex].Index == i)
                        {
                            shouldAdd = false;
                            break;
                        }
                        else if (children[childIndex].Index > i)
                        {
                            break;
                        }
                        else
                        {
                            childIndex++;
                        }
                    }

                    if (shouldAdd)
                    {
                        DataTreeView.TreeSource.Insert(treeIndex + childIndex, item);
                        children.Insert(childIndex, item);
                    }

                    childIndex++;
                }
            }
        }
    }

    TreeItemViewModel MakeItem(object? value, int index)
    {
        TreeItemViewModel item;
        if (value is TreeItemViewModel _item)
            item = _item;
        else
            item = new(DataTreeView, value: value);

        item.Level = Level + 1;
        item.Index = index;
        return item;
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