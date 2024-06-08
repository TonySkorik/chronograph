using Chronograph.Core.Logging;

namespace Chronograph.Tests.Abstractions;

public interface ITestLogger
{
	public List<(ChronographLoggerEventLevel level, string message)> WrittenEvents { set; get; }
}
