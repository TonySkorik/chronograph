using System.Text;

using Serilog;
using Serilog.Events;

namespace Chronograph.Tests.TestLoggers;

internal class TestSerilogLogger : ILogger
{
    public List<(LogEvent logEvent, string message)> WrittenEvents { set; get; } = new();

    public void Write(LogEvent logEvent)
    {
        WrittenEvents.Add((logEvent, RenderLogMessage(logEvent)));
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