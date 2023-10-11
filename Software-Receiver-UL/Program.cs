using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

static class TcpClientExample
{
    private static ConcurrentDictionary<int, TcpClient> connectedClients = new ConcurrentDictionary<int, TcpClient>();
    private static TcpClient proxy = new TcpClient();

    // proxy
    private static readonly string proxyServerIp = "212.116.138.116";
    private static readonly int proxyServerPort = 4545;

    // cms
    private static readonly string cmsServerIp = "127.0.0.1";
    private static readonly int cmsServerPort = 4545;

    static void Main()
    {
        _ = Task.Run(OpenConnections);
        while (true) { }
    }

    static async Task OpenConnections() {
        try
        {
            //Console.WriteLine("Start Connections");
            // Start client
            _ = Task.Run(ReceiveMessagesFromProxyAsync);

            // Start server
            TcpListener listener = new TcpListener(IPAddress.Parse(cmsServerIp), cmsServerPort);
            listener.Start();

            int i = 0;

            while (true)
            {
                // Accept incoming client connections
                TcpClient client = await listener.AcceptTcpClientAsync();
                //Console.WriteLine("New client " + client);
                connectedClients.TryAdd(i, client);

                // Start a new task to handle the client connection
                _ = Task.Run(() => ReceiveMessagesFromCMSAsync(client, i++));
            }

        }
        catch (Exception ex)
        {
            //Console.WriteLine("Error: " + ex.Message);
            _ =  Task.Run(OpenConnections);
        }

    }

    static async Task ReceiveMessagesFromProxyAsync()
    {
        //Console.WriteLine("Receive From Proxy");
        try
        {
            await proxy.ConnectAsync(proxyServerIp, proxyServerPort);

            NetworkStream stream = proxy.GetStream();
            byte[] buffer = new byte[1024];

            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                //if (!string.IsNullOrEmpty(receivedMessage) && bytesRead > 0)
                //{
                    SendToAllClients(receivedMessage);
                //}
            }
        }
        catch (Exception ex)
        {
            //Console.WriteLine("Error in ReceiveMessages! " + ex.Message );
            proxy = new TcpClient();
            _ = Task.Run(ReceiveMessagesFromProxyAsync);
        }
    }

    static void SendToAllClients(string message)
    {
        //Console.WriteLine("Send To All");

        byte[] data = Encoding.UTF8.GetBytes(message);
        foreach (var client in connectedClients)
        {
            try
            {
                NetworkStream stream = client.Value.GetStream();
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error sending to client: " + ex.Message);
                //connectedClients.TryRemove(client.Key, out var notNeeded);
            }
        }
    }

    // Task with CMS communication
    static async Task ReceiveMessagesFromCMSAsync(TcpClient client, int i)
    {
        //Console.WriteLine("Receive From CMS");
        try
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                if(!string.IsNullOrEmpty(receivedMessage) && bytesRead > 0) 
                { 
                    SendMessageToProxyAsync(receivedMessage);
                }
            }
        }
        catch (Exception ex)
        {
            //Console.WriteLine("Removing a channel: " + ex.Message);
            connectedClients.TryRemove(i, out TcpClient? notNeeded);
        }
    }

    static void SendMessageToProxyAsync(string messageToSend)
    {
        //Console.WriteLine("Send To Proxy");
        byte[] data = Encoding.UTF8.GetBytes(messageToSend);

        try
        {
            NetworkStream stream = proxy.GetStream();
            stream.Write(data, 0, data.Length);
        }
        catch (Exception ex)
        {
            //Console.WriteLine("Error sending to proxy: " + ex.Message);
        }
    }
}