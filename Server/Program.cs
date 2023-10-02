using System.Drawing;
using System.Net.Sockets;
using System.Net;
using System.Text.Json;
using Shared_Data;
using System.ComponentModel;

namespace Server
{
    internal class Program
    {
        static private readonly string ip = "127.0.0.1";

        static private readonly int port = 8080;

        static  Task Main(string[] args)
        {
            IPEndPoint localEndPoint = new(IPAddress.Parse(ip), port);
            TcpListener server = new(localEndPoint);
            server.Start(10);
            while (true)
            {
                Console.WriteLine("\tWaiting command...");
                TcpClient client = server.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem<TcpClient>(clientProcess,client,true);
            }
        }

        private static void clientProcess(TcpClient client)
        {
            Console.WriteLine($"Client  {client.Client.RemoteEndPoint} connected");
            using StreamReader reader = new(client.GetStream());
            ClientCommand? command = JsonSerializer.Deserialize<ClientCommand>(reader.ReadLine());
            Console.WriteLine($"Client {client.Client.RemoteEndPoint} send command {command?.Command}");
            using StreamWriter writer = new(client.GetStream());
            switch (command?.Command)
            {
                case Command.Screenshot:
                     SendScreenShotAsync(writer,client.Client.RemoteEndPoint);
                    break;
                case Command.AutoScreenshotStart:

                    while (client.Connected)
                    {
                         SendScreenShotAsync(writer, client.Client.RemoteEndPoint);
                         Thread.Sleep(command.Period * 1000);
                    }
                    break;
                default: throw new InvalidEnumArgumentException();
            }
            Console.WriteLine($"Client {client.Client.RemoteEndPoint} disconnected");
        }

        private static  void SendScreenShotAsync(StreamWriter writer,EndPoint endPoint)
        {
            ImageConverter converter = new();
            byte[]? buffer = (byte[]?) converter.ConvertTo(BitmapLibrary.TakeScreenshot(), typeof(byte[]));
            try
            {
                string json = JsonSerializer.Serialize(buffer);
                writer.WriteLine(json);
                writer.Flush();
                Console.WriteLine($"Screenshoot sended to client {endPoint}");
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
    }
}