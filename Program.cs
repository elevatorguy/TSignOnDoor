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
                bytesRead = 0;

                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, message.Length);
                }
                catch
                {
                    //a socket error has occured
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }
               // ProgramLog.Log("read from client");
                //message has successfully been received

              //  string numbers = "";
              //  for (int i = 0; i < message[0]+4; i++ )
              //  {
              //      string s = Convert.ToString(message[i], 16);
               //     numbers = numbers + " " + s;
              //  }
              //  ProgramLog.Log(numbers);

                ASCIIEncoding asen = new ASCIIEncoding();
                byte[] down = asen.GetBytes(mmessage);

                byte[] buffer2 = { (byte)(down.Length+1), 0x00, 0x00, 0x00, 0x02 };

                byte[] msg = new byte[down.Length + 5];
                Array.Copy(buffer2, 0, msg, 0, 5);
                Array.Copy(down, 0, msg, 5, down.Length);
                clientStream.Write(msg, 0, msg.Length);
                clientStream.Flush();
              //  ProgramLog.Log("write to client");
            }
            value = DateTime.UtcNow;
            time = value.ToString("yyyy/MM/dd HH:mm:ss");
            ProgramLog.Log(time+">("+tcpClient.Client.RemoteEndPoint.ToString() + ") disconnected...");
            clientStream.Close();
            tcpClient.Close();
        }
	}
}
