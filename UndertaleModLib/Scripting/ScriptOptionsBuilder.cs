using System.Collections.Generic;

namespace UndertaleModLib.Scripting;

public enum ScriptOptionType
{
    Text,
    Bool,
    Radio,
    Directory
}

public class ScriptOption
{
    public string Id { get; set; }
    public string Label { get; set; }
    public ScriptOptionType Type { get; set; }
    public object DefaultValue { get; set; }
    public string[] Choices { get; set; }
    public bool IsMultiline { get; set; }
}

public class ScriptOptionsBuilder
{
    private readonly List<ScriptOption> _options = new();

    public IReadOnlyList<ScriptOption> Options => _options;

    public ScriptOptionsBuilder AddText(string id, string label, string defaultValue = "", bool multiline = false)
    {
        _options.Add(new ScriptOption { Id = id, Label = label, Type = ScriptOptionType.Text, DefaultValue = defaultValue, IsMultiline = multiline });
        return this;
    }

    public ScriptOptionsBuilder AddBool(string id, string label, bool defaultValue = false)
    {
        _options.Add(new ScriptOption { Id = id, Label = label, Type = ScriptOptionType.Bool, DefaultValue = defaultValue });
        return this;
    }

    public ScriptOptionsBuilder AddRadio(string id, string label, params string[] choices)
    {
        string defaultValue = choices is { Length: > 0 } ? choices[0] : "";
        _options.Add(new ScriptOption { Id = id, Label = label, Type = ScriptOptionType.Radio, DefaultValue = defaultValue, Choices = choices });
        return this;
    }

    public ScriptOptionsBuilder AddDirectory(string id, string label, string defaultValue = "")
    {
        _options.Add(new ScriptOption { Id = id, Label = label, Type = ScriptOptionType.Directory, DefaultValue = defaultValue });
        return this;
    }
}
