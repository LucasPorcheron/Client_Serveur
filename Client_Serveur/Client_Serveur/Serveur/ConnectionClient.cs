using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Driver;   
using MongoDB.Bson;

namespace Serveur;

public class ServeurChat
{
        private static List<TcpClient> clients = new List<TcpClient>();
    private static object lockObj = new object();
    private static int clientCounter = 0;

    // Connexion à MongoDB Atlas
    private static MongoClient mongoClient = new MongoClient("mongodb+srv://EstiamUser:EstiamUserPassword@cluster0.q1fcc2e.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0");
    private static IMongoDatabase database = mongoClient.GetDatabase("Client_Serveur");
    private static IMongoCollection<BsonDocument> messagesCollection = database.GetCollection<BsonDocument>("Message");

    public static void Main()
    {
        // Lancemennt du Serveur
        TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8001);
        listener.Start();
        Console.WriteLine("Serveur démarré en 127.0.0.1:8001...");

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

        // Boucle infinie pour accepter les connexions des clients
        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();

            lock (lockObj) clients.Add(client);

            // Génération d’un identifiant unique pour chaque client
            clientCounter++;
            string clientName = $"Client{clientCounter}";

            Console.WriteLine($"{clientName} connecté : {client.Client.RemoteEndPoint}");

            NetworkStream stream = client.GetStream();
            byte[] welcomeMsg = Encoding.ASCII.GetBytes($"Tu es le {clientName}\n");
            stream.Write(welcomeMsg, 0, welcomeMsg.Length);

            // Lancement d’un thread pour gérer ce client
            Task.Run(() => HandleClient(client, clientName));
        }
    }

    // Gère la communication avec un client donné
    private static void HandleClient(TcpClient client, string clientName)
    {
        try
        {
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                // Boucle de réception des messages du client
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();

                    // Vérifie si le client a demandé l’historique
                    if (message == "/historique")
                    {
                        SendHistory(stream); 
                        continue;
                    }

                    // Affiche côté serveur
                    Console.WriteLine($"[{clientName}]: {message}");

                    // Sauvegarde du message dans MongoDB
                    var doc = new BsonDocument
                    {
                        { "user", clientName },
                        { "content", message },
                        { "sent_at", DateTime.UtcNow }
                    };
                    messagesCollection.InsertOne(doc);

                    // Diffusion du message à tous les autres clients
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
            // Retire le client de la liste quand il se déconnecte
            lock (lockObj) clients.Remove(client);
            client.Close();
        }
    }

    // Diffuse un message à tous les clients sauf l’expéditeur
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
                    catch
                    {
                    }
                }
            }
        }
    }

    // Envoie uniquement les 20 derniers messages 
    private static void SendHistory(NetworkStream stream)
    {
        // Récupère les 20 derniers messages, triés par date 
        var history = messagesCollection.Find(new BsonDocument())
                                        .Sort("{sent_at: -1}") 
                                        .Limit(20)
                                        .Sort("{sent_at: 1}") 
                                        .ToList();

        foreach (var msg in history)
        {
            string line = $"[{msg["sent_at"]}] {msg["user"]}: {msg["content"]}\n";
            byte[] data = Encoding.ASCII.GetBytes(line);
            stream.Write(data, 0, data.Length);
        }
    }

}
