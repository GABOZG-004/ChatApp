using System.Collections.Generic;
using System.Net.Sockets;

namespace ChatApp.Models
{
  public class User
  {
    public string Username { get; private set; }
    public string Status { get; set; }
    public List<Room> ConnectedRooms { get; set; }
    public NetworkStream NetworkStream { get; set; }  // Add this field for communication stream

    public User(string username, NetworkStream stream)
    {
      Username = username;
      Status = "ACTIVE";  // Default status on login
      ConnectedRooms = new List<Room>();
      NetworkStream = stream; // Assign the NetworkStream when the user connects
    }

    public void JoinRoom(Room room)
    {
      if (!ConnectedRooms.Contains(room))
      {
        ConnectedRooms.Add(room);
      }
    }

    public void LeaveRoom(Room room)
    {
      if (ConnectedRooms.Contains(room))
      {
        ConnectedRooms.Remove(room);
      }
    }
  }
}
