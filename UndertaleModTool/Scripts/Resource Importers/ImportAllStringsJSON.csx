using System.Text.Json;

EnsureDataLoaded();

string path = PromptLoadFile("", "JSON files (*.json)|*.json|TXT files (*.txt)|*.txt|All files (*.*)|*.*");
if (string.IsNullOrWhiteSpace(path))
{
    throw new ScriptException("The import file was not set.");
}

string file = File.ReadAllText(path);
JsonElement json = JsonSerializer.Deserialize<JsonElement>(file);
JsonElement.ArrayEnumerator array = json.GetProperty("Strings").EnumerateArray();
int i = 0;
foreach (JsonElement elmnt in array)
    Data.Strings[i++].Content = elmnt.ToString();
ScriptMessage("Successfully imported");
