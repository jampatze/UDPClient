using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UDPClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Socket clientSocket;
        int port = 9999;
        EndPoint epServer;
        // data stream
        private byte[] dataStream = new byte[1024];
        // list of users on channel
        ObservableCollection<string> users = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();
            buttonSend.IsEnabled = false;
            chatBox.TextChanged += chatBox_TextChanged;
            listBoxUsers.ItemsSource = users;
        }


        /// <summary>
        /// Parses the message and sends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CustomPacket sendData = new CustomPacket();
                sendData.strName = textBoxName.Text;
                sendData.strMessage = textBoxMessage.Text.Trim();
                sendData.cmdCommand = Command.Message;

                // Get packet as byte array
                byte[] byteData = sendData.ToByte();

                // Send packet to the server
                clientSocket.BeginSendTo(byteData, 0, byteData.Length, SocketFlags.None, epServer, new AsyncCallback(this.SendData), null);


                textBoxMessage.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Send while sending message: " + ex.Message, "UDP Client");
            }
        }

        /// <summary>
        /// Logs the user in to the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CustomPacket sendData = new CustomPacket();
                sendData.strName = this.textBoxName.Text;
                sendData.strMessage = null;
                sendData.cmdCommand = Command.Login;

                this.clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                IPAddress serverIP = IPAddress.Parse(textBoxServer.Text);
                IPEndPoint server = new IPEndPoint(serverIP, port);
                epServer = (EndPoint)server;

                byte[] data = sendData.ToByte();

                clientSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, epServer, new AsyncCallback(this.SendData), null);

                this.dataStream = new byte[1024];

                sendData = new CustomPacket();
                sendData.strName = null;
                sendData.strMessage = null;
                sendData.cmdCommand = Command.List;

                data = sendData.ToByte();

                clientSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, epServer, new AsyncCallback(this.SendData), null);

                this.dataStream = new byte[1024];

                // Begin listening for broadcasts
                clientSocket.BeginReceiveFrom(this.dataStream, 0, this.dataStream.Length, SocketFlags.None, ref epServer, new AsyncCallback(this.ReceiveData), null);
                buttonSend.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while connecting to server: " + ex.Message, "UDP Client");
            }
        }


        /// <summary>
        /// Sends data
        /// </summary>
        /// <param name="ar"></param>
        private void SendData(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndSend(ar);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while sending data: " + ex.Message, "UDP Client");
            }
        }

        /// <summary>
        /// Revieves data
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveData(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndReceive(ar);
                CustomPacket receivedData = new CustomPacket(dataStream);

                this.Dispatcher.Invoke(() =>
                {
                    if (receivedData.cmdCommand == Command.List) UpdateList(receivedData.strMessage);

                    if (receivedData.strMessage != null && receivedData.cmdCommand != Command.List)
                        DisplayMessage(receivedData.strMessage + Environment.NewLine);
                });

                this.dataStream = new byte[1024];

                // listen for more broadasts
                clientSocket.BeginReceiveFrom(this.dataStream, 0, this.dataStream.Length, SocketFlags.None, ref epServer, new AsyncCallback(this.ReceiveData), null);
            }

            catch (ObjectDisposedException)
            { }

            catch (Exception ex)
            {
                MessageBox.Show("Error while recieving data: " + ex.Message, "UDP Client");
            }
        }

        /// <summary>
        /// Scrolls the chatBox to bottom
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chatBox_TextChanged(object sender, EventArgs e)
        {
            chatBox.ScrollToEnd();
        }

        /// <summary>
        /// Displays the message in the chatBox
        /// </summary>
        /// <param name="messge"></param>
        private void DisplayMessage(string message)
        {
            chatBox.AppendText(message + Environment.NewLine);
        }


        /// <summary>
        /// Parses the list-message and updates the list
        /// </summary>
        /// <param name="message">message to be parsed</param>
        private void UpdateList(string message)
        {
            char[] separators = { '*' };

            string[] substrings = message.Split(separators);

            // add new users to users
            foreach (string newuser in substrings)
            {
                if (!String.IsNullOrEmpty(newuser))
                {
                    // if user already exists in users, dont add
                    if (!users.Any(user => user == newuser)) users.Add(newuser);
                }
            }
            
            // removes old users from the list
            for (int i = (users.Count -1); i >= 0; i--)
            {
                string oldUser = users[i];
                if (!substrings.Contains(oldUser)) users.Remove(oldUser);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (this.clientSocket != null)
                {
                    CustomPacket sendData = new CustomPacket();
                    sendData.cmdCommand = Command.Logout;
                    sendData.strName = textBoxName.Text;
                    sendData.strMessage = null;

                    byte[] byteData = sendData.ToByte();

                    this.clientSocket.SendTo(byteData, 0, byteData.Length, SocketFlags.None, epServer);

                    sendData = new CustomPacket();
                    sendData.cmdCommand = Command.List;
                    sendData.strName = null;
                    sendData.strMessage = null;

                    byteData = sendData.ToByte();

                    this.clientSocket.SendTo(byteData, 0, byteData.Length, SocketFlags.None, epServer);

                    this.clientSocket.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while closing: " + ex.Message, "UDP Client");
            }
        }
    }
}
