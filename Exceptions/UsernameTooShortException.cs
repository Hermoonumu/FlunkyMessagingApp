namespace MessagingApp.Exceptions;

[Serializable]
public class UsernameTooShortException : Exception
{
    public UsernameTooShortException() : base() { }
    public UsernameTooShortException(string msg) : base(msg) { }
    public UsernameTooShortException(string msg, Exception innerException) : base(msg, innerException){}
}