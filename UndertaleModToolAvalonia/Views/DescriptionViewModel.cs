namespace UndertaleModToolAvalonia.Views;

public class DescriptionViewModel : ViewModelBase
{
    public string Heading { get; set; }
    public string Description { get; set; }
    public DescriptionViewModel(string heading, string description)
    {
        Heading = heading;
        Description = description;
    }
}