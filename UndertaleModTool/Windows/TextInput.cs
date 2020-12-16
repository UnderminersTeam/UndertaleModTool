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
        public TextInput(string message, string title, string defaultValue, bool allowMultiline)
        {
            InitializeComponent();
            Message = message;
            Title = title;
            DefaultValue = defaultValue;
            Text = Title;
            AllowMultiline = allowMultiline;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            label1.Text = message;
            richTextBox1.Text = DefaultValue;
            richTextBox1.Multiline = AllowMultiline;
            label1.AutoSize = false;
        }

        private void TextInput_Load(object sender, EventArgs e)
        {
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
    }
}
