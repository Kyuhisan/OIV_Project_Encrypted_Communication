using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace E2EE_Communication_Project
{
    public partial class MainForm : Form
    {
        #region KONSTANTE
        private Thread serverThread;
        private Thread clientThread;
        private TcpListener server;
        private TcpClient client;
        private TcpClient connectedClient;
        private NetworkStream serverStream;
        private NetworkStream clientStream;
        private bool run = true;
        private bool isClientConnected = false;
        private byte[] symmetricKey;
        private ECDiffieHellmanCng dh;
        private byte[] publicKey;
        #endregion

        #region INICIALIZACIJA
        public MainForm()
        {
            InitializeComponent();
            InitializeCryptography();
        }
        private void InitializeCryptography()
        {
            dh = new ECDiffieHellmanCng
            {
                KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
                HashAlgorithm = CngAlgorithm.Sha256
            };
            publicKey = dh.PublicKey.ToByteArray();
        }
        #endregion

        #region SERVER
        private void StartServer(string ip, int port)
        {
            serverThread = new Thread(() =>
            {
                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    //server = new TcpListener(IPAddress.Parse(ip), port);
                    server.Start();
                    UpdateMessages($"Server started on {ip}:{port}...");

                    connectedClient = server.AcceptTcpClient();
                    serverStream = connectedClient.GetStream();
                    UpdateMessages("Client connected to the server.");

                    PerformKeyExchange(serverStream, "Server");
                    HandleFileTransfer(serverStream, "Client");
                }
                catch (Exception ex)
                {
                    UpdateMessages($"Server error: {ex.Message}");
                }
            })
            { IsBackground = true };

            serverThread.Start();
        }
        #endregion

        #region CLIENT
        private void StartClient(string ip, int port)
        {
            clientThread = new Thread(() =>
            {
                try
                {
                    client = new TcpClient(ip, port);
                    clientStream = client.GetStream();
                    isClientConnected = true;
                    UpdateMessages($"Connected to the server at {ip}:{port}.");

                    PerformKeyExchange(clientStream, "Client");
                    HandleFileTransfer(clientStream, "Server");
                }
                catch (Exception ex)
                {
                    UpdateMessages($"Client error: {ex.Message}");
                }
            })
            { IsBackground = true };

            clientThread.Start();
        }
        #endregion

        #region DEFFIE-HELLMAN
        private void PerformKeyExchange(NetworkStream stream, string role)
        {
            if (role == "Server")
            {
                byte[] clientPublicKey = ReceiveData(stream, asString: false);
                UpdateMessages($"Received client's public key: {BitConverter.ToString(clientPublicKey).Replace("-", "")}");

                SendData(stream, publicKey, isString: false);
                UpdateMessages($"Sent public key to client: {BitConverter.ToString(publicKey).Replace("-", "")}");

                symmetricKey = dh.DeriveKeyMaterial(CngKey.Import(clientPublicKey, CngKeyBlobFormat.EccPublicBlob));
            }
            else if (role == "Client")
            {
                SendData(stream, publicKey, isString: false);
                UpdateMessages($"Sent public key to server: {BitConverter.ToString(publicKey).Replace("-", "")}");

                byte[] serverPublicKey = ReceiveData(stream, asString: false);
                UpdateMessages($"Received server's public key: {BitConverter.ToString(serverPublicKey).Replace("-", "")}");

                symmetricKey = dh.DeriveKeyMaterial(CngKey.Import(serverPublicKey, CngKeyBlobFormat.EccPublicBlob));
            }

            UpdateMessages($"Generated symmetric key: {BitConverter.ToString(symmetricKey).Replace("-", "")}");
        }
        #endregion

        #region MESSAGE
        private void ReceiveMessage(NetworkStream stream)
        {
            string senderInstanceName = Encoding.UTF8.GetString(ReceiveData(stream, asString: true));
            byte[] encryptedMessage = ReceiveData(stream, asString: false);
            string message = Encoding.UTF8.GetString(DecryptData(encryptedMessage));

            UpdateMessages($"{senderInstanceName}: {message}");
        }

        private void SendMessage(object sender, EventArgs e)
        {
            if (!isClientConnected)
            {
                UpdateMessages("Message transfer is only allowed when connected.");
                return;
            }

            string message = ((TextBox)this.Controls["MessageInput"]).Text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            string instanceName = ((TextBox)this.Controls["InstanceNameInput"]).Text.Trim();
            if (string.IsNullOrEmpty(instanceName)) instanceName = "Unknown";

            NetworkStream stream = clientStream ?? serverStream;

            SendData(stream, Encoding.UTF8.GetBytes("[MESSAGE]"), isString: true);
            SendData(stream, Encoding.UTF8.GetBytes(instanceName), isString: true);
            SendData(stream, EncryptData(Encoding.UTF8.GetBytes(message)), isString: false);

            UpdateMessages($"{instanceName}: {message}");
            ((TextBox)this.Controls["MessageInput"]).Text = "";
        }
        #endregion

        #region PRENOS DATOTEK
        private void HandleFileTransfer(NetworkStream stream, string senderName)
        {
            while (run)
            {
                string header = Encoding.UTF8.GetString(ReceiveData(stream, asString: true));

                if (header == "[MESSAGE]")
                {
                    ReceiveMessage(stream);
                }

                if (header == "[FILE]")
                {
                    ReceiveFile(stream, senderName);
                }
            }
        }
        private void ReceiveFile(NetworkStream stream, string senderName)
        {
            string fileName = Encoding.UTF8.GetString(ReceiveData(stream, asString: true));
            long fileSize = long.Parse(Encoding.UTF8.GetString(ReceiveData(stream, asString: true)));

            string savePath = GetReceivedFilePath(fileName, senderName);

            using (FileStream fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write))
            {
                long totalBytesReceived = 0;

                while (totalBytesReceived < fileSize)
                {
                    byte[] encryptedChunk = ReceiveData(stream, asString: false);
                    byte[] decryptedChunk = DecryptData(encryptedChunk);
                    fileStream.Write(decryptedChunk, 0, decryptedChunk.Length);

                    totalBytesReceived += decryptedChunk.Length;
                    UpdateProgress((int)(100 * totalBytesReceived / fileSize));
                }
            }

            UpdateMessages($"File '{fileName}' received and saved to {Path.GetDirectoryName(savePath)}.");
        }

        private void SendFile(object sender, EventArgs e)
        {
            if (!isClientConnected)
            {
                UpdateMessages("File transfer is only allowed when connected.");
                return;
            }

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    string fileName = Path.GetFileName(filePath);
                    long fileSize = new FileInfo(filePath).Length;

                    NetworkStream stream = clientStream ?? serverStream;

                    SendData(stream, Encoding.UTF8.GetBytes("[FILE]"), isString: true);
                    SendData(stream, Encoding.UTF8.GetBytes(fileName), isString: true);
                    SendData(stream, Encoding.UTF8.GetBytes(fileSize.ToString()), isString: true);

                    using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        byte[] buffer = new byte[64 * 1024];
                        int bytesRead;
                        long totalBytesSent = 0;

                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            byte[] encryptedChunk = EncryptData(buffer.Take(bytesRead).ToArray());
                            SendData(stream, encryptedChunk, isString: false);

                            totalBytesSent += bytesRead;
                            UpdateProgress((int)(100 * totalBytesSent / fileSize));
                        }
                    }

                    UpdateMessages($"File '{fileName}' sent successfully.");
                }
            }
        }

        private string GetReceivedFilePath(string fileName, string senderName)
        {
            string instanceName = ((TextBox)this.Controls["InstanceNameInput"]).Text.Trim();
            if (string.IsNullOrEmpty(instanceName)) instanceName = Environment.MachineName;

            string receivedFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"PREJETO_{instanceName}_{senderName}"
            );

            if (!Directory.Exists(receivedFolderPath))
            {
                Directory.CreateDirectory(receivedFolderPath);
            }

            return Path.Combine(receivedFolderPath, fileName);
        }
        #endregion

        #region ŠIFRIRANJE
        private byte[] EncryptData(byte[] data)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = symmetricKey;
                aes.GenerateIV();
                byte[] iv = aes.IV;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    byte[] encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);
                    return iv.Concat(encryptedData).ToArray();
                }
            }
        }

        private byte[] DecryptData(byte[] encryptedData)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = symmetricKey;

                byte[] iv = encryptedData.Take(16).ToArray();
                aes.IV = iv;

                byte[] ciphertext = encryptedData.Skip(16).ToArray();
                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                }
            }
        }
        #endregion

        #region PRENOS PODATKOV
        private void SendData(NetworkStream stream, byte[] data, bool isString)
        {
            if (isString)
            {
                data = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(data) + "\0");
            }

            byte[] sizeBytes = BitConverter.GetBytes(data.Length);
            stream.Write(sizeBytes, 0, sizeBytes.Length);
            stream.Write(data, 0, data.Length);
        }

        private byte[] ReceiveData(NetworkStream stream, bool asString)
        {
            byte[] sizeBuffer = new byte[4];
            stream.Read(sizeBuffer, 0, sizeBuffer.Length);
            int dataSize = BitConverter.ToInt32(sizeBuffer, 0);

            byte[] dataBuffer = new byte[dataSize];
            int totalBytesRead = 0;

            while (totalBytesRead < dataSize)
            {
                int bytesRead = stream.Read(dataBuffer, totalBytesRead, dataSize - totalBytesRead);
                if (bytesRead == 0) break;
                totalBytesRead += bytesRead;
            }

            return asString ? Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(dataBuffer).TrimEnd('\0')) : dataBuffer;
        }
        #endregion

        #region GUI SPREMEMBE
        private void UpdateMessages(string message)
        {
            RichTextBox messagesBox = (RichTextBox)this.Controls["MessagesBox"];
            messagesBox.Invoke(new Action(() => messagesBox.AppendText(message + Environment.NewLine)));
        }

        private void UpdateProgress(int progressPercentage)
        {
            ProgressBar progressBar = (ProgressBar)this.Controls["ProgressBar"];
            progressBar.Invoke(new Action(() => progressBar.Value = progressPercentage));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            run = false;
            server?.Stop();
            client?.Close();
            connectedClient?.Close();
            base.OnFormClosing(e);
        }
        #endregion
    }
}
