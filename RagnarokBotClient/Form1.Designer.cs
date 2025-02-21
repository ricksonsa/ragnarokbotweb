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
            TestBtn = new Button();
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
            LogBox.Location = new Point(12, 85);
            LogBox.Name = "LogBox";
            LogBox.ReadOnly = true;
            LogBox.Size = new Size(776, 353);
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
            // TestBtn
            // 
            TestBtn.Location = new Point(104, 47);
            TestBtn.Name = "TestBtn";
            TestBtn.Size = new Size(75, 23);
            TestBtn.TabIndex = 4;
            TestBtn.Text = "Test";
            TestBtn.UseVisualStyleBackColor = true;
            TestBtn.Click += TestBtn_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(TestBtn);
            Controls.Add(StartButton);
            Controls.Add(LogBox);
            Controls.Add(StatusValue);
            Controls.Add(StatusText);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "Form1";
            Text = "Ragnarok Bot";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label StatusText;
        private Label StatusValue;
        private RichTextBox LogBox;
        private Button StartButton;
        private Button TestBtn;
    }
}
