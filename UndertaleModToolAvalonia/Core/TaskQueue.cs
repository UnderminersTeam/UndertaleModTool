using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia;

/// <summary>
/// Executes tasks in a queue ordering. Not thread safe.
/// </summary>
public class TaskQueue
{
    public int Count => queue.Count;

    readonly Queue<TaskQueueItem> queue = new();

    public Task Add(Func<Task> taskFunc)
    {
        var taskQueueItem = new TaskQueueItemNonGeneric(taskFunc, new TaskCompletionSource());

        if (queue.Count == 0)
        {
            queue.Enqueue(taskQueueItem);
            Process();
        }
        else
        {
            queue.Enqueue(taskQueueItem);
        }

        return taskQueueItem.TaskCompletionSource.Task;
    }

    public Task<T> Add<T>(Func<Task<T>> taskFunc)
    {
        var taskQueueItem = new TaskQueueItemGeneric<T>(taskFunc, new TaskCompletionSource<T>());

        if (queue.Count == 0)
        {
            queue.Enqueue(taskQueueItem);
            Process();
        }
        else
        {
            queue.Enqueue(taskQueueItem);
        }

        return taskQueueItem.TaskCompletionSource.Task;
    }

    async void Process()
    {
        while (queue.Count > 0)
        {
            TaskQueueItem taskQueueItem = queue.Peek();
            try
            {
                Task task = taskQueueItem.TaskFunc();
                await task;
                queue.Dequeue();
                taskQueueItem.SetTaskCompletionSourceFromTask(task);
            }
            catch (Exception ex)
            {
                queue.Dequeue();
                taskQueueItem.SetTaskCompletionSourceException(ex);
            }
        }
    }

    abstract class TaskQueueItem(Func<Task> taskFunc)
    {
        public readonly Func<Task> TaskFunc = taskFunc;
        public abstract void SetTaskCompletionSourceFromTask(Task task);
        public abstract void SetTaskCompletionSourceException(Exception ex);
    }

    class TaskQueueItemNonGeneric(Func<Task> taskFunc, TaskCompletionSource taskCompletionSource) : TaskQueueItem(taskFunc)
    {
        public readonly TaskCompletionSource TaskCompletionSource = taskCompletionSource;
        public override void SetTaskCompletionSourceFromTask(Task task) => TaskCompletionSource.SetFromTask(task);
        public override void SetTaskCompletionSourceException(Exception ex) => TaskCompletionSource.SetException(ex);
    }

    class TaskQueueItemGeneric<T>(Func<Task> taskFunc, TaskCompletionSource<T> taskCompletionSource) : TaskQueueItem(taskFunc)
    {
        public readonly TaskCompletionSource<T> TaskCompletionSource = taskCompletionSource;
        public override void SetTaskCompletionSourceFromTask(Task task) => TaskCompletionSource.SetFromTask((Task<T>)task);
        public override void SetTaskCompletionSourceException(Exception ex) => TaskCompletionSource.SetException(ex);
    }
}
