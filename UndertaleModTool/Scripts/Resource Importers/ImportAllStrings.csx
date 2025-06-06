using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

string stringsPath = PromptLoadFile("", "TXT files (*.txt)|*.txt|All files (*.*)|*.*");
if (string.IsNullOrWhiteSpace(stringsPath))
{
    throw new ScriptException("The import file was not set.");
}

int file_length = 0;
string line = "";
using (StreamReader reader = new StreamReader(stringsPath))
{
    while ((line = reader.ReadLine()) is not null)
    {
        file_length += 1;
    }
}

int validStringsCount = 0;
foreach (var str in Data.Strings)
{
    if (str.Content.Contains("\n") || str.Content.Contains("\r"))
        continue;
    validStringsCount += 1;
}

if (file_length < validStringsCount)
{
    ScriptError("ERROR 0: Unexpected end of file at line: " + file_length.ToString() + ". Expected file length was: " + validStringsCount.ToString() + ". No changes have been made.", "Error");
    return;
}
else if (file_length > validStringsCount)
{
    ScriptError("ERROR 1: Line count exceeds expected count. Current count: " + file_length.ToString() + ". Expected count: " + validStringsCount.ToString() + ". No changes have been made.", "Error");
    return;
}

using (StreamReader reader = new StreamReader(stringsPath))
{
    int line_no = 1;
    line = "";
    foreach (var str in Data.Strings)
    {
        if (str.Content.Contains("\n") || str.Content.Contains("\r"))
            continue;
        if (!((line = reader.ReadLine()) is not null))
        {
            ScriptError("ERROR 2: Unexpected end of file at line: " + line_no.ToString() + ". Expected file length was: " + validStringsCount.ToString() + ". No changes have been made.", "Error");
            return;
        }
        line_no += 1;
    }
}

using (StreamReader reader = new StreamReader(stringsPath))
{
    int line_no = 1;
    line = "";
    foreach (var str in Data.Strings)
    {
        if (str.Content.Contains("\n") || str.Content.Contains("\r"))
            continue;
        if ((line = reader.ReadLine()) is not null)
            str.Content = line;
        else
        {
            ScriptError("ERROR 3: Unexpected end of file at line: " + line_no.ToString() + ". Expected file length was: " + validStringsCount.ToString() + ". All lines within the file have been applied. Please check for errors.", "Error");
            return;
        }
        line_no += 1;
    }
}
