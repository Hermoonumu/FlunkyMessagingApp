using MessagingApp.DTO;

namespace MessagingApp.Services;


public interface IValidationService
{
    public Task AddUserValidateAsync(AuthDTO authDTO);
}