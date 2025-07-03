namespace MessagingApp.Models;

public class User
{
    public long? ID { set; get; }
    public string Username { set; get; }
    public string PasswordHash { set; get; }
    public RoleENUM Role { set; get; }
    public List<Chat> enrolledChats{ set; get; }


    public List<Message> SentMessages
    { set; get; } = new List<Message>();
    public List<Message> ReceivedMessages { set; get; } = new List<Message>();

    public enum RoleENUM
    {
        USER = 0,
        ADMIN = 1
    }
}