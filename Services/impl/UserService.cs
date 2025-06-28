using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MessagingApp.DTO;
using MessagingApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;

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
        User potentialUser = _db.Users.FirstOrDefault(search => search.Username == userCreds.Username);
        if (potentialUser == null) return 1;
        if (new PasswordHasher<User>().VerifyHashedPassword(potentialUser, potentialUser.PasswordHash, userCreds.Password)
        == PasswordVerificationResult.Failed) return 2;
        return 0;
    }

    public async Task<User?> GetUserAsync(AuthDTO userCreds)
    {
        return await _db.Users
        .Include(u => u.ReceivedMessages)
        .Include(u => u.SentMessages)
        .FirstOrDefaultAsync(search => search.Username == userCreds.Username);
    }
}