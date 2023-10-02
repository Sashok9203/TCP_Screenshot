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

        static void Main(string[] args)
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

        private static async void clientProcess(TcpClient client)
        {
            Console.WriteLine($"Client  {client.Client.RemoteEndPoint} connected");
            using StreamReader reader = new(client.GetStream());
            ClientCommand? command = JsonSerializer.Deserialize<ClientCommand>(reader.ReadLine());
            Console.WriteLine($"Client {client.Client.RemoteEndPoint} send command {command?.Command}");
            using StreamWriter writer = new(client.GetStream());
            switch (command?.Command)
            {
                case Command.Screenshot:
                    await SendScreenShot(writer,client.Client.RemoteEndPoint);
                    break;
                case Command.AutoScreenshotStart:

                    while (client.Connected)
                    {
                       await SendScreenShot(writer, client.Client.RemoteEndPoint);
                       await Task.Delay(command.Period * 1000);
                    }
                    break;
                default: throw new InvalidEnumArgumentException();
            }
            Console.WriteLine($"Client {client.Client.RemoteEndPoint} disconnected");
        }

        private static  Task  SendScreenShot(StreamWriter writer,EndPoint endPoint)
        {
            return Task.Run(async () =>
            {
                ImageConverter converter = new();
                byte[]? buffer = (byte[]?)converter.ConvertTo(BitmapLibrary.TakeScreenshot(), typeof(byte[]));
                try
                {
                    string json = JsonSerializer.Serialize(buffer);
                  
                    await writer.WriteLineAsync(json);
                    
                    Console.WriteLine($"Screenshoot sended to client {endPoint}");
                    buffer = null;
                }
                catch (Exception ex) { Console.WriteLine(ex.Message); }
            });
        }
    }
}