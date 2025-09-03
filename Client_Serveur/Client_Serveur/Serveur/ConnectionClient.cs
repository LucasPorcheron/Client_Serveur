using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Serveur;

public class ServeurChat
{
    // Liste qui contient tous les clients connectés au serveur
    private static List<TcpClient> clients = new List<TcpClient>();
    
    // Objet de verrouillage pour protéger l'accès concurrent à "clients"
    private static object lockObj = new object();

    public static void Main()
    {
        // Création du serveur TCP qui écoute sur l'adresse locale 127.0.0.1 et le port 8001
        TcpListener listener = new TcpListener(IPAddress.Parse("192.0.0.3"), 8001);
        listener.Start();
        Console.WriteLine("Chat server started on 192.0.0.3:8001...");

        // Lancement d’un thread parallèle pour permettre au serveur d’envoyer lui-même des messages
        Task.Run(() =>
        {
            while (true)
            {
                Console.Write("> ");
                string message = Console.ReadLine();
                if (!string.IsNullOrEmpty(message))
                {
                    // Diffuse le message du serveur à tous les clients connectés
                    Broadcast($"[Server]: {message}", null);
                }
            }
        });

        // Boucle principale du serveur : attendre et accepter de nouveaux clients
        while (true)
        {
            // Bloque jusqu'à ce qu’un client se connecte
            TcpClient client = listener.AcceptTcpClient();

            // Ajoute le client à la liste protégée par un lock
            lock (lockObj) clients.Add(client);

            Console.WriteLine("Client connected: " + client.Client.RemoteEndPoint);

            // Lance un nouveau thread (Task) pour gérer la communication avec ce client
            Task.Run(() => HandleClient(client));
        }
    }

    // Gère la communication avec un client donné
    private static void HandleClient(TcpClient client)
    {
        try
        {
            // "using" garantit que le flux réseau sera correctement libéré
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                // Boucle de lecture des messages envoyés par le client
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Convertit les octets reçus en string
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    // Affiche le message côté serveur (log)
                    Console.WriteLine(message);

                    // Diffuse le message à tous les autres clients connectés
                    Broadcast(message, client);
                }
            }
        }
        catch
        {
            // Si une erreur survient (ex. client ferme sa connexion), on log la déconnexion
            Console.WriteLine("Client disconnected.");
        }
        finally
        {
            // Retire le client de la liste et ferme la connexion proprement
            lock (lockObj) clients.Remove(client);
            client.Close();
        }
    }

    // Diffuse un message à tous les clients connectés sauf à l’expéditeur (si défini)
    private static void Broadcast(string message, TcpClient? sender)
    {
        // Convertit le message en tableau d’octets
        byte[] data = Encoding.ASCII.GetBytes(message);

        lock (lockObj)
        {
            foreach (var client in clients)
            {
                // Ne renvoie pas le message à l’expéditeur
                if (client != sender)
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(data, 0, data.Length); // envoi du message
                    }
                    catch
                    {
                        // On ignore les erreurs si un client est déconnecté
                    }
                }
            }
        }
    }
}
