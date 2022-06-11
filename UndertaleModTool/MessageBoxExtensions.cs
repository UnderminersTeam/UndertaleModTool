using System.Windows;

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
			return MessageBox.Show(window, text, title, buttons, image);
		}
	}
}