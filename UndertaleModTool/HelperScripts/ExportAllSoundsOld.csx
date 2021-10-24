using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

int progress = 0;
string sndFolder = GetFolder(FilePath) + "Export_Sounds" + Path.DirectorySeparatorChar;
CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
CancellationToken token = cancelTokenSource.Token;

if (Directory.Exists(sndFolder)) 
{
    ScriptError("A sound export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(sndFolder);

Task.Run(ProgressUpdater);

await DumpSounds();

cancelTokenSource.Cancel(); //stop ProgressUpdater
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + sndFolder);


void UpdateProgress()
{
    UpdateProgressBar(null, "Sounds", progress, Data.Sounds.Count);
}
void IncProgress()
{
    Interlocked.Increment(ref progress); //"thread-safe" increment
}
async Task ProgressUpdater()
{
    while (true)
    {
        if (token.IsCancellationRequested)
            return;

        UpdateProgress();

        await Task.Delay(100); //10 times per second
    }
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}


async Task DumpSounds() 
{
    await Task.Run(() => Parallel.ForEach(Data.Sounds, DumpSound));

    progress--;
}

void DumpSound(UndertaleSound sound) 
{
    if (sound.AudioFile != null && !File.Exists(sndFolder + sound.File.Content))
        File.WriteAllBytes(sndFolder + sound.File.Content, sound.AudioFile.Data);

    IncProgress();
}
