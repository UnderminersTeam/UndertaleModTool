// Extract data file from executable or memory dump

using System;
using System.IO;
using UndertaleModLib.Util;

byte[] extracted_data_file;

ScriptMessage("This script can extract an embedded data file from a YYC compiled game or from a dump file from memory.");

string assetNamePath = PromptLoadFile("Choose file containing the data file", "All files|*.*|All files|*");
if (assetNamePath == null)
{
    ScriptError("No file chosen", "Error");
    return;
}

if (File.Exists(Path.GetDirectoryName(assetNamePath) + Path.DirectorySeparatorChar + "data_extracted.win"))
{
    bool overwriteCheckOne = ScriptQuestion(@"A 'data_extracted.win' file already exists. 
Would you like to remove it for overwriting? This may some time. 

Note: If an error window appears, please try again or delete the file manually.
");
    if (overwriteCheckOne)
        File.Delete(Path.GetDirectoryName(assetNamePath) + Path.DirectorySeparatorChar + "data_extracted.win");
    if (!overwriteCheckOne)
    {
        ScriptError("A 'data_extracted.win' file already exists. Please remove it.", "Error: Dump already exists.");
        return;
    }
}

ExtractDataFile(assetNamePath);

void ExtractDataFile(string assetNamePath)
{
    if (File.Exists(assetNamePath))
    {
        try
        {
            using (BinaryReader reader = new BinaryReader(File.Open(assetNamePath, FileMode.Open)))
            {
                while (!CheckFORMHeader(reader))
                {
                }
                uint size = reader.ReadUInt32();
                reader.BaseStream.Seek(-8L, SeekOrigin.Current);
                size += 8U;
                extracted_data_file = reader.ReadBytes((int)size);
            }
        }
        catch (Exception exc)
        {
            ScriptError(@"Error: The file may be locked.

" + exc.ToString(), "Locked.");
        }
    }
    string path = Path.GetDirectoryName(assetNamePath) + Path.DirectorySeparatorChar + "data_extracted.win";
    try
    {
        File.WriteAllBytes(path, extracted_data_file);
        ScriptMessage("The data file has been extracted to \"data_extracted.win\" in the exe directory.");
        return;
    }
    catch (Exception exp)
    {
        ScriptError(@"An unknown error occurred.
Error:
" + exp.Message, "Unknown error");
        return;
    }
}
bool CheckFORMHeader(BinaryReader reader)
{
    //Check to see if the byte section is "FORM"
    bool isFORM = reader.ReadByte() == 0x46 && reader.ReadByte() == 0x4F && reader.ReadByte() == 0x52 && reader.ReadByte() == 0x4D;
    bool isGEN8 = false;
    if (isFORM)
    {
        //Skip 4 bytes, GEN8 should be right after.
        reader.ReadUInt32();
        //Check to see if the byte section is "GEN8".
        isGEN8 = reader.ReadByte() == 0x47 && reader.ReadByte() == 0x45 && reader.ReadByte() == 0x4E && reader.ReadByte() == 0x38;
        //If it is, it's time to read it, starting from behind "FORM".
        if (isGEN8)
        {
            reader.BaseStream.Seek(-8L, SeekOrigin.Current);
        }
    }
    return isFORM && isGEN8;
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

