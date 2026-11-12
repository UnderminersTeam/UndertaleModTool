using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Threading;

namespace UndertaleModToolAvalonia;

/// <summary>
/// This class allows you to filter, sort and transform an input observable collection, providing itself as an observable collection, which will be kept in sync with the input.
/// </summary>
public class ObservableCollectionView : IList, INotifyCollectionChanged
{
    public readonly record struct IndexValue(int Index, object? Value);

    protected readonly List<IndexValue> internalList = new();

    readonly IList input;
    readonly Func<object?, object?>? transformFunc;
    Predicate<object?>? filterPredicate;
    Comparison<object?>? sortComparision;

    readonly Comparer<IndexValue> sortComparer;

    public ObservableCollectionView(IList input, Func<object?, object?>? transform = null, Predicate<object?>? filter = null, Comparison<object?>? sort = null)
    {
        this.input = input;
        this.filterPredicate = filter;
        this.transformFunc = transform;
        this.sortComparision = sort;

        if (input is INotifyCollectionChanged inputNotifyCollectionChanged)
            inputNotifyCollectionChanged.CollectionChanged += OnInputCollectionChanged;
        else
            throw new InvalidOperationException($"ObservableCollectionView input ({input}) does not implement INotifyCollectionChanged");

        sortComparer = Comparer<IndexValue>.Create((x, y) =>
        {
            int r = 0;
            if (this.sortComparision is not null)
                r = this.sortComparision(x.Value, y.Value);
            if (r == 0)
                r = x.Index.CompareTo(y.Index);
            return r;
        });

        Reset();
    }

    public void SetFilter(Predicate<object?>? filter)
    {
        filterPredicate = filter;
        Filter();
    }

    public void SetSort(Comparison<object?>? sort)
    {
        sortComparision = sort;
        Sort();
    }

    void OnInputCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                OnInputAdd(e);
                break;

            case NotifyCollectionChangedAction.Remove:
                OnInputRemove(e);
                break;

            case NotifyCollectionChangedAction.Replace:
                OnInputReplace(e);
                break;

            case NotifyCollectionChangedAction.Move:
                OnInputMove(e);
                break;

            case NotifyCollectionChangedAction.Reset:
                OnInputReset();
                break;
        }
    }

    void OnInputAdd(NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems!.Count != 1)
            throw new InvalidOperationException("Modifying multiple items is not supported");

        // Increase all indexes greater than inserted input indexes
        IncreaseAllIndexesIfGreaterOrEqualThan(e.NewItems.Count, e.NewStartingIndex);

        object? item = e.NewItems[0]!;
        object? transformedItem = TransformItem(item);

        if (DoesPassFilter(transformedItem))
        {
            // TODO: Because sorting can change, this may not have the correct index
            // Find where in output to insert
            int i = BinarySearchIndexValue(new(e.NewStartingIndex, transformedItem), sortComparer);

            if (i < 0)
                i = ~i;

            InsertIndexValue(i, new(e.NewStartingIndex, transformedItem));
        }
    }

    void OnInputRemove(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems!.Count != 1)
            throw new InvalidOperationException("Modifying multiple items is not supported");

        // Find where in output to remove
        int i = IndexOfIndex(e.OldStartingIndex);
        if (i != -1)
        {
            RemoveAtIndexValue(i);
        }

        IncreaseAllIndexesIfGreaterOrEqualThan(-e.OldItems.Count, e.OldStartingIndex + e.OldItems.Count);
    }

    void OnInputReplace(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems!.Count != 1 || e.NewItems!.Count != 1)
            throw new InvalidOperationException("Modifying multiple items is not supported");

        object? item = e.NewItems[0]!;
        object? transformedItem = TransformItem(item);

        bool passes = DoesPassFilter(transformedItem);

        // Find where in output to remove
        int i = IndexOfIndex(e.OldStartingIndex);
        if (i != -1)
        {
            RemoveAtIndexValue(i);
        }

        // If it passes, find where in output to insert
        if (passes)
        {
            // TODO: Because sorting can change, this may not have the correct index
            i = BinarySearchIndexValue(new(e.OldStartingIndex, transformedItem), sortComparer);

            if (i < 0)
                i = ~i;

            InsertIndexValue(i, new(e.OldStartingIndex, transformedItem));
        }
    }

    void OnInputMove(NotifyCollectionChangedEventArgs e)
    {
        // TODO: Actually call Move().
        OnInputRemove(e);
        OnInputAdd(e);
    }

    void OnInputReset()
    {
        Reset();
    }

    void Filter()
    {
        // TODO: Maybe not do this?
        Reset();
    }

    void Sort()
    {
        SortIndexValue(sortComparer);
    }

    void Reset()
    {
        internalList.Clear();

        for (int inputIndex = 0; inputIndex < input.Count; inputIndex++)
        {
            object? item = input[inputIndex];
            object? transformedItem = TransformItem(item);

            if (DoesPassFilter(transformedItem))
            {
                internalList.Add(new(inputIndex, transformedItem));
            }
        }

        SortIndexValue(sortComparer);
    }

    bool DoesPassFilter(object? item) => filterPredicate is null || filterPredicate(item);

    object? TransformItem(object? item)
    {
        if (transformFunc is not null)
            return transformFunc(item);

        return item;
    }

    #region internalList methods

    public void IncreaseAllIndexesIfGreaterOrEqualThan(int increment, int ifGreaterOrEqualThan)
    {
        for (int i = 0; i < internalList.Count; i++)
        {
            IndexValue item = internalList[i];
            if (item.Index >= ifGreaterOrEqualThan)
            {
                internalList[i] = internalList[i] with { Index = item.Index + increment };
            }
        }
    }

    public void AddIndexValue(IndexValue item)
    {
        internalList.Add(item);
        SendEvent(new(NotifyCollectionChangedAction.Add, item.Value, internalList.Count - 1));
    }

    public void InsertIndexValue(int index, IndexValue item)
    {
        internalList.Insert(index, item);
        SendEvent(new(NotifyCollectionChangedAction.Add, item.Value, index));
    }

    public void RemoveAtIndexValue(int index)
    {
        IndexValue item = internalList[index];

        internalList.RemoveAt(index);
        SendEvent(new(NotifyCollectionChangedAction.Remove, item.Value, index));
    }

    public void ClearIndexValue()
    {
        internalList.Clear();
        SendEvent(new(NotifyCollectionChangedAction.Reset));
    }

    public void MoveIndexValue(int oldIndex, int newIndex)
    {
        IndexValue removedItem = internalList[oldIndex];
        internalList.RemoveAt(oldIndex);
        internalList.Insert(newIndex, removedItem);

        SendEvent(new(NotifyCollectionChangedAction.Move, removedItem.Value, newIndex, oldIndex));
    }

    public int BinarySearchIndexValue(IndexValue item, IComparer<IndexValue> comparer)
    {
        return internalList.BinarySearch(item, comparer);
    }

    public void SortIndexValue(IComparer<IndexValue> comparer)
    {
        internalList.Sort(comparer);
        SendEvent(new(NotifyCollectionChangedAction.Reset));
    }

    public int IndexOfIndex(int itemIndex)
    {
        return internalList.FindIndex(x => x.Index == itemIndex);
    }

    void SendEvent(NotifyCollectionChangedEventArgs e)
    {
        if (CollectionChanged is not null)
        {
            Dispatcher.UIThread.Invoke(() => CollectionChanged(this, e));
        }
    }

    #endregion

    #region INotifyCollectionChanged and IList

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int Count => internalList.Count;
    public bool IsReadOnly => true;
    public bool IsFixedSize => false;
    public bool IsSynchronized => false;
    public object SyncRoot => this;

    object? IList.this[int index]
    {
        get => internalList[index].Value;
        set => throw new NotSupportedException();
    }

    public int IndexOf(object? value) => internalList.FindIndex(x => x.Value == value);
    public bool Contains(object? value) => internalList.FindIndex(x => x.Value == value) != -1;
    public void CopyTo(Array array, int index)
    {
        object?[] newArray = [.. internalList.Select(x => x.Value)];
        Array.Copy(newArray, 0, array, index, newArray.Length);
    }
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<object?>)this).GetEnumerator();

    int IList.Add(object? value) => throw new NotSupportedException();
    void IList.Insert(int index, object? value) => throw new NotSupportedException();
    void IList.RemoveAt(int index) => throw new NotSupportedException();
    void IList.Remove(object? value) => throw new NotSupportedException();
    void IList.Clear() => throw new NotSupportedException();

    #endregion
}

/// <summary>
/// <inheritdoc />
/// </summary>
/// <typeparam name="TOutput">Type of item in output collection.</typeparam>
public class ObservableCollectionView<TOutput> : ObservableCollectionView, IList<TOutput>
    where TOutput : class?
{
    public ObservableCollectionView(IList input, Func<object?, TOutput>? transform = null, Predicate<TOutput>? filter = null, Comparison<TOutput>? sort = null) : base
        (
            input,
            // transform is null ? null : a => transform(a),
            transform,
            filter is null ? null : a => filter((TOutput)a!),
            (Comparison<object?>?)sort
        )
    {
        //
    }

    #region IList<TOutput>

    TOutput IList<TOutput>.this[int index]
    {
        get => (TOutput)internalList[index].Value!;
        set => throw new NotSupportedException();
    }

    public int IndexOf(TOutput item) => IndexOf((object?)item);
    public bool Contains(TOutput item) => Contains((object?)item);
    public void CopyTo(TOutput[] array, int arrayIndex) => CopyTo((Array)array, arrayIndex);
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<TOutput>)this).GetEnumerator();
    IEnumerator<TOutput> IEnumerable<TOutput>.GetEnumerator()
    {
        foreach (var item in internalList)
        {
            yield return (TOutput)item.Value!;
        }
    }

    void ICollection<TOutput>.Add(TOutput item) => throw new NotSupportedException();
    void IList<TOutput>.Insert(int index, TOutput item) => throw new NotSupportedException();
    void IList<TOutput>.RemoveAt(int index) => throw new NotSupportedException();
    bool ICollection<TOutput>.Remove(TOutput item) => throw new NotSupportedException();
    void ICollection<TOutput>.Clear() => throw new NotSupportedException();

    public void SetFilter(Predicate<TOutput>? filter) => SetFilter((Predicate<object?>?)filter);

    public void SetSort(Comparison<TOutput>? sort) => SetSort((Comparison<object?>?)sort);

    #endregion
}

/// <summary>
/// <inheritdoc />
/// </summary>
/// <typeparam name="TInput">Type of item in input collection.</typeparam>
/// <typeparam name="TOutput">Type of item in output collection.</typeparam>
public class ObservableCollectionView<TInput, TOutput> : ObservableCollectionView<TOutput>
    where TInput : class?
    where TOutput : class?
{
    public ObservableCollectionView(IList<TInput> input, Func<TInput, TOutput>? transform = null, Predicate<TOutput>? filter = null, Comparison<TOutput>? sort = null) : base
        (
            (IList)input,
            transform is null ? null : a => transform((TInput)a!),
            filter is null ? null : a => filter((TOutput)a!),
            (Comparison<object?>?)sort
        )
    {
        //
    }
}