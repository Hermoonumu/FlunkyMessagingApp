using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MessagingApp.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MessagingApp.Services;


public class UserService
{
    private readonly DataContext _db;
    public UserService(DataContext db) {
        _db = db;
    }


    public async Task<int> AddUser(User user)
    {
        try
        {
            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            if (((NpgsqlException)e.InnerException).SqlState == "23505")
            {
                return 1;
            }
        }
        return 0;
    }
}