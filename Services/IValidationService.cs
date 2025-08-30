using MessagingApp.DTO;
using MessagingApp.Models;

namespace MessagingApp.Services;


public interface IValidationService
{
    public Task AddUserValidateAsync(AuthDTO authDTO);
    public Task<Chat> ValidateChatAlreadyExists(object chatIdentifier);
    public Task<Chat> ValidateChatDoesntExist(object chatIdentifier);
}