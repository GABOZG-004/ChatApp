using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;  // For JSON parsing
using ChatApp.Models;

namespace ChatApp.Server
{
  public class ChatServer
  {
    private TcpListener _server;
    private Dictionary<string, User> _connectedUsers;
    private Dictionary<string, Room> _activeRooms;

    public ChatServer()
    {
      _server = new TcpListener(IPAddress.Any, 8000);  // Listening on port 8000
      _connectedUsers = new Dictionary<string, User>();
      _activeRooms = new Dictionary<string, Room>();
    }

    public void Start()
    {
      _server.Start();
      Console.WriteLine("Chat Server started, , waiting for connections on port 8000...");
      while (true)
      {
        TcpClient client = _server.AcceptTcpClient();
        Console.WriteLine("New client connected...");
        Thread clientThread = new Thread(() => HandleClient(client));
        clientThread.Start();
      }
    }

    private void HandleClient(TcpClient client)
    {
      NetworkStream stream = client.GetStream();
      byte[] buffer = new byte[1024];
      int bytesRead;

      while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
      {
        string messageJson = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        Message? message = JsonConvert.DeserializeObject<Message>(messageJson);

        if (message != null)
        {
          ProcessMessage(message, stream);
        }
        else
        {
          DisconnectClient(client, stream);
          break;
        }
      }
    }

    private void ProcessMessage(Message message, NetworkStream stream)
    {
      switch (message.Type)
      {
        case "IDENTIFY":
          HandleIdentify(message, stream);
          break;
        case "STATUS":
          HandleStatus(message, stream);
          break;
        case "TEXT":
          HandleText(message, stream);
          break;
        case "PUBLIC_TEXT":
          HandlePublicText(message, stream);
          break;
        case "NEW_ROOM":
          HandleNewRoom(message, stream);
          break;
        case "INVITE":
          HandleInvite(message, stream);
          break;
        case "ROOM_TEXT":
          HandleRoomText(message, stream);
          break;
        case "LEAVE_ROOM":
          HandleLeaveRoom(message, stream);
          break;
        case "DISCONNECT":
          DisconnectClient(null, stream);
          break;
        default:
          SendError(stream, "INVALID", "NOT_IDENTIFIED");
          break;
      }
    }

    private void HandleIdentify(Message message, NetworkStream stream)
    {
      string username = message.Username;
      if (_connectedUsers.ContainsKey(username))
      {
        SendResponse(stream, "IDENTIFY", "USER_ALREADY_EXISTS", username);
      }
      else
      {
        User newUser = new User(username, stream);
        _connectedUsers.Add(username, newUser);
        SendResponse(stream, "IDENTIFY", "SUCCESS", username);
        BroadcastNewUser(username);
      }
    }

    private void BroadcastNewUser(string username)
    {
      Message newUserMessage = new Message { Type = "NEW_USER", Username = username };
      BroadcastMessage(newUserMessage);
    }

    private void HandleStatus(Message message, NetworkStream stream)
    {
      if (_connectedUsers.ContainsKey(message.Username))
      {
        User user = _connectedUsers[message.Username];
        user.Status = message.Status;
        BroadcastStatus(user.Username, user.Status);
      }
    }

    private void BroadcastStatus(string username, string status)
    {
      Message statusMessage = new Message { Type = "NEW_STATUS", Username = username, Extra = status };
      BroadcastMessage(statusMessage);
    }

    private void HandleText(Message message, NetworkStream stream)
    {
      if (_connectedUsers.ContainsKey(message.Username))
      {
        User recipient = _connectedUsers[message.Username];
        Message textMessage = new Message
        {
          Type = "TEXT_FROM",
          Username = message.Username,
          Text = message.Text
        };
        SendMessageToUser(recipient, textMessage);
      }
      else
      {
        SendError(stream, "TEXT", "NO_SUCH_USER", message.Username);
      }
    }

    private void HandlePublicText(Message message, NetworkStream stream)
    {
      Message publicTextMessage = new Message
      {
        Type = "PUBLIC_TEXT_FROM",
        Username = message.Username,
        Text = message.Text
      };
      BroadcastMessage(publicTextMessage);
    }

    private void HandleNewRoom(Message message, NetworkStream stream)
    {
      string roomName = message.RoomName;
      if (!_activeRooms.ContainsKey(roomName))
      {
        Room newRoom = new Room(roomName);
        _activeRooms.Add(roomName, newRoom);
        SendResponse(stream, "NEW_ROOM", "SUCCESS", roomName);
      }
      else
      {
        SendError(stream, "NEW_ROOM", "ROOM_ALREADY_EXISTS", roomName);
      }
    }

    private void HandleInvite(Message message, NetworkStream stream)
    {
      string roomName = message.RoomName;
      List<string> usernamesToInvite = message.Extra.Split(',').ToList();  // Expecting usernames as comma-separated

      if (_activeRooms.ContainsKey(roomName))
      {
        Room room = _activeRooms[roomName];
        foreach (string username in usernamesToInvite)
        {
          if (_connectedUsers.ContainsKey(username))
          {
            User invitedUser = _connectedUsers[username];
            room.AddUser(invitedUser);
            invitedUser.JoinRoom(room);

            // Notify the invited user
            Message invitationMessage = new Message
            {
              Type = "INVITATION",
              Username = username,
              RoomName = roomName
            };
            SendMessageToUser(invitedUser, invitationMessage);
          }
          else
          {
            // If the user does not exist, return an error
            SendError(stream, "INVITE", "NO_SUCH_USER", username);
            return;
          }
        }
      }
      else
      {
        // If the room does not exist, return an error
        SendError(stream, "INVITE", "NO_SUCH_ROOM", roomName);
      }
    }

    private void HandleRoomText(Message message, NetworkStream stream)
    {
      string roomName = message.RoomName;
      string sender = message.Username;

      if (_activeRooms.ContainsKey(roomName))
      {
        Room room = _activeRooms[roomName];
        User senderUser = _connectedUsers[sender];

        if (room.Users.Contains(senderUser))
        {
          // Broadcast message to all users in the room
          Message roomTextMessage = new Message
          {
            Type = "ROOM_TEXT_FROM",
            RoomName = roomName,
            Username = sender,
            Text = message.Text
          };

          foreach (User user in room.Users)
          {
            if (user.Username != sender)  // Don't send the message to the sender
            {
              SendMessageToUser(user, roomTextMessage);
            }
          }
        }
        else
        {
          // The user is not in the room
          SendError(stream, "ROOM_TEXT", "NOT_JOINED", roomName);
        }
      }
      else
      {
        // The room does not exist
        SendError(stream, "ROOM_TEXT", "NO_SUCH_ROOM", roomName);
      }
    }

    private void HandleLeaveRoom(Message message, NetworkStream stream)
    {
      string roomName = message.RoomName;
      string username = message.Username;

      if (_activeRooms.ContainsKey(roomName))
      {
        Room room = _activeRooms[roomName];
        User user = _connectedUsers[username];

        if (room.Users.Contains(user))
        {
          room.RemoveUser(user);
          user.LeaveRoom(room);

          // Notify other users in the room
          Message leftRoomMessage = new Message
          {
            Type = "LEFT_ROOM",
            RoomName = roomName,
            Username = username
          };

          foreach (User remainingUser in room.Users)
          {
            SendMessageToUser(remainingUser, leftRoomMessage);
          }

          // If the room is empty, delete it
          if (room.IsEmpty())
          {
            _activeRooms.Remove(roomName);
          }
        }
        else
        {
          // The user was not in the room
          SendError(stream, "LEAVE_ROOM", "NOT_JOINED", roomName);
        }
      }
      else
      {
        // The room does not exist
        SendError(stream, "LEAVE_ROOM", "NO_SUCH_ROOM", roomName);
      }
    }

    private void DisconnectClient(TcpClient? client, NetworkStream stream)
    {
      // Logic for disconnecting a client
      stream.Close();
      if (client != null)
        client.Close();
    }

    private void SendError(NetworkStream stream, string operation, string result, string? extra = null)
    {
            Message errorMessage = new Message { Type = "RESPONSE", Operation = operation, Result = result, Extra = extra ?? "nothing" };
            SendMessage(stream, errorMessage);
    }

    private void SendResponse(NetworkStream stream, string operation, string result, string? extra = null)
    {
      Message responseMessage = new Message { Type = "RESPONSE", Operation = operation, Result = result, Extra = extra ?? "nothing" };
      SendMessage(stream, responseMessage);
    }

    private void BroadcastMessage(Message message)
    {
      foreach (var user in _connectedUsers.Values)  // Iterate over all connected users
      {
        SendMessageToUser(user, message);  // Send the message to each user
      }
    }


    private void SendMessageToUser(User user, Message message)
    {
      // Check if the user has a valid network stream (connected)
      if (user.ConnectedRooms.Count > 0)
      {
        try
        {
          // Convert the message object to JSON
          string messageJson = JsonConvert.SerializeObject(message);

          // Convert the JSON string to bytes
          byte[] data = Encoding.ASCII.GetBytes(messageJson);

          // Send the message through the user's network stream
          user.NetworkStream.Write(data, 0, data.Length);
          user.NetworkStream.Flush();  // Make sure all data is sent
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Error sending message to {user.Username}: {ex.Message}");
        }
      }
    }

    private void SendMessage(NetworkStream stream, Message message)
    {
      string jsonMessage = JsonConvert.SerializeObject(message);
      byte[] data = Encoding.ASCII.GetBytes(jsonMessage);
      stream.Write(data, 0, data.Length);
    }
  }
}
