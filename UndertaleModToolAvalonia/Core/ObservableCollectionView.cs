using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Threading;

namespace UndertaleModToolAvalonia;

public abstract class ObservableCollectionView
{
    public abstract void SetFilter(Predicate<object?>? filter);
    public abstract void SetSort(Comparison<object?>? sort);
}

/// <summary>
/// This class allows you to filter and transform an input observable collection, providing an output observable collection, which will be kept in sync with the input.
/// </summary>
/// <typeparam name="TInput">Type of item in input collection.</typeparam>
/// <typeparam name="TOutput">Type of item in output collection.</typeparam>
public class ObservableCollectionView<TInput, TOutput> : ObservableCollectionView
    where TInput : class?
    where TOutput : class?
{
    public class CustomObservableCollection<T> : IList, IList<T>, INotifyCollectionChanged
        where T : class?
    {
        public readonly record struct IndexValue(int Index, T Value);

        readonly List<IndexValue> internalList = new();

        bool isNotSendingEvents = false;
        bool isDelayingEvents = false;
        readonly List<NotifyCollectionChangedEventArgs> delayedEvents = [];

        public void StartDelayingEvents()
        {
            isDelayingEvents = true;
        }

        public void FinishDelayingEvents()
        {
            isDelayingEvents = false;

            // HACK: Don't you love magic numbers?
            if (delayedEvents.Count > 100)
            {
                SendReset();
            }
            else
            {
                foreach (NotifyCollectionChangedEventArgs e in delayedEvents)
                {
                    if (CollectionChanged is not null)
                        CollectionChanged(this, e);
                }
            }

            delayedEvents.Clear();
        }

        public void StartNoEvents()
        {
            isNotSendingEvents = true;
        }

        public void FinishNoEvents()
        {
            isNotSendingEvents = false;
            SendReset();
        }

        public IndexValue this[int index]
        {
            get => internalList[index];
            set
            {
                IndexValue oldItem = internalList[index];

                internalList[index] = value;
                SendEvent(new(NotifyCollectionChangedAction.Replace, value.Value, oldItem.Value, index));
            }
        }

        public void SetIndex(int index, int itemIndex)
        {
            internalList[index] = internalList[index] with { Index = itemIndex };
        }

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

        void SendReset()
        {
            if (CollectionChanged is not null)
                CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
        }

        void SendEvent(NotifyCollectionChangedEventArgs e)
        {
            if (isNotSendingEvents)
                return;
            if (isDelayingEvents)
                delayedEvents.Add(e);
            else if (CollectionChanged is not null)
            {
                Dispatcher.UIThread.Invoke(() => CollectionChanged(this, e));
            }
        }

        // INotifyCollectionChanged
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        // IList
        public int Count => internalList.Count;
        public bool IsReadOnly => true;
        public bool IsFixedSize => ((IList)internalList).IsFixedSize;
        public bool IsSynchronized => false;
        public object SyncRoot => ((IList)internalList).SyncRoot;

        object? IList.this[int index]
        {
            get => internalList[index].Value;
            set => throw new NotSupportedException();
        }
        T IList<T>.this[int index]
        {
            get => internalList[index].Value;
            set => throw new NotSupportedException();
        }

        public int IndexOf(object? value) => internalList.FindIndex(x => x.Value == value);
        public int IndexOf(T item) => IndexOf((object?)item);
        public bool Contains(object? value) => internalList.FindIndex(x => x.Value == value) != -1;
        public bool Contains(T item) => Contains((object?)item);
        public void CopyTo(Array array, int index)
        {
            T[] newArray = [.. internalList.Select(x => x.Value)];
            Array.Copy(newArray, 0, array, index, newArray.Length);
        }
        public void CopyTo(T[] array, int arrayIndex) => CopyTo((Array)array, arrayIndex);
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            foreach (var item in internalList)
            {
                yield return item.Value;
            }
        }

        int IList.Add(object? value) => throw new NotSupportedException();
        void ICollection<T>.Add(T item) => throw new NotSupportedException();
        void IList.Insert(int index, object? value) => throw new NotSupportedException();
        void IList<T>.Insert(int index, T item) => throw new NotSupportedException();
        void IList.RemoveAt(int index) => throw new NotSupportedException();
        void IList<T>.RemoveAt(int index) => throw new NotSupportedException();
        void IList.Remove(object? value) => throw new NotSupportedException();
        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
        void IList.Clear() => throw new NotSupportedException();
        void ICollection<T>.Clear() => throw new NotSupportedException();
    }

    public CustomObservableCollection<TOutput> Output { get; } = [];

    readonly IList<TInput> input;
    readonly Func<TInput, TOutput>? transformFunc;
    Predicate<TOutput>? filterPredicate;
    Comparison<TOutput>? sortComparision;

    readonly Comparer<CustomObservableCollection<TOutput>.IndexValue> sortComparer;

    public ObservableCollectionView(IList<TInput> input, Func<TInput, TOutput>? transform = null, Predicate<TOutput>? filter = null, Comparison<TOutput>? sort = null)
    {
        this.input = input;
        this.filterPredicate = filter;
        this.transformFunc = transform;
        this.sortComparision = sort;

        if (input is INotifyCollectionChanged inputNotifyCollectionChanged)
            inputNotifyCollectionChanged.CollectionChanged += OnInputCollectionChanged;
        else
            throw new InvalidOperationException($"ObservableCollectionView input ({input}) does not implement INotifyCollectionChanged");

        sortComparer = Comparer<CustomObservableCollection<TOutput>.IndexValue>.Create((x, y) =>
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

    public void SetFilter(Predicate<TOutput>? filter)
    {
        filterPredicate = filter;
        Filter();
    }
    public override void SetFilter(Predicate<object?>? _filterPredicate) => SetFilter((Predicate<TOutput>?)_filterPredicate);

    public void SetSort(Comparison<TOutput>? sort)
    {
        sortComparision = sort;
        Sort();
    }
    public override void SetSort(Comparison<object?>? sort) => SetSort((Comparison<TOutput>?)sort);

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
        Output.IncreaseAllIndexesIfGreaterOrEqualThan(e.NewItems.Count, e.NewStartingIndex);

        TInput item = (TInput)e.NewItems[0]!;
        TOutput transformedItem = TransformItem(item);

        if (DoesPassFilter(transformedItem))
        {
            // TODO: Because sorting can change, this may not have the correct index
            // Find where in output to insert
            int i = Output.BinarySearchIndexValue(new(e.NewStartingIndex, transformedItem), sortComparer);

            if (i < 0)
                i = ~i;

            Output.InsertIndexValue(i, new(e.NewStartingIndex, transformedItem));
        }
    }

    void OnInputRemove(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems!.Count != 1)
            throw new InvalidOperationException("Modifying multiple items is not supported");

        // Find where in output to remove
        int i = Output.IndexOfIndex(e.OldStartingIndex);
        if (i != -1)
        {
            Output.RemoveAtIndexValue(i);
        }

        Output.IncreaseAllIndexesIfGreaterOrEqualThan(-e.OldItems.Count, e.OldStartingIndex + e.OldItems.Count);
    }

    void OnInputReplace(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems!.Count != 1 || e.NewItems!.Count != 1)
            throw new InvalidOperationException("Modifying multiple items is not supported");

        TInput item = (TInput)e.NewItems[0]!;
        TOutput transformedItem = TransformItem(item);

        bool passes = DoesPassFilter(transformedItem);

        // Find where in output to remove
        int i = Output.IndexOfIndex(e.OldStartingIndex);
        if (i != -1)
        {
            Output.RemoveAtIndexValue(i);
        }

        // If it passes, find where in output to insert
        if (passes)
        {
            // TODO: Because sorting can change, this may not have the correct index
            i = Output.BinarySearchIndexValue(new(e.OldStartingIndex, transformedItem), sortComparer);

            if (i < 0)
                i = ~i;

            Output.InsertIndexValue(i, new(e.OldStartingIndex, transformedItem));
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
        Output.SortIndexValue(sortComparer);
    }

    void Reset()
    {
        Output.StartNoEvents();

        Output.ClearIndexValue();

        for (int inputIndex = 0; inputIndex < input.Count; inputIndex++)
        {
            TInput item = input[inputIndex];
            TOutput transformedItem = TransformItem(item);

            if (DoesPassFilter(transformedItem))
            {
                Output.AddIndexValue(new(inputIndex, transformedItem));
            }
        }

        Output.SortIndexValue(sortComparer);

        Output.FinishNoEvents();
    }

    bool DoesPassFilter(TOutput item) => filterPredicate is null || filterPredicate(item);

    TOutput TransformItem(TInput item)
    {
        if (transformFunc is not null)
            return transformFunc(item);

        if (item is TOutput itemAsTOutput)
            return itemAsTOutput;

        throw new InvalidOperationException("Input and output types are different without a transform function");
    }
}