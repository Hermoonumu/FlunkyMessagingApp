using MessagingApp.DTO;
using MessagingApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MessagingApp.Services.Implementation;

public class UserService(DataContext _db, ILoggerService _logSvc) : IUserService
{
    public async Task<User> AddUserAsync(AuthDTO user)
    {
        await _logSvc.LogInfo("Adding new user", "USERSvc");
        await _logSvc.LogInfo("Hashing password", "USERSvc");
        PasswordHasher<User> passwordHasher = new PasswordHasher<User>();
        User newRegUser = new User();
        await _logSvc.LogInfo("Initializing fields", "USERSvc");
        newRegUser.Username = user.Username;
        newRegUser.PasswordHash = passwordHasher.HashPassword(newRegUser, user.Password);
        newRegUser.Role = Models.User.RoleENUM.USER;
        try
        {
            await _logSvc.LogInfo("Attempting to write to database", "USERSvc");
            await _db.Users.AddAsync(newRegUser);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            await _logSvc.LogError("An error occurred, see the next log entry:", "USERSvc");
            if (((NpgsqlException)e.InnerException).SqlState == "23505")
            {
                await _logSvc.LogError("Unique constraint violated, change username", "USERSvc");
                return null;
            }
        }
        await _logSvc.LogInfo("User successfully created, returning new user object to the caller", "USERSvc");
        return newRegUser;
    }


    public async Task<int> AuthenticateUserAsync(AuthDTO userCreds)
    {
        await _logSvc.LogInfo("Trying to authenticate user", "USERSvc");
        await _logSvc.LogError("Attempting to pull user off DB", "USERSvc");
        User? potentialUser = await _db.Users.FirstOrDefaultAsync(search => search.Username == userCreds.Username);
        if (potentialUser == null)
        {
            await _logSvc.LogError("User not found", "USERSvc");
            return 1;
        }
        if (new PasswordHasher<User>().VerifyHashedPassword(potentialUser, potentialUser.PasswordHash, userCreds.Password)
        == PasswordVerificationResult.Failed) {
            await _logSvc.LogError("Password hash check failed, check the password", "USERSvc");
            return 2; }
        await _logSvc.LogInfo("User authenticated, returning success to the caller", "USERSvc");
        return 0;
    }

    public async Task<User?> GetUserAsync(AuthDTO userCreds, bool includeMsgs = false)
    {
        await _logSvc.LogInfo("Attempting to obtain user with credentials", "USERSvc");
        IQueryable<User> query = _db.Users;
        if (includeMsgs) {
            await _logSvc.LogInfo("Including messages to the query", "USERSvc");
            query = query.Include(u => u.ReceivedMessages).ThenInclude(m => m.OriginUser)
            .Include(u => u.SentMessages).ThenInclude(m => m.DestinationUser);
        }
        await _logSvc.LogInfo("Returning results to the caller", "USERSvc");
        return await query.FirstOrDefaultAsync(search => search.Username == userCreds.Username);
    }
}