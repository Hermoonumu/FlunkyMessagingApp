using MessagingApp.DTO;
using MessagingApp.Exceptions;
using MessagingApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MessagingApp.Services.Implementation;

public class UserService(DataContext _db, ILogger<UserService> _logger) : IUserService
{
    public async Task<bool> AddUserAsync(AuthDTO user)
    {
        _logger.LogInformation("Creating new user");
        PasswordHasher<User> passwordHasher = new PasswordHasher<User>();
        _logger.LogInformation("Composing new user object");
        User newRegUser = new User()
        {
            Username = user.Username!,
            Role = Models.User.RoleENUM.USER
        };
        _logger.LogInformation("Generating password hash");
        newRegUser.PasswordHash = passwordHasher.HashPassword(newRegUser, user.Password);
        try
        {
            _logger.LogInformation("Saving new user...");
            await _db.Users.AddAsync(newRegUser);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            if (((NpgsqlException)e.InnerException).SqlState == "23505")
            {
                _logger.LogWarning("User with such username already exists");
                return false;
            }
        }
        return true;
    }


    public async Task AuthenticateUserAsync(AuthDTO userCreds)
    {
        _logger.LogInformation("Authenticating user");
        User? potentialUser = await _db.Users.FirstOrDefaultAsync(search => search.Username == userCreds.Username);
        if (potentialUser == null)
        {
            _logger.LogWarning("Couldn't authenticate, no such user");
            throw new UserDoesntExistException();
        }
        if (new PasswordHasher<User>().VerifyHashedPassword(potentialUser, potentialUser.PasswordHash, userCreds.Password)
        == PasswordVerificationResult.Failed) {
            _logger.LogWarning("Couldn't authenticate, passwords don't match");
            throw new PasswordCheckFailedException();
        }
    }

    public async Task<User?> GetUserAsync(AuthDTO userCreds, bool includeMsgs = false)
    {
        _logger.LogInformation($"Obtaining user by credentials {(includeMsgs?"(Messages included)":"(Messages excluded)")}");
        IQueryable<User> query = _db.Users;
        if (includeMsgs) {
            query = query.Include(u => u.ReceivedMessages).ThenInclude(m => m.OriginUser)
            .Include(u => u.SentMessages).ThenInclude(m => m.DestinationUser);
        }
        return await query.FirstOrDefaultAsync(search => search.Username == userCreds.Username);
    }
}