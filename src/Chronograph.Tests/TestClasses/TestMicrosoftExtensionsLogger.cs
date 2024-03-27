using Chronograph.Core.Logging;
using Chronograph.Microsoft.Extensions.Logging.Helpers;
using Chronograph.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chronograph.Tests.TestClasses;

[TestClass]
public class TestMicrosoftExtensionsLogger
{
    private TestMicrosoftLogger GetLogger() => new();

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

        var chrono = logger.Chrono()
            .For("test operation")
            .Report("Test report testCount={testCounterValue}", () => 42)
            .Start();

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
        var chrono = logger.Chrono()
            .For("test operation")
            .Report(
                "Test report testCount={testCounterValue}, testString={testStringValue}, testDoubleValue={testDoubleValue}",
                () => 42,
                () => "test string value",
                () => 0.5
            )
            .Start();

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
        MicrosoftExtensionsLoggerHelper.DefaultChronographEventLevel = ChronographLoggerEventLevel.Error;

        var logger = GetLogger();
        var chrono = logger.Chrono().For("test error operation").Start();
        chrono.Dispose();

        logger.WrittenEvents.Should().HaveCount(2);
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test error operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test error operation"));
        logger.WrittenEvents.Should().OnlyContain(e => e.level == LogLevel.Error);
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
                && m.level == LogLevel.Warning
        );

        logger.WrittenEvents.Should().Contain(
            m =>
                m.message.Contains("2 parameter provider functions for end message template")
                && m.level == LogLevel.Warning
        );

        // reported messages

        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("testParameter=43"));
    }
    
    [TestMethod]
    public async Task TestLongRunningOperationWithoutParametersWithoutSpecificMessage()
    {
        var logger = GetLogger();
        
        var chrono = logger.Chrono().For("test operation")
            .WithLongRunningOperationReport(TimeSpan.FromMilliseconds(1))
            .Start();

        await Task.Delay(TimeSpan.FromMilliseconds(2));
        
        chrono.Dispose();

        logger.WrittenEvents.Should().HaveCount(3);
        
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("took a long time to finish"));
    }

    [TestMethod]
    public async Task TestLongRunningOperationWithParametersWithoutSpecificMessage()
    {
        var logger = GetLogger();

        var chrono = logger.Chrono()
            .For("test operation {IntParameter}", 42)
            .WithLongRunningOperationReport(TimeSpan.FromMilliseconds(1))
            .Start();

        await Task.Delay(TimeSpan.FromMilliseconds(2));

        chrono.Dispose();

        logger.WrittenEvents.Should().HaveCount(3);

        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test operation"));
        logger.WrittenEvents.Should().Contain(
            m => m.message.Contains("test operation 42 took a long time to finish"));
    }

    [TestMethod]
    public async Task TestLongRunningOperationWithParametersWithSpecificMessage()
    {
        var logger = GetLogger();

        var chrono = logger.Chrono()
            .For("test operation {IntParameter}", 42)
            .WithLongRunningOperationReport(
                TimeSpan.FromMilliseconds(1),
                "Long runing operation {LongParameter} detected",
                1567L)
            .Start();

        await Task.Delay(TimeSpan.FromMilliseconds(2));

        chrono.Dispose();

        logger.WrittenEvents.Should().HaveCount(3);

        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test operation"));
        logger.WrittenEvents.Should().Contain(
            m => m.message.Contains("Long runing operation 1567 detected"));
    }
}