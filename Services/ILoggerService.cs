using MessagingApp.Services.Implementation;

namespace MessagingApp.Services;

public interface ILoggerService
{
    public static LoggerService Instance;
    public Task LogInfo(string msg, string source);
    public Task LogError(string msg, string source);
    public void WriteTo(string PATH);
    public void Dispose();
    public int GetLogsNo();
}