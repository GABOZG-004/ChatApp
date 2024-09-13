using System.Collections.Generic;

namespace ChatApp.Models
{
  public class Room
  {
    public string RoomName { get; private set; }
    public List<User> Users { get; private set; }

    public Room(string roomName)
    {
      RoomName = roomName;
      Users = new List<User>();
    }

    public void AddUser(User user)
    {
      if (!Users.Contains(user))
      {
        Users.Add(user);
      }
    }

    public void RemoveUser(User user)
    {
      if (Users.Contains(user))
      {
        Users.Remove(user);
      }
    }

    public bool IsEmpty()
    {
      return Users.Count == 0;
    }
  }
}
