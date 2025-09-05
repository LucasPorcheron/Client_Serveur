using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client;

public class ClientChat
{
    public static void Main()
    {
        try
        {
            TcpClient client = new TcpClient("127.0.0.1", 8001);
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string welcome = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            Console.WriteLine(welcome);
            Console.WriteLine("Connecté au chat du serveur. Ecrit ton message et appuyer sur Entrée.");

            Task.Run(() =>
            {
                try
                {
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Console.WriteLine("\n" + message);
                        Console.Write("> ");
                    }
                }
                catch
                {
                    Console.WriteLine("Déconnecté du chat du serveur.");
                }
            });

            // Envoi de messages
            while (true)
            {
                Console.Write("> ");
                string message = Console.ReadLine();

                if (string.IsNullOrEmpty(message)) continue;

                // Conversion en octets avant envoi
                byte[] data = Encoding.ASCII.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erreur: " + ex.Message);
        }
    }
}
