namespace UndertaleModToolAvalonia.Views;

public class TabItemViewModel
{
    public object Content { get; set; }
    public TabItemViewModel(object content)
    {
        Content = content;
    }
}