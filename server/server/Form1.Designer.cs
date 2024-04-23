namespace server
{
    partial class Form1
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
            this.textBox_port = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.button_listen = new System.Windows.Forms.Button();
            this.logs = new System.Windows.Forms.RichTextBox();
            this.Label2 = new System.Windows.Forms.Label();
            this.textBox_message = new System.Windows.Forms.TextBox();
            this.button_send = new System.Windows.Forms.Button();

            this.leaderboard = new System.Windows.Forms.ListBox();

            this.SuspendLayout();
            // 
            // textBox_port
            // 
            this.textBox_port.Location = new System.Drawing.Point(88, 69);
          
            this.textBox_port.Margin = new System.Windows.Forms.Padding(2);

            this.textBox_port.Name = "textBox_port";
            this.textBox_port.Size = new System.Drawing.Size(138, 20);
            this.textBox_port.TabIndex = 0;
            this.textBox_port.Text = "3131";
            this.textBox_port.TextChanged += new System.EventHandler(this.textBox_port_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(40, 70);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Port:";
            // 
            // button_listen
            // 
            this.button_listen.Location = new System.Drawing.Point(234, 66);

            this.button_listen.Margin = new System.Windows.Forms.Padding(2);

            this.button_listen.Name = "button_listen";
            this.button_listen.Size = new System.Drawing.Size(56, 22);
            this.button_listen.TabIndex = 2;
            this.button_listen.Text = "Listen";
            this.button_listen.UseVisualStyleBackColor = true;
            this.button_listen.Click += new System.EventHandler(this.button_listen_Click);
            // 
            // logs
            // 
            this.logs.Location = new System.Drawing.Point(42, 112);

            this.logs.Margin = new System.Windows.Forms.Padding(2);

            this.logs.Name = "logs";
            this.logs.Size = new System.Drawing.Size(338, 171);
            this.logs.TabIndex = 3;
            this.logs.Text = "";
            this.logs.TextChanged += new System.EventHandler(this.logs_TextChanged);
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Location = new System.Drawing.Point(40, 312);
            this.Label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(53, 13);
            this.Label2.TabIndex = 4;
            this.Label2.Text = "Message:";
            // 
            // textBox_message
            // 
            this.textBox_message.Enabled = false;
            this.textBox_message.Location = new System.Drawing.Point(96, 312);

            this.textBox_message.Margin = new System.Windows.Forms.Padding(2);

            this.textBox_message.Name = "textBox_message";
            this.textBox_message.Size = new System.Drawing.Size(216, 20);
            this.textBox_message.TabIndex = 5;
            // 
            // button_send
            // 
            this.button_send.Enabled = false;
            this.button_send.Location = new System.Drawing.Point(320, 306);

            this.button_send.Margin = new System.Windows.Forms.Padding(2);

            this.button_send.Name = "button_send";
            this.button_send.Size = new System.Drawing.Size(59, 28);
            this.button_send.TabIndex = 6;
            this.button_send.Text = "send";
            this.button_send.UseVisualStyleBackColor = true;
            this.button_send.Click += new System.EventHandler(this.button_send_Click);
            // 
            // leaderboard
            // 

            this.leaderboard.FormattingEnabled = true;
            this.leaderboard.Location = new System.Drawing.Point(399, 70);
            this.leaderboard.Name = "leaderboard";
            this.leaderboard.Size = new System.Drawing.Size(221, 264);
            this.leaderboard.TabIndex = 7;
            this.leaderboard.SelectedIndexChanged += new System.EventHandler(this.leaderboard_SelectedIndexChanged);

            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;

            this.ClientSize = new System.Drawing.Size(632, 382);

            this.Controls.Add(this.leaderboard);
            this.Controls.Add(this.button_send);
            this.Controls.Add(this.textBox_message);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.logs);
            this.Controls.Add(this.button_listen);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox_port);

            this.Margin = new System.Windows.Forms.Padding(2);

            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_port;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button_listen;
        private System.Windows.Forms.RichTextBox logs;
        private System.Windows.Forms.Label Label2;
        private System.Windows.Forms.TextBox textBox_message;
        private System.Windows.Forms.Button button_send;

        private System.Windows.Forms.ListBox leaderboard;

    }
}

