
namespace UndertaleModTool.Windows
{
    partial class SearchInCodeInput
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            richTextBox1 = new System.Windows.Forms.RichTextBox();
            textCommandsMenu = new System.Windows.Forms.ContextMenuStrip(components);
            copyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            copyAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            cutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            pasteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            button1 = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            textCopyMenu = new System.Windows.Forms.ContextMenuStrip(components);
            copyMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            copyAllMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            regex = new System.Windows.Forms.CheckBox();
            caseSensitive = new System.Windows.Forms.CheckBox();
            textCommandsMenu.SuspendLayout();
            textCopyMenu.SuspendLayout();
            SuspendLayout();
            // 
            // richTextBox1
            // 
            richTextBox1.BackColor = System.Drawing.SystemColors.ControlLight;
            richTextBox1.ContextMenuStrip = textCommandsMenu;
            richTextBox1.HideSelection = false;
            richTextBox1.Location = new System.Drawing.Point(10, 53);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new System.Drawing.Size(498, 207);
            richTextBox1.TabIndex = 0;
            richTextBox1.Text = "Input Text Here";
            richTextBox1.TextChanged += richTextBox1_TextChanged;
            // 
            // textCommandsMenu
            // 
            textCommandsMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { copyMenuItem, copyAllMenuItem, cutMenuItem, pasteMenuItem });
            textCommandsMenu.Name = "textCommandsMenu";
            textCommandsMenu.ShowImageMargin = false;
            textCommandsMenu.Size = new System.Drawing.Size(93, 92);
            textCommandsMenu.Opening += textCommandsMenu_Opening;
            // 
            // copyMenuItem
            // 
            copyMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            copyMenuItem.Name = "copyMenuItem";
            copyMenuItem.Size = new System.Drawing.Size(92, 22);
            copyMenuItem.Text = "Copy";
            copyMenuItem.Click += copyMenuItem_Click;
            // 
            // copyAllMenuItem
            // 
            copyAllMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            copyAllMenuItem.Name = "copyAllMenuItem";
            copyAllMenuItem.Size = new System.Drawing.Size(92, 22);
            copyAllMenuItem.Text = "Copy all";
            copyAllMenuItem.Click += copyAllMenuItem_Click;
            // 
            // cutMenuItem
            // 
            cutMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            cutMenuItem.Name = "cutMenuItem";
            cutMenuItem.Size = new System.Drawing.Size(92, 22);
            cutMenuItem.Text = "Cut";
            cutMenuItem.Click += cutMenuItem_Click;
            // 
            // pasteMenuItem
            // 
            pasteMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            pasteMenuItem.Name = "pasteMenuItem";
            pasteMenuItem.Size = new System.Drawing.Size(92, 22);
            pasteMenuItem.Text = "Paste";
            pasteMenuItem.Click += pasteMenuItem_Click;
            // 
            // button1
            // 
            button1.BackColor = System.Drawing.SystemColors.Control;
            button1.Location = new System.Drawing.Point(168, 291);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(165, 38);
            button1.TabIndex = 1;
            button1.Text = "Done";
            button1.UseCompatibleTextRendering = true;
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Dock = System.Windows.Forms.DockStyle.Top;
            label1.Location = new System.Drawing.Point(0, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(112, 15);
            label1.TabIndex = 2;
            label1.Text = "Default Prompt Text";
            label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            label1.Click += label1_Click;
            // 
            // textCopyMenu
            // 
            textCopyMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { copyMenuItem1, copyAllMenuItem1 });
            textCopyMenu.Name = "textCopyMenu";
            textCopyMenu.ShowImageMargin = false;
            textCopyMenu.Size = new System.Drawing.Size(93, 48);
            textCopyMenu.Opening += textCopyMenu_Opening;
            // 
            // copyMenuItem1
            // 
            copyMenuItem1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            copyMenuItem1.Name = "copyMenuItem1";
            copyMenuItem1.Size = new System.Drawing.Size(92, 22);
            copyMenuItem1.Text = "Copy";
            copyMenuItem1.Click += copyMenuItem_Click;
            // 
            // copyAllMenuItem1
            // 
            copyAllMenuItem1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            copyAllMenuItem1.Name = "copyAllMenuItem1";
            copyAllMenuItem1.Size = new System.Drawing.Size(92, 22);
            copyAllMenuItem1.Text = "Copy all";
            copyAllMenuItem1.Click += copyAllMenuItem_Click;
            // 
            // regex
            // 
            regex.AutoSize = true;
            regex.Location = new System.Drawing.Point(12, 266);
            regex.Name = "regex";
            regex.Size = new System.Drawing.Size(95, 19);
            regex.TabIndex = 3;
            regex.Text = "Regex search";
            regex.UseVisualStyleBackColor = true;
            // 
            // caseSensitive
            // 
            caseSensitive.AutoSize = true;
            caseSensitive.Location = new System.Drawing.Point(113, 266);
            caseSensitive.Name = "caseSensitive";
            caseSensitive.Size = new System.Drawing.Size(99, 19);
            caseSensitive.TabIndex = 4;
            caseSensitive.Text = "Case sensitive";
            caseSensitive.UseVisualStyleBackColor = true;
            // 
            // SearchInCodeInput
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Window;
            ClientSize = new System.Drawing.Size(520, 343);
            Controls.Add(caseSensitive);
            Controls.Add(regex);
            Controls.Add(label1);
            Controls.Add(button1);
            Controls.Add(richTextBox1);
            Name = "SearchInCodeInput";
            Text = "Default Title Message";
            Load += TextInputNew_Load;
            textCommandsMenu.ResumeLayout(false);
            textCopyMenu.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        public System.Windows.Forms.ContextMenuStrip textCommandsMenu;
        private System.Windows.Forms.ToolStripMenuItem copyMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cutMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteMenuItem;
        public System.Windows.Forms.ContextMenuStrip textCopyMenu;
        private System.Windows.Forms.ToolStripMenuItem copyMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem copyAllMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem copyAllMenuItem;
        private System.Windows.Forms.CheckBox regex;
        private System.Windows.Forms.CheckBox caseSensitive;
    }
}