namespace MessagingApp.Exceptions;

[Serializable]
public class ChatAlreadyExistsException : Exception
{
    public ChatAlreadyExistsException() : base() { }
    public ChatAlreadyExistsException(string msg) : base(msg) { }
    public ChatAlreadyExistsException(string msg, Exception innerException) : base(msg, innerException){}
}