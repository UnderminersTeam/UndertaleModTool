// Backup System by Grossley.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

EnsureDataLoaded();

if (!ScriptQuestion(@"This script requires permission to write to the game directory. 
If you are editing Deltarune from its default installation location this script may not function properly.
Please make sure UndertaleModTool can write to the game directory.
If it cannot, or you are not sure, please select 'no' to cancel the script."))
{
    SetUMTConsoleText("Setup has been cancelled.");
    SetFinishedMessage(false);
    return;
}

int progress = 0;
bool debug_override = false;

// Setup root export folder.
string winFolder = GetFolder(FilePath); // The folder data.win is located in.
string BackupFolder = winFolder + "/Backups/";

if (!(Directory.Exists(BackupFolder)))
{
    Directory.CreateDirectory(BackupFolder);
}

System.Timers.Timer aTimer;
// Create a timer with a minute interval defined by 'minutes_per_backup'.

double minutes_per_backup = 0;
while (true)
{
    try
    {
        //                               SimpleTextInput(string titleText,                                                                      string labelText, string defaultInputBoxText, bool isMultiline)
        double result = Convert.ToDouble(SimpleTextInput("Automatic backup script.", "Please enter the backup interval in minutes (minimum interval 5 minutes).", "30", false));
        if ((result < 5) && (!debug_override))
        {
            continue;
        }
        else
        {
            minutes_per_backup = result;
            break;
        }
    }
    catch (FormatException)
    {
        continue;
    }
}

int maximum_number_of_runs = 0;
while (true)
{
    try
    {
        //                            SimpleTextInput(string titleText,                                                                      string labelText, string defaultInputBoxText, bool isMultiline)
        int result2 = Convert.ToInt32(SimpleTextInput("Automatic backup script.", "Please enter the maximum number of unique backups this session (0 for unlimited).", "200", false));
        if (result2 < 0)
        {
            continue;
        }
        else
        {
            maximum_number_of_runs = result2;
            break;
        }
    }
    catch (FormatException)
    {
        continue;
    }
}

double number_of_runs_per_continue_prompt = 5;
double times_ran = 0;
double backups_count = 0;
double timer_delay = 1000*60*minutes_per_backup;
aTimer = new System.Timers.Timer(timer_delay);
// Hook up the Elapsed event for the timer. 
aTimer.Elapsed += OnTimedEvent;
aTimer.AutoReset = true;
aTimer.Enabled = true;
ScriptMessage("Automatic backup activated. The game will be backed up every " + minutes_per_backup.ToString() + " minutes.");
if ((maximum_number_of_runs <= 5) && (maximum_number_of_runs != 0))
{
    number_of_runs_per_continue_prompt = 1;
}
ScriptMessage("You will get a prompt regarding whether you wish to continue making automatic backups after every " + number_of_runs_per_continue_prompt.ToString() + " backups. (" + (minutes_per_backup * number_of_runs_per_continue_prompt).ToString() + " minutes).");
if (maximum_number_of_runs != 0)
{
    ScriptMessage("Automatic backups will end after " + maximum_number_of_runs.ToString() + " unique backups. (" + (minutes_per_backup * maximum_number_of_runs).ToString() + " minutes).");
}
String older_filename = "";
String filename = "";
void OnTimedEvent(Object source, ElapsedEventArgs e)
{
    if ((maximum_number_of_runs == 0) || (backups_count < maximum_number_of_runs))
    {
        String current_date_and_time = DateTime.Now.ToString("_\\M\\D\\Y_\\H\\M\\S_MM-dd-yyyy_HH-mm-ss");
        times_ran += 1;
        if (Data == null || Data.UnsupportedBytecodeVersion)
        {
            aTimer.Stop();
            aTimer.Dispose();
            ScriptError("Cannot save due to null data file or unsupported bytecode version.", "Save error");
            ScriptMessage("Automatic backups are off. Run the script again to turn back on automatic backups.");
            SetUMTConsoleText("Automatic backups are off. Run the script again to turn back on automatic backups.");
            SetFinishedMessage(false);
            return;
        }
        filename = "data_backup" + current_date_and_time + ".win";
        String SaveFilePath = BackupFolder + filename;
        //ScriptMessage("Now saving to " + SaveFilePath);
        try
        {
            using (var stream = new FileStream(SaveFilePath, FileMode.Create, FileAccess.Write))
            {
                UndertaleIO.Write(stream, Data);
            }
        }
        catch(Exception error)
        {
            ScriptError("An error occured while trying to save:\n" + error.Message, "Save error");
            ScriptMessage("Automatic backups are off. Run the script again to turn back on automatic backups.");
            aTimer.Stop();
            aTimer.Dispose();
            SetUMTConsoleText("Automatic backups are off. Run the script again to turn back on automatic backups.");
            SetFinishedMessage(false);
            return;
        }
        if (times_ran > 1)
            FileCompare();
        else
        {
            older_filename = filename;
            backups_count += 1;
        }
        if (((times_ran % number_of_runs_per_continue_prompt) == 0) && (times_ran != 0))
        {
            if (!(ScriptQuestion("The application has been running for about: " + (times_ran*minutes_per_backup).ToString() + " minutes with " + times_ran.ToString() + " runs this session. The current time is: " + DateTime.Now + ". Continue backing up?")))
            {
                aTimer.Stop();
                aTimer.Dispose();
                ScriptMessage("Automatic backups are off. Run the script again to turn back on automatic backups.");
                SetUMTConsoleText("Automatic backups are off. Run the script again to turn back on automatic backups.");
                SetFinishedMessage(false);
                return;
            }
        }
    }
    else
    {
        aTimer.Stop();
        aTimer.Dispose();
        ScriptMessage(backups_count.ToString() + " unique backups have been reached. (" + (backups_count*minutes_per_backup).ToString() + " minutes). Automatic backups are now off. Run the script again to turn back on automatic backups.");
        SetUMTConsoleText(backups_count.ToString() + " unique backups have been reached. (" + (backups_count*minutes_per_backup).ToString() + " minutes). Automatic backups are now off. Run the script again to turn back on automatic backups.");
        SetFinishedMessage(false);
        return;
    }
}

string GetFolder(string path) 
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

void FileCompare()
{
    string old_data_path = BackupFolder + older_filename;
    string new_data_path = BackupFolder + filename;
    int file1byte;
    int file2byte;
    int file3byte;
    int file4byte;
    FileStream fs1;
    FileStream fs2;
    FileStream fs3;
    FileStream fs4;

    //safety test
    try
    {
        fs3 = new FileStream(old_data_path, FileMode.Open);
        fs4 = new FileStream(new_data_path, FileMode.Open);
        file3byte = fs3.ReadByte();
        file4byte = fs4.ReadByte();
        fs3.Close();
        fs4.Close();
    }
    catch
    {
        older_filename = filename;
        return;
    }
    
    // Open the two files.
    fs1 = new FileStream(old_data_path, FileMode.Open);
    fs2 = new FileStream(new_data_path, FileMode.Open);
    // Check the file sizes. If they are not the same, the files
    // are not the same.
    if (fs1.Length != fs2.Length)
    {
        // Close the file
        fs1.Close();
        fs2.Close();
        older_filename = filename;
        backups_count += 1;
        // Return false to indicate files are different
        return;
    }
    else
    {
        // Read and compare a byte from each file until either a
        // non-matching set of bytes is found or until the end of
        // file1 is reached.
        do
        {
            // Read one byte from each file.
            file1byte = fs1.ReadByte();
            file2byte = fs2.ReadByte();
        }
        while ((file1byte == file2byte) && (file1byte != -1));

        // Close the files.
        fs1.Close();
        fs2.Close();

        // Return the success of the comparison. "file1byte" is
        // equal to "file2byte" at this point only if the files are
        // the same.
        if ((file1byte - file2byte) == 0)
        {
            File.Delete(new_data_path);
        }
        else
        {
            older_filename = filename;
            backups_count += 1;
        }
    }
}


string isLimited = "";
string minutes_per_backup_string = minutes_per_backup.ToString();
if (maximum_number_of_runs != 0)
{
    isLimited = " Automatic backups will end after " + maximum_number_of_runs.ToString() + " unique backups. (" + (minutes_per_backup * maximum_number_of_runs).ToString() + " minutes).";
}

SetUMTConsoleText("Automatic backup activated. The game will be checked for changes every " + minutes_per_backup_string + " minutes and, if so, a backup will be made to your computer." + isLimited);
SetFinishedMessage(false);
