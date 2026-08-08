using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;

namespace UndertaleModToolAvalonia;

public class ValueDataTemplate : DataTemplate, IDataTemplate
{
    public object? Value { get; set; }

    bool IDataTemplate.Match(object? data)
    {
        return (DataType is null || DataType.IsInstanceOfType(data))
            && (Value is null || (data?.Equals(Value) ?? false));
    }
}
