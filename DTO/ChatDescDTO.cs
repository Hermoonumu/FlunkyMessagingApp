namespace MessagingApp.DTO;

public class ChatDescDTO
{
    public string Name { set; get; }
    public List<string> Members { set; get; } = new List<string>();
}