using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows.Forms;

SaveFileDialog saveFileDialog = new SaveFileDialog()
{
    InitialDirectory = FilePath,
    Filter = "TXT files (*.txt)|*.txt|JSON files (*.json)|*.json|All files (*.*)|*.*"
};
if (saveFileDialog.ShowDialog() != DialogResult.OK)
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

File.WriteAllText(saveFileDialog.FileName, json.ToString());
MessageBox.Show($"Successfully exported to\n{saveFileDialog.FileName}", "String export");

static JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
{
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};
const string regexPattern = @"(?<!\\)\\u[0-9a-fA-F]{4}";
static string JsonifyString(string str)
{
    string output = JsonSerializer.Serialize(str, serializerOptions);
    Match match = Regex.Match(output, regexPattern);
    while (match.Success)
    {
        output = output.Replace(
            match.Value,
            Char.ToString((char)Convert.ToInt32(match.Value.Substring(2), 16)));
        match = match.NextMatch();
    }
    return output;
}