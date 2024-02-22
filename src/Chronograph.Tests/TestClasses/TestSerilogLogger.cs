using Chronograph.Core.Logging;
using Chronograph.Serilog.Helpers;
using Chronograph.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace Chronograph.Tests.TestClasses;

[TestClass]
public class TestSerilogLogger
{
    private Infrastructure.TestSerilogLogger GetLogger() => new();

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
    public void TestOperationDescriptionWithCurlyBraces()
    {
        var logger = GetLogger();
        var chrono = logger.Chrono()
            .For(
                $"test operation description with number {1567 + DateTime.Now.Hour} "
                + $"and record {new TestRecord(42, "test", DateTime.Now)}")
            .Start();

        chrono.Dispose();

        logger.WrittenEvents.Should().HaveCount(2);
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test operation"));
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

    [TestMethod]
    public void TestSimpleOperationWithMessageOnDispose()
    {
        var logger = GetLogger();

        var chrono = logger.Chrono().For("test operation").Start();

        chrono.Dispose("Test end mesage template");

        logger.WrittenEvents.Should().HaveCount(2);
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test operation"));

        logger.WrittenEvents.Should().Contain(
            m => m.message.Contains("Test end mesage template"));
    }

    [TestMethod]
    public void TestSimpleOperationWithMessageOnDisposeAndParameters()
    {
        var logger = GetLogger();

        var chrono = logger.Chrono().For("test operation").Start();

        chrono.Dispose("Test end mesage template testParameter={TestParameter}", () => 42);

        logger.WrittenEvents.Should().HaveCount(2);
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test operation"));

        logger.WrittenEvents.Should().Contain(
            m => m.message.Contains("testParameter=42"));
    }

    [TestMethod]
    public void TestSimpleOperationWithReportAndMessageOnDispose()
    {
        var logger = GetLogger();

        var chrono = logger.Chrono()
            .For("test operation")
            .Report(
                "Test report testCount={testCounterValue}, testSecondCount={testCounterValue2}",
                () => 42,
                () => 1567
            )
            .Start();

        chrono.Dispose("Test end mesage template testParameter={TestParameter}", () => 43);

        logger.WrittenEvents.Should().HaveCount(4);

        // warning messages

        logger.WrittenEvents.Should().Contain(
            m =>
                m.message.Contains("end message template for operation 'test operation' was previously configured")
                && m.logEvent.Level == LogEventLevel.Warning
        );

        logger.WrittenEvents.Should().Contain(
            m =>
                m.message.Contains("2 parameter provider functions for end message template")
                && m.logEvent.Level == LogEventLevel.Warning
        );

        // reported messages

        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("testParameter=43"));
    }
}