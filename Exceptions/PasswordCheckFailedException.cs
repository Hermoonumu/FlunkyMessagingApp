namespace MessagingApp.Exceptions;

[Serializable]
public class PasswordCheckFailedException : Exception
{
    public PasswordCheckFailedException() : base() { }
    public PasswordCheckFailedException(string msg) : base(msg) { }
    public PasswordCheckFailedException(string msg, Exception innerException) : base(msg, innerException){}
}