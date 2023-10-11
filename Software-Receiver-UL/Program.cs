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

    private static bool isProxyConnected = false;

    // proxy
    private static readonly string proxyServerIp = "212.116.138.116";
    private static readonly int proxyServerPort = 4545;

    // cms
    // private static readonly string cmsServerIp = "127.0.0.1";
    private static readonly int cmsServerPort = 4545;



    static void Main()
    {
        _ = Task.Run(OpenConnections);
        while (true) { }
    }

    // Start the client to the proxy and the server for all the CMS-s
    static async Task OpenConnections()
    {
        try
        {
            // Start client
            _ = Task.Run(ConnectToProxyAsync);

            // Start server
            TcpListener listener = new TcpListener(IPAddress.Any, cmsServerPort);
            listener.Start();

            int i = 0;

            while (true)
            {
                // Accept incoming client connections
                TcpClient client = await listener.AcceptTcpClientAsync();
                connectedClients.TryAdd(i, client);

                // Start a new task to handle the client connection
                _ = Task.Run(() => ReceiveMessagesFromCMSAsync(client, i++));
            }

        }
        catch (Exception ex)
        {
            _ = Task.Run(OpenConnections);
        }

    }

    // Tasked with Proxy side communication
    static async Task ConnectToProxyAsync()
    {
        // Have the duty to manage the conenction to the proxy
        while (true)
        {
            if (!isProxyConnected)
            {
                proxy = new TcpClient();
                await proxy.ConnectAsync(proxyServerIp, proxyServerPort);
                isProxyConnected = true;
                _ = Task.Run(ReceiveMessagesFromProxyAsync);
            }
        }
    }

    static async Task ReceiveMessagesFromProxyAsync()
    {
        try
        {
            NetworkStream stream = proxy.GetStream();
            byte[] buffer = new byte[1024];

            while (isProxyConnected && proxy.Connected && stream.CanRead)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                if (!string.IsNullOrEmpty(receivedMessage) && bytesRead > 0)
                {
                    SendToAllClients(receivedMessage);
                }
            }
        }
        catch (Exception ex)
        {
            isProxyConnected = false;
        }
    }

    static void SendToAllClients(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        
        List<int> keys = new List<int>();  
        foreach (var client in connectedClients)
        {
            try
            {
                NetworkStream stream = client.Value.GetStream();
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                // if the .Write() throws an error the client should be remove from connectedClients
                keys.Add(client.Key);          
            }
        }

        foreach(int key in keys) 
        {
            connectedClients.TryRemove(key, out var notNeeded);
        }
    }

    // Task with CMS communication
    static async Task ReceiveMessagesFromCMSAsync(TcpClient client, int i)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            while (true)
            {
                // if the socket closes this ReadAsync will throw an error
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
            // when the read throws an error the client should be removed from the concurentDictionary
            connectedClients.TryRemove(i, out TcpClient? notNeeded);
        }
    }

    static void SendMessageToProxyAsync(string messageToSend)
    {
        byte[] data = Encoding.UTF8.GetBytes(messageToSend);

        try
        {
            NetworkStream stream = proxy.GetStream();
            stream.Write(data, 0, data.Length);
        }
        catch (Exception ex)
        {
            isProxyConnected = false;
        }
    }
}