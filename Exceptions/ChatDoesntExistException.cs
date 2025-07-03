namespace MessagingApp.Exceptions;

[Serializable]
public class ChatDoesntExistException : Exception
{
    public ChatDoesntExistException() : base() { }
    public ChatDoesntExistException(string msg) : base(msg) { }
    public ChatDoesntExistException(string msg, Exception innerException) : base(msg, innerException){}
}