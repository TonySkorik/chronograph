using Chronograph.Tests.Mocks;

using DiagnosticExtensions.Chronograph.Core.Logging;
using DiagnosticExtensions.Chronograph.Microsoft.Extensions.Logging.Helpers;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chronograph.Tests;

[TestClass]
public class TestMicrosoftExtensionsLogger
{
	private MicrosoftLoggerMock GetLogger() => new();
		
	[TestMethod]
	public void TestSimpleOperation()
	{
		var logger = GetLogger();
		var chrono = logger.Chrono().For("test operation").Start();
		chrono.Dispose();

		logger.WrittenEvents.Should().HaveCount(2);
		logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test operation"));
		logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test operation"));
	}

	[TestMethod]
	public void TestSimpleOperationWithReport()
	{
		var logger = GetLogger();
		var chrono = logger.Chrono().For("test operation").Report("Test report testCount={testCounterValue}", () => 42).Start();
		chrono.Dispose();

		logger.WrittenEvents.Should().HaveCount(2);
		logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test operation"));
		logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test operation"));
		logger.WrittenEvents.Should().Contain(m => m.message.Contains("testCount=42"));
	}

	[TestMethod]
	public void TestSimpleOperationWithMultipleReports()
	{
		var logger = GetLogger();
		var chrono = logger.Chrono().For("test operation").Report(
			"Test report testCount={testCounterValue}, testString={testStringValue}, testDoubleValue={testDoubleValue}",
			() => 42,
			() => "test string value",
			() => 0.5).Start();

		chrono.Dispose();

		logger.WrittenEvents.Should().HaveCount(2);
		logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test operation"));
		logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test operation"));

		logger.WrittenEvents.Should().Contain(
			m => m.message.Contains("testCount=42")
				&& m.message.Contains("testString=test string value")
				&& m.message.Contains("testDoubleValue=0.5"));
	}

	[TestMethod]
	public void TestOperationWithEventLevel()
	{
		MicrosoftExtensionsLoggerHelper.DefaultChronographEventLevel = ChronographLoggerEventLevel.Error;

		var logger = GetLogger();
		var chrono = logger.Chrono().For("test error operation").Start();
		chrono.Dispose();

		logger.WrittenEvents.Should().HaveCount(2);
		logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test error operation"));
		logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test error operation"));
		logger.WrittenEvents.Should().OnlyContain(e => e.level == LogLevel.Error);
	}
}