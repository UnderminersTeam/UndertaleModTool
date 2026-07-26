using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace UndertaleModToolAvalonia;

public static class Extensions
{
    /// <summary>
    /// Waits on a <see cref="Task{TResult}"/> without blocking the main thread.
    /// </summary>
    public static TResult WaitOnDispatcherFrame<TResult>(this Task<TResult> task)
    {
        if (!task.IsCompleted)
        {
            DispatcherFrame frame = new();
            _ = task.ContinueWith(static (_, s) => ((DispatcherFrame)s!).Continue = false, frame);
            Dispatcher.UIThread.PushFrame(frame);
        }

        return task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Waits on a <see cref="Task"/> without blocking the main thread.
    /// </summary>
    public static void WaitOnDispatcherFrame(this Task task)
    {
        if (!task.IsCompleted)
        {
            DispatcherFrame frame = new();
            _ = task.ContinueWith(static (_, s) => ((DispatcherFrame)s!).Continue = false, frame);
            Dispatcher.UIThread.PushFrame(frame);
        }

        task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Returns the SolidColorBrush resource in the key. Throws if key is invalid.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public static SolidColorBrush GetSolidColorBrushResource(this StyledElement styledElement, string key)
    {
        if (styledElement.TryFindResource(key, styledElement.ActualThemeVariant, out object? resource))
        {
            if (resource is SolidColorBrush brush)
                return brush;
        }
        throw new InvalidOperationException($"Key {key} is not a valid resource");
    }
}
