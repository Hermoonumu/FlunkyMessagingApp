namespace MessagingApp.DTO;



public class MessageReceivedDTO
{
    public string SenderUsername { set; get; }
    public DateTime Timestamp { set; get; }
    public string MessageText { set; get; }
    public bool isRead{ set; get; }
}