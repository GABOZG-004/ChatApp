namespace ChatApp.Models
{
  public class Message
  {
    public string Type { get; set; } = string.Empty;  // Default to empty string
    public string Operation { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string Extra { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public Message() { }
  }
}
