namespace MessagingApp.DTO;


public class AuthDTO
{
    public string? Username { set; get; }
    public string? Password { set; get; }

        public async Task<int> AddUserValidate()
    {
        if (Username == null
            || Password == null
            || String.Equals(Password, "")
            || String.Equals(Username, "")) return 1;
        if (Username.Count() <= 6) return 2;
        if (Password.Count() <= 8) return 3;
        return 0;
    }
}