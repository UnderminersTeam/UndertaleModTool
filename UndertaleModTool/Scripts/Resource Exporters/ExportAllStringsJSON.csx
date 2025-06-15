using System.Linq;
using System.Text;

EnsureDataLoaded();

string path = PromptSaveFile(".json", "JSON files (*.json)|*.json|TXT files (*.txt)|*.txt|All files (*.*)|*.*");
if (string.IsNullOrWhiteSpace(path))
    return;

StringBuilder json = new StringBuilder("{\r\n    \"Strings\": [\r\n");
const string
    prefix = "        ",
    suffix = ",\r\n";
foreach (string str in Data.Strings.Select(str => str.Content))
    json.Append(
        prefix
        + JsonifyString(str)
        + suffix);
json.Length -= suffix.Length;
json.Append("\r\n    ]\r\n}");

File.WriteAllText(path, json.ToString());
ScriptMessage($"Successfully exported to\n{path}");

static string JsonifyString(string str)
{
    StringBuilder sb = new StringBuilder();
    foreach (char ch in str)
    {    // Characters that JSON requires escaping
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
