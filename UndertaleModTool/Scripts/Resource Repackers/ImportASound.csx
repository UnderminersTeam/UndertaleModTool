// By Jockeholm & Nik the Neko & Grossley - Version 7 - April 19th, 2020 - Adds new audiogroups automatically and adds sound to new empty audiogroup if they do not exist yet.
// By Jockeholm & Nik the Neko & Grossley - Version 6 - April 19th, 2020 - Corrected an oversight which caused errors while trying to add sounds to new audiogroups.
// By Jockeholm & Nik the Neko - Version 5 06/03/2020 - Replace existing sounds if needed.
// By Jockeholm & Nik the Neko - Version 4 06/03/2020 - Specify audiogroup name with a folder name.
// By Jockeholm & Nik the Neko - Version 3 03/01/2020 - Massive im_purr_ovements.
// By Jockeholm                - Version 2 15/02/2020 - Currently supports embedded WAV files only

using System.Windows.Forms;
using UndertaleModLib;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleSound;
using static UndertaleModLib.UndertaleData;

EnsureDataLoaded();

string GetFolder(string path) 
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

UndertaleEmbeddedAudio audioFile = null;
int  audioID                = -1;
int  audioGroupID           = -1;
int  embAudioID             = -1;
bool usesAGRP               = (Data.AudioGroups.Count > 0);

if (!usesAGRP)
{
    ScriptError("ERROR!\nThis game doesn't use audiogroups!\nImporting to external audiogroups is disabled.", "ImportSound");
    //return;
}

OpenFileDialog fileDialog   = new OpenFileDialog();
fileDialog.InitialDirectory = Path.GetDirectoryName(FilePath) + Path.DirectorySeparatorChar;
fileDialog.Filter           = "Sound files (*.WAV;*.OGG)|*.WAV;*.OGG"; // Limits the dialog to displaying WAV files only
fileDialog.Title            = "Select a sound file to import"; // Sets the dialog title
DialogResult dialogRet      = fileDialog.ShowDialog(); // opens a file select window

if (dialogRet == DialogResult.OK)
{
    string fname      = fileDialog.SafeFileName;
    string sound_name = fname.Substring(0, fname.LastIndexOf('.')); // creates a string of the wav file's filename without it's extension
    bool   isOGG      = Path.GetExtension(fileDialog.FileName) == ".ogg";
    bool   embedSound = false;
    bool   decodeLoad = false;
    if (isOGG)
    {
        embedSound = ScriptQuestion("Your sound appears to be an OGG.\nNo - keep it external\nYes - embed sound into the game (use responsibly!)");
        decodeLoad = false;
        if (embedSound)
        {
            decodeLoad = ScriptQuestion("Do you want to Uncompress this sound on load? (Higher Memory, low CPU)");
        }
    }
    else
    {
        // How can a .wav be external?
        embedSound = true;
        decodeLoad = false;
    }
    string AGRPname    = "";
    string FolderName  = Path.GetFileName( Path.GetDirectoryName( fileDialog.FileName ) );
    bool   needAGRP    = false;
    bool   ifRightAGRP = false;
    string[] splitArr  = new string[2];
    splitArr[0] = sound_name;
    splitArr[1] = FolderName;
    
    bool soundExists = false;
    
    UndertaleSound existing_snd = null;
    
    for (var i = 0; i < Data.Sounds.Count; i++)
    {
        if (Data.Sounds[i].Name.Content == sound_name)
        {
            soundExists = true;
            existing_snd = Data.Sounds[i];
            ScriptMessage("WARNING!:\nThe sound with this name already exists in the game, it will be replaced instead of added.\n\nsndname: " + existing_snd.Name.Content);
            break;
        }
    }

    if (embedSound && usesAGRP && !soundExists)
    {
        needAGRP = ScriptQuestion("Your last folder name is: " + FolderName + "\nDo you want to treat it as audiogroup's name?\n(Answer No for audiogroup_default)");
    }

    if (needAGRP && usesAGRP && embedSound)
    {
        AGRPname     = splitArr[1];
        ifRightAGRP  = (needAGRP && embedSound);
        if (ifRightAGRP)
        {
            while (audioGroupID == -1)
            {
                // find the agrp we need.
                for (int i = 0; i < Data.AudioGroups.Count; i++)
                {
                    string name = Data.AudioGroups[i].Name.Content;
                    if (name == AGRPname)
                    {
                        audioGroupID = i;
                        break;
                    }
                }
                if (audioGroupID == -1) // still -1? o_O
                {
                    File.WriteAllBytes(GetFolder(FilePath) + "audiogroup" + Data.AudioGroups.Count + ".dat", Convert.FromBase64String("Rk9STQwAAABBVURPBAAAAAAAAAA="));
                    var newAudioGroup = new UndertaleAudioGroup()
                    {
                        Name        = Data.Strings.MakeString(FolderName),
                    };
                    Data.AudioGroups.Add(newAudioGroup);
                }
            }
        }
        else
        {
            return;
        }
    }

    if (soundExists)
    {
        for (int i = 0; i < Data.Sounds.Count; i++)
        {
            string name = sound_name;
            if (name == Data.Sounds[i].Name.Content)
            {
                audioGroupID = Data.Sounds[i].GroupID;
                break;
            }
        }
    }
    if (audioGroupID == 0) //If the audiogroup is zero then 
        needAGRP = false;
        
    UndertaleEmbeddedAudio soundData = null;

    if ((embedSound && !needAGRP) || (needAGRP))
    {
        soundData = new UndertaleEmbeddedAudio() { Data = File.ReadAllBytes(fileDialog.FileName) };
        Data.EmbeddedAudio.Add(soundData);
        if (soundExists)
            Data.EmbeddedAudio.Remove(existing_snd.AudioFile);
        embAudioID = Data.EmbeddedAudio.Count - 1;
        //ScriptMessage("len " + Data.EmbeddedAudio[embAudioID].Data.Length.ToString());
    }
    
    //ScriptMessage("11");
    
    if (needAGRP)
    {
        var audioGroupReadStream =
        (
            new FileStream(GetFolder(FilePath) + "audiogroup" + audioGroupID.ToString() + ".dat", FileMode.Open, FileAccess.Read)
        ); // Load the audiogroup dat into memory
        UndertaleData audioGroupDat = UndertaleIO.Read(audioGroupReadStream); // Load as UndertaleData
        audioGroupReadStream.Dispose();
        audioGroupDat.EmbeddedAudio.Add(soundData); // Adds the embeddedaudio entry to the dat data in memory
        if (soundExists)
            audioGroupDat.EmbeddedAudio.Remove(existing_snd.AudioFile);
        audioID = audioGroupDat.EmbeddedAudio.Count - 1;
        var audioGroupWriteStream =
        (
            new FileStream(GetFolder(FilePath) + "audiogroup" + audioGroupID.ToString() + ".dat", FileMode.Create)
        );
        UndertaleIO.Write(audioGroupWriteStream, audioGroupDat); // Write it to the disk
        audioGroupWriteStream.Dispose();
    }

    UndertaleSound.AudioEntryFlags flags = UndertaleSound.AudioEntryFlags.Regular;
    
    if (isOGG && embedSound && decodeLoad)  // OGG, embed, decode on load.
        flags = UndertaleSound.AudioEntryFlags.IsEmbedded   | UndertaleSound.AudioEntryFlags.IsCompressed | UndertaleSound.AudioEntryFlags.Regular;
    if (isOGG && embedSound && !decodeLoad) // OGG, embed, not decode on load.
        flags = UndertaleSound.AudioEntryFlags.IsCompressed | UndertaleSound.AudioEntryFlags.Regular;
    if (!isOGG)                                // WAV, always embed.
        flags = UndertaleSound.AudioEntryFlags.IsEmbedded   | UndertaleSound.AudioEntryFlags.Regular;
    if (isOGG && !embedSound)                // OGG, external.
    {
        flags = UndertaleSound.AudioEntryFlags.Regular;
        audioID = -1;
    }
    
    UndertaleEmbeddedAudio RaudioFile = null;
    if (!embedSound)             
        RaudioFile = null;
    if (embedSound && !needAGRP) 
        RaudioFile = Data.EmbeddedAudio[embAudioID];
    if (embedSound && needAGRP)  
        RaudioFile = null;
    string soundfname = sound_name;
    
    UndertaleAudioGroup groupID = null;
    if (!usesAGRP)
        groupID = null;
    else
        groupID = needAGRP ? Data.AudioGroups[audioGroupID] : Data.AudioGroups[Data.GetBuiltinSoundGroupID()];
    
    //ScriptMessage("12");
    
    if (!soundExists)
    {
        var snd_to_add = new UndertaleSound()
        {
            Name        = Data.Strings.MakeString(soundfname),
            Flags       = flags,
            Type        = (isOGG      ? Data.Strings.MakeString(".ogg") : Data.Strings.MakeString(".wav")               ),
            File        = Data.Strings.MakeString(fname),
            Effects     = 0,
            Volume      = 1.0F,
            Pitch       = 0,
            AudioID     = audioID,
            AudioFile   = RaudioFile,
            AudioGroup  = groupID,
            GroupID     = (needAGRP   ? audioGroupID                   : Data.GetBuiltinSoundGroupID()                  )
        };
        
        Data.Sounds.Add(snd_to_add);
        ChangeSelection(snd_to_add);
    }
    else
    {
        existing_snd.AudioFile = RaudioFile;
        existing_snd.AudioID   = audioID;
        ChangeSelection(existing_snd);
    }
    
    ScriptMessage("Sound added successfully!\nEnjoy your meowing day!");
}