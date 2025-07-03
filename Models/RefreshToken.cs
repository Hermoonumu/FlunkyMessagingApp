namespace MessagingApp.Models;


public class RefreshToken
{
    public long ID { set; get; }
    public long UserID { set; get; }
    public User user{ set; get; }
    public string Token { set; get; }
    public DateTime IssuedAt { set; get; }
    public DateTime ExpiresAt { set; get; }
}