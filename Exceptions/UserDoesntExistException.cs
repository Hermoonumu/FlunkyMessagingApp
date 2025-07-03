namespace MessagingApp.Exceptions;

[Serializable]
public class UserDoesntExistException : Exception
{
    public UserDoesntExistException() : base() { }
    public UserDoesntExistException(string msg) : base(msg) { }
    public UserDoesntExistException(string msg, Exception innerException) : base(msg, innerException){}
}