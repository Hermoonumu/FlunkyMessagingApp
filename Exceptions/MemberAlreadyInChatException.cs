namespace MessagingApp.Exceptions;

[Serializable]
public class MemberAlreadyInChatException : Exception
{
    public MemberAlreadyInChatException() : base() { }
    public MemberAlreadyInChatException(string msg) : base(msg) { }
    public MemberAlreadyInChatException(string msg, Exception innerException) : base(msg, innerException){}
}