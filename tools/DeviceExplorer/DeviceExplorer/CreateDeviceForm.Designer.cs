namespace DeviceExplorer
{
    partial class CreateDeviceForm
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
            this.createButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.deviceIDTextBox = new System.Windows.Forms.TextBox();
            this.generateIDCheckBox = new System.Windows.Forms.CheckBox();
            this.primaryKeyTextBox = new System.Windows.Forms.TextBox();
            this.generateKeysCheckBox = new System.Windows.Forms.CheckBox();
            this.secondaryKeyTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.primaryLabel = new System.Windows.Forms.Label();
            this.secondaryLabel = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.x509RadioButton = new System.Windows.Forms.RadioButton();
            this.keysRadioButton = new System.Windows.Forms.RadioButton();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // createButton
            // 
            this.createButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.createButton.Location = new System.Drawing.Point(76, 226);
            this.createButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.createButton.Name = "createButton";
            this.createButton.Size = new System.Drawing.Size(142, 27);
            this.createButton.TabIndex = 5;
            this.createButton.Text = "Create";
            this.createButton.UseVisualStyleBackColor = true;
            this.createButton.Click += new System.EventHandler(this.createButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cancelButton.Location = new System.Drawing.Point(267, 226);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(142, 27);
            this.cancelButton.TabIndex = 6;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // deviceIDTextBox
            // 
            this.deviceIDTextBox.Location = new System.Drawing.Point(137, 76);
            this.deviceIDTextBox.Name = "deviceIDTextBox";
            this.deviceIDTextBox.Size = new System.Drawing.Size(344, 20);
            this.deviceIDTextBox.TabIndex = 0;
            // 
            // generateIDCheckBox
            // 
            this.generateIDCheckBox.AutoSize = true;
            this.generateIDCheckBox.Location = new System.Drawing.Point(137, 176);
            this.generateIDCheckBox.Name = "generateIDCheckBox";
            this.generateIDCheckBox.Size = new System.Drawing.Size(109, 17);
            this.generateIDCheckBox.TabIndex = 3;
            this.generateIDCheckBox.Text = "Auto Generate ID";
            this.generateIDCheckBox.UseVisualStyleBackColor = true;
            this.generateIDCheckBox.CheckedChanged += new System.EventHandler(this.generateIDCheckBox_CheckedChanged);
            // 
            // primaryKeyTextBox
            // 
            this.primaryKeyTextBox.Location = new System.Drawing.Point(137, 106);
            this.primaryKeyTextBox.Name = "primaryKeyTextBox";
            this.primaryKeyTextBox.Size = new System.Drawing.Size(344, 20);
            this.primaryKeyTextBox.TabIndex = 1;
            // 
            // generateKeysCheckBox
            // 
            this.generateKeysCheckBox.AutoSize = true;
            this.generateKeysCheckBox.Location = new System.Drawing.Point(362, 176);
            this.generateKeysCheckBox.Name = "generateKeysCheckBox";
            this.generateKeysCheckBox.Size = new System.Drawing.Size(121, 17);
            this.generateKeysCheckBox.TabIndex = 4;
            this.generateKeysCheckBox.Text = "Auto Generate Keys";
            this.generateKeysCheckBox.UseVisualStyleBackColor = true;
            this.generateKeysCheckBox.CheckedChanged += new System.EventHandler(this.generateKeysCheckBox_CheckedChanged);
            // 
            // secondaryKeyTextBox
            // 
            this.secondaryKeyTextBox.Location = new System.Drawing.Point(137, 133);
            this.secondaryKeyTextBox.Name = "secondaryKeyTextBox";
            this.secondaryKeyTextBox.Size = new System.Drawing.Size(344, 20);
            this.secondaryKeyTextBox.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 80);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "Device ID:";
            // 
            // primaryLabel
            // 
            this.primaryLabel.AutoSize = true;
            this.primaryLabel.Location = new System.Drawing.Point(10, 110);
            this.primaryLabel.Name = "primaryLabel";
            this.primaryLabel.Size = new System.Drawing.Size(65, 13);
            this.primaryLabel.TabIndex = 10;
            this.primaryLabel.Text = "Primary Key:";
            // 
            // secondaryLabel
            // 
            this.secondaryLabel.AutoSize = true;
            this.secondaryLabel.Location = new System.Drawing.Point(10, 137);
            this.secondaryLabel.Name = "secondaryLabel";
            this.secondaryLabel.Size = new System.Drawing.Size(82, 13);
            this.secondaryLabel.TabIndex = 11;
            this.secondaryLabel.Text = "Secondary Key:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.x509RadioButton);
            this.groupBox1.Controls.Add(this.keysRadioButton);
            this.groupBox1.Location = new System.Drawing.Point(123, 10);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox1.Size = new System.Drawing.Size(256, 47);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Device Authentication";
            // 
            // x509RadioButton
            // 
            this.x509RadioButton.AutoSize = true;
            this.x509RadioButton.Location = new System.Drawing.Point(164, 21);
            this.x509RadioButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.x509RadioButton.Name = "x509RadioButton";
            this.x509RadioButton.Size = new System.Drawing.Size(50, 17);
            this.x509RadioButton.TabIndex = 1;
            this.x509RadioButton.TabStop = true;
            this.x509RadioButton.Text = "X509";
            this.x509RadioButton.UseVisualStyleBackColor = true;
            // 
            // keysRadioButton
            // 
            this.keysRadioButton.AutoSize = true;
            this.keysRadioButton.Location = new System.Drawing.Point(56, 21);
            this.keysRadioButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.keysRadioButton.Name = "keysRadioButton";
            this.keysRadioButton.Size = new System.Drawing.Size(89, 17);
            this.keysRadioButton.TabIndex = 0;
            this.keysRadioButton.TabStop = true;
            this.keysRadioButton.Text = "Security Keys";
            this.keysRadioButton.UseVisualStyleBackColor = true;
            this.keysRadioButton.CheckedChanged += new System.EventHandler(this.keysRadioButton_CheckedChanged);
            // 
            // CreateDeviceForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnablePreventFocusChange;
            this.ClientSize = new System.Drawing.Size(529, 273);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.secondaryLabel);
            this.Controls.Add(this.primaryLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.secondaryKeyTextBox);
            this.Controls.Add(this.generateKeysCheckBox);
            this.Controls.Add(this.primaryKeyTextBox);
            this.Controls.Add(this.generateIDCheckBox);
            this.Controls.Add(this.deviceIDTextBox);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.createButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "CreateDeviceForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Create Device";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button createButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.TextBox deviceIDTextBox;
        private System.Windows.Forms.CheckBox generateIDCheckBox;
        private System.Windows.Forms.TextBox primaryKeyTextBox;
        private System.Windows.Forms.CheckBox generateKeysCheckBox;
        private System.Windows.Forms.TextBox secondaryKeyTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label primaryLabel;
        private System.Windows.Forms.Label secondaryLabel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton x509RadioButton;
        private System.Windows.Forms.RadioButton keysRadioButton;
    }
}