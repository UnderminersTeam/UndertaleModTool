//By Grossley

using System;
using System.Collections.Generic;
using System.ComponentModel;
using UndertaleModLib.Scripting;

EnsureDataLoaded();

string output = "";
string used_objects_names = "";
ScriptMessage("Enter the object(s) to find");

List<string> splitStringsList = new List<string>();
List<string> gameObjectsUsedList = new List<string>();
string InputtedText = "";
InputtedText = SimpleTextInput("Menu", "Enter name(s) of game object(s)", InputtedText, true);
string[] IndividualLineArray = InputtedText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
int usage_count = 0;
int unique_objects_used = 0;
foreach (var OneLine in IndividualLineArray)
{
    string query = OneLine.Trim();
    if (query != "")
        splitStringsList.Add(query);
}
if (splitStringsList.Count < 1)
{
    ScriptError("Search cannot be empty or null.");
    return;
}
for (var k = 0; k < splitStringsList.Count; k++)
{
    bool found = false;
    for (var i = 0; i < Data.Rooms.Count; i++)
    {
        for (var j = 0; j < Data.Rooms[i].GameObjects.Count; j++)
        {
            if (Data.Rooms[i].GameObjects[j].ObjectDefinition != null)
            {
                if (Data.Rooms[i].GameObjects[j].ObjectDefinition.Name.Content.ToLower() == splitStringsList[k].ToLower())
                {
                    output += ("FOUND in room \"" + Data.Rooms[i].Name.Content + "\" (ID: " + i.ToString() + ") in instance position " + j.ToString() + " of instance id " + Data.Rooms[i].GameObjects[j].InstanceID.ToString() + " of name \"" + Data.Rooms[i].GameObjects[j].ObjectDefinition.Name.Content + "\" (id: " + Data.GameObjects.IndexOf(Data.Rooms[i].GameObjects[j].ObjectDefinition).ToString() + ")\r\n");
                    usage_count += 1;
                    if (!found)
                    {
                        gameObjectsUsedList.Add(Data.Rooms[i].GameObjects[j].ObjectDefinition.Name.Content);
                    }
                    found = true;
                }
            }
        }
    }
    if (found)
    {
        unique_objects_used += 1;
    }
}
if (gameObjectsUsedList.Count < 1)
{
    SimpleTextOutput("No results for your query below", "No results for your query below", InputtedText, true);
    return;
}
else
{
    foreach (string str in gameObjectsUsedList)
    {
        used_objects_names += (str + "\r\n");
    }
    EnableUI();
    string results_message = "Found " + unique_objects_used.ToString() + " unique objects used out of the " + splitStringsList.Count.ToString() + " objects searched. Instance results count: " + usage_count.ToString();
    SimpleTextOutput("Search results.", results_message, results_message + "\n\n" + output, true);
}
