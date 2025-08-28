namespace MessagingApp.DTO;

public class NewMemberChatDTO
{
    public long ChatID { set; get; }
    public List<string> Members { set; get; } = new List<string>();
}