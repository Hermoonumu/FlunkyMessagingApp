namespace MessagingApp.DTO;



public class UserDTO
{
    public long? ID { set; get; } = null;
    public string? Username { set; get; } = null;
    public string? Password { set; get; } = null;
    public MessagingApp.Models.User.RoleENUM? Role { set; get; } = null;


    public List<MessagingApp.Models.Message>? SentMessages
    { set; get; } = null;
    public List<MessagingApp.Models.Message>? ReceivedMessages { set; get; } = null;
}