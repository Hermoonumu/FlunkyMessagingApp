using MessagingApp.DTO;
using MessagingApp.Models;

namespace MessagingApp.Mappers;



public class UserMapper
{
    public static User DTOToUser(UserDTO uDTO)
    {
        return new User()
        {
            ID = uDTO.ID==null?null:uDTO.ID,
            Username = uDTO.Username==null?String.Empty:uDTO.Username,
            Role = uDTO.Role==null?User.RoleENUM.USER:(User.RoleENUM)uDTO.Role,
            SentMessages = uDTO.SentMessages==null?new List<Message>():uDTO.SentMessages,
            ReceivedMessages = uDTO.ReceivedMessages==null?new List<Message>():uDTO.ReceivedMessages

        };
    }
}