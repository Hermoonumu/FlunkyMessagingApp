using System.Text;
using System.Threading.Tasks;

namespace MessagingApp.Services.Implementation

; 
public class LoggerService : ILoggerService, IDisposable{
    private int logsNo = default;
    private StreamWriter writeTo=null;
    private readonly IConfiguration _conf;
    private LoggerService(){}
    public LoggerService(IConfiguration conf)
    {
        _conf = conf;
        WriteTo(null);
    }
    public async Task LogInfo(string msg, string source){
        if (writeTo==null){throw new PathNotSetException("You didn't set a path to a logging file");}
        try{
            await writeTo.WriteLineAsync(new StringBuilder().Append($"[Info@{source}]: ").Append(msg));
            await writeTo.FlushAsync();
        } catch (IOException e) {
            throw new Exception($"Couldn't log to a file: {e}");
        }
        logsNo++;
    }
    public async Task LogError(string msg, string source){
        if (writeTo==null){throw new PathNotSetException("You didn't set a path to a logging file");}
        try{
            await writeTo.WriteLineAsync(new StringBuilder().Append($"[Error@{source}]: ").Append(msg));
            await writeTo.FlushAsync();
        } catch (IOException e) {
            throw new Exception($"Couldn't log to a file: {e}");
        }
        logsNo++;
    }
    public void WriteTo(string PATH)
    {
        if (PATH is null)
        {
            writeTo = new StreamWriter(_conf.GetValue<string>("LoggerPath")!, true);
        } else {
            writeTo = new StreamWriter(PATH!);
        }
    }
    public void Dispose()
    {
        writeTo.Dispose();
    }
    public int GetLogsNo() => logsNo;
}

public class PathNotSetException : ApplicationException
{
    public PathNotSetException(string msg) : base(msg) { }
}