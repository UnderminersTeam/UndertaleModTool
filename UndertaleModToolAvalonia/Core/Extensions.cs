using System.Threading.Tasks;
using Avalonia.Threading;

namespace UndertaleModToolAvalonia;

public static class Extensions
{
    /// <summary>
    /// Waits on a Task without blocking the main thread.
    /// </summary>
    public static T WaitOnDispatcherFrame<T>(this Task<T> task)
    {
        if (!task.IsCompleted)
        {
            DispatcherFrame frame = new();
            _ = task.ContinueWith(static (_, s) => ((DispatcherFrame)s!).Continue = false, frame);
            Dispatcher.UIThread.PushFrame(frame);
        }

        return task.GetAwaiter().GetResult();
    }
}
