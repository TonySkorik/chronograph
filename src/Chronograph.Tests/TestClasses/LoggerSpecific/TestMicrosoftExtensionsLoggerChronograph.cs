using Chronograph.Microsoft.Extensions.Logging.Helpers;
using Chronograph.Tests.Abstractions;
using Chronograph.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chronograph.Tests.TestClasses.LoggerSpecific;

[TestClass]
public class TestMicrosoftExtensionsLoggerChronograph : TestChronograph
{
	protected override (Core.Chronograph, ITestLogger) GetChronographAndLogger()
	{
		var logger = new TestMicrosoftLogger();
		var chrono = logger.Chrono();

		return (chrono, logger);
	}
}
