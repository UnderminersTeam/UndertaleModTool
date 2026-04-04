using System.Collections.Generic;
using PropertyChanged.SourceGenerator;

namespace UndertaleModToolAvalonia;

public interface ITabContent
{
    /// <summary>Runs after the tab content is attached to a tab, i.e. when it becomes a tab's content.</summary>
    void OnAttached() { }
    /// <summary>Runs before the tab content is detached to a tab, i.e. when it stops being a tab's content.</summary>
    void OnDetached() { }
}

public partial class TabItemViewModel
{
    [Notify]
    private ITabContent _Content = null!;
    [Notify]
    private bool _IsSelected = false;

    [Notify]
    private bool _CanGoBack = false;
    [Notify]
    private bool _CanGoForward = false;

    private readonly List<ITabContent> history = [];
    private int historyPosition = -1;

    public TabItemViewModel(ITabContent content, bool isSelected = false)
    {
        Content = content;
        IsSelected = isSelected;

        history.Add(Content);
        historyPosition = 0;

        Content.OnAttached();
    }

    public void GoTo(ITabContent content)
    {
        if (content == Content)
            return;

        Content.OnDetached();

        Content = content;

        history.RemoveRange(historyPosition + 1, history.Count - (historyPosition + 1));

        history.Add(content);
        historyPosition++;

        CanGoBack = true;
        CanGoForward = false;

        Content.OnAttached();
    }

    public void GoBack()
    {
        Content.OnDetached();

        historyPosition--;
        Content = history[historyPosition];

        CanGoBack = (historyPosition != 0);
        CanGoForward = true;

        Content.OnAttached();
    }

    public void GoForward()
    {
        Content.OnDetached();

        historyPosition++;
        Content = history[historyPosition];

        CanGoBack = true;
        CanGoForward = (historyPosition != history.Count - 1);

        Content.OnAttached();
    }
}