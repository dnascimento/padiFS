namespace PuppetMaster
{
    partial class PuppetInterface
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
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.TexboxLogStatus = new System.Windows.Forms.TextBox();
            this.buttonAddMeta2 = new System.Windows.Forms.Button();
            this.buttonAddMeta1 = new System.Windows.Forms.Button();
            this.buttonAddMeta0 = new System.Windows.Forms.Button();
            this.buttonAddData1 = new System.Windows.Forms.Button();
            this.ButtonNextStep = new System.Windows.Forms.Button();
            this.ButtonRun = new System.Windows.Forms.Button();
            this.TextBoxManualCommand = new System.Windows.Forms.TextBox();
            this.ButtonLoadScript = new System.Windows.Forms.Button();
            this.ButtonExecute = new System.Windows.Forms.Button();
            this.TextBoxCommandList = new System.Windows.Forms.TextBox();
            this.LabelOpenFile = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.button1 = new System.Windows.Forms.Button();
            this.ButtonAsyncExecute = new System.Windows.Forms.Button();
            this.buttonAddClient1 = new System.Windows.Forms.Button();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.button2 = new System.Windows.Forms.Button();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog1_FileOk);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.TexboxLogStatus);
            this.groupBox2.Location = new System.Drawing.Point(3, 263);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(600, 269);
            this.groupBox2.TabIndex = 12;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "LogBox";
            this.groupBox2.Enter += new System.EventHandler(this.groupBox2_Enter);
            // 
            // TexboxLogStatus
            // 
            this.TexboxLogStatus.AccessibleRole = System.Windows.Forms.AccessibleRole.Dialog;
            this.TexboxLogStatus.BackColor = System.Drawing.SystemColors.ControlText;
            this.TexboxLogStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TexboxLogStatus.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TexboxLogStatus.ForeColor = System.Drawing.Color.Gold;
            this.TexboxLogStatus.Location = new System.Drawing.Point(9, 19);
            this.TexboxLogStatus.Multiline = true;
            this.TexboxLogStatus.Name = "TexboxLogStatus";
            this.TexboxLogStatus.ReadOnly = true;
            this.TexboxLogStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.TexboxLogStatus.Size = new System.Drawing.Size(585, 244);
            this.TexboxLogStatus.TabIndex = 9;
            this.TexboxLogStatus.TextChanged += new System.EventHandler(this.TexboxLogStatus_TextChanged);
            // 
            // buttonAddMeta2
            // 
            this.buttonAddMeta2.BackColor = System.Drawing.Color.DarkRed;
            this.buttonAddMeta2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAddMeta2.Location = new System.Drawing.Point(6, 181);
            this.buttonAddMeta2.Name = "buttonAddMeta2";
            this.buttonAddMeta2.Size = new System.Drawing.Size(71, 39);
            this.buttonAddMeta2.TabIndex = 3;
            this.buttonAddMeta2.Text = "Meta 2";
            this.buttonAddMeta2.UseVisualStyleBackColor = false;
            this.buttonAddMeta2.Click += new System.EventHandler(this.buttonAddMeta2_Click);
            // 
            // buttonAddMeta1
            // 
            this.buttonAddMeta1.BackColor = System.Drawing.Color.DarkOrange;
            this.buttonAddMeta1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAddMeta1.Location = new System.Drawing.Point(6, 135);
            this.buttonAddMeta1.Name = "buttonAddMeta1";
            this.buttonAddMeta1.Size = new System.Drawing.Size(71, 42);
            this.buttonAddMeta1.TabIndex = 2;
            this.buttonAddMeta1.Text = "Meta 1";
            this.buttonAddMeta1.UseVisualStyleBackColor = false;
            this.buttonAddMeta1.Click += new System.EventHandler(this.buttonAddMeta1_Click);
            // 
            // buttonAddMeta0
            // 
            this.buttonAddMeta0.BackColor = System.Drawing.Color.DarkGreen;
            this.buttonAddMeta0.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAddMeta0.Location = new System.Drawing.Point(6, 88);
            this.buttonAddMeta0.Name = "buttonAddMeta0";
            this.buttonAddMeta0.Size = new System.Drawing.Size(71, 41);
            this.buttonAddMeta0.TabIndex = 1;
            this.buttonAddMeta0.Text = "Meta 0";
            this.buttonAddMeta0.UseVisualStyleBackColor = false;
            this.buttonAddMeta0.Click += new System.EventHandler(this.buttonAddMeta_Click);
            // 
            // buttonAddData1
            // 
            this.buttonAddData1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAddData1.Location = new System.Drawing.Point(6, 57);
            this.buttonAddData1.Name = "buttonAddData1";
            this.buttonAddData1.Size = new System.Drawing.Size(71, 28);
            this.buttonAddData1.TabIndex = 4;
            this.buttonAddData1.Text = "Data +";
            this.buttonAddData1.UseVisualStyleBackColor = true;
            this.buttonAddData1.Click += new System.EventHandler(this.buttonAddData1_Click);
            // 
            // ButtonNextStep
            // 
            this.ButtonNextStep.Enabled = false;
            this.ButtonNextStep.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ButtonNextStep.ForeColor = System.Drawing.Color.DarkGreen;
            this.ButtonNextStep.Location = new System.Drawing.Point(391, 121);
            this.ButtonNextStep.Name = "ButtonNextStep";
            this.ButtonNextStep.Size = new System.Drawing.Size(111, 44);
            this.ButtonNextStep.TabIndex = 2;
            this.ButtonNextStep.Text = "Next Step";
            this.ButtonNextStep.UseVisualStyleBackColor = true;
            this.ButtonNextStep.Click += new System.EventHandler(this.ButtonNextStep_Click);
            // 
            // ButtonRun
            // 
            this.ButtonRun.Enabled = false;
            this.ButtonRun.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ButtonRun.ForeColor = System.Drawing.Color.Maroon;
            this.ButtonRun.Location = new System.Drawing.Point(391, 71);
            this.ButtonRun.Name = "ButtonRun";
            this.ButtonRun.Size = new System.Drawing.Size(111, 44);
            this.ButtonRun.TabIndex = 1;
            this.ButtonRun.Text = "Run";
            this.ButtonRun.UseVisualStyleBackColor = true;
            this.ButtonRun.Click += new System.EventHandler(this.ButtonRun_Click);
            // 
            // TextBoxManualCommand
            // 
            this.TextBoxManualCommand.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TextBoxManualCommand.Location = new System.Drawing.Point(6, 221);
            this.TextBoxManualCommand.Name = "TextBoxManualCommand";
            this.TextBoxManualCommand.Size = new System.Drawing.Size(303, 29);
            this.TextBoxManualCommand.TabIndex = 4;
            this.TextBoxManualCommand.Text = "Manual Command";
            this.TextBoxManualCommand.MouseClick += new System.Windows.Forms.MouseEventHandler(this.TextBoxManualCommand_MouseClick);
            this.TextBoxManualCommand.TextChanged += new System.EventHandler(this.TextBoxManualCommand_TextChanged);
            this.TextBoxManualCommand.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBoxManualCommand_KeyPress);
            // 
            // ButtonLoadScript
            // 
            this.ButtonLoadScript.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ButtonLoadScript.Location = new System.Drawing.Point(391, 17);
            this.ButtonLoadScript.Name = "ButtonLoadScript";
            this.ButtonLoadScript.Size = new System.Drawing.Size(109, 48);
            this.ButtonLoadScript.TabIndex = 0;
            this.ButtonLoadScript.Text = "Load Script";
            this.ButtonLoadScript.UseVisualStyleBackColor = true;
            this.ButtonLoadScript.Click += new System.EventHandler(this.ButtonLoadScript_Click);
            // 
            // ButtonExecute
            // 
            this.ButtonExecute.BackColor = System.Drawing.Color.DarkOliveGreen;
            this.ButtonExecute.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ButtonExecute.ForeColor = System.Drawing.SystemColors.ControlText;
            this.ButtonExecute.Location = new System.Drawing.Point(391, 221);
            this.ButtonExecute.Name = "ButtonExecute";
            this.ButtonExecute.Size = new System.Drawing.Size(109, 32);
            this.ButtonExecute.TabIndex = 5;
            this.ButtonExecute.Text = "Execute";
            this.ButtonExecute.UseVisualStyleBackColor = false;
            this.ButtonExecute.Click += new System.EventHandler(this.ButtonExecute_Click);
            // 
            // TextBoxCommandList
            // 
            this.TextBoxCommandList.AccessibleRole = System.Windows.Forms.AccessibleRole.Dialog;
            this.TextBoxCommandList.BackColor = System.Drawing.SystemColors.ControlText;
            this.TextBoxCommandList.ForeColor = System.Drawing.Color.Lime;
            this.TextBoxCommandList.Location = new System.Drawing.Point(6, 34);
            this.TextBoxCommandList.Multiline = true;
            this.TextBoxCommandList.Name = "TextBoxCommandList";
            this.TextBoxCommandList.ReadOnly = true;
            this.TextBoxCommandList.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.TextBoxCommandList.Size = new System.Drawing.Size(376, 181);
            this.TextBoxCommandList.TabIndex = 10;
            this.TextBoxCommandList.Text = "Command List";
            // 
            // LabelOpenFile
            // 
            this.LabelOpenFile.AutoSize = true;
            this.LabelOpenFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LabelOpenFile.Location = new System.Drawing.Point(6, 16);
            this.LabelOpenFile.Name = "LabelOpenFile";
            this.LabelOpenFile.Size = new System.Drawing.Size(107, 15);
            this.LabelOpenFile.TabIndex = 6;
            this.LabelOpenFile.Text = "File not loaded ";
            this.LabelOpenFile.Click += new System.EventHandler(this.LabelOpenFile_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.button1);
            this.groupBox1.Controls.Add(this.ButtonAsyncExecute);
            this.groupBox1.Controls.Add(this.LabelOpenFile);
            this.groupBox1.Controls.Add(this.TextBoxCommandList);
            this.groupBox1.Controls.Add(this.ButtonExecute);
            this.groupBox1.Controls.Add(this.ButtonLoadScript);
            this.groupBox1.Controls.Add(this.TextBoxManualCommand);
            this.groupBox1.Controls.Add(this.ButtonRun);
            this.groupBox1.Controls.Add(this.ButtonNextStep);
            this.groupBox1.Location = new System.Drawing.Point(3, 1);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(511, 256);
            this.groupBox1.TabIndex = 11;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "CommandBox";
            this.groupBox1.Enter += new System.EventHandler(this.groupBox1_Enter);
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.Color.Maroon;
            this.button1.Location = new System.Drawing.Point(393, 169);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(109, 44);
            this.button1.TabIndex = 12;
            this.button1.Text = "Async Step";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // ButtonAsyncExecute
            // 
            this.ButtonAsyncExecute.BackColor = System.Drawing.Color.DarkRed;
            this.ButtonAsyncExecute.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ButtonAsyncExecute.ForeColor = System.Drawing.SystemColors.ControlText;
            this.ButtonAsyncExecute.Location = new System.Drawing.Point(315, 221);
            this.ButtonAsyncExecute.Name = "ButtonAsyncExecute";
            this.ButtonAsyncExecute.Size = new System.Drawing.Size(70, 32);
            this.ButtonAsyncExecute.TabIndex = 11;
            this.ButtonAsyncExecute.Text = "Async";
            this.ButtonAsyncExecute.UseVisualStyleBackColor = false;
            this.ButtonAsyncExecute.Click += new System.EventHandler(this.button1_Click);
            // 
            // buttonAddClient1
            // 
            this.buttonAddClient1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAddClient1.Location = new System.Drawing.Point(6, 19);
            this.buttonAddClient1.Name = "buttonAddClient1";
            this.buttonAddClient1.Size = new System.Drawing.Size(71, 31);
            this.buttonAddClient1.TabIndex = 4;
            this.buttonAddClient1.Text = "Client +";
            this.buttonAddClient1.UseVisualStyleBackColor = true;
            this.buttonAddClient1.Click += new System.EventHandler(this.buttonAddClient1_Click);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.button2);
            this.groupBox4.Controls.Add(this.buttonAddMeta2);
            this.groupBox4.Controls.Add(this.buttonAddClient1);
            this.groupBox4.Controls.Add(this.buttonAddMeta1);
            this.groupBox4.Controls.Add(this.buttonAddData1);
            this.groupBox4.Controls.Add(this.buttonAddMeta0);
            this.groupBox4.Location = new System.Drawing.Point(520, 1);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(83, 267);
            this.groupBox4.TabIndex = 14;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "DataServers";
            // 
            // button2
            // 
            this.button2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.Location = new System.Drawing.Point(6, 221);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(71, 41);
            this.button2.TabIndex = 5;
            this.button2.Text = "Load State";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // PuppetInterface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(613, 544);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "PuppetInterface";
            this.Text = "Puppet Master - PADI FS";
            this.Load += new System.EventHandler(this.PuppetMaster_Load);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button buttonAddMeta2;
        private System.Windows.Forms.Button buttonAddMeta1;
        private System.Windows.Forms.Button buttonAddMeta0;
        private System.Windows.Forms.Button buttonAddData1;
        private System.Windows.Forms.Button ButtonNextStep;
        private System.Windows.Forms.Button ButtonRun;
        private System.Windows.Forms.TextBox TextBoxManualCommand;
        private System.Windows.Forms.Button ButtonLoadScript;
        private System.Windows.Forms.Button ButtonExecute;
        private System.Windows.Forms.TextBox TextBoxCommandList;
        private System.Windows.Forms.Label LabelOpenFile;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button ButtonAsyncExecute;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox TexboxLogStatus;
        private System.Windows.Forms.Button buttonAddClient1;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button button2;
    }
}

