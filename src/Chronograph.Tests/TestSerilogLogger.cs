using Chronograph.Tests.Mocks;
using Diagnostics.Chronograph.Core.Logging;
using Diagnostics.Chronograph.Serilog.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace Chronograph.Tests;

[TestClass]
public class TestSerilogLogger
{
	private SerilogLoggerMock GetLogger() => new();
		
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
				&& m.message.Contains("testString=\"test string value\"")
				&& m.message.Contains("testDoubleValue=0.5"));
	}

	[TestMethod]
	public void TestOperationWithEventLevel()
	{
		var initialChronoLevel = SerilogLoggerHelper.DefaultChronographEventLevel;
		SerilogLoggerHelper.DefaultChronographEventLevel = ChronographLoggerEventLevel.Error;
			
		var logger = GetLogger();
		var chrono = logger.Chrono().For("test error operation").Start();
		chrono.Dispose();

		logger.WrittenEvents.Should().HaveCount(2);
		logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test error operation"));
		logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test error operation"));
		logger.WrittenEvents.Should().OnlyContain(e => e.logEvent.Level == LogEventLevel.Error);

		SerilogLoggerHelper.DefaultChronographEventLevel = initialChronoLevel;
	}
}