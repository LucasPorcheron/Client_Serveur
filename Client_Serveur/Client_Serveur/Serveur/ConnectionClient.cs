using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Serveur;

public class ServeurChat
{
    // Liste qui contient tous les clients connectés
    private static List<TcpClient> clients = new List<TcpClient>();

    // Objet pour gérer la synchronisation quand plusieurs threads accèdent à "clients"
    private static object lockObj = new object();

    // Compteur pour attribuer des identifiants uniques (Client1, Client2, …)
    private static int clientCounter = 0;

    public static void Main()
    {
        // Création d’un serveur TCP qui écoute sur l’IP locale (à adapter selon ta machine)
        TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8001);
        listener.Start();
        Console.WriteLine("Serveur démarré en 127.0.0.1:8001...");

        // Thread séparé pour que le serveur puisse écrire dans le chat
        Task.Run(() =>
        {
            while (true)
            {
                Console.Write("> ");
                string message = Console.ReadLine();
                if (!string.IsNullOrEmpty(message))
                {
                    Broadcast($"[Serveur]: {message}", null);
                }
            }
        });

        // Boucle principale du serveur → attend des connexions de clients
        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();

            // Ajout du client à la liste partagée (protégée par un lock)
            lock (lockObj) clients.Add(client);

            // Génération d’un identifiant unique pour ce client
            clientCounter++;
            string clientName = $"Client{clientCounter}";

            Console.WriteLine($"{clientName} connecté : {client.Client.RemoteEndPoint}");

            // Envoi d’un message de bienvenue au client avec son identifiant
            NetworkStream stream = client.GetStream();
            byte[] welcomeMsg = Encoding.ASCII.GetBytes($"Tu es le {clientName}");
            stream.Write(welcomeMsg, 0, welcomeMsg.Length);

            // Lancement d’un thread pour gérer ce client en particulier
            Task.Run(() => HandleClient(client, clientName));
        }
    }

    // Gère la communication avec un client spécifique
    private static void HandleClient(TcpClient client, string clientName)
    {
        try
        {
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                // Lecture en boucle des messages envoyés par le client
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    // Log côté serveur
                    Console.WriteLine($"[{clientName}]: {message}");

                    // Envoi du message à tous les autres clients
                    Broadcast($"[{clientName}]: {message}", client);
                }
            }
        }
        catch
        {
            Console.WriteLine($"{clientName} déconnecté.");
        }
        finally
        {
            // Nettoyage : retrait du client de la liste
            lock (lockObj) clients.Remove(client);
            client.Close();
        }
    }

    // Diffuse un message à tous les clients connectés sauf à l’expéditeur
    private static void Broadcast(string message, TcpClient? sender)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);

        lock (lockObj)
        {
            foreach (var client in clients)
            {
                // évite de renvoyer au client qui a envoyé
                if (client != sender)
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(data, 0, data.Length);
                    }
                    catch
                    {
                        // Si un client est déconnecté, on ignore l’erreur
                    }
                }
            }
        }
    }
}
