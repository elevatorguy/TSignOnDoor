using System.Net.Sockets;
using System.Threading;
using System;
using System.Text;
using System.Net;
using NDesk.Options;

namespace Terraria_Server
{
	public class Program
    {
        private static TcpListener l;
        private static Thread listenThread;
        private static Boolean enabled = true;
        private static String mmessage;
        private static String port = "7777";

        public static void Main(string[] argz)
		{
            try
            {
                Thread.CurrentThread.Name = "Main";

                string MODInfo = "Terraria Sign On Door. ";
                try
                {
                    Console.Title = MODInfo;
                }
                catch
                {

                }

                var options = new OptionSet()
            {
         		{ "p|port=", v => port = v },
            };

                var args = options.Parse(argz);

                if (args.Count == 0)
                {
                    mmessage = "The server is down for maintenance.";
                }
                else if (args.Count == 1)
                {
                    mmessage = args[0];
                }
                else
                {
                    mmessage = "";
                    for (int i = 0; i < args.Count; i++)
                    {
                        mmessage = mmessage + " " + args[i];
                    }
                }

                l = new TcpListener(IPAddress.Any, Convert.ToInt32(port));
                listenThread = new Thread(new ThreadStart(ListenForClients));
                listenThread.Start();
                while (!listenThread.IsAlive) ;

                ProgramLog.Log("Starting TSignOnDoor \"" + mmessage + "\" ");
            }
            catch(FormatException e)
            {
                ProgramLog.Log("Port must be an Integer.");
            }
		}

        private static void ListenForClients()
        {
            try
            {
                l.Start();
                ProgramLog.Log("Listening for clients on port " + port + ".");

                while (enabled)
                {
                    //blocks until a client has connected to the server
                    TcpClient client = l.AcceptTcpClient();

                    //create a thread to handle communication
                    //with connected client
                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                    clientThread.Start(client);
                }

                ProgramLog.Log("Stopped listening for clients.");
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == 10013)
                {
                    ProgramLog.Log("Access denied for that port.");
                }
                else if (e.ErrorCode == 10048)
                {
                    ProgramLog.Log("Port already in use.");
                }
                else
                {
                    ProgramLog.Log(e.StackTrace);
                }
            }
        }
        
        private static void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] message = new byte[4096];
            int bytesRead;

            DateTime value = DateTime.UtcNow;
            string time = value.ToString("yyyy/MM/dd HH:mm:ss");
            ProgramLog.Log(time+">("+tcpClient.Client.RemoteEndPoint.ToString()+") Connecting...");

            while (enabled)
            {
                bool finished = false;
                bool error = false;
                while (!finished)
                {
                    bytesRead = 0;
                    message = new byte[4096];
                    try
                    {
                        //blocks until a client sends a message
                        bytesRead = clientStream.Read(message, 0, message.Length);
                    }
                    catch
                    {
                        //a socket error has occured
                        error = true;
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        //the client has disconnected from the server
                        error = true;
                        break;
                    }
                  //  ProgramLog.Log("read from client");
                    //message has successfully been received

                 //   string numbers = "";
                 //   for (int j = 0; j < message[0] + 4; j++)
                 //   {
                 //       string s = Convert.ToString(message[j], 16);
                 //       numbers = numbers + " " + s;
                 //   }
                 //   ProgramLog.Log(numbers);

                    //accept connection after first read
                    if (message[4] == 0x01)
                    {
                        byte[] accept = { 0x02, 0x00, 0x00, 0x00, 0x03, 0x05 };

                        clientStream.Write(accept, 0, accept.Length);
                        clientStream.Flush();
                       // ProgramLog.Log("write to client");
                    }

                    if (message[4] == 0x04)
                    {
                        // parses the player info to get the player's name
                        ASCIIEncoding en = new ASCIIEncoding();
                        string name = en.GetString(message, 30, ((message[0] + 4) - 30));
                        ProgramLog.Log(name + " tried to join.");
                    }

                    // client send request for world info, so we are done reading
                    if (message[4] == 0x06)
                    {
                        // kick the client
                        ASCIIEncoding asen = new ASCIIEncoding();
                        byte[] down = asen.GetBytes(mmessage);

                        byte[] buffer2 = { (byte)(down.Length + 1), 0x00, 0x00, 0x00, 0x02 };

                        byte[] msg = new byte[down.Length + 5];
                        Array.Copy(buffer2, 0, msg, 0, 5);
                        Array.Copy(down, 0, msg, 5, down.Length);
                        clientStream.Write(msg, 0, msg.Length);
                        clientStream.Flush();
                       // ProgramLog.Log("write to client");

                        finished = true;
                    }

                }

                if (error)
                    break;

            }
            value = DateTime.UtcNow;
            time = value.ToString("yyyy/MM/dd HH:mm:ss");
            ProgramLog.Log(time+">("+tcpClient.Client.RemoteEndPoint.ToString() + ") disconnected...");
            clientStream.Close();
            tcpClient.Close();
        }
	}
}
