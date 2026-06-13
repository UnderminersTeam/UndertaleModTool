using System.Linq;
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

var builder = CreateScriptOptionsBuilder()
    .AddRadio("format", "Export format:", "Plain text (.txt)", "JSON (.json)")
    .AddBool("skipNewlines", "Skip strings containing newlines (It will break reimporting)");

var result = ShowScriptOptionsDialog("Export Strings", builder);
if (result is null) return;

string format = result["format"] as string;
bool skipNewlines = result["skipNewlines"] as bool? == true;

if (format == "Plain text (.txt)")
{
    string stringsPath = PromptSaveFile(".txt", "TXT files (*.txt)|*.txt|All files (*.*)|*.*");
    if (string.IsNullOrWhiteSpace(stringsPath))
        return;

    using (StreamWriter writer = new StreamWriter(stringsPath))
    {
        foreach (var str in Data.Strings)
        {
            if (str.Content.Contains('\n') || str.Content.Contains('\r'))
            {
                if (skipNewlines)
                    continue;
            }
            writer.WriteLine(str.Content);
        }
    }
}
else
{
    string path = PromptSaveFile(".json", "JSON files (*.json)|*.json|TXT files (*.txt)|*.txt|All files (*.*)|*.*");
    if (string.IsNullOrWhiteSpace(path))
        return;

    StringBuilder json = new StringBuilder("{\r\n    \"Strings\": [\r\n");
    const string prefix = "        ";
    const string suffix = ",\r\n";
    foreach (string str in Data.Strings.Select(str => str.Content))
    {
        if (str.Contains('\n') || str.Contains('\r'))
        {
            if (skipNewlines)
                continue;
        }
        json.Append(prefix + JsonifyString(str) + suffix);
    }
    json.Length -= suffix.Length;
    json.Append("\r\n    ]\r\n}");

    File.WriteAllText(path, json.ToString());
    ScriptMessage($"Successfully exported to\n{path}");
}

static string JsonifyString(string str)
{
    StringBuilder sb = new StringBuilder();
    foreach (char ch in str)
    {
        if (ch == '\"') { sb.Append("\\\""); continue; }
        if (ch == '\\') { sb.Append("\\\\"); continue; }
        if (ch == '\b') { sb.Append("\\b"); continue; }
        if (ch == '\f') { sb.Append("\\f"); continue; }
        if (ch == '\n') { sb.Append("\\n"); continue; }
        if (ch == '\r') { sb.Append("\\r"); continue; }
        if (ch == '\t') { sb.Append("\\t"); continue; }
        if (Char.IsControl(ch))
        {
            sb.Append("\\u" + Convert.ToByte(ch).ToString("x4"));
            continue;
        }
        sb.Append(ch);
    }
    return "\"" + sb.ToString() + "\"";
}
