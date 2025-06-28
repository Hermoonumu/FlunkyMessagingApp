using MessagingApp.DTO;
using MessagingApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MessagingApp.Services.Implementation;

public class UserService(DataContext _db) : IUserService
{
    public async Task<User> AddUserAsync(AuthDTO user)
    {
        PasswordHasher<User> passwordHasher = new PasswordHasher<User>();
        User newRegUser = new User();
        newRegUser.Username = user.Username;
        newRegUser.PasswordHash = passwordHasher.HashPassword(newRegUser, user.Password);
        newRegUser.Role = Models.User.RoleENUM.USER;
        try
        {
            await _db.Users.AddAsync(newRegUser);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            if (((NpgsqlException)e.InnerException).SqlState == "23505")
            {
                return null;
            }
        }
        return newRegUser;
    }


    public async Task<int> AuthenticateUserAsync(AuthDTO userCreds)
    {
        User? potentialUser = await _db.Users.FirstOrDefaultAsync(search => search.Username == userCreds.Username);
        if (potentialUser == null) return 1;
        if (new PasswordHasher<User>().VerifyHashedPassword(potentialUser, potentialUser.PasswordHash, userCreds.Password)
        == PasswordVerificationResult.Failed) return 2;
        return 0;
    }

    public async Task<User?> GetUserAsync(AuthDTO userCreds, bool includeMsgs = false)
    {
        IQueryable<User> query = _db.Users;
        if (includeMsgs) {
            query = query.Include(u => u.ReceivedMessages).ThenInclude(m => m.OriginUser)
            .Include(u => u.SentMessages).ThenInclude(m => m.DestinationUser);
        }
        return await query.FirstOrDefaultAsync(search => search.Username == userCreds.Username);
    }
}