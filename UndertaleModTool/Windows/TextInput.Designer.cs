
namespace UndertaleModTool.Windows
{
    partial class TextInput
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
            this.components = new System.ComponentModel.Container();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.textCommandsMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textCopyMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.copyAllMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.textCommandsMenu.SuspendLayout();
            this.textCopyMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // richTextBox1
            // 
            this.richTextBox1.ContextMenuStrip = this.textCommandsMenu;
            this.richTextBox1.HideSelection = false;
            this.richTextBox1.Location = new System.Drawing.Point(12, 57);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(569, 283);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = "Input Text Here";
            this.richTextBox1.TextChanged += new System.EventHandler(this.richTextBox1_TextChanged);
            // 
            // textCommandsMenu
            // 
            this.textCommandsMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyMenuItem,
            this.copyAllMenuItem,
            this.cutMenuItem,
            this.pasteMenuItem});
            this.textCommandsMenu.Name = "textCommandsMenu";
            this.textCommandsMenu.ShowImageMargin = false;
            this.textCommandsMenu.Size = new System.Drawing.Size(93, 92);
            this.textCommandsMenu.Opening += new System.ComponentModel.CancelEventHandler(this.textCommandsMenu_Opening);
            // 
            // copyMenuItem
            // 
            this.copyMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.copyMenuItem.Name = "copyMenuItem";
            this.copyMenuItem.Size = new System.Drawing.Size(92, 22);
            this.copyMenuItem.Text = "Copy";
            this.copyMenuItem.Click += new System.EventHandler(this.copyMenuItem_Click);
            // 
            // copyAllMenuItem
            // 
            this.copyAllMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.copyAllMenuItem.Name = "copyAllMenuItem";
            this.copyAllMenuItem.Size = new System.Drawing.Size(92, 22);
            this.copyAllMenuItem.Text = "Copy all";
            this.copyAllMenuItem.Click += new System.EventHandler(this.copyAllMenuItem_Click);
            // 
            // cutMenuItem
            // 
            this.cutMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.cutMenuItem.Name = "cutMenuItem";
            this.cutMenuItem.Size = new System.Drawing.Size(92, 22);
            this.cutMenuItem.Text = "Cut";
            this.cutMenuItem.Click += new System.EventHandler(this.cutMenuItem_Click);
            // 
            // pasteMenuItem
            // 
            this.pasteMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.pasteMenuItem.Name = "pasteMenuItem";
            this.pasteMenuItem.Size = new System.Drawing.Size(92, 22);
            this.pasteMenuItem.Text = "Paste";
            this.pasteMenuItem.Click += new System.EventHandler(this.pasteMenuItem_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(211, 346);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(189, 41);
            this.button1.TabIndex = 1;
            this.button1.Text = "Done";
            this.button1.UseCompatibleTextRendering = true;
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(133, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "Default Prompt Text";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // textCopyMenu
            // 
            this.textCopyMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyMenuItem1,
            this.copyAllMenuItem1});
            this.textCopyMenu.ShowImageMargin = false;
            this.textCopyMenu.Name = "textCopyMenu";
            this.textCopyMenu.Size = new System.Drawing.Size(118, 48);
            this.textCopyMenu.Opening += new System.ComponentModel.CancelEventHandler(this.textCopyMenu_Opening);
            // 
            // copyMenuItem1
            // 
            this.copyMenuItem1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.copyMenuItem1.Name = "copyMenuItem1";
            this.copyMenuItem1.Size = new System.Drawing.Size(117, 22);
            this.copyMenuItem1.Text = "Copy";
            this.copyMenuItem1.Click += new System.EventHandler(this.copyMenuItem_Click);
            // 
            // copyAllMenuItem1
            // 
            this.copyAllMenuItem1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.copyAllMenuItem1.Name = "copyAllMenuItem1";
            this.copyAllMenuItem1.Size = new System.Drawing.Size(117, 22);
            this.copyAllMenuItem1.Text = "Copy all";
            this.copyAllMenuItem1.Click += new System.EventHandler(this.copyAllMenuItem_Click);
            // 
            // TextInput
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(594, 397);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.richTextBox1);
            this.Name = "TextInput";
            this.Text = "Default Title Message";
            this.Load += new System.EventHandler(this.TextInput_Load);
            this.textCommandsMenu.ResumeLayout(false);
            this.textCopyMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

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
    }
}