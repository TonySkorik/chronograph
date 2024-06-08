using Chronograph.Serilog.Helpers;
using Chronograph.Tests.Abstractions;
using Chronograph.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chronograph.Tests.TestClasses.LoggerSpecific;

[TestClass]
public class TestSerilogLoggerChronograph : TestChronograph
{
	protected override (Core.Chronograph, ITestLogger) GetChronographAndLogger()
	{
		TestSerilogLogger logger = new();
		var chrono = logger.Chrono();

		return (chrono, logger);
	}
}
