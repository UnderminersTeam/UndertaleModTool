using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UndertaleModToolAvalonia;

public interface ITabContent
{
    /// <summary>
    /// Runs after the tab content is attached to a tab, i.e. when it becomes a tab's content.
    /// </summary>
    void OnAttached() { }

    /// <summary>
    /// Runs before the tab content is detached from a tab, i.e. when it stops being a tab's content.
    /// </summary>
    void OnDetached() { }

    /// <summary>
    /// Runs before <see cref="OnDetached"/>, when the data file is not closing. Use it to save the tab's temporary contents to the data file.
    /// Return true if saving was successful; if false is returned, detaching will not continue.
    /// </summary>
    async Task<bool> OnSave() => true;
}

public partial class TabItemViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ITabContent Content { get; set; } = null!;

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = false;

    [ObservableProperty]
    public partial bool CanGoBack { get; set; } = false;

    [ObservableProperty]
    public partial bool CanGoForward { get; set; } = false;

    private readonly List<ITabContent> history = [];
    private int historyPosition = -1;

    public TabItemViewModel(ITabContent content, bool isSelected = false)
    {
        Content = content;
        IsSelected = isSelected;

        history.Add(Content);
        historyPosition = 0;
    }

    public void OnOpen()
    {
        Content.OnAttached();
    }

    public void OnClose()
    {
        Content.OnDetached();
    }

    public async Task<bool> Save()
    {
        return await Content.OnSave();
    }

    public async Task<bool> GoTo(ITabContent content)
    {
        if (content == Content)
            return true;

        if (!await Content.OnSave())
            return false;

        Content.OnDetached();

        Content = content;

        history.RemoveRange(historyPosition + 1, history.Count - (historyPosition + 1));

        history.Add(content);
        historyPosition++;

        CanGoBack = true;
        CanGoForward = false;

        Content.OnAttached();

        return true;
    }

    public async Task<bool> GoBack()
    {
        if (!await Content.OnSave())
            return false;

        Content.OnDetached();

        historyPosition--;
        Content = history[historyPosition];

        CanGoBack = (historyPosition != 0);
        CanGoForward = true;

        Content.OnAttached();

        return true;
    }

    public async Task<bool> GoForward()
    {
        if (!await Content.OnSave())
            return false;

        Content.OnDetached();

        historyPosition++;
        Content = history[historyPosition];

        CanGoBack = true;
        CanGoForward = (historyPosition != history.Count - 1);

        Content.OnAttached();

        return true;
    }
}