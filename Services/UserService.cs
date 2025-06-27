using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MessagingApp.DTO;
using MessagingApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MessagingApp.Services;


public class UserService
{
    private readonly DataContext _db;
    public UserService(DataContext db)
    {
        _db = db;
    }


    public async Task<User> AddUser(AuthDTO user)
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
}