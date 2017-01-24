using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDPClient
{
    // Commands interaction between server and client
    enum Command
    {
        Login,
        Logout,
        Message,
        // get a list of all the users in the room
        List,
        // nothing
        Null
    }

    // Packet structure
    // Description:     -> |Commands|name length|message length|    name   |    message   |
    // Size in bytes:   -> |   4    |     4     |       4      |name length|message length|


    /// <summary>
    /// A custom packet-class for the chat
    /// </summary>
    class CustomPacket
    {
        // Attributes:
        // Chat users login-name
        public string strName;
        // Message text
        public string strMessage;
        // Command type
        public Command cmdCommand;

        /// <summary>
        /// Constructor for the custom packet
        /// </summary>
        public CustomPacket()
        {
            this.cmdCommand = Command.Null;
            this.strMessage = null;
            this.strName = null;
        }


        /// <summary>
        /// Constructor that converts bytes into a Data-object
        /// </summary>
        /// <param name="data"></param>
        public CustomPacket(byte[] data)
        {
            this.cmdCommand = (Command)BitConverter.ToInt32(data, 0);

            int nameLen = BitConverter.ToInt32(data, 4);
            int msgLen = BitConverter.ToInt32(data, 8);

            // Make sure that the name has been passed
            if (nameLen > 0) this.strName = Encoding.UTF8.GetString(data, 12, nameLen);
            else this.strMessage = null;

            // Check for an empty message field
            if (msgLen > 0) this.strMessage = Encoding.UTF8.GetString(data, 12 + nameLen, msgLen);
            else this.strMessage = null;
        }


        /// <summary>
        /// Convert a Data-object to byte stream
        /// </summary>
        /// <returns></returns>
        public byte[] ToByte()
        {

            List<byte> result = new List<byte>();

            result.AddRange(BitConverter.GetBytes((int)cmdCommand));

            if (strName != null) result.AddRange(BitConverter.GetBytes(strName.Length));
            else result.AddRange(BitConverter.GetBytes(0));

            if (strMessage != null) result.AddRange(BitConverter.GetBytes(strMessage.Length));
            else result.AddRange(BitConverter.GetBytes(0));

            if (strName != null) result.AddRange(Encoding.UTF8.GetBytes(strName));
            if (strMessage != null) result.AddRange(Encoding.UTF8.GetBytes(strMessage));

            return result.ToArray();
        }
    }
}
