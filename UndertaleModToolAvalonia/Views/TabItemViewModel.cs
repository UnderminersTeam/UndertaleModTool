using PropertyChanged.SourceGenerator;

namespace UndertaleModToolAvalonia.Views;

public partial class TabItemViewModel
{
    public object Content { get; set; }

    [Notify]
    private bool _IsSelected = false;

    public TabItemViewModel(object content, bool isSelected = false)
    {
        Content = content;
        IsSelected = isSelected;
    }
}