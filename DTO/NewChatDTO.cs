namespace MessagingApp.DTO;

public class NewChatDTO
{
    public string Name { set; get; }
    public List<string> Members { set; get; } = new List<string>();
}