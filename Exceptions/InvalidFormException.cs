namespace MessagingApp.Exceptions;

[Serializable]
public class InvalidFormException : Exception
{
    public InvalidFormException() : base() { }
    public InvalidFormException(string msg) : base(msg) { }
    public InvalidFormException(string msg, Exception innerException) : base(msg, innerException){}
}