using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Serveur;
public class ServerChat
{
    private static List<TcpClient> clients = new List<TcpClient>();
    private static object lockObj = new object();

    public static void Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 8001);
        listener.Start();
        Console.WriteLine("Chat server started on 127.0.0.1:8001...");

        // Thread pour que le serveur puisse écrire dans le chat
        Task.Run(() =>
        {
            while (true)
            {
                string message = Console.ReadLine();
                if (!string.IsNullOrEmpty(message))
                {
                    Broadcast($"[Server]: {message}", null);
                }
            }
        });

        // Boucle d’attente des clients
        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            lock (lockObj) clients.Add(client);

            Console.WriteLine("Client connected: " + client.Client.RemoteEndPoint);

            Task.Run(() => HandleClient(client));
        }
    }

    private static void HandleClient(TcpClient client)
    {
        try
        {
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Console.WriteLine(message); // log côté serveur

                    Broadcast(message, client);
                }
            }
        }
        catch
        {
            Console.WriteLine("Client disconnected.");
        }
        finally
        {
            lock (lockObj) clients.Remove(client);
            client.Close();
        }
    }

    private static void Broadcast(string message, TcpClient? sender)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);

        lock (lockObj)
        {
            foreach (var client in clients)
            {
                if (client != sender)
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(data, 0, data.Length);
                    }
                    catch { }
                }
            }
        }
    }
}
