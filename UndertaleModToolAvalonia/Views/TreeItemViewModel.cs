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
            // Index of first child
            int index = TreeSource.IndexOf(this) + 1;

            for (int i = children.Count - 1; i >= 0; i--)
            {
                TreeItemViewModel child = children[i];

                if (child.Source is INotifyCollectionChanged _sourceNotifyCollectionChanged)
                    _sourceNotifyCollectionChanged.CollectionChanged -= Source_CollectionChanged;

                if (child.IsExpanded)
                    child.RemoveChildren();

                TreeSource.RemoveAt(index + i);
            }

            children.Clear();
        }
    }

    public void AddChildren()
    {
        if (internalSource is IList _sourceList)
        {
            // Find own position in list.
            int index = TreeSource.IndexOf(this);

            foreach (var obj in _sourceList)
            {
                // One below previous one.
                index++;
                // Add to list at the proper position.
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

                TreeSource.Insert(index, item);
                children.Add(item);
            }

            foreach (var item in children)
            {
                // item.UpdateSource();
                if (item.Source is INotifyCollectionChanged _sourceNotifyCollectionChanged)
                    _sourceNotifyCollectionChanged.CollectionChanged += Source_CollectionChanged;

                if (item.IsExpanded)
                    item.AddChildren();
            }
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

    private void Source_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // TODO: Check if this works when moving
        if (IsExpanded)
        {
            if (e.OldItems is not null)
            {
                // Index doesn't change because items move up
                int index = TreeSource.IndexOf(this) + 1;
                foreach (object? obj in e.OldItems)
                {
                    TreeSource.RemoveAt(index + e.OldStartingIndex);
                    children.RemoveAt(e.OldStartingIndex);
                }
            }
            if (e.NewItems is not null)
            {
                int index = TreeSource.IndexOf(this) + 1;
                foreach (object? obj in e.NewItems)
                {
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

                    TreeSource.Insert(index + e.NewStartingIndex, item);
                    children.Insert(e.NewStartingIndex, item);
                    index++;
                }

                foreach (var item in children)
                {
                    item.UpdateSource();
                }
            }
        }

        // TODO: Check if this is the correct way of doing it, I just copied from the generated code lol
        this.OnPropertyChanged(global::PropertyChanged.SourceGenerator.Internal.EventArgsCache.PropertyChanged_IsExpanderVisible);
    }
}