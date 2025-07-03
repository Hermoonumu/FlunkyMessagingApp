namespace MessagingApp.Models;


public class RevokedJWTs
{
    public long ID { set; get; }
    public string Token { set; get; }
    public string JTI { set; get; }
}