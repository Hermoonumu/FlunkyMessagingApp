using MessagingApp.DTO;
using MessagingApp.Exceptions;
using MessagingApp.Models;
using MessagingApp.Services;

namespace MessagingApp.Validator;


public class ValidationService : IValidationService
{
    public async Task AddUserValidateAsync(AuthDTO authDTO)
    {
        if (authDTO.Username == null
            || authDTO.Password == null
            || String.Equals(authDTO.Password, "")
            || String.Equals(authDTO.Username, "")) throw new InvalidFormException();
        if (authDTO.Username.Count() <= 6) throw new UsernameTooShortException();
        if (authDTO.Password.Count() <= 8) throw new PasswordTooShortException();
    }
}