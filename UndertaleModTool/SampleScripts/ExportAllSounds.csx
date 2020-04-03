using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

int progress = 0;
string sndFolder = GetFolder(FilePath) + "Export_Sounds" + Path.DirectorySeparatorChar;

if (Directory.Exists(sndFolder)) {
    ScriptError("A sound export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(sndFolder);
UpdateProgress();
await DumpSounds();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + sndFolder);

void UpdateProgress() {
    UpdateProgressBar(null, "Sounds", progress++, Data.Sounds.Count);
}

string GetFolder(string path) {
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}


async Task DumpSounds() {
    await Task.Run(() => Parallel.ForEach(Data.Sounds, DumpSound));
}

void DumpSound(UndertaleSound sound) {
    if (sound.AudioFile != null && !File.Exists(sndFolder + sound.File.Content))
        File.WriteAllBytes(sndFolder + sound.File.Content, sound.AudioFile.Data);
    UpdateProgress();
}
