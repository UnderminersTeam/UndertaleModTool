using System.Text.Json;

EnsureDataLoaded();

string path = PromptLoadFile("", "TXT files (*.txt)|*.txt|JSON files (*.json)|*.json|All files (*.*)|*.*");
if (string.IsNullOrWhiteSpace(path))
    return;

string file = File.ReadAllText(path);
JsonElement json = JsonSerializer.Deserialize<JsonElement>(file);
JsonElement.ArrayEnumerator array = json.GetProperty("Strings").EnumerateArray();
int i = 0;
foreach (JsonElement elmnt in array)
    Data.Strings[i++].Content = elmnt.ToString();
ScriptMessage("Successfully imported");
