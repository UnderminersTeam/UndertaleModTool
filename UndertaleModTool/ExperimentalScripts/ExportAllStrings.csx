//Adapted from original script by Grossley
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// Setup root export folder.
string winFolder = GetFolder(FilePath); // The folder data.win is located in.

string GetFolder(string path) 
{
	return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

void MakeFolder(String folderName) 
{
	if (!Directory.Exists(winFolder + folderName + "/"))
		Directory.CreateDirectory(winFolder + folderName + "/");
}

//Overwrite Check One
if (File.Exists(winFolder + "strings.txt"))
{
	bool overwriteCheckOne = ScriptQuestion(@"A 'strings.txt' file already exists. 
Would you like to overwrite it?");
	if (overwriteCheckOne)
		File.Delete(winFolder + "strings.txt");
	if (!overwriteCheckOne)
	{
		ScriptError("A 'strings.txt' file already exists. Please remove it and try again.", "Error: Export already exists.");
		return;
	}
}

using (StreamWriter writer = new StreamWriter(winFolder + "strings.txt"))
{
	foreach (var str in Data.Strings)
	{
		if (str.Content.Contains("\n") || str.Content.Contains("\r"))
			continue;
		writer.WriteLine(str.Content);
	}
}
