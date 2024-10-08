﻿using AppLimiterLibrary;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LimiterMessaging
{
    public partial class LimiterMessagingForm : Form
    {
        private string _processName;
        private Label lblMessage;
        private Button okBtn;
        private Button ignoreLimitsBtn;
        private AppRepository _repo;

        public LimiterMessagingForm(string message, string processName)
        {
            _repo = new AppRepository();
            _processName = processName;
            InitializeComponent();
            lblMessage.Text = message;
        }

        private void InitializeComponent()
        {
            lblMessage = new Label();
            okBtn = new Button();
            ignoreLimitsBtn = new Button();
            SuspendLayout();
            // 
            // lblMessage
            // 
            lblMessage.Location = new Point(12, 10);
            lblMessage.Name = "lblMessage";
            lblMessage.Size = new Size(279, 87);
            lblMessage.TabIndex = 0;
            // 
            // okBtn
            // 
            okBtn.Location = new Point(241, 100);
            okBtn.Name = "okBtn";
            okBtn.Size = new Size(50, 25);
            okBtn.TabIndex = 1;
            okBtn.Text = "OK";
            okBtn.UseVisualStyleBackColor = true;
            okBtn.Click += OkBtn_Click;
            // 
            // ignoreLimitsBtn
            // 
            ignoreLimitsBtn.Location = new Point(10, 100);
            ignoreLimitsBtn.Name = "ignoreLimitsBtn";
            ignoreLimitsBtn.Size = new Size(100, 25);
            ignoreLimitsBtn.TabIndex = 2;
            ignoreLimitsBtn.Text = "Ignore Limits";
            ignoreLimitsBtn.UseVisualStyleBackColor = true;
            ignoreLimitsBtn.Click += IgnoreLimitsBtn_Click;
            // 
            // LimiterMessagingForm
            // 
            ClientSize = new Size(303, 137);
            Controls.Add(ignoreLimitsBtn);
            Controls.Add(okBtn);
            Controls.Add(lblMessage);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "LimiterMessagingForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Usage Warning";
            ResumeLayout(false);
        }

        private async void IgnoreLimitsBtn_Click(object sender, EventArgs e)
        {
            await _repo.UpdateIgnoreStatus(_processName, true);
            this.Close();
        }

        private void OkBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}