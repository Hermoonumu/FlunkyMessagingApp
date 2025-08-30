using MessagingApp.DTO;
using MessagingApp.Exceptions;
using MessagingApp.Models;
using MessagingApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MessagingApp.Validator;


public class ValidationService(DataContext _db) : IValidationService
{
    public async Task AddUserValidateAsync(AuthDTO authDTO)
    {
        if (authDTO.Username == null
            || authDTO.Password == null
            || String.Equals(authDTO.Password, "")
            || String.Equals(authDTO.Username, "")) throw new InvalidFormException();
        if (authDTO.Username.Count() < 6) throw new UsernameTooShortException();
        if (authDTO.Password.Count() < 8) throw new PasswordTooShortException();
    }

    public async Task<Chat> ValidateChatDoesntExist(object chatIdentifier)
    {
        Chat? chat;
        if (chatIdentifier is int chatId)
        {
            chat = await _db.Chats.FindAsync(chatId);
            if (chat == null)
                throw new ChatDoesntExistException();
        }
        else if (chatIdentifier is string chatName)
        {
            chat = await _db.Chats.FirstOrDefaultAsync(c => c.Name == chatName);
            if (chat == null)
                throw new ChatDoesntExistException();
        }
        else
        {
            throw new ArgumentException("Chat identifier must be an int or string.");
        }
        return chat;
    }

    public async Task<Chat> ValidateChatAlreadyExists(object chatIdentifier)
    {
        Chat? chat;
        if (chatIdentifier is int chatId)
        {
            chat = await _db.Chats.FindAsync(chatId);
            if (chat != null)
                throw new ChatAlreadyExistsException();
        }
        else if (chatIdentifier is string chatName)
        {
            chat = await _db.Chats.FirstOrDefaultAsync(c => c.Name == chatName);
            if (chat != null)
                throw new ChatAlreadyExistsException();
        }
        else
        {
            throw new ArgumentException("Chat identifier must be an int or string.");
        }
        return chat;
    }
}