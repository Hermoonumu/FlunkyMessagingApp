using MessagingApp.Services.Implementation;

namespace MessagingApp.Services;

public interface ILoggerService
{
    public static LoggerService Instance;
    public void LogInfo(string msg, string source);
    public void LogError(string msg, string source);
    public void WriteTo(string PATH);
    public void Dispose();
    public int GetLogsNo();
}