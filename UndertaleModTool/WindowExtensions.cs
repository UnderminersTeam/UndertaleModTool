using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Interop;

namespace UndertaleModTool
{
	/// <summary>
	/// Provides <see cref="MessageBox"/> extensions for <see cref="Window"/>s.
	/// </summary>
	public static class MessageBoxExtensions
	{
		/// <summary>
		/// Shows an informational <see cref="MessageBox"/> with <paramref name="window"/> as the parent.
		/// </summary>
		/// <param name="window">The parent from which the <see cref="MessageBox"/> will show.</param>
		/// <param name="messageBoxText">A <see cref="string"/> that specifies the text to display.</param>
		/// <param name="title">A <see cref="string"/> that specifies the title bar caption to display.</param>
		/// <returns><see cref="MessageBoxResult.OK"/> or <see cref="MessageBoxResult.None"/> if
		/// the <see cref="MessageBox"/> was cancelled.</returns>
		public static MessageBoxResult ShowMessage(this Window window, string messageBoxText, string title = "UndertaleModTool")
		{
			return ShowCore(window, messageBoxText, title, MessageBoxButton.OK, MessageBoxImage.Information);
		}

		/// <summary>
		/// Shows a <see cref="MessageBox"/> prompting for a yes/no question with <paramref name="window"/> as the parent.
		/// </summary>
		/// <param name="window">The parent from which the <see cref="MessageBox"/> will show.</param>
		/// <param name="messageBoxText">A <see cref="string"/> that specifies the text to display.</param>
		/// <param name="icon">The <see cref="MessageBoxImage"/> to display.</param>
		/// <param name="title">A <see cref="string"/> that specifies the title bar caption to display.</param>
		/// <returns><see cref="MessageBoxResult.Yes"/> or <see cref="MessageBoxResult.No"/> depending on the users' answer.
		/// <see cref="MessageBoxResult.None"/> if the <see cref="MessageBox"/> was cancelled.</returns>
		public static MessageBoxResult ShowQuestion(this Window window, string messageBoxText, MessageBoxImage icon = MessageBoxImage.Question, string title = "UndertaleModTool")
		{
			return ShowCore(window, messageBoxText, title, MessageBoxButton.YesNo, icon);
		}

		/// <summary>
		/// Shows a <see cref="MessageBox"/> prompting for a yes/no/cancel question with <paramref name="window"/> as the parent.
		/// </summary>
		/// <param name="window">The parent from which the <see cref="MessageBox"/> will show.</param>
		/// <param name="messageBoxText">A <see cref="string"/> that specifies the text to display.</param>
		/// <param name="icon">The <see cref="MessageBoxImage"/> to display.</param>
		/// <param name="title">A <see cref="string"/> that specifies the title bar caption to display.</param>
		/// <returns><see cref="MessageBoxResult.Yes"/>, <see cref="MessageBoxResult.No"/> or <see cref="MessageBoxResult.Cancel"/> depending on the users' answer.</returns>
		public static MessageBoxResult ShowQuestionWithCancel(this Window window, string messageBoxText, MessageBoxImage icon = MessageBoxImage.Question, string title = "UndertaleModTool")
		{
			return ShowCore(window, messageBoxText, title, MessageBoxButton.YesNoCancel, icon);
		}

		/// <summary>
		/// Shows a warning <see cref="MessageBox"/> with <paramref name="window"/> as the parent.
		/// </summary>
		/// <param name="window">The parent from which the <see cref="MessageBox"/> will show.</param>
		/// <param name="messageBoxText">A <see cref="string"/> that specifies the text to display.</param>
		/// <param name="title">A <see cref="string"/> that specifies the title bar caption to display.</param>
		/// <returns><see cref="MessageBoxResult.OK"/> or <see cref="MessageBoxResult.None"/> if
		/// the <see cref="MessageBox"/> was cancelled.</returns>
		public static MessageBoxResult ShowWarning(this Window window, string messageBoxText, string title = "Warning")
		{
			return ShowCore(window, messageBoxText, title, MessageBoxButton.OK, MessageBoxImage.Warning);
		}

		/// <summary>
		/// Shows an error <see cref="MessageBox"/> with <paramref name="window"/> as the parent.
		/// </summary>
		/// <param name="window">The parent from which the <see cref="MessageBox"/> will show.</param>
		/// <param name="messageBoxText">A <see cref="string"/> that specifies the text to display.</param>
		/// <param name="title">A <see cref="string"/> that specifies the title bar caption to display.</param>
		/// <returns><see cref="MessageBoxResult.OK"/> or <see cref="MessageBoxResult.None"/> if
		/// the <see cref="MessageBox"/> was cancelled.</returns>
		public static MessageBoxResult ShowError(this Window window, string messageBoxText, string title = "Error")
		{
			return ShowCore(window, messageBoxText, title, MessageBoxButton.OK, MessageBoxImage.Error);
		}

		/// <summary>
		/// The wrapper for the extensions to directly call <see cref="MessageBox.Show(Window, string, string, MessageBoxButton, MessageBoxImage)"/>.
		/// </summary>
		/// <param name="window">A Window that represents the owner window of the message box.</param>
		/// <param name="text">A <see cref="string"/> that specifies the text to display.</param>
		/// <param name="title">A <see cref="string"/> that specifies the title bar caption to display.</param>
		/// <param name="buttons">A <see cref="MessageBoxButton"/> value that specifies which button or buttons to display.</param>
		/// <param name="image">A <see cref="MessageBoxImage"/> value that specifies the icon to display.</param>
		/// <returns>A <see cref="MessageBoxResult"/> value that specifies which message box button is clicked by the user.</returns>
		private static MessageBoxResult ShowCore(this Window window, string text, string title, MessageBoxButton buttons, MessageBoxImage image)
		{
			return window.Dispatcher.Invoke(() => MessageBox.Show(window, text, title, buttons, image));
		}
	}

	// Mostly from https://github.com/microsoft/wpf-samples/tree/main/Windows/SaveWindowState
	public static class WindowPlacementExtensions
	{
        private const int SwShowNormal = 1;
        private const int SwShowMinimized = 2;
		private const int SwShowMaximized = 3;
		private const int WPFRestoreToMaximized = 0x2;

        [StructLayout(LayoutKind.Sequential)]
		public struct Point
		{
            [JsonInclude] public int X;
            [JsonInclude] public int Y;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Rect
		{
            [JsonInclude] public int Left;
            [JsonInclude] public int Top;
            [JsonInclude] public int Right;
            [JsonInclude] public int Bottom;
		}

        [StructLayout(LayoutKind.Sequential)]
		public struct WindowPlacement
		{
            public int length = Marshal.SizeOf(typeof(WindowPlacement));
            public int flags = 0;
            [JsonInclude] public int showCmd = SwShowNormal;
			public Point minPosition = new() { X = -1, Y = -1 }; 
            public Point maxPosition = new() { X = -1, Y = -1 };
            [JsonInclude] public Rect normalPosition = new();

            public WindowPlacement()
            {
            }
        }

		[DllImport("user32.dll")]
		private static extern bool SetWindowPlacement(IntPtr hWnd, in WindowPlacement lpwndpl);

		[DllImport("user32.dll")]
		private static extern bool GetWindowPlacement(IntPtr hWnd, out WindowPlacement lpwndpl);

		public static void SetPlacement(this Window window, WindowPlacement? windowPlacement)
		{
			if (windowPlacement is null) return;

			IntPtr handle = new WindowInteropHelper(window).Handle;

			if (!SetWindowPlacement(handle, windowPlacement.Value))
			{
				Trace.WriteLine("SetWindowPlacement failed");
			}
        }

		public static WindowPlacement? GetPlacement(this Window window)
		{
            IntPtr handle = new WindowInteropHelper(window).Handle;
			if (!GetWindowPlacement(handle, out WindowPlacement windowPlacement))
			{
                Trace.WriteLine("GetWindowPlacement failed");
				return null;
            }

            // If minimized, consider it as restored or maximized depending on previous state.
            windowPlacement.showCmd = (windowPlacement.showCmd == SwShowMinimized
                ? ((windowPlacement.flags & WPFRestoreToMaximized) != 0 ? SwShowMaximized : SwShowNormal)
                : windowPlacement.showCmd);

            return windowPlacement;
        }
	}
}