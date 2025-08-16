using System.Collections.Generic;
using PropertyChanged.SourceGenerator;

namespace UndertaleModToolAvalonia;

public partial class TabItemViewModel
{
    [Notify]
    private object _Content = null!;
    [Notify]
    private bool _IsSelected = false;

    [Notify]
    private bool _CanGoBack = false;
    [Notify]
    private bool _CanGoForward = false;

    private readonly List<object> history = [];
    private int historyPosition = -1;

    public TabItemViewModel(object content, bool isSelected = false)
    {
        Content = content;
        IsSelected = isSelected;

        history.Add(Content);
        historyPosition = 0;
    }

    public void GoTo(object content)
    {
        if (content == Content)
            return;

        Content = content;

        history.RemoveRange(historyPosition + 1, history.Count - (historyPosition + 1));

        history.Add(content);
        historyPosition++;

        CanGoBack = true;
        CanGoForward = false;
    }

    public void GoBack()
    {
        historyPosition--;
        Content = history[historyPosition];

        CanGoBack = (historyPosition != 0);
        CanGoForward = true;
    }

    public void GoForward()
    {
        historyPosition++;
        Content = history[historyPosition];

        CanGoBack = true;
        CanGoForward = (historyPosition != history.Count - 1);
    }
}