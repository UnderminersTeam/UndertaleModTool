// by porog
// TODO: this heavily uses Windows stuff, should be made cross platform

using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using UndertaleModLib.Util;
using ImageMagick;

EnsureDataLoaded();

UndertaleFont font = FontPickerResult(); //GUI dropdown selection list of fonts
if (font == null) return; //the 'Cancel' or 'X' button is hit
using (TextureWorker textureWorker = new())
{
    new FontEditorGUI(font, textureWorker).ShowDialog(); //font editor GUI
}

class FontEditorGUI : Form
{
    UndertaleFont font;
    
    List<Letter> letterData = new List<Letter>();
    TextureWorker textureWorker = null;
    
    ListView listView;
    bool savePrompt = false;
    public FontEditorGUI(UndertaleFont font, TextureWorker textureWorker)
    {
        this.font = font;
        this.textureWorker = textureWorker;
        
        Text = font.Name.Content;
        MinimumSize = new Size(275, 150);
        StartPosition = FormStartPosition.CenterScreen;
        TrySetFormIcon(this);
        
        MenuStrip menuBar = new MenuStrip();
        Controls.Add(menuBar);
        ToolStripMenuItem fileMenu = new ToolStripMenuItem("&File");
        ToolStripMenuItem saveOption = new ToolStripMenuItem("&Save");
        saveOption.Click += saveOptionClick;
        fileMenu.DropDownItems.Add(saveOption);
        menuBar.Items.Add(fileMenu);
        ToolStripMenuItem editMenu = new ToolStripMenuItem("&Edit");
        ToolStripMenuItem exportOption = new ToolStripMenuItem("Export font to &ZIP");
        exportOption.Click += exportOptionClick;
        ToolStripMenuItem importOption = new ToolStripMenuItem("Import font f&rom ZIP");
        importOption.Click += importOptionClick;
        editMenu.DropDownItems.Add(importOption);
        editMenu.DropDownItems.Add(exportOption);
        menuBar.Items.Add(editMenu);
        
        listView = new ListView()
        {
            View = View.Details,
            HideSelection = false,
            FullRowSelect = true,
            Location = new Point(5, menuBar.Height + 5),
            GridLines = true,
            Size = new Size(this.Size.Width - 26, this.Size.Height - 96),
            Anchor =  AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom
        };
        listView.Columns.Add("Char", 80);
        listView.Columns.Add("Char ID", 80);
        Controls.Add(listView);
        
        int buttonY = this.Size.Height - 65;
        Button editBtn = new Button()
        {
            Text = "Edi&t",
            Location = new Point(5, buttonY),
            Anchor =  AnchorStyles.Left | AnchorStyles.Bottom
        };
        Controls.Add(editBtn);
        Button duplicateBtn = new Button()
        {
            Text = "Duplic&ate",
            Location = new Point(5 + editBtn.Location.X + editBtn.Size.Width, buttonY),
            Anchor =  AnchorStyles.Left | AnchorStyles.Bottom
        };
        Controls.Add(duplicateBtn);
        Button deleteBtn = new Button()
        {
            Text = "&Delete",
            Location = new Point(5 + duplicateBtn.Location.X + duplicateBtn.Size.Width, buttonY),
            Anchor =  AnchorStyles.Left | AnchorStyles.Bottom
        };
        Controls.Add(deleteBtn);
        
        EventHandler editBtnClick = (o, e) =>
        {
            Letter letter = letterData[listView.SelectedIndices[0]];
            if (letter.Bitmap == null)
            {
                MessageBox.Show("Cannot edit this letter because it has a"
                    + " width or height of 0. You have to delete it and"
                    + " re-add it.");
                return;
            }
            EditLetterGUI editLetterGui = new EditLetterGUI(letter);
            editLetterGui.ShowDialog();
            if (editLetterGui.ChangesMade)
            {
                savePrompt = true;
                repaintThumbnails();
            }
        };
        editBtn.Click += editBtnClick;
        duplicateBtn.Click += (o, e) =>
        {
            Letter originalLetter = letterData[listView.SelectedIndices[0]];
            
            bool formCanceled = true;
            Form form = new Form()
            {
                Size = new Size(210,140),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                MaximizeBox = false,
                MinimizeBox = false,
                Text = "New letter",
                StartPosition = FormStartPosition.CenterScreen
            };
            TrySetFormIcon(form);
            
            Label letterLabel = new Label()
            {
                Location = new Point(5, 5),
                Text = "Enter letter:"
            };
            letterLabel.Size = new Size(80, letterLabel.Size.Height);
            form.Controls.Add(letterLabel);
            TextBox letterBox = new TextBox()
            {
                Location = new Point(95, 5)
            };
            letterBox.Size = new Size(25, letterBox.Size.Height);
            form.Controls.Add(letterBox);
            
            Label letterIDLabel = new Label()
            {
                Location = new Point(5, 40),
                Text = "Enter letter ID:"
            };
            letterIDLabel.Size = new Size(90, letterIDLabel.Size.Height);
            form.Controls.Add(letterIDLabel);
            NumericUpDown letterIDBox = new NumericUpDown()
            {
                Location = new Point(95, 40),
                Minimum = 0,
                Maximum = char.MaxValue
            };
            letterIDBox.Size = new Size(91, letterIDBox.Size.Height);
            form.Controls.Add(letterIDBox);
            Label orLabel = new Label()
            {
                Location = new Point(35, 22),
                Text = "or"
            };
            form.Controls.Add(orLabel);
            
            Action<int> setCharacter = delegate(int character)
            {
                letterBox.Text = ((char)character).ToString();
                letterIDBox.Value = character;
            };
            letterBox.TextChanged += (o, e) =>
            {
                if (letterBox.Text.Length == 0)
                {
                    letterIDBox.Text = "";
                    return;
                }
                int character = (int)letterBox.Text.ToCharArray()[0];
                setCharacter(character);
            };
            letterIDBox.ValueChanged += (o, e) =>
                setCharacter((int)letterIDBox.Value);
                
            int buttonY = form.Size.Height - 66;
            Button okBtn = new Button()
            {
                Location = new Point(5, buttonY),
                Size = new Size(45, 23),
                Text = "&OK"
            };
            form.Controls.Add(okBtn);
            Button cancelBtn = new Button()
            {
                Location = new Point(5 + okBtn.Size.Width + okBtn.Location.X, buttonY),
                Size = new Size(59, 23),
                Text = "&Cancel"
            };
            form.Controls.Add(cancelBtn);
            
            okBtn.Click += (o, e) =>
            {
                if (letterBox.Text.Length == 0
                    || letterIDBox.Text.Length == 0)
                {
                    MessageBox.Show("The text box is empty");
                    return;
                }
                if (letterData.FirstOrDefault(
                    letter => letter.Character ==
                        (ushort)letterIDBox.Value)
                            != null)
                {
                    MessageBox.Show(
                        "The entered letter already exists"
                        + " in this font");
                    return;
                }
                
                formCanceled = false;
                form.Close();
            };
            cancelBtn.Click += (o, e) => form.Close();
            
            setCharacter((int)originalLetter.Character);
            form.ShowDialog();
            
            if (!formCanceled)
            {
                ushort character = (ushort)letterIDBox.Value;
                short shift = originalLetter.Shift;
                short offset = originalLetter.Offset;
                Bitmap bitmap = new Bitmap(originalLetter.Bitmap);
                
                Letter letter = new Letter(character, bitmap, shift, offset);
                letterData.Add(letter);
                repopulate();
                
                int itemIndex = listView.Items.IndexOf(
                    listView.Items.Cast<ListViewItem>().First(
                        item => item.SubItems[1].Text == character.ToString()));
                listView.EnsureVisible(itemIndex);
                listView.Items[itemIndex].Selected = true;
                repaintThumbnails();
                listView.Focus();
                savePrompt = true;
            }
        };
        deleteBtn.Click += (o, e) =>
        {
            if (listView.SelectedIndices.Count >= listView.Items.Count)
            {
                MessageBox.Show("Cannot delete all letters");
                return;
            }
            if (MessageBox.Show(
                "Delete selection?",
                "Delete",
                MessageBoxButtons.YesNo)
                    != DialogResult.Yes)
                return;
                
            List<ListViewItem> delQueue = new List<ListViewItem>();
            List<Letter> delQueue2 = new List<Letter>();
            foreach (int index in listView.SelectedIndices)
            {
                delQueue.Add(listView.Items[index]);
                delQueue2.Add(letterData[index]);
            }
            
            foreach (ListViewItem item in delQueue)
                item.Remove();
            foreach (Letter letter in delQueue2)
                letterData.Remove(letter);
        };
        
        EventHandler indexChanged = (o, e) =>
        {
            bool hasSelection = listView.SelectedIndices.Count > 0;
            bool oneSelected = listView.SelectedIndices.Count == 1;
            editBtn.Enabled = hasSelection && oneSelected;
            duplicateBtn.Enabled = hasSelection && oneSelected;
            deleteBtn.Enabled = hasSelection;
        };
        listView.SelectedIndexChanged += indexChanged;
        listView.MouseDoubleClick += (o, e) =>
        {
            if (editBtn.Enabled)
                editBtnClick(o, e);
        };
        indexChanged(null, null);
        
        FormClosing += (o, e) =>
        {
            if (savePrompt)
            {
                DialogResult dialogResult = MessageBox.Show(
                    "Exit without saving?",
                    "Font editor",
                    MessageBoxButtons.YesNo);
                if (dialogResult != DialogResult.Yes)
                    ((FormClosingEventArgs)e).Cancel = true;
            }
        };

        //Populate list from font
        using IMagickImage<byte> fontSheetMagickImg = textureWorker.GetTextureFor(font.Texture, null);
        IUnsafePixelCollection<byte> fontSheetMagickPixels = fontSheetMagickImg.GetPixelsUnsafe();
        Bitmap fontSheetImg = new Bitmap(fontSheetMagickImg.Width, fontSheetMagickImg.Height, 4 * fontSheetMagickImg.Width, PixelFormat.Format32bppArgb,
                                         fontSheetMagickPixels.GetAreaPointer(0, 0, fontSheetMagickImg.Width, fontSheetMagickImg.Height));
        List<Letter> letters = new List<Letter>();
        foreach (UndertaleFont.Glyph glyph in font.Glyphs)
        {
            Rectangle cropArea = new Rectangle(
                glyph.SourceX,
                glyph.SourceY, 
                glyph.SourceWidth,
                glyph.SourceHeight);
            Bitmap bitmap = 
                glyph.SourceWidth == 0 || glyph.SourceHeight == 0
                ? null
                : fontSheetImg.Clone(cropArea, fontSheetImg.PixelFormat);
            
            Letter letter = new Letter(glyph.Character, bitmap, glyph.Shift, glyph.Offset);
            letters.Add(letter);
        }
        populate(letters);
    }
    
    //GUI table
    void populate(List<Letter> letters)
    {
        letterData.Clear();
        foreach (Letter letter in letters)
            letterData.Add(letter);
        repopulate();
    }
    void repopulate()
    {
        letterData.Sort((a, b) => a.Character.CompareTo(b.Character));
        
        listView.Items.Clear();
        foreach (Letter letter in letterData)
        {
            string[] item = new string[2];
            item[0] = ((char)letter.Character).ToString();
            item[1] = letter.Character.ToString();
            ListViewItem listViewItem = new ListViewItem(item);
            listViewItem.ImageKey = item[1];
            listView.Items.Add(listViewItem);
        }
        
        repaintThumbnails();
    }
    
    static readonly Image blankThumbnail = new Bitmap(1, 1);
    void repaintThumbnails()
    {
        ImageList imageList = new ImageList();
        for (int i = 0; i < listView.Items.Count; i++)
        {
            string imageKey = i.ToString();
            Image imageValue =
                letterData[i].Bitmap == null
                ? blankThumbnail
                : enlargeBitmap(invertColors(letterData[i].Bitmap), 2);
            listView.Items[i].ImageKey = imageKey;
            imageList.Images.Add(
                imageKey,
                imageValue);
        }
        listView.SmallImageList = imageList;
        listView.Refresh();
    }
    
    //Import and export
    const string
        dialogFilter = "ZIP files (*.zip)|*.zip|All files (*.*)|*.*",
        dialogExt = "zip",
        extraCharDataFile = "otherletters.csv";
    void importOptionClick(object sender, EventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog()
        {
            Filter = dialogFilter,
            DefaultExt = dialogExt
        };
        if (openFileDialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }
        
        List<Letter> importData = new List<Letter>();
        using (ZipArchive archive = ZipFile.OpenRead(openFileDialog.FileName))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string fileName = entry.FullName;
                if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    string[] deserialize = fileName.Replace(".png", "").Split(';');
                    for (int i = 0; i < deserialize.Length; i++)
                        deserialize[i] =
                            deserialize[i].Substring(
                                1 + deserialize[i].IndexOf('='));
                    
                    ushort character = UInt16.Parse(deserialize[0]);
                    short shift = Int16.Parse(deserialize[1]);
                    short offset = Int16.Parse(deserialize[2]);
                    Bitmap bitmap = null;
                    using (Stream stream = entry.Open())
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            bitmap = new Bitmap(ms);
                        }
                    }
                    
                    Letter letter = new Letter(character, bitmap, shift, offset);
                    importData.Add(letter);
                }
                else if (fileName.Equals(extraCharDataFile))
                {
                    string fileContent = null;
                    using (Stream stream = entry.Open())
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            fileContent = Encoding.ASCII.GetString(ms.ToArray());
                        }
                    }
                    
                    foreach (string tuple in fileContent.Split('\n'))
                    {
                        string[] deserialize = tuple.Split(';');
                        for (int i = 0; i < deserialize.Length; i++)
                        deserialize[i] =
                            deserialize[i].Substring(
                                1 + deserialize[i].IndexOf('='));
                                
                        ushort character = UInt16.Parse(deserialize[0]);
                        short shift = Int16.Parse(deserialize[1]);
                        short offset = Int16.Parse(deserialize[2]);
                        Bitmap bitmap = null;
                        
                        Letter letter = new Letter(character, bitmap, shift, offset);
                        importData.Add(letter);
                    }
                }
            }
        }
        
        populate(importData);
        savePrompt = true;
        MessageBox.Show("Successfully imported");
    }
    void exportOptionClick(object sender, EventArgs e)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog()
        {
            Filter = dialogFilter,
            DefaultExt = dialogExt
        };
        if (saveFileDialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }
        
        using (FileStream fileStream = File.Create(saveFileDialog.FileName))
        {
            using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                StringBuilder extraCharData = new StringBuilder();
                bool saveExtraCharData = false;
                foreach (Letter letter in letterData)
                {
                    if (letter.Bitmap != null)
                    {
                        string fileName =
                            "char=" + letter.Character
                            + ";shift=" + letter.Shift
                            + ";offset=" + letter.Offset
                            + ".png";
                        ZipArchiveEntry entry = archive.CreateEntry(fileName);
                        Bitmap copy = new Bitmap(letter.Bitmap.Width, letter.Bitmap.Height);
                        using (Graphics g = Graphics.FromImage(copy))
                        {
                            g.DrawImage(letter.Bitmap, new Point(0, 0));
                        }
                        using (MemoryStream ms = new MemoryStream())
                        {
                            copy.Save(ms, ImageFormat.Png);
                            using (StreamWriter writer = new StreamWriter(entry.Open()))
                            {
                                byte[] imgBytes = ms.ToArray();
                                writer.BaseStream.Write(imgBytes, 0, imgBytes.Length);
                            }
                        }
                    }
                    else
                    {
                        saveExtraCharData = true;
                        string appendix =
                            "char=" + letter.Character
                            + ";width=0;height=0"
                            + ";shift=" + letter.Shift
                            + ";offset=" + letter.Offset
                            + "\n";
                        extraCharData.Append(appendix);
                    }
                }
                
                if (saveExtraCharData)
                {
                    extraCharData.Length = extraCharData.Length - 1;
                    ZipArchiveEntry entry = archive.CreateEntry(extraCharDataFile);
                    using (StreamWriter writer = new StreamWriter(entry.Open()))
                    {
                        byte[] content = Encoding.ASCII.GetBytes(extraCharData.ToString());
                        writer.BaseStream.Write(content, 0, content.Length);
                    }
                }
            }
        }
        
        MessageBox.Show("Exported successfully");
    }
    
    //Save
    void saveOptionClick(object sender, EventArgs e)
    {
        List<Letter> exportData = new List<Letter>();
        foreach (Letter letter in letterData)
            exportData.Add(letter);
        exportData.Sort((a, b) =>
        {
            int aHeight = a.Bitmap == null ? 0 : a.Bitmap.Height;
            int bHeight = b.Bitmap == null ? 0 : b.Bitmap.Height;
            return bHeight.CompareTo(aHeight);
        });
        
        //Generate font sheet image and glyph points
        Bitmap fontSheetImg = new Bitmap(font.Texture.SourceWidth, font.Texture.SourceHeight);
        List<Point> glyphPoints = new List<Point>();
        int gap = 1;
        
        int xPos = gap;
        int yPos = gap;
        int rowMaxHeight = -1;
        foreach (Letter letter in exportData)
        {
            bool letterDrawable = letter.Bitmap != null;
            int letterWidth = letterDrawable ? letter.Bitmap.Width : 0;
            int letterHeight = letterDrawable ? letter.Bitmap.Height : 0;
            
            if (letterHeight > rowMaxHeight)
                rowMaxHeight = letterHeight;
            if (xPos + letterWidth > fontSheetImg.Width)
            {
                xPos = gap;
                yPos += rowMaxHeight + gap;
                rowMaxHeight = -1;
            }
            glyphPoints.Add(new Point(xPos, yPos));
            
            if (xPos + letterWidth > fontSheetImg.Width
            ||    yPos + letterHeight > fontSheetImg.Height)
            {
                MessageBox.Show("All characters do not fit in the texture.",
                    "Too many characters");
                return;
            }
            
            if (letterDrawable)
            {
                using (Graphics g = Graphics.FromImage(fontSheetImg))
                {
                    g.DrawImage(letter.Bitmap, new Point(xPos, yPos));
                }
            }
            
            xPos += letterWidth + gap;
        }
        
        //Set font sheet image
        Bitmap spriteSheetImg = null;
        using (var ms = new MemoryStream(font.Texture.TexturePage.TextureData.Image.ConvertToPng().ToSpan().ToArray()))
        {
            spriteSheetImg = new Bitmap(ms);
        }
        int x = font.Texture.SourceX;
        int y = font.Texture.SourceY;
        for (int i = x; i < x + fontSheetImg.Width; i++)
        {
            for (int ii = y; ii < y + fontSheetImg.Height; ii++)
            {
                spriteSheetImg.SetPixel(i, ii, Color.FromArgb(0, 255, 255, 255));
            }
        }
        using (Graphics g = Graphics.FromImage(spriteSheetImg))
        {
            g.DrawImage(fontSheetImg, new Point(x, y));
        }
        using (MemoryStream ms = new MemoryStream())
        {
            Bitmap copy = new Bitmap(spriteSheetImg.Width, spriteSheetImg.Height);
            using (Graphics g = Graphics.FromImage(copy))
            {
                g.DrawImage(spriteSheetImg, new Point(0, 0));
            }
            copy.Save(ms, ImageFormat.Png);
            
            font.Texture.TexturePage.TextureData.Image = GMImage.FromPng(ms.ToArray());
            font.Texture.TargetX = 0;
            font.Texture.TargetY = 0;
        }
        
        //Generate and set glyph data
        List<UndertaleFont.Glyph> glyphs = new List<UndertaleFont.Glyph>();
        for (int i = 0; i < exportData.Count; i++)
        {
            UndertaleFont.Glyph glyph = new UndertaleFont.Glyph();
            Letter letter = exportData[i];
            Point glyphPoint = glyphPoints[i];
            
            glyph.Character = letter.Character;
            glyph.SourceX = (ushort) glyphPoint.X;
            glyph.SourceY = (ushort) glyphPoint.Y;
            glyph.SourceWidth = letter.Bitmap == null ? (ushort) 0 : (ushort) letter.Bitmap.Width;
            glyph.SourceHeight = letter.Bitmap == null ? (ushort) 0 : (ushort) letter.Bitmap.Height;
            glyph.Shift = letter.Shift;
            glyph.Offset = letter.Offset;
            
            glyphs.Add(glyph);
        }
        glyphs.Sort((x, y) => x.Character.CompareTo(y.Character));
        font.Glyphs.Clear();
        foreach (UndertaleFont.Glyph glyph in glyphs)
            font.Glyphs.Add(glyph);
        font.RangeStart = (ushort) glyphs[0].Character;
        font.RangeEnd = glyphs[glyphs.Count - 1].Character;
        
        //Finish
        MessageBox.Show("Saved");
        savePrompt = false;
    }
}

class EditLetterGUI : Form
{
    Panel picturePanel;
    PictureBox pictureBox;
    
    static Size rememberSize = new Size(280,340);
    static Point rememberLocation = new Point(-1, -1);
    static bool rememberMaximized;
    static bool defaultLocation = true;
    
    CheckBox blackColorOption;
    CheckBox transparentColorOption;
    CheckBox whiteColorOption;
    
    static readonly Color
        black = Color.FromArgb(255, 255, 255, 255),
        transparent = Color.FromArgb(127, 255, 255, 255),
        white = Color.FromArgb(0, 255, 255, 255);
    
    public bool ChangesMade = false;
    bool savePrompt = false;
    
    int imgScale = 4;
    Bitmap rawImg;
    short shift, offset;
    Label
        widthLabel,
        heightLabel,
        shiftLabel,
        offsetLabel;
    public EditLetterGUI(Letter letter)
    {
        shift = letter.Shift;
        offset = letter.Offset;
        
        Text = ((char)letter.Character).ToString();
        if (defaultLocation)
            StartPosition = FormStartPosition.CenterScreen;
        else
        {
            Shown += (o, e) =>
            {
                this.Location = rememberLocation;
                if (rememberMaximized)
                    WindowState = FormWindowState.Maximized;
            };
        }
        Size = rememberSize;
        MinimumSize = new Size(170, 300);
        TrySetFormIcon(this);
        
        picturePanel = new Panel()
        {
            Location = new Point(5,5),
            BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        };
        pictureBox = new PictureBox();
        SetPictureBoxImage(letter.Bitmap);
        picturePanel.Controls.Add(pictureBox);
        Controls.Add(picturePanel);
        
        int rightButtonsX = picturePanel.Size.Width + 5;
        
        Label colorLabel = new Label()
        {
            Location = new Point(rightButtonsX, 5),
            Text = "Draw color:"
        };
        Controls.Add(colorLabel);
        blackColorOption = new CheckBox()
        {
            Location = new Point(rightButtonsX, 23),
            Text = "Black"
        };
        Controls.Add(blackColorOption);
        transparentColorOption = new CheckBox()
        {
            Location = new Point(rightButtonsX, 41),
            Text = "Transparent"
        };
        Controls.Add(transparentColorOption);
        whiteColorOption = new CheckBox()
        {
            Location = new Point(rightButtonsX, 59),
            Text = "White"
        };
        Controls.Add(whiteColorOption);
        SetCheckBoxOption(0);
        blackColorOption.CheckedChanged += (o, e) => SetCheckBoxOption(0);
        transparentColorOption.CheckedChanged += (o, e) => SetCheckBoxOption(1);
        whiteColorOption.CheckedChanged += (o, e) => SetCheckBoxOption(2);
        
        widthLabel = new Label()
        {
            Location = new Point(rightButtonsX, 82),
            Size = new Size(100, 16)
        };
        Controls.Add(widthLabel);
        heightLabel = new Label()
        {
            Location = new Point(rightButtonsX, 95),
            Size = new Size(100, 16)
        };
        Controls.Add(heightLabel);
        refreshWidthHeightLabels();
        Button sizeBtn = new Button()
        {
            Location = new Point(rightButtonsX, 110),
            Text = "Edit size"
        };
        Controls.Add(sizeBtn);
        sizeBtn.Click += sizeBtnClick;
        
        shiftLabel = new Label()
        {
            Location = new Point(rightButtonsX, 147),
            Size = new Size(100, 16)
        };
        Controls.Add(shiftLabel);
        offsetLabel = new Label()
        {
            Location = new Point(rightButtonsX, 162),
            Size = new Size(100, 16)
        };
        Controls.Add(offsetLabel);
        refreshShiftOffsetLabels();
        Button shiftOffsetBtn = new Button()
        {
            Location = new Point(rightButtonsX, 177),
            Size = new Size(75, 37),
            Text = "Edit shift/\noffset"
        };
        Controls.Add(shiftOffsetBtn);
        shiftOffsetBtn.Click += shiftOffsetBtnClick;
        
        Button saveBtn = new Button()
        {
            Location = new Point(rightButtonsX, 230),
            Text = "Save letter"
        };
        Controls.Add(saveBtn);
        
        movableControls.Add(colorLabel);
        movableControls.Add(blackColorOption);
        movableControls.Add(transparentColorOption);
        movableControls.Add(whiteColorOption);
        movableControls.Add(widthLabel);
        movableControls.Add(heightLabel);
        movableControls.Add(shiftLabel);
        movableControls.Add(offsetLabel);
        movableControls.Add(sizeBtn);
        movableControls.Add(shiftOffsetBtn);
        movableControls.Add(saveBtn);
        
        EventHandler pictureDraw = (o, e) =>
        {
            ChangesMade = true;
            savePrompt = true;
            MouseEventArgs e2 = (MouseEventArgs) e;
            int mag = imgScale * imgScale;
            int x = (int) ((double)e2.X / (double)mag);
            int y = (int) ((double)e2.Y / (double)mag);
            
            switch (checkBoxOption)
            {
                case 0:
                    rawImg.SetPixel(x, y, black);
                    break;
                case 1:
                    rawImg.SetPixel(x, y, transparent);
                    break;
                case 2:
                    rawImg.SetPixel(x, y, white);
                    break;
            }
            
            SetPictureBoxImage(rawImg);
        };
        pictureBox.Click += pictureDraw;
        
        //Picture box click and drag
        System.Timers.Timer mouseDownLoop = new System.Timers.Timer()
        {
            Interval = 40,
            AutoReset = true,
            Enabled = false
        };
        MouseEventArgs mouseEventArgs = null;
        MouseEventHandler mouseEventArgsRefresh = (o, e) =>
            mouseEventArgs = e;
        mouseDownLoop.Elapsed += (o, e) =>
            pictureDraw(null, mouseEventArgs);
        pictureBox.MouseDown += (o, e) =>
        {
            pictureBox.MouseMove += mouseEventArgsRefresh;
            mouseDownLoop.Enabled = true;
        };
        pictureBox.MouseUp += (o, e) =>
        {
            pictureBox.MouseMove -= mouseEventArgsRefresh;
            mouseDownLoop.Enabled = false;
        };
        
        saveBtn.Click += (o, e) =>
        {
            letter.Bitmap = new Bitmap(rawImg);
            letter.Shift = shift;
            letter.Offset = offset;
            savePrompt = false;
        };
        
        FormClosing += (o, e) =>
        {
            if (savePrompt)
            {
                DialogResult dialogResult = MessageBox.Show(
                    "Exit this letter without saving?",
                    "Letter edit",
                    MessageBoxButtons.YesNo);
                if (dialogResult != DialogResult.Yes)
                    ((FormClosingEventArgs)e).Cancel = true;
                else
                    ChangesMade = false;
            }
            defaultLocation = false;
            rememberMaximized = WindowState == FormWindowState.Maximized;
            if (!rememberMaximized)
            {
                rememberLocation = this.Location;
                rememberSize = this.Size;
            }
        };
        Resize += (o, e) =>
            SetPictureBoxImage(rawImg, true);
    }
    
    void SetPictureBoxImage(Bitmap glyphBitmap, bool forceResize = false)
    {
        bool resize =
            forceResize ||
            rawImg == null ||
            !(rawImg.Size.Equals(glyphBitmap.Size));
        rawImg = new Bitmap(glyphBitmap);
        pictureBox.Image = enlargeBitmap(invertColors(glyphBitmap), imgScale);
        if (resize)
        {
            pictureBox.Size = new Size(pictureBox.Image.Width, pictureBox.Image.Height);
            picturePanel.Size = pictureBox.Size;
            if (picturePanel.Size.Width > this.Size.Width - 117
                || picturePanel.Size.Height > this.Size.Height - 50)
            {
                picturePanel.AutoScroll = true;
                int height = pictureBox.Image.Height;
                if (picturePanel.Size.Height > this.Size.Height - 50)
                {
                    height = this.Size.Height - 50;
                }
                picturePanel.Size = new Size(this.Size.Width - 117, height);
            }
            else
            {
                picturePanel.AutoScroll = false;
            }
            
            positionControls();
        }
    }
    HashSet<Control> movableControls = new HashSet<Control>();
    void positionControls()
    {
        int rightButtonsX = picturePanel.Size.Width + 5;
        foreach (Control c in movableControls)
            c.Location = new Point(rightButtonsX , c.Location.Y);
    }
    
    void refreshWidthHeightLabels()
    {
        widthLabel.Text = $"Width: {rawImg.Width}";
        heightLabel.Text = $"Height: {rawImg.Height}";
    }
    void refreshShiftOffsetLabels()
    {
        shiftLabel.Text = $"Shift: {shift}";
        offsetLabel.Text = $"Offset: {offset}";
    }

    int checkBoxOption;
    bool recursiveCall = false;
    void SetCheckBoxOption(int option)
    {
        if (recursiveCall) return;
        recursiveCall = true;
        switch (option)
        {
            case 0:
                blackColorOption.Checked = true;
                transparentColorOption.Checked = false;
                whiteColorOption.Checked = false;
                checkBoxOption = 0;
                break;
            case 1:
                blackColorOption.Checked = false;
                transparentColorOption.Checked = true;
                whiteColorOption.Checked = false;
                checkBoxOption = 1;
                break;
            case 2:
                blackColorOption.Checked = false;
                transparentColorOption.Checked = false;
                whiteColorOption.Checked = true;
                checkBoxOption = 2;
                break;
        }
        recursiveCall = false;
    }
    
    void sizeBtnClick(object sender, EventArgs e)
    {
        TupleGUI tupleGui = new TupleGUI()
        {
            Title = "New size",
            FirstValueTitle = "Width",
            SecondValueTitle = "Height",
            FirstValue = rawImg.Width,
            SecondValue = rawImg.Height,
            Minimum = 1,
            Maximum = ushort.MaxValue
        };
        tupleGui.ShowDialog();
        if (tupleGui.ResultOK)
        {
            int width = tupleGui.FirstValue, height = tupleGui.SecondValue;
            if (width == rawImg.Width && height == rawImg.Height)
                return;
            Bitmap newImage = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(newImage))
            {
                g.DrawImage(rawImg, 0, 0);
            }
            SetPictureBoxImage(newImage);
            refreshWidthHeightLabels();
            ChangesMade = true;
            savePrompt = true;
        }
    }
    
    void shiftOffsetBtnClick(object sender, EventArgs e)
    {
        TupleGUI tupleGui = new TupleGUI()
        {
            Title = "Shift/offset",
            FirstValueTitle = "Shift",
            SecondValueTitle = "Offset",
            FirstValue = shift,
            SecondValue = offset,
            Minimum = short.MinValue,
            Maximum = short.MaxValue
        };
        tupleGui.ShowDialog();
        if (tupleGui.ResultOK)
        {
            short newShift = (short) tupleGui.FirstValue;
            short newOffset = (short) tupleGui.SecondValue;
            if (newShift == shift && newOffset == offset)
                return;
            shift = newShift;
            offset = newOffset;
            refreshShiftOffsetLabels();
            ChangesMade = true;
            savePrompt = true;
        }
    }
    
    class TupleGUI
    {
        public bool ResultOK
        {
            get
            {
                return resultVal;
            }
        }
        public string Title
        {
            get
            {
                return form.Text;
            }
            set
            {
                form.Text = value;
            }
        }
        public string FirstValueTitle
        {
            get
            {
                return firstValueLabel.Text;
            }
            set
            {
                firstValueLabel.Text = value;
            }
        }
        public string SecondValueTitle
        {
            get
            {
                return secondValueLabel.Text;
            }
            set
            {
                secondValueLabel.Text = value;
            }
        }
        
        public int FirstValue
        {
            get
            {
                return Convert.ToInt32(firstValueBox.Value);
            }
            set
            {
                firstValueBox.Value = value;
            }
        }
        public int SecondValue
        {
            get
            {
                return Convert.ToInt32(secondValueBox.Value);
            }
            set
            {
                secondValueBox.Value = value;
            }
        }
        public int Minimum
        {
            get
            {
                return Convert.ToInt32(firstValueBox.Minimum);
            }
            set
            {
                firstValueBox.Minimum = value;
                secondValueBox.Minimum = value;
            }
        }
        public int Maximum
        {
            get
            {
                return Convert.ToInt32(firstValueBox.Maximum);
            }
            set
            {
                firstValueBox.Maximum = value;
                secondValueBox.Maximum = value;
            }
        }
        
        Form form;
        Label firstValueLabel, secondValueLabel;
        NumericUpDown firstValueBox, secondValueBox;
        bool resultVal = false;
        public TupleGUI()
        {
            form = new Form()
            {
                Size = new Size(176, 118),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterScreen
            };
            TrySetFormIcon(form);
            
            firstValueLabel = new Label()
            {
                Location = new Point(5,5),
                Size = new Size(80,16)
            };
            form.Controls.Add(firstValueLabel);
            firstValueBox = new NumericUpDown()
            {
                Location = new Point(5, 23),
                Size = new Size(70, 16)
            };
            form.Controls.Add(firstValueBox);
            
            secondValueLabel = new Label()
            {
                Location = new Point(85,5),
                Size = new Size(80,16)
            };
            form.Controls.Add(secondValueLabel);
            secondValueBox = new NumericUpDown()
            {
                Location = new Point(85, 23),
                Size = new Size(70, 16)
            };
            form.Controls.Add(secondValueBox);
            
            Button okBtn = new Button()
            {
                Location = new Point(5, 51),
                Size = new Size(50, 22),
                Text = "&OK"
            };
            form.Controls.Add(okBtn);
            Button cancelBtn = new Button()
            {
                Location = new Point(60, 51),
                Size = new Size(70, 22),
                Text = "&Cancel"
            };
            form.Controls.Add(cancelBtn);
            
            cancelBtn.Click += (o, e) =>
                form.Close();
            okBtn.Click += (o, e) =>
            {
                resultVal = true;
                form.Close();
            };
        }
        
        public void ShowDialog()
        {
            form.ShowDialog();
        }
    }
}

class Letter
{
    public ushort Character { get; set; }
    public Bitmap Bitmap { get; set; }
    public short Shift { get; set; }
    public short Offset { get; set; }
    public Letter(ushort character, Bitmap bitmap, short shift, short offset)
    {
        this.Character = character;
        this.Bitmap = bitmap;
        this.Shift = shift;
        this.Offset = offset;
    }
}

static Bitmap invertColors(Bitmap bitmap)
{
    Bitmap result = new Bitmap(bitmap);
    for (int y = 0; (y <= (result.Height - 1)); y++) {
        for (int x = 0; (x <= (result.Width - 1)); x++) {
            Color inv = result.GetPixel(x, y);
            inv = Color.FromArgb(inv.A, (255 - inv.R), (255 - inv.G), (255 - inv.B));
            result.SetPixel(x, y, inv);
        }
    }
    return result;
}

static Bitmap enlargeBitmap(Bitmap bitmap, int scale)
{
    Bitmap result = new Bitmap(bitmap);
    for (int i = 0; i < scale; i++)
    {
        Bitmap resized = new Bitmap(result.Width * 2, result.Height * 2);
        using (Graphics graphics = Graphics.FromImage(resized))
        {
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            graphics.DrawImage(result, 0, 0, result.Width * 2+1, result.Height * 2+1);
        }
        result = resized;
    }
    return result;
}

UndertaleFont FontPickerResult()
{
    UndertaleFont result = null;
    
    Form form = new Form()
    {
        Size = new Size(230, 100),
        MinimumSize = new Size(160, 100),
        MaximizeBox = false,
        MinimizeBox = false,
        Text = "Select a font",
        StartPosition = FormStartPosition.CenterScreen
    };
    TrySetFormIcon(form);
    
    ComboBox comboBox = new ComboBox();
    comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
    comboBox.Location = new Point(5,5);
    comboBox.Size = new Size(form.Size.Width - 25, comboBox.Size.Height);
    comboBox.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
    foreach (UndertaleFont font in Data.Fonts)
        comboBox.Items.Add(font.Name.Content);
    int defaultSelection = comboBox.Items.IndexOf("fnt_maintext");
    comboBox.SelectedIndex = defaultSelection == -1 ? 0 : defaultSelection;
    form.Controls.Add(comboBox);
    
    int bottomY = form.Size.Height - 67;
    
    Button okBtn = new Button();
    okBtn.Text = "&OK";
    okBtn.Size = new Size(50, okBtn.Size.Height);
    okBtn.Location = new Point(5, bottomY);
    okBtn.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
    form.Controls.Add(okBtn);
    
    Button cancelBtn = new Button();
    cancelBtn.Text = "&Cancel";
    cancelBtn.Size = new Size(60, cancelBtn.Size.Height);
    cancelBtn.Location = new Point(5 + okBtn.Size.Width + okBtn.Location.X, bottomY);
    cancelBtn.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
    form.Controls.Add(cancelBtn);
    
    okBtn.Click += (o, e) =>
    {
        result = Data.Fonts[comboBox.SelectedIndex];
        form.Close();
    };
    cancelBtn.Click += (o, e) =>
    {
        result = null;
        form.Close();
    };
    
    form.ShowDialog();
    
    return result;
}

static void TrySetFormIcon(Form form)
{
    try
    {
        string part1 = Path.GetDirectoryName(Application.ExecutablePath);
        string part2 = AppDomain.CurrentDomain.FriendlyName + ".exe";
        string exepath = Path.Combine(part1, part2);
        Icon icon = Icon.ExtractAssociatedIcon(exepath);
        form.Icon = icon;
    }
    catch
    {
        form.ShowIcon = false;
    }
}
