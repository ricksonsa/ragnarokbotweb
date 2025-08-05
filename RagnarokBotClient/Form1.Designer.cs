namespace RagnarokBotClient
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            StatusText = new Label();
            StatusValue = new Label();
            LogBox = new RichTextBox();
            StartButton = new Button();
            AuthPanel = new Panel();
            AuthFeedback = new Label();
            PasswordLabel = new Label();
            EmailLabel = new Label();
            LoginButton = new Button();
            PasswordBox = new TextBox();
            EmailBox = new TextBox();
            ServersPanel = new Panel();
            label1 = new Label();
            ServerListBox = new ListBox();
            debugCheckBox = new CheckBox();
            AuthPanel.SuspendLayout();
            ServersPanel.SuspendLayout();
            SuspendLayout();
            // 
            // StatusText
            // 
            StatusText.AutoSize = true;
            StatusText.Font = new Font("Segoe UI", 13F);
            StatusText.Location = new Point(12, 9);
            StatusText.Name = "StatusText";
            StatusText.Size = new Size(64, 25);
            StatusText.TabIndex = 0;
            StatusText.Text = "Status:";
            // 
            // StatusValue
            // 
            StatusValue.AutoSize = true;
            StatusValue.Font = new Font("Segoe UI", 13F);
            StatusValue.Location = new Point(73, 9);
            StatusValue.Name = "StatusValue";
            StatusValue.Size = new Size(72, 25);
            StatusValue.TabIndex = 1;
            StatusValue.Text = "Waiting";
            StatusValue.Click += StatusValue_Click;
            // 
            // LogBox
            // 
            LogBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            LogBox.Location = new Point(12, 111);
            LogBox.Name = "LogBox";
            LogBox.ReadOnly = true;
            LogBox.Size = new Size(495, 330);
            LogBox.TabIndex = 2;
            LogBox.Text = "";
            // 
            // StartButton
            // 
            StartButton.Location = new Point(12, 47);
            StartButton.Name = "StartButton";
            StartButton.Size = new Size(75, 23);
            StartButton.TabIndex = 3;
            StartButton.Text = "Start";
            StartButton.UseVisualStyleBackColor = true;
            StartButton.Click += StartButton_Click;
            // 
            // AuthPanel
            // 
            AuthPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            AuthPanel.Controls.Add(AuthFeedback);
            AuthPanel.Controls.Add(PasswordLabel);
            AuthPanel.Controls.Add(EmailLabel);
            AuthPanel.Controls.Add(LoginButton);
            AuthPanel.Controls.Add(PasswordBox);
            AuthPanel.Controls.Add(EmailBox);
            AuthPanel.Location = new Point(12, 9);
            AuthPanel.Name = "AuthPanel";
            AuthPanel.Size = new Size(495, 93);
            AuthPanel.TabIndex = 4;
            // 
            // AuthFeedback
            // 
            AuthFeedback.AutoSize = true;
            AuthFeedback.ForeColor = Color.IndianRed;
            AuthFeedback.Location = new Point(70, 64);
            AuthFeedback.Name = "AuthFeedback";
            AuthFeedback.Size = new Size(141, 15);
            AuthFeedback.TabIndex = 5;
            AuthFeedback.Text = "Invalid email or password";
            AuthFeedback.Visible = false;
            // 
            // PasswordLabel
            // 
            PasswordLabel.AutoSize = true;
            PasswordLabel.Location = new Point(7, 41);
            PasswordLabel.Name = "PasswordLabel";
            PasswordLabel.Size = new Size(57, 15);
            PasswordLabel.TabIndex = 4;
            PasswordLabel.Text = "Password";
            // 
            // EmailLabel
            // 
            EmailLabel.AutoSize = true;
            EmailLabel.Location = new Point(28, 12);
            EmailLabel.Name = "EmailLabel";
            EmailLabel.Size = new Size(36, 15);
            EmailLabel.TabIndex = 3;
            EmailLabel.Text = "Email";
            // 
            // LoginButton
            // 
            LoginButton.Location = new Point(325, 38);
            LoginButton.Name = "LoginButton";
            LoginButton.Size = new Size(75, 26);
            LoginButton.TabIndex = 2;
            LoginButton.Text = "Connect";
            LoginButton.UseVisualStyleBackColor = true;
            LoginButton.Click += LoginButton_Click;
            // 
            // PasswordBox
            // 
            PasswordBox.Location = new Point(70, 38);
            PasswordBox.Name = "PasswordBox";
            PasswordBox.Size = new Size(249, 23);
            PasswordBox.TabIndex = 1;
            // 
            // EmailBox
            // 
            EmailBox.Location = new Point(70, 9);
            EmailBox.Name = "EmailBox";
            EmailBox.Size = new Size(249, 23);
            EmailBox.TabIndex = 0;
            EmailBox.TextChanged += EmailBox_TextChanged;
            // 
            // ServersPanel
            // 
            ServersPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            ServersPanel.Controls.Add(label1);
            ServersPanel.Controls.Add(ServerListBox);
            ServersPanel.Location = new Point(12, 9);
            ServersPanel.Name = "ServersPanel";
            ServersPanel.Size = new Size(495, 432);
            ServersPanel.TabIndex = 5;
            ServersPanel.Visible = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 12F);
            label1.Location = new Point(13, 27);
            label1.Name = "label1";
            label1.Size = new Size(134, 21);
            label1.TabIndex = 1;
            label1.Text = "Select your server";
            label1.Click += label1_Click;
            // 
            // ServerListBox
            // 
            ServerListBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            ServerListBox.BorderStyle = BorderStyle.FixedSingle;
            ServerListBox.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ServerListBox.FormattingEnabled = true;
            ServerListBox.ItemHeight = 30;
            ServerListBox.Location = new Point(13, 68);
            ServerListBox.Name = "ServerListBox";
            ServerListBox.Size = new Size(467, 332);
            ServerListBox.TabIndex = 0;
            ServerListBox.SelectedIndexChanged += ServerListBox_SelectedIndexChanged;
            // 
            // debugCheckBox
            // 
            debugCheckBox.AutoSize = true;
            debugCheckBox.Location = new Point(12, 83);
            debugCheckBox.Name = "debugCheckBox";
            debugCheckBox.Size = new Size(83, 19);
            debugCheckBox.TabIndex = 6;
            debugCheckBox.Text = "Show Logs";
            debugCheckBox.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(519, 453);
            Controls.Add(ServersPanel);
            Controls.Add(AuthPanel);
            Controls.Add(StartButton);
            Controls.Add(LogBox);
            Controls.Add(StatusValue);
            Controls.Add(StatusText);
            Controls.Add(debugCheckBox);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Form1";
            Text = "The SCUM Bot";
            Load += Form1_Load;
            AuthPanel.ResumeLayout(false);
            AuthPanel.PerformLayout();
            ServersPanel.ResumeLayout(false);
            ServersPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label StatusText;
        private Label StatusValue;
        private RichTextBox LogBox;
        private Button StartButton;
        private Panel AuthPanel;
        private Label PasswordLabel;
        private Label EmailLabel;
        private Button LoginButton;
        private TextBox PasswordBox;
        private TextBox EmailBox;
        private Label AuthFeedback;
        private Panel ServersPanel;
        private Label label1;
        private ListBox ServerListBox;
        private CheckBox debugCheckBox;
    }
}
