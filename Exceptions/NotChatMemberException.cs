namespace MessagingApp.Exceptions;

[Serializable]
public class NotChatMemberException : Exception
{
    public NotChatMemberException() : base() { }
    public NotChatMemberException(string msg) : base(msg) { }
    public NotChatMemberException(string msg, Exception innerException) : base(msg, innerException){}
}