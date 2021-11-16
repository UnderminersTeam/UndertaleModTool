using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UndertaleModTool.Windows
{
    public partial class TextInput : Form
    {
        public string Message { get; }
        public string Title { get; }
        public bool AllowMultiline { get; }
        public string DefaultValue { get; }
        public string ReturnString { get; set; }
        public TextInput(string message, string title, string defaultValue, bool allowMultiline, bool readOnly = false)
        {
            InitializeComponent();
            Icon = new Icon(App.GetResourceStream(new Uri("pack://application:,,,/icon.ico")).Stream); // "UndertaleModTool/icon.ico"
            Message = message;
            Title = title;
            DefaultValue = defaultValue;
            Text = Title;
            AllowMultiline = allowMultiline;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            label1.Text = message;
            richTextBox1.Multiline = AllowMultiline;
            richTextBox1.DetectUrls = false;
            richTextBox1.LanguageOption = RichTextBoxLanguageOptions.UIFonts; //prevents the bug with Japanese characters
            richTextBox1.ReadOnly = readOnly;

            label1.AutoSize = false;
        }

        private void TextInput_Load(object sender, EventArgs e)
        {
            richTextBox1.Clear(); //remove "Input text here"

            if (DefaultValue.Length > 0)
            {
                richTextBox1.AppendText(DefaultValue);
                richTextBox1.SelectionStart = 0;
                richTextBox1.ScrollToCaret();
            }

            if (richTextBox1.ReadOnly)
            {
                richTextBox1.BackColor = SystemColors.Window; //restore color to default one.
                richTextBox1.ContextMenuStrip = textCopyMenu;
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.ReturnString = richTextBox1.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void copyMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Copy();
        }

        private void cutMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Cut();
        }

        private void pasteMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Paste();
        }

        private void copyAllMenuItem_Click(object sender, EventArgs e)
        {
            if (richTextBox1.Text.Length > 0)
                Clipboard.SetText(richTextBox1.Text, TextDataFormat.Text);
        }

        private void textCommandsMenu_Opening(object sender, CancelEventArgs e)
        {
            copyMenuItem.Enabled = (richTextBox1.SelectionLength > 0);
            pasteMenuItem.Enabled = Clipboard.ContainsText();
        }

        private void textCopyMenu_Opening(object sender, CancelEventArgs e)
        {
            copyMenuItem1.Enabled = (richTextBox1.SelectionLength > 0);
        }
    }
}
