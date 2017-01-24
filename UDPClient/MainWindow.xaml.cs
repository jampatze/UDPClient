using System;
using System.Collections.Generic;
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
        // display message delegate
        private delegate void DisplayMessageDelegate(string message);
        private DisplayMessageDelegate displayMessageDelegate = null;

        public MainWindow()
        {
            InitializeComponent();
            buttonSend.IsEnabled = false;
        }

        /// <summary>
        /// Initialises the delegate on load
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_Load(object sender, EventArgs e)
        {
            this.displayMessageDelegate = new DisplayMessageDelegate(this.DisplayMessage);
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
                    if (receivedData.strMessage != null)
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
        /// Displays the message in the chatBox
        /// </summary>
        /// <param name="messge"></param>
        private void DisplayMessage(string message)
        {
            chatBox.AppendText(message + Environment.NewLine);
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
