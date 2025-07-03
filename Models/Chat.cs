namespace MessagingApp.Models;



public class Chat
{
    public long? ID { set; get; }
    public string Name { set; get; }
    public List<User> Members { set; get; } = new List<User>();
    public List<Message> Messages { set; get; } = new List<Message>();
    public User Owner { set; get; }
    public long? OwnerID { set; get; }
}