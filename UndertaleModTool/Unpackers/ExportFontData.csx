// Made by mono21400

using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using System.Linq;
using System.Windows.Forms;

EnsureDataLoaded();

int progress = 0;
string fntFolder = GetFolder(FilePath) + "Export_Fonts" + Path.DirectorySeparatorChar;
TextureWorker worker = new TextureWorker();
Directory.CreateDirectory(fntFolder);
List<string> input = new List<string>();

if (ShowInputDialog() == System.Windows.Forms.DialogResult.Cancel)
    return;

string[] arrayString = input.ToArray();

UpdateProgress();

await DumpFonts();
worker.Cleanup();

HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + fntFolder);


void UpdateProgress()
{
    UpdateProgressBar(null, "Fonts", progress, Data.Fonts.Count);
    Interlocked.Increment(ref progress); //"thread-safe" increment
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

async Task DumpFonts()
{
    await Task.Run(() => Parallel.ForEach(Data.Fonts, DumpFont));

    progress--;
}

void DumpFont(UndertaleFont font)
{
    if (arrayString.Contains(font.Name.ToString().Replace("\"", "")))
    {
        worker.ExportAsPNG(font.Texture, fntFolder + font.Name.Content + ".png");
        using (StreamWriter writer = new StreamWriter(fntFolder + "glyphs_" + font.Name.Content + ".csv"))
        {
            writer.WriteLine(font.DisplayName + ";" + font.EmSize + ";" + font.Bold + ";" + font.Italic + ";" + font.Charset + ";" + font.AntiAliasing + ";" + font.ScaleX + ";" + font.ScaleY);

            foreach (var g in font.Glyphs)
            {
                writer.WriteLine(g.Character + ";" + g.SourceX + ";" + g.SourceY + ";" + g.SourceWidth + ";" + g.SourceHeight + ";" + g.Shift + ";" + g.Offset);
            }
        }
    }

    UpdateProgress();
}

private DialogResult ShowInputDialog()
{
    System.Drawing.Size size = new System.Drawing.Size(400, 400);
    Form inputBox = new Form();
    bool check_all = true;

    inputBox.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
    inputBox.ClientSize = size;
    inputBox.Text = "Fonts exporter";

    System.Windows.Forms.CheckedListBox fonts_list = new CheckedListBox();
    //fonts_list.Items.Add("All");
    foreach (var x in Data.Fonts)
    {
        fonts_list.Items.Add(x.Name.ToString().Replace("\"", ""));
    }

    fonts_list.Size = new System.Drawing.Size(size.Width - 10, size.Height - 50);
    fonts_list.Location = new System.Drawing.Point(5, 5);
    inputBox.Controls.Add(fonts_list);

    Button okButton = new Button();
    okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
    okButton.Name = "okButton";
    okButton.Size = new System.Drawing.Size(75, 23);
    okButton.Text = "&OK";
    okButton.Location = new System.Drawing.Point(size.Width - 80 - 80, size.Height - 39);
    inputBox.Controls.Add(okButton);

    Button cancelButton = new Button();
    cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
    cancelButton.Name = "cancelButton";
    cancelButton.Size = new System.Drawing.Size(75, 23);
    cancelButton.Text = "&Cancel";
    cancelButton.Location = new System.Drawing.Point(size.Width - 85, size.Height - 39);
    inputBox.Controls.Add(cancelButton);

    Button toggleSelAllButton = new Button();
    toggleSelAllButton.Name = "toggleSelAllButton";
    toggleSelAllButton.Size = new System.Drawing.Size(80, 23);
    toggleSelAllButton.Text = "&Select All";
    toggleSelAllButton.Location = new System.Drawing.Point(size.Width - 160 - 160 - 75, size.Height - 39);
    inputBox.Controls.Add(toggleSelAllButton);
    toggleSelAllButton.Click += toggleSelAllButton_Click;

    inputBox.AcceptButton = okButton;
    inputBox.CancelButton = cancelButton;

    void toggleSelAllButton_Click(object sender, EventArgs e)
    {
        for (int i = 0; i < fonts_list.Items.Count; i++)
        {
            fonts_list.SetItemChecked(i, check_all);
        }
        if (check_all == true)
        {
            toggleSelAllButton.Text = "&Un-select All";
            check_all = false;
        }
        else
        {
            toggleSelAllButton.Text = "&Select All";
            check_all = true;
        }
    }

    DialogResult result = inputBox.ShowDialog();

    foreach (var item in fonts_list.CheckedItems)
    {
        input.Add(item.ToString());
    }

    return result;
}