using System;
using System.Runtime.InteropServices;
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

    public static void SetDarkTitleBar(this Window window, bool isDark)
    {
        if (OperatingSystem.IsWindows())
        {
            WindowsDllImports.SetDarkTitleBar(window, isDark);
            window.Activate();
        }
    }
}

public static class WindowsDllImports
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    public static void SetDarkTitleBar(Window window, bool isDark)
    {
        nint? handle = window.TryGetPlatformHandle()?.Handle;
        if (handle is null) return;

        int value = isDark ? 1 : 0;
        // Attribute 20: DWMWA_USE_IMMERSIVE_DARK_MODE
        _ = DwmSetWindowAttribute(handle.Value, 20, ref value, sizeof(int));
    }
}