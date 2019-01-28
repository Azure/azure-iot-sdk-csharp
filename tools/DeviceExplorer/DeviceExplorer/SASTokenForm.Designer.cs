namespace DeviceExplorer
{
    partial class SASTokenForm
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
            this.deviceIDComboBox = new System.Windows.Forms.ComboBox();
            this.doneButton = new System.Windows.Forms.Button();
            this.generateButton = new System.Windows.Forms.Button();
            this.deviceIDLabel = new System.Windows.Forms.Label();
            this.numericUpDownTTL = new System.Windows.Forms.NumericUpDown();
            this.sasRichTextBox = new System.Windows.Forms.RichTextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.ttlInDaysUpDown = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.deviceKeyComboBox = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownTTL)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ttlInDaysUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // deviceIDComboBox
            // 
            this.deviceIDComboBox.AccessibleName = "Device ID";
            this.deviceIDComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.deviceIDComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.deviceIDComboBox.FormattingEnabled = true;
            this.deviceIDComboBox.Location = new System.Drawing.Point(84, 27);
            this.deviceIDComboBox.Name = "deviceIDComboBox";
            this.deviceIDComboBox.Size = new System.Drawing.Size(386, 21);
            this.deviceIDComboBox.TabIndex = 2;
            this.deviceIDComboBox.SelectedIndexChanged += new System.EventHandler(this.deviceIDComboBox_SelectedIndexChanged);
            // 
            // doneButton
            // 
            this.doneButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.doneButton.Location = new System.Drawing.Point(251, 333);
            this.doneButton.Margin = new System.Windows.Forms.Padding(2);
            this.doneButton.Name = "doneButton";
            this.doneButton.Size = new System.Drawing.Size(78, 26);
            this.doneButton.TabIndex = 9;
            this.doneButton.Text = "Done";
            this.doneButton.UseVisualStyleBackColor = true;
            this.doneButton.Click += new System.EventHandler(this.doneButton_Click);
            // 
            // generateButton
            // 
            this.generateButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.generateButton.Location = new System.Drawing.Point(116, 333);
            this.generateButton.Margin = new System.Windows.Forms.Padding(2);
            this.generateButton.Name = "generateButton";
            this.generateButton.Size = new System.Drawing.Size(78, 26);
            this.generateButton.TabIndex = 8;
            this.generateButton.Text = "Generate";
            this.generateButton.UseVisualStyleBackColor = true;
            this.generateButton.Click += new System.EventHandler(this.generateButton_Click);
            // 
            // deviceIDLabel
            // 
            this.deviceIDLabel.AutoSize = true;
            this.deviceIDLabel.Location = new System.Drawing.Point(27, 30);
            this.deviceIDLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.deviceIDLabel.Name = "deviceIDLabel";
            this.deviceIDLabel.Size = new System.Drawing.Size(52, 13);
            this.deviceIDLabel.TabIndex = 1;
            this.deviceIDLabel.Text = "DeviceID";
            // 
            // numericUpDownTTL
            // 
            this.numericUpDownTTL.Location = new System.Drawing.Point(-294, 41);
            this.numericUpDownTTL.Margin = new System.Windows.Forms.Padding(2);
            this.numericUpDownTTL.Maximum = new decimal(new int[] {
            365,
            0,
            0,
            0});
            this.numericUpDownTTL.Name = "numericUpDownTTL";
            this.numericUpDownTTL.Size = new System.Drawing.Size(144, 20);
            this.numericUpDownTTL.TabIndex = 21;
            // 
            // sasRichTextBox
            // 
            this.sasRichTextBox.AccessibleName = "Generated SAS token";
            this.sasRichTextBox.AccessibleRole = System.Windows.Forms.AccessibleRole.StaticText;
            this.sasRichTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sasRichTextBox.Location = new System.Drawing.Point(30, 146);
            this.sasRichTextBox.Margin = new System.Windows.Forms.Padding(2);
            this.sasRichTextBox.Name = "sasRichTextBox";
            this.sasRichTextBox.ReadOnly = true;
            this.sasRichTextBox.Size = new System.Drawing.Size(418, 156);
            this.sasRichTextBox.TabIndex = 7;
            this.sasRichTextBox.Text = "";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(-376, 41);
            this.label10.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(60, 13);
            this.label10.TabIndex = 19;
            this.label10.Text = "TTL (Days)";
            // 
            // ttlInDaysUpDown
            // 
            this.ttlInDaysUpDown.AccessibleName = "TTL (Days)";
            this.ttlInDaysUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ttlInDaysUpDown.Location = new System.Drawing.Point(171, 97);
            this.ttlInDaysUpDown.Margin = new System.Windows.Forms.Padding(2);
            this.ttlInDaysUpDown.Maximum = new decimal(new int[] {
            365,
            0,
            0,
            0});
            this.ttlInDaysUpDown.Name = "ttlInDaysUpDown";
            this.ttlInDaysUpDown.Size = new System.Drawing.Size(299, 20);
            this.ttlInDaysUpDown.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(98, 99);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "TTL (Days)";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 64);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "DeviceKeys";
            // 
            // deviceKeyComboBox
            // 
            this.deviceKeyComboBox.AccessibleName = "Device Keys";
            this.deviceKeyComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.deviceKeyComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.deviceKeyComboBox.FormattingEnabled = true;
            this.deviceKeyComboBox.Location = new System.Drawing.Point(84, 61);
            this.deviceKeyComboBox.Name = "deviceKeyComboBox";
            this.deviceKeyComboBox.Size = new System.Drawing.Size(386, 21);
            this.deviceKeyComboBox.TabIndex = 4;
            this.deviceKeyComboBox.SelectedIndexChanged += new System.EventHandler(this.deviceKeyComboBox_SelectedIndexChanged);
            // 
            // SASTokenForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(482, 380);
            this.Controls.Add(this.deviceKeyComboBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ttlInDaysUpDown);
            this.Controls.Add(this.numericUpDownTTL);
            this.Controls.Add(this.sasRichTextBox);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.deviceIDComboBox);
            this.Controls.Add(this.doneButton);
            this.Controls.Add(this.generateButton);
            this.Controls.Add(this.deviceIDLabel);
            this.MinimumSize = new System.Drawing.Size(498, 379);
            this.Name = "SASTokenForm";
            this.Text = "SASTokenForm";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownTTL)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ttlInDaysUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ComboBox deviceIDComboBox;
        private System.Windows.Forms.Button doneButton;
        private System.Windows.Forms.Button generateButton;
        private System.Windows.Forms.Label deviceIDLabel;
        private System.Windows.Forms.NumericUpDown numericUpDownTTL;
        private System.Windows.Forms.RichTextBox sasRichTextBox;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.NumericUpDown ttlInDaysUpDown;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox deviceKeyComboBox;
    }
}