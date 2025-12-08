using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace UndertaleModToolAvalonia;

public class ObservableCollectionView<TInput, TOutput>
{
    public class CustomObservableCollection<T> : Collection<T>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

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

        public void SendReset()
        {
            if (CollectionChanged is not null)
                CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
        }

        protected override void ClearItems()
        {
            base.ClearItems();

            SendEvent(new(NotifyCollectionChangedAction.Reset));
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);

            SendEvent(new(NotifyCollectionChangedAction.Add, item, index));
        }

        protected override void RemoveItem(int index)
        {
            T removedItem = this[index];
            base.RemoveItem(index);

            SendEvent(new(NotifyCollectionChangedAction.Remove, removedItem, index));
        }

        protected override void SetItem(int index, T item)
        {
            T originalItem = this[index];
            base.SetItem(index, item);

            SendEvent(new(NotifyCollectionChangedAction.Replace, item, originalItem, index));
        }

        public void Move(int oldIndex, int newIndex)
        {
            T removedItem = this[oldIndex];

            base.RemoveItem(oldIndex);
            base.InsertItem(newIndex, removedItem);

            SendEvent(new(NotifyCollectionChangedAction.Move, removedItem, newIndex, oldIndex));
        }

        void SendEvent(NotifyCollectionChangedEventArgs e)
        {
            if (isDelayingEvents)
                delayedEvents.Add(e);
            else if (CollectionChanged is not null)
                CollectionChanged(this, e);
        }
    }

    public CustomObservableCollection<TOutput> Output { get; } = [];

    private readonly ObservableCollection<TInput> input;

    private readonly List<int> outputIndexToInputIndexMap = [];

    private Predicate<TInput>? filterPredicate;

    private readonly Func<TInput, TOutput>? transformFunc;

    public ObservableCollectionView(ObservableCollection<TInput> input, Predicate<TInput>? filter = null, Func<TInput, TOutput>? transform = null)
    {
        this.input = input;
        this.filterPredicate = filter;
        this.transformFunc = transform;

        this.input.CollectionChanged += OnInputCollectionChanged;

        Filter();
    }

    public void SetFilter(Predicate<TInput>? _filterPredicate)
    {
        filterPredicate = _filterPredicate;
        Filter();
    }

    private void OnInputCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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

    private void OnInputAdd(NotifyCollectionChangedEventArgs e)
    {
        // TODO: Binary search
        TInput item = (TInput)e.NewItems![0]!;

        if (DoesPassFilter(item))
        {
            int i = 0;

            // Find where in output to insert item
            while (i < outputIndexToInputIndexMap.Count && outputIndexToInputIndexMap[i] < e.NewStartingIndex)
                i++;

            Output.Insert(i, TransformItem(item));
            outputIndexToInputIndexMap.Insert(i, e.NewStartingIndex);
            i++;

            // Increase all indexes after
            while (i < outputIndexToInputIndexMap.Count)
                outputIndexToInputIndexMap[i]++;
        }
    }

    private void OnInputRemove(NotifyCollectionChangedEventArgs e)
    {
        // TODO: Binary search
        for (int i = 0; i < outputIndexToInputIndexMap.Count; i++)
        {
            if (outputIndexToInputIndexMap[i] == e.OldStartingIndex)
            {
                Output.RemoveAt(i);
                outputIndexToInputIndexMap.RemoveAt(i);
                i--;
            }
            else if (outputIndexToInputIndexMap[i] >= e.OldStartingIndex)
                outputIndexToInputIndexMap[i]--;
        }
    }

    private void OnInputReplace(NotifyCollectionChangedEventArgs e)
    {
        // TODO: Binary search
        TInput item = (TInput)e.NewItems![0]!;
        bool passes = DoesPassFilter(item);

        for (int i = 0; i < outputIndexToInputIndexMap.Count; i++)
        {
            if (passes)
            {
                if (outputIndexToInputIndexMap[i] == e.OldStartingIndex)
                {
                    Output[i] = TransformItem(item);
                    break;
                }
                else if (outputIndexToInputIndexMap[i] > e.OldStartingIndex)
                {
                    Output.Insert(i, TransformItem(item));
                    outputIndexToInputIndexMap.Insert(i, e.OldStartingIndex);
                    break;
                }
            }
            if (!passes)
            {
                if (outputIndexToInputIndexMap[i] == e.OldStartingIndex)
                {
                    Output.RemoveAt(i);
                    outputIndexToInputIndexMap.RemoveAt(i);
                    break;
                }
                else if (outputIndexToInputIndexMap[i] > e.OldStartingIndex)
                    break;
            }
        }
    }

    private void OnInputMove(NotifyCollectionChangedEventArgs e)
    {
        // TODO: Binary search
        int? moveFrom = null;
        int? moveTo = null;

        for (int i = 0; i < outputIndexToInputIndexMap.Count; i++)
        {
            if (outputIndexToInputIndexMap[i] >= e.NewStartingIndex)
                moveTo = i;
            else if (outputIndexToInputIndexMap[i] == e.OldStartingIndex)
                moveFrom = i;

            if (moveFrom is not null && moveTo is not null)
                break;
        }

        if (moveFrom is not null)
        {
            moveTo ??= outputIndexToInputIndexMap.Count;
            Output.Move((int)moveFrom, (int)moveTo);
        }
    }

    private void OnInputReset()
    {
        Output.Clear();
        outputIndexToInputIndexMap.Clear();

        Filter();
    }

    private void Filter()
    {
        // TODO: This can obviously be improved by batch adding and removing everything instead of using the regular RemoveAt and Insert functions.

        Output.StartDelayingEvents();

        // Remove all that don't pass from output.
        for (int i = Output.Count - 1; i >= 0; i--)
        {
            if (!DoesPassFilter(input[outputIndexToInputIndexMap[i]]))
            {
                Output.RemoveAt(i);
                outputIndexToInputIndexMap.RemoveAt(i);
            }
        }

        // Insert all that pass from input to output.
        int outputIndex = 0;
        for (int inputIndex = 0; inputIndex < input.Count; inputIndex++)
        {
            var inputItem = input[inputIndex];

            // Find next output item that matches or passes after the current input index.
            while (outputIndex < outputIndexToInputIndexMap.Count && outputIndexToInputIndexMap[outputIndex] < inputIndex)
            {
                outputIndex++;
            }

            if (outputIndex >= outputIndexToInputIndexMap.Count)
            {
                // If past end of list, then add to end if it passes.
                if (DoesPassFilter(inputItem))
                {
                    TOutput transformedInputItem = TransformItem(inputItem);
                    Output.Add(transformedInputItem);
                    outputIndexToInputIndexMap.Add(inputIndex);
                    outputIndex++;
                }
            }
            else if (outputIndexToInputIndexMap[outputIndex] == inputIndex)
            {
                // If exactly on item, then we know if passes since otherwise it would've been removed before.
                outputIndex++;
            }
            else if (outputIndexToInputIndexMap[outputIndex] > inputIndex)
            {
                // If past item, insert it before that if it passes.
                if (DoesPassFilter(inputItem))
                {
                    TOutput transformedInputItem = TransformItem(inputItem);
                    Output.Insert(outputIndex, transformedInputItem);
                    outputIndexToInputIndexMap.Insert(outputIndex, inputIndex);
                    outputIndex++;
                }
            }
        }

        Output.FinishDelayingEvents();
    }

    private bool DoesPassFilter(TInput item) => filterPredicate is null || filterPredicate(item);

    private TOutput TransformItem(TInput item)
    {
        if (transformFunc is not null)
            return transformFunc(item);

        if (item is TOutput itemAsTOutput)
            return itemAsTOutput;

        throw new InvalidOperationException("Input and output types are different without a transform function");
    }
}