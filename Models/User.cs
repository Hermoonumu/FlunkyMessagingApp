namespace MessagingApp.Models;

public class User
{
    public long ID { set; get; }
    public string Username { set; get; }
    public string PasswordHash { set; get; }
    public RoleENUM Role { set; get; }


    public List<Message> SentMessages
    { set; get; }
    public List<Message> ReceivedMessages { set; get; }

    public enum RoleENUM
    {
        USER = 0,
        ADMIN = 1
    }
}