namespace MessagingApp.Models;

public class User
{
    public long ID { set; get; }
    public string Username { set; get; }
    public string PasswordHash { set; get; }


    public List<Message> SentMessages { set; get; }
    public List<Message> ReceivedMessages { set; get; }
}