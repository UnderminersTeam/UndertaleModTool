EnsureDataLoaded();
string codePath = Path.GetDirectoryName(FilePath) + Path.DirectorySeparatorChar + "scr_enums.gml";

string code = "";
code += "enum states {\n";
foreach (KeyValuePair<int, string> kvp in AssetTypeResolver.PTStates) {
	code += $"    {kvp.Value} = ({kvp.Key} << 0),\n";
}
code += "}\n";

File.WriteAllText(codePath, code);

ScriptMessage("Exported to: " + codePath);