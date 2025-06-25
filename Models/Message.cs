namespace MessagingApp.Models;


public class Message
{
    public long ID { set; get; }

    public long OriginID { set; get; }
    public long? DestinationID { set; get; }
    public User OriginUser { set; get; }
    public User DestinationUser { set; get; }


    public string Text { set; get; }
    public DateTime Timestamp { set; get; }
}