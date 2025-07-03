namespace MessagingApp.Models;



public class Chat
{
    public long? ID { set; get; }
    public List<User> Members { set; get; }
    public List<Message> Messages { set; get; }
}