using System;
using ChatApp.Client;

namespace ChatApp
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Enter your username:");
      string? username = Console.ReadLine();

      if (string.IsNullOrEmpty(username))
      {
        Console.WriteLine("Invalid username. Exiting...");
        return;  // Exit early if no username is provided
      }

      ChatClient client = new ChatClient(username);

      client.Connect("127.0.0.1", 8000);  // Connect to server on localhost and port 8000

      while (true)
      {
        Console.WriteLine("Enter a command: (status, private, public, create_room, join_room, leave_room, disconnect)");
        string? command = Console.ReadLine();

        if (string.IsNullOrEmpty(command)) continue;  // Skip if no command is entered

        if (command.StartsWith("status"))
        {
          Console.WriteLine("Enter your new status (ACTIVE, AWAY, BUSY):");
          string? status = Console.ReadLine();

          if (!string.IsNullOrEmpty(status))
            client.ChangeStatus(status);
        }
        else if (command.StartsWith("private"))
        {
          Console.WriteLine("Enter recipient username:");
          string? recipient = Console.ReadLine();

          Console.WriteLine("Enter your message:");
          string? text = Console.ReadLine();

          if (!string.IsNullOrEmpty(recipient) && !string.IsNullOrEmpty(text))
            client.SendPrivateMessage(recipient, text);
        }
        else if (command.StartsWith("public"))
        {
          Console.WriteLine("Enter your message:");
          string? text = Console.ReadLine();

          if (!string.IsNullOrEmpty(text))
            client.SendPublicMessage(text);
        }
        else if (command.StartsWith("create_room"))
        {
          Console.WriteLine("Enter room name:");
          string? roomName = Console.ReadLine();

          if (!string.IsNullOrEmpty(roomName))
            client.CreateRoom(roomName);
        }
        else if (command.StartsWith("join_room"))
        {
          Console.WriteLine("Enter room name:");
          string? roomName = Console.ReadLine();

          if (!string.IsNullOrEmpty(roomName))
            client.JoinRoom(roomName);
        }
        else if (command.StartsWith("leave_room"))
        {
          Console.WriteLine("Enter room name:");
          string? roomName = Console.ReadLine();

          if (!string.IsNullOrEmpty(roomName))
            client.LeaveRoom(roomName);
        }
        else if (command.StartsWith("disconnect"))
        {
          client.Disconnect();
          break;
        }
      }
    }
  }
}
