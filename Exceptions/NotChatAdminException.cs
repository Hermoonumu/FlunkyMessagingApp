namespace MessagingApp.Exceptions;

[Serializable]
public class NotChatAdminException : Exception
{
    public NotChatAdminException() : base() { }
    public NotChatAdminException(string msg) : base(msg) { }
    public NotChatAdminException(string msg, Exception innerException) : base(msg, innerException){}
}