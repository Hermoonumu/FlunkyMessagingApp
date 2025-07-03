namespace MessagingApp.Exceptions;

[Serializable]
public class PasswordTooShortException : Exception
{
    public PasswordTooShortException
() : base() { }
    public PasswordTooShortException
(string msg) : base(msg) { }
    public PasswordTooShortException
(string msg, Exception innerException) : base(msg, innerException){}
}