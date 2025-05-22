using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UndertaleModLib;

string dlg = PromptLoadFile("win", "GameMaker data files (.win, .unx, .ios)|*.win;*.unx;*.ios|All files|*");

if (!string.IsNullOrEmpty(dlg))
{
    string dlgout = PromptSaveFile("txt", "Text files (.txt)|*.txt|All files|*");

    string dlgin = PromptLoadFile("txt", "Text files (.txt)|*.txt|All files|*");
    if (string.IsNullOrEmpty(dlgin))
    {
        ScriptMessage("An error occured.");
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
    if (!string.IsNullOrEmpty(dlgout))
    {
        Task t = Task.Run(() =>
        {
            try
            {
                StreamReader file = new StreamReader(dlgin);
                string line;
                line = file.ReadLine();
                using (var stream = new FileStream(dlg, FileMode.Open, FileAccess.Read))
                {
                    var offsets = UndertaleIO.GenerateOffsetMap(stream);
                    using (var writer = File.CreateText(dlgout))
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
                ScriptError("An error occured while trying to load:\n" + ex.Message);
            }

        });
        await t;
    }
}
