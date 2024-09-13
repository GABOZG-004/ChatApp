using ChatApp.Server;

namespace ChatApp
{
  class Program
  {
    static void Main(string[] args)
    {
      // Create an instance of ChatServer
      ChatServer server = new ChatServer();

      // Start the server
      Console.WriteLine("Starting chat server...");
      server.Start();  // This will start the server and wait for client connections
    }
  }
}
