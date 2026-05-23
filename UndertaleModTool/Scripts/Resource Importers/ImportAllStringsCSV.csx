using System.Text;

EnsureDataLoaded();

string path = PromptLoadFile(".csv", "CSV files (*.csv)|*.csv|TXT files (*.txt)|*.txt|All files (*.*)|*.*");
if (string.IsNullOrWhiteSpace(path))
{
    throw new ScriptException("The import file was not set.");
}


string[] lines = File.ReadAllLines(path, Encoding.UTF8);

if (lines.Length <= 1)
{
    throw new ScriptException("The CSV file does not contain any data.");
}


for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
{
    string line = lines[lineIndex];
    if (string.IsNullOrWhiteSpace(line))
        continue;

    string[] cols = ParseCsvLine(line);


    if (cols.Length >= 3)
    {

        if (int.TryParse(cols[0], out int id))
        {
            int index = id - 1; // бо Data.Strings індексується з 0
            if (index >= 0 && index < Data.Strings.Count)
            {
                string newContent = cols[2];
                if (!string.IsNullOrWhiteSpace(newContent))
                {
                    Data.Strings[index].Content = newContent;
                }
            }
        }
    }
}

ScriptMessage("Successfully imported");



static string[] ParseCsvLine(string line)
{
    List<string> result = new List<string>();
    StringBuilder sb = new StringBuilder();
    bool inQuotes = false;

    for (int i = 0; i < line.Length; i++)
    {
        char c = line[i];

        if (c == '\"')
        {
            if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
            {
                sb.Append('\"');
                i++;
            }
            else
            {
                inQuotes = !inQuotes;
            }
        }
        else if (c == ',' && !inQuotes)
        {
            result.Add(sb.ToString());
            sb.Clear();
        }
        else
        {
            sb.Append(c);
        }
    }

    result.Add(sb.ToString());
    return result.ToArray();
}
