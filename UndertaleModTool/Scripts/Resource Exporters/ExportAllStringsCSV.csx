using System.Linq;
using System.Text;
using System.IO;

EnsureDataLoaded();

string path = PromptSaveFile(".csv", "CSV files (*.csv)|*.csv|TXT files (*.txt)|*.txt|All files (*.*)|*.*");
if (string.IsNullOrWhiteSpace(path))
    return;

StringBuilder csv = new StringBuilder();


csv.AppendLine("ID,Source,Translation");

int i = 1;
foreach (string str in Data.Strings.Select(s => s.Content))
{
    csv.AppendLine($"{i},{CsvEscape(str)}");
    i++;
}

File.WriteAllText(path, csv.ToString(), Encoding.UTF8);
ScriptMessage($"Successfully exported to\n{path}");

static string CsvEscape(string str)
{

    if (str.Contains(",") || str.Contains("\"") || str.Contains("\n") || str.Contains("\r"))
    {
        str = "\"" + str.Replace("\"", "\"\"") + "\"";
    }
    return str;
}
