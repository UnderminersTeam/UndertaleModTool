using System.Text.Json;
using System.Windows.Forms;

OpenFileDialog openFileDialog = new OpenFileDialog()
{
    InitialDirectory = FilePath,
    Filter = "TXT files (*.txt)|*.txt|JSON files (*.json)|*.json|All files (*.*)|*.*"
};
if (openFileDialog.ShowDialog() != DialogResult.OK)
    return;

string file = File.ReadAllText(openFileDialog.FileName);
JsonElement json = JsonSerializer.Deserialize<JsonElement>(file);
JsonElement.ArrayEnumerator array = json.GetProperty("Strings").EnumerateArray();
int i = 0;
foreach (JsonElement elmnt in array)
    Data.Strings[i++].Content = elmnt.ToString();
MessageBox.Show("Successfully imported", "Strings import");
