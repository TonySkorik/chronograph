using System.Text;
using Chronograph.Core.Logging;
using Chronograph.Serilog.Logging;
using Chronograph.Tests.Abstractions;
using Serilog;
using Serilog.Events;

namespace Chronograph.Tests.Infrastructure;

internal class TestSerilogLogger : ILogger, ITestLogger
{
    public List<(ChronographLoggerEventLevel level, string message)> WrittenEvents { set; get; } = [];

    public void Write(LogEvent logEvent)
    {
        WrittenEvents.Add((SerilogChronographLogger.ToAbstractEventLevel(logEvent.Level), RenderLogMessage(logEvent)));
    }

    private string RenderLogMessage(LogEvent logEvent)
    {
        MemoryStream ms = new();
        using (TextWriter tw = new StreamWriter(ms))
        {
            logEvent.RenderMessage(tw);
        }

        var byteMessage = ms.ToArray();

        var stringMessage = Encoding.UTF8.GetString(byteMessage);

        return stringMessage;
    }
}