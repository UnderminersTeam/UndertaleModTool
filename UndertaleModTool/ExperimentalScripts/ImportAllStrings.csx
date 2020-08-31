// Adapted from original script by Grossley

using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// Setup root folder.
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
if (!File.Exists(winFolder + "strings.txt"))
{
	ScriptError("No 'strings.txt' file exists!", "Error");
	return;
}

using (StreamReader reader = new StreamReader(winFolder + "strings.txt"))
{
	foreach (var str in Data.Strings)
	{
		if (str.Content.Contains("\n") || str.Content.Contains("\r"))
			continue;
		str.Content = reader.ReadLine();
	}
}