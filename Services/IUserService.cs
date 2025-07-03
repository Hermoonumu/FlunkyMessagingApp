using MessagingApp.DTO;
using MessagingApp.Models;

namespace MessagingApp.Services;


public interface IUserService
{
    public Task<User> AddUserAsync(AuthDTO user);
    public Task AuthenticateUserAsync(AuthDTO userCreds);
    public Task<User?> GetUserAsync(AuthDTO userCreds, bool includeMsgs);
}