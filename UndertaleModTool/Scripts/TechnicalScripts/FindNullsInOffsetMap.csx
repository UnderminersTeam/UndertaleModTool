using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using UndertaleModLib;

OpenFileDialog dlg = new OpenFileDialog();

dlg.DefaultExt = "win";
dlg.Filter = "Game Maker Studio data files (.win, .unx, .ios)|*.win;*.unx;*.ios|All files|*";

if (dlg.ShowDialog() == true)
{
    SaveFileDialog dlgout = new SaveFileDialog();
    dlgout.DefaultExt = "txt";
    dlgout.Filter = "Text files (.txt)|*.txt|All files|*";
    dlgout.FileName = dlg.FileName + ".nulltrunc.txt";

    OpenFileDialog dlgin = new OpenFileDialog();
    dlgin.DefaultExt = "txt";
    dlgin.Filter = "Text files (.txt)|*.txt|All files|*";
    dlgin.FileName = "Select the null_offsets.txt file.";
    if (dlgin.ShowDialog() == false)
    {
        MessageBox.Show("An error occured.", "Load error", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
    }
    uint scrutiny = 0;
    while (scrutiny == 0)
    {
        try
        {
            scrutiny = UInt32.Parse(SimpleTextInput("Scrutiny.", "Select context scope (default 1000 bytes)", "1000", false));
        }
        catch
        {
        }
    }
    if (dlgout.ShowDialog() == true)
    {
        Task t = Task.Run(() =>
        {
            try
            {
                StreamReader file = new StreamReader(dlgin.FileName);
                string line;
                line = file.ReadLine();
                using (var stream = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read))
                {
                    var offsets = UndertaleIO.GenerateOffsetMap(stream);
                    using (var writer = File.CreateText(dlgout.FileName))
                    {
                        while((line = file.ReadLine()) != null)
                        {
                            writer.WriteLine("");
                            writer.WriteLine("NULL ERROR: " + line);
                            uint check_me = 0;
                            var fetched = line.Substring(line.LastIndexOf("at 0x") + 5);
                            //MessageBox.Show(fetched, "Load error", MessageBoxButton.OK, MessageBoxImage.Error);
                            check_me = uint.Parse(fetched, System.Globalization.NumberStyles.HexNumber);
                            foreach(var off in offsets.OrderBy((x) => x.Key))
                            {
                                if ((Math.Abs(uint.Parse(off.Key.ToString("X8"), System.Globalization.NumberStyles.HexNumber) - check_me)) < scrutiny)
                                    writer.WriteLine(off.Key.ToString("X8") + " " + off.Value.ToString().Replace("\n", "\\\n"));
                            }
                            writer.WriteLine("///////////////////////////////////////////////////////////////////////////////////////////////");
                        }
                        file.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured while trying to load:\n" + ex.Message, "Load error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        });
        await t;
    }
}
