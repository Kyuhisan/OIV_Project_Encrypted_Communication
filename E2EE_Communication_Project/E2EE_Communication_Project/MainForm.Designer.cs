using System.Windows.Forms;

namespace E2EE_Communication_Project
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "Peer-to-Peer Communication";
            this.Width = 800;
            this.Height = 600;

            Label serverIpLabel = new Label { Text = "Server IP:", Top = 10, Left = 10, Width = 80 };
            TextBox serverIpInput = new TextBox { Top = 10, Left = 90, Width = 150, Text = "127.0.0.1" };
            serverIpInput.Name = "ServerIPInput";

            Label serverPortLabel = new Label { Text = "Server Port:", Top = 10, Left = 260, Width = 80 };
            TextBox serverPortInput = new TextBox { Top = 10, Left = 340, Width = 100, Text = "25252" };
            serverPortInput.Name = "ServerPortInput";

            Button startServerButton = new Button { Text = "Start Server", Top = 10, Left = 460 };
            startServerButton.Click += (sender, e) => StartServer(serverIpInput.Text, int.Parse(serverPortInput.Text));

            Label clientIpLabel = new Label { Text = "Connect IP:", Top = 50, Left = 10, Width = 80 };
            TextBox clientIpInput = new TextBox { Top = 50, Left = 90, Width = 150, Text = "127.0.0.1" };
            clientIpInput.Name = "ClientIPInput";

            Label clientPortLabel = new Label { Text = "Connect Port:", Top = 50, Left = 260, Width = 80 };
            TextBox clientPortInput = new TextBox { Top = 50, Left = 340, Width = 100, Text = "25252" };
            clientPortInput.Name = "ClientPortInput";

            Button connectButton = new Button { Text = "Connect", Top = 50, Left = 460 };
            connectButton.Click += (sender, e) => StartClient(clientIpInput.Text, int.Parse(clientPortInput.Text));

            Label instanceNameLabel = new Label { Text = "Instance Name:", Top = 90, Left = 10, Width = 100 };
            TextBox instanceNameInput = new TextBox { Top = 90, Left = 120, Width = 150, Text = "Instance" };
            instanceNameInput.Name = "InstanceNameInput";

            RichTextBox messagesBox = new RichTextBox { Top = 130, Left = 10, Width = 760, Height = 350, ReadOnly = true };
            messagesBox.Name = "MessagesBox";

            Button sendFileButton = new Button { Text = "Send File", Top = 540, Left = 10 };
            sendFileButton.Click += SendFile;

            ProgressBar progressBar = new ProgressBar { Top = 540, Left = 120, Width = 550, Height = 20 };
            progressBar.Name = "ProgressBar";

            Button sendMessageButton = new Button { Text = "Send Message", Top = 500, Left = 10 };
            sendMessageButton.Click += SendMessage;

            TextBox messageInput = new TextBox { Top = 500, Left = 120, Width = 550 };
            messageInput.Name = "MessageInput";

            this.Controls.Add(messageInput);
            this.Controls.Add(sendMessageButton);


            this.Controls.Add(serverIpLabel);
            this.Controls.Add(serverPortLabel);
            this.Controls.Add(clientIpLabel);
            this.Controls.Add(clientPortLabel);
            this.Controls.Add(instanceNameLabel);

            this.Controls.Add(serverIpInput);
            this.Controls.Add(serverPortInput);
            this.Controls.Add(clientIpInput);
            this.Controls.Add(clientPortInput);
            this.Controls.Add(instanceNameInput);

            this.Controls.Add(connectButton);
            this.Controls.Add(startServerButton);
            this.Controls.Add(sendFileButton);

            this.Controls.Add(progressBar);
            this.Controls.Add(messagesBox);

            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.ResumeLayout(false);
        }
    }

}

