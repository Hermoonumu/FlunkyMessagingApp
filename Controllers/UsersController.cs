using Microsoft.AspNetCore.Mvc;

namespace MessagingApp.Controllers;



[ApiController]
[Route("/api/user")]
public class UsersController : ControllerBase
{
    private readonly DataContext _db;

    public UsersController(DataContext db)
    {
        _db = db;
    }
    

}