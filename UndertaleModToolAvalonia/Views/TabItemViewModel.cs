namespace UndertaleModToolAvalonia.Views;

public class TabItemViewModel : ViewModelBase
{
    public object Content { get; set; }
    public TabItemViewModel(object content)
    {
        Content = content;
    }
}