// By Grossley - Version 8 - September 15th, 2020 - Makes the sound importer work in bulk.
// By Jockeholm & Nik the Neko & Grossley - Version 7 - April 19th, 2020 - Adds new audiogroups automatically and adds sound to new empty audiogroup if they do not exist yet.
// By Jockeholm & Nik the Neko & Grossley - Version 6 - April 19th, 2020 - Corrected an oversight which caused errors while trying to add sounds to new audiogroups.
// By Jockeholm & Nik the Neko - Version 5 06/03/2020 - Replace existing sounds if needed.
// By Jockeholm & Nik the Neko - Version 4 06/03/2020 - Specify audiogroup name with a folder name.
// By Jockeholm & Nik the Neko - Version 3 03/01/2020 - Massive im_purr_ovements.
// By Jockeholm                - Version 2 15/02/2020 - Currently supports embedded WAV files only

using UndertaleModLib;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleSound;
using static UndertaleModLib.UndertaleData;
using System.Threading.Tasks;

EnsureDataLoaded();

int maxCount = 1;

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
if (Data.IsVersionAtLeast(2024, 14))
{
    ScriptWarning("This script may act erroneously on GameMaker version 2024.14 and later.");
}

// Check code directory.
string importFolder = PromptChooseDirectory();
if (importFolder == null)
    throw new ScriptException("The import folder was not set.");

string[] dirFiles = Directory.GetFiles(importFolder);

bool manuallySpecifyEverySound = false;
bool GeneralSound_embedSound = false;
bool GeneralSound_decodeLoad = false;
bool GeneralSound_needAGRP = false;
string FolderName = new DirectoryInfo(importFolder).Name;

bool emergencyCancel = ScriptQuestion(@"This script imports sounds in bulk. Do you wish to continue?");
if (!emergencyCancel)
    return;

bool replaceSoundPropertiesCheck = ScriptQuestion("WARNING!:\nIf a sound already exists in the game, it will be replaced instead of added. Would you like to replace the sound properties as well?");

bool autoSpecifyEverySound = ScriptQuestion(@"Would you like to automatically specify the characteristics of each sound?
If you select no you will have to manually specify all sounds.");

manuallySpecifyEverySound = (autoSpecifyEverySound ? false : true);

if (manuallySpecifyEverySound == false)
{
    GeneralSound_embedSound = ScriptQuestion("Do you want to keep your OGG files external or internal?\nNo - keep it external\nYes - embed sound into the game (use responsibly!)");
    if (GeneralSound_embedSound)
    {
        GeneralSound_decodeLoad = ScriptQuestion("Do you want to Uncompress sounds on load? (Higher Memory, low CPU)");
    }
    if (GeneralSound_embedSound && (Data.AudioGroups.Count > 0))
    {
        GeneralSound_needAGRP = ScriptQuestion("Your last folder name is: " + FolderName + "\nDo you want to treat it as audiogroup's name?\n(Answer No for audiogroup_default)");
    }
    ScriptMessage("WARNING!:\nIf a sound already exists in the game, it will be replaced instead of added.");
}

maxCount = dirFiles.Length;
SetProgressBar(null, "Importing sounds", 0, maxCount);
StartProgressBarUpdater();

SyncBinding("AudioGroups, EmbeddedAudio, Sounds, Strings", true);
await Task.Run(() => {
    foreach (string file in dirFiles)
    {
        IncProgressLocal();

        string fname = Path.GetFileName(file);
        string temp = fname.ToLower();
        if (((temp.EndsWith(".ogg")) || (temp.EndsWith(".wav"))) == false)
        {
            //ScriptMessage(fname);
            //ScriptMessage(Path.GetExtension(fname).ToLower());
            continue; // Restarts loop if file is not a valid sound asset.
        }
        //else
        //{
        //    ScriptMessage("You did it");
        //}
        string sound_name = Path.GetFileNameWithoutExtension(file);
        bool isOGG = Path.GetExtension(fname) == ".ogg";
        bool embedSound = false;
        bool decodeLoad = false;
        if ((isOGG) && (manuallySpecifyEverySound == true))
        {
            embedSound = ScriptQuestion("Your sound appears to be an OGG.\nNo - keep it external\nYes - embed sound into the game (use responsibly!)");
            decodeLoad = false;
            if (embedSound)
            {
                decodeLoad = ScriptQuestion("Do you want to Uncompress this sound on load? (Higher Memory, low CPU)");
            }
        }
        else if ((isOGG) && (manuallySpecifyEverySound == false))
        {
            embedSound = GeneralSound_embedSound;
            decodeLoad = GeneralSound_decodeLoad;
        }
        else
        {
            // How can a .wav be external?
            embedSound = true;
            decodeLoad = false;
        }
        string AGRPname = "";
        string FolderName = new DirectoryInfo(importFolder).Name;
        bool needAGRP = false;
        bool ifRightAGRP = false;
        string[] splitArr = new string[2];
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
                if (manuallySpecifyEverySound == true)
                    ScriptMessage("WARNING!:\nThe sound with this name already exists in the game, it will be replaced instead of added.\n\nsndname: " + existing_snd.Name.Content);
                break;
            }
        }

        if (embedSound && usesAGRP && !soundExists)
        {
            if (manuallySpecifyEverySound)
                needAGRP = ScriptQuestion("Your last folder name is: " + FolderName + "\nDo you want to treat it as audiogroup's name?\n(Answer No for audiogroup_default)");
            else
                needAGRP = GeneralSound_needAGRP;
        }

        if (needAGRP && usesAGRP && embedSound)
        {
            AGRPname = splitArr[1];
            ifRightAGRP = (needAGRP && embedSound);
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
                            Name = Data.Strings.MakeString(FolderName),
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
            soundData = new UndertaleEmbeddedAudio() { Data = File.ReadAllBytes(importFolder + "/" + fname) };
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
            flags = UndertaleSound.AudioEntryFlags.IsEmbedded | UndertaleSound.AudioEntryFlags.IsCompressed | UndertaleSound.AudioEntryFlags.Regular;
        if (isOGG && embedSound && !decodeLoad) // OGG, embed, not decode on load.
            flags = UndertaleSound.AudioEntryFlags.IsCompressed | UndertaleSound.AudioEntryFlags.Regular;
        if (!isOGG)                                // WAV, always embed.
            flags = UndertaleSound.AudioEntryFlags.IsEmbedded | UndertaleSound.AudioEntryFlags.Regular;
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
                Name = Data.Strings.MakeString(soundfname),
                Flags = flags,
                Type = (isOGG ? Data.Strings.MakeString(".ogg") : Data.Strings.MakeString(".wav")),
                File = Data.Strings.MakeString(fname),
                Effects = 0,
                Volume = 1.0F,
                Pitch = 1.0F,
                AudioID = audioID,
                AudioFile = RaudioFile,
                AudioGroup = groupID,
                GroupID = (needAGRP ? audioGroupID : Data.GetBuiltinSoundGroupID())
            };
            Data.Sounds.Add(snd_to_add);
            //ChangeSelection(snd_to_add);
        }
        else if (replaceSoundPropertiesCheck)
        {
            var snd_to_add = Data.Sounds.ByName(soundfname);
            snd_to_add.Name = Data.Strings.MakeString(soundfname);
            snd_to_add.Flags = flags;
            snd_to_add.Type = (isOGG ? Data.Strings.MakeString(".ogg") : Data.Strings.MakeString(".wav"));
            snd_to_add.File = Data.Strings.MakeString(fname);
            snd_to_add.Effects = 0;
            snd_to_add.Volume = 1.0F;
            snd_to_add.Pitch = 1.0F;
            snd_to_add.AudioID = audioID;
            snd_to_add.AudioFile = RaudioFile;
            snd_to_add.AudioGroup = groupID;
            snd_to_add.GroupID = (needAGRP ? audioGroupID : Data.GetBuiltinSoundGroupID());
        }
        else
        {
            existing_snd.AudioFile = RaudioFile;
            existing_snd.AudioID = audioID;
            //ChangeSelection(existing_snd);
        }
    }
});
DisableAllSyncBindings();

await StopProgressBarUpdater();
ScriptMessage("Sound added successfully!\nEnjoy your meowing day!");


void IncProgressLocal()
{
    if (GetProgress() < maxCount)
        IncrementProgress();
}
