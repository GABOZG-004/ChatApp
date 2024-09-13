using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;  // For JSON parsing
using ChatApp.Models;

namespace ChatApp.Client
{
  public class ChatClient
  {
    private TcpClient? _client;
    private NetworkStream? _stream;
    private string _username;

    public ChatClient(string username)
    {
      _username = username;
    }

    public void Connect(string serverAddress, int port)
    {
      try
      {
        _client = new TcpClient(serverAddress, port);
        _stream = _client.GetStream();
        Console.WriteLine("Connected to server...");

        // Identify client to server
        Identify();

        // Start a new thread to listen for server messages
        Thread listenThread = new Thread(StartListening);
        listenThread.Start();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Connection failed: {ex.Message}");
      }
    }

    private void Identify()
    {
      // Create the IDENTIFY message
      Message identifyMessage = new Message
      {
        Type = "IDENTIFY",
        Username = _username
      };
      SendMessage(identifyMessage);
    }

    public void ChangeStatus(string status)
    {
      string[] validStatuses = { "ACTIVE", "AWAY", "BUSY" };
      
      if (string.IsNullOrEmpty(status) || Array.IndexOf(validStatuses, status.ToUpper()) == -1){
        Console.WriteLine("Invalid status. Valid options are: ACTIVE, AWAY, BUSY.");
        return;
      }

      Message statusMessage = new Message
      {
        Type = "STATUS",
        Status = status
      };
      SendMessage(statusMessage);
    }

    public void SendPrivateMessage(string recipientUsername, string text)
    {
      Message textMessage = new Message
      {
        Type = "TEXT",
        Username = recipientUsername,
        Text = text
      };
      SendMessage(textMessage);
    }

    public void SendPublicMessage(string text)
    {
      Message publicMessage = new Message
      {
        Type = "PUBLIC_TEXT",
        Username = _username,
        Text = text
      };
      SendMessage(publicMessage);
    }

    public void CreateRoom(string roomName)
    {
      Message newRoomMessage = new Message
      {
        Type = "NEW_ROOM",
        RoomName = roomName
      };
      SendMessage(newRoomMessage);
    }

    public void JoinRoom(string roomName)
    {
      Message joinRoomMessage = new Message
      {
        Type = "JOIN_ROOM",
        RoomName = roomName
      };
      SendMessage(joinRoomMessage);
    }

    public void LeaveRoom(string roomName)
    {
      Message leaveRoomMessage = new Message
      {
        Type = "LEAVE_ROOM",
        RoomName = roomName
      };
      SendMessage(leaveRoomMessage);
    }

    public void Disconnect()
    {
      Message disconnectMessage = new Message { Type = "DISCONNECT" };
      SendMessage(disconnectMessage);
      if(_client != null)
      {
        _client.Close();
      }
      Console.WriteLine("Disconnected from server.");
    }

    private void SendMessage(Message message)
    {
      string messageJson = JsonConvert.SerializeObject(message);
      byte[] data = Encoding.ASCII.GetBytes(messageJson);
      if (_stream != null)
        _stream.Write(data, 0, data.Length);
    }

    private void StartListening()
    {
      byte[] buffer = new byte[1024];
      int bytesRead;
      if (_stream != null)
      {
        while ((bytesRead = _stream.Read(buffer, 0, buffer.Length)) != 0)
        {
          string messageJson = Encoding.ASCII.GetString(buffer, 0, bytesRead);
          Message? message = JsonConvert.DeserializeObject<Message>(messageJson);

          if (message != null)
          {
            HandleServerMessage(message);
          }
        }
      }
    }

    private void HandleServerMessage(Message message)
    {
      switch (message.Type)
      {
        case "NEW_USER":
          Console.WriteLine($"[Server] New user joined: {message.Username}");
          break;
        case "NEW_STATUS":
          Console.WriteLine($"[Server] {message.Username} changed status to: {message.Status}");
          if (message.Username == _username) {
            Console.WriteLine("[Info] Your status has been updated successfully.");
          }
          break;
        case "TEXT_FROM":
          Console.WriteLine($"[Private] {message.Username}: {message.Text}");
          Console.WriteLine($"[Info] Private message from {message.Username} received successfully.");
          break;
        case "PUBLIC_TEXT_FROM":
          Console.WriteLine($"[Public] {message.Username}: {message.Text}");
          Console.WriteLine($"[Info] Public message from {message.Username} received successfully.");
          break;
        case "ROOM_TEXT_FROM":
          Console.WriteLine($"[Room] {message.RoomName} - {message.Username}: {message.Text}");
          break;
        case "INVITATION":
          Console.WriteLine($"[Server] {message.Username} invited you to room: {message.RoomName}");
          break;
        case "LEFT_ROOM":
          Console.WriteLine($"[Server] {message.Username} left room: {message.RoomName}");
          break;
        case "DISCONNECTED":
          Console.WriteLine($"[Server] {message.Username} disconnected.");
          break;
        case "RESPONSE":
          HandleResponseMessage(message);
          break;
        default:
          Console.WriteLine($"[Error] Unrecognized message type: {message.Type}");
          break;
      }
    }

    private void HandleResponseMessage(Message message)
    {
      switch (message.Operation)
      {
        case "IDENTIFY":
          if (message.Result == "SUCCESS")
          {
            Console.WriteLine($"[Server] You have successfully identified as {message.Extra}.");
          }
          else if (message.Result == "USER_ALREADY_EXISTS")
          {
            Console.WriteLine($"[Server] The username '{message.Extra}' is already in use.");
          }
          break;
        case "NEW_ROOM":
          if (message.Result == "SUCCESS")
          {
            Console.WriteLine($"[Server] Room '{message.Extra}' created successfully.");
          }
          else if (message.Result == "ROOM_ALREADY_EXISTS")
          {
            Console.WriteLine($"[Server] The room '{message.Extra}' already exists.");
          }
          break;
        case "TEXT":
          if (message.Result == "NO_SUCH_USER")
          {
            Console.WriteLine($"[Server] User '{message.Extra}' does not exist.");
          }
          break;
        case "ROOM_TEXT":
          if (message.Result == "NO_SUCH_ROOM")
          {
            Console.WriteLine($"[Server] Room '{message.Extra}' does not exist.");
          }
          else if (message.Result == "NOT_JOINED")
          {
            Console.WriteLine($"[Server] You have not joined room '{message.Extra}'.");
          }
          break;
        case "LEAVE_ROOM":
          if (message.Result == "NO_SUCH_ROOM")
          {
            Console.WriteLine($"[Server] Room '{message.Extra}' does not exist.");
          }
          else if (message.Result == "NOT_JOINED")
          {
            Console.WriteLine($"[Server] You have not joined room '{message.Extra}'.");
          }
          break;
        default:
          Console.WriteLine($"[Server] Unhandled RESPONSE for operation: {message.Operation}");
          break;
      }
    }

    private void HandleUserInput()
    {
      while (true)
      {
        Console.WriteLine("Enter a command: (status, private, public, create_room, join_room, leave_room, disconnect)");
        string? command = Console.ReadLine();
        if(command != null)
        {
          if (command.StartsWith("status"))
          {
            Console.WriteLine("Enter your new status (ACTIVE, AWAY, BUSY):");
            string? status = Console.ReadLine();
            if(status != null)
              ChangeStatus(status);
          }
          else if (command.StartsWith("private"))
          {
            Console.WriteLine("Enter recipient username:");
            string? recipient = Console.ReadLine();
            Console.WriteLine("Enter your message:");
            string? text = Console.ReadLine();
            if(text != null && recipient != null)
              SendPrivateMessage(recipient, text);
          }
          else if (command.StartsWith("public"))
          {
            Console.WriteLine("Enter your message:");
            string? text = Console.ReadLine();
            if(text != null)
              SendPublicMessage(text);
          }
          else if (command.StartsWith("create_room"))
          {
            Console.WriteLine("Enter room name:");
            string? roomName = Console.ReadLine();
            if (roomName != null)
              CreateRoom(roomName);
          }
          else if (command.StartsWith("join_room"))
          {
            Console.WriteLine("Enter room name:");
            string? roomName = Console.ReadLine();
            if (roomName != null)
              JoinRoom(roomName);
          }
          else if (command.StartsWith("leave_room"))
          {
            Console.WriteLine("Enter room name:");
            string? roomName = Console.ReadLine();
            if (roomName != null)
              LeaveRoom(roomName);
          }
          else if (command.StartsWith("disconnect"))
          {
            Disconnect();
            break;
          }
        }
      }
    }
  }
}
