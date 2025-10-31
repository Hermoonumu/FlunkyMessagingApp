using MessagingApp.DTO;
using MessagingApp.Models;

namespace MessagingApp.Services;


public interface IValidationService
{
    public Task AddUserValidateAsync(AuthDTO authDTO);
    public Task<Chat> VerifyChatAlreadyExists(object chatIdentifier);
    public Task<Chat> VerifyChatDoesntExist(object chatIdentifier);
    public Task<User> VerifyUserAlreadyExists(string username);
}