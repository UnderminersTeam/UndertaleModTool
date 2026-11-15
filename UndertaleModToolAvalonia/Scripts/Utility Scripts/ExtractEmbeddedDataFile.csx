using System;
using System.IO;
using UndertaleModLib.Util;

byte[] extractedDataBuffer;

ScriptMessage("This script can extract an embedded data file from a YYC compiled game or from a dump file from memory.");

string assetNamePath = PromptLoadFile("", "All files|*.*|All files|*");
if (assetNamePath is null)
{
    return;
}

string extractedDataPath = PromptSaveFile(".win", "GameMaker data files (.win, .unx, .ios, .droid)|*.win;*.unx;*.ios;*.droid|All files|*");
if (string.IsNullOrWhiteSpace(extractedDataPath))
{
    return;
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
                extractedDataBuffer = reader.ReadBytes((int)size);
            }
        }
        catch (Exception exc)
        {
            ScriptError(@"Error: The file may be locked.

" + exc.ToString(), "Locked.");
        }
    }
    try
    {
        File.WriteAllBytes(extractedDataPath, extractedDataBuffer);
        ScriptMessage("The data file has been extracted successfully.");
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
    // Check to see if the byte section is "FORM"
    bool isFORM = reader.ReadByte() == 0x46 && reader.ReadByte() == 0x4F && reader.ReadByte() == 0x52 && reader.ReadByte() == 0x4D;
    bool isGEN8 = false;
    if (isFORM)
    {
        // Skip 4 bytes, GEN8 should be right after.
        reader.ReadUInt32();
        // Check to see if the byte section is "GEN8".
        isGEN8 = reader.ReadByte() == 0x47 && reader.ReadByte() == 0x45 && reader.ReadByte() == 0x4E && reader.ReadByte() == 0x38;
        // If it is, it's time to read it, starting from behind "FORM".
        if (isGEN8)
        {
            reader.BaseStream.Seek(-8L, SeekOrigin.Current);
        }
    }
    return isFORM && isGEN8;
}
