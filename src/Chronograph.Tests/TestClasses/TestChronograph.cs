using System.Diagnostics;
using Chronograph.Core.Logging;
using Chronograph.Microsoft.Extensions.Logging.Helpers;
using Chronograph.Serilog.Helpers;
using Chronograph.Tests.Abstractions;
using Chronograph.Tests.Model;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chronograph.Tests.TestClasses;

[TestClass]
public abstract class TestChronograph
{
    protected abstract (Core.Chronograph, ITestLogger) GetChronographAndLogger(); 

    [TestMethod]
    public void TestSimpleOperation()
    {
        var (chronograph, logger) = GetChronographAndLogger();
        var chrono = chronograph.For("test operation").Start();
        chrono.Dispose();

        logger.WrittenEvents.Should().HaveCount(2);
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test operation"));
    }

    [TestMethod]
    public void TestSimpleOperationWithReport()
    {
        var (chronograph, logger) = GetChronographAndLogger();

        var chrono = chronograph
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
        var (chronograph, logger) = GetChronographAndLogger();
        
        var chrono = chronograph
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
                && m.message.Contains("testString=")
                && m.message.Contains("test string value")
                && m.message.Contains("testDoubleValue=0.5"));
    }

    [TestMethod]
    public void TestOperationDescriptionWithCurlyBraces()
    {
        var (chronograph, logger) = GetChronographAndLogger();
        var chrono = chronograph
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
        SerilogLoggerHelper.DefaultChronographEventLevel = ChronographLoggerEventLevel.Error;
        MicrosoftExtensionsLoggerHelper.DefaultChronographEventLevel = ChronographLoggerEventLevel.Error;

        var (chronograph, logger) = GetChronographAndLogger();
        var chrono = chronograph.For("test error operation").Start();
        chrono.Dispose();

        logger.WrittenEvents.Should().HaveCount(2);
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test error operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test error operation"));
        logger.WrittenEvents.Should().OnlyContain(e => e.level == ChronographLoggerEventLevel.Error);

        // restore default event level
        
        SerilogLoggerHelper.DefaultChronographEventLevel = ChronographLoggerEventLevel.Information;
        MicrosoftExtensionsLoggerHelper.DefaultChronographEventLevel = ChronographLoggerEventLevel.Information;
    }

    [TestMethod]
    public void TestSimpleOperationWithMessageOnDispose()
    {
        var (chronograph, logger) = GetChronographAndLogger();

        var chrono = chronograph.For("test operation").Start();

        chrono.Dispose("Test end message template");

        logger.WrittenEvents.Should().HaveCount(2);
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test operation"));

        logger.WrittenEvents.Should().Contain(
            m => m.message.Contains("Test end message template"));
    }

    [TestMethod]
    public void TestSimpleOperationWithMessageOnDisposeAndParameters()
    {
        var (chronograph, logger) = GetChronographAndLogger();

        var chrono = chronograph.For("test operation").Start();

        chrono.Dispose("Test end message template testParameter={TestParameter}", () => 42);

        logger.WrittenEvents.Should().HaveCount(2);
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test operation"));

        logger.WrittenEvents.Should().Contain(
            m => m.message.Contains("testParameter=42"));
    }

    [TestMethod]
    public void TestSimpleOperationWithReportAndMessageOnDispose()
    {
        var (chronograph, logger) = GetChronographAndLogger();

        var chrono = chronograph
            .For("test operation")
            .Report(
                "Test report testCount={testCounterValue}, testSecondCount={testCounterValue2}",
                () => 42,
                () => 1567
            )
            .Start();

        chrono.Dispose("Test end message template testParameter={TestParameter}", () => 43);

        logger.WrittenEvents.Should().HaveCount(4);

        // warning messages

        logger.WrittenEvents.Should().Contain(
            m =>
                m.message.Contains("end message template for operation 'test operation' was previously configured")
                && m.level == ChronographLoggerEventLevel.Warning
        );

        logger.WrittenEvents.Should().Contain(
            m =>
                m.message.Contains("2 parameter provider functions for end message template")
                && m.level == ChronographLoggerEventLevel.Warning
        );

        // reported messages

        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("testParameter=43"));
    }
    
    [TestMethod]
    public async Task TestLongRunningOperationWithoutParametersWithoutSpecificMessage()
    {
        var (chronograph, logger) = GetChronographAndLogger();
        
        var chrono = chronograph.For("test operation")
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
        var (chronograph, logger) = GetChronographAndLogger();

        var chrono = chronograph
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
        var (chronograph, logger) = GetChronographAndLogger();

        var chrono = chronograph
            .For("test operation {IntParameter}", 42)
            .WithLongRunningOperationReport(
                TimeSpan.FromMilliseconds(1),
                "Long running operation {LongParameter} detected",
                1567L)
            .Start();

        await Task.Delay(TimeSpan.FromMilliseconds(2));

        chrono.Dispose();

        logger.WrittenEvents.Should().HaveCount(3);

        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test operation"));
        logger.WrittenEvents.Should().Contain(
            m => m.message.Contains("Long running operation 1567 detected"));
    }

    [TestMethod]
    public async Task TestLongRunningOperationWithParametersWithSpecificMessageWithParameters()
    {
        var (chronograph, logger) = GetChronographAndLogger();

        int testValue = 0;

        var chrono = chronograph
            .For("test operation {IntParameter}", 42)
            .WithLongRunningOperationReport(
                TimeSpan.FromMilliseconds(1),
                "Long running operation {LongParameter} detected",
                // ReSharper disable once AccessToModifiedClosure | Justification - intended capture
                () => testValue)
            .Start();

        testValue = 1567;

        await Task.Delay(TimeSpan.FromMilliseconds(2));

        chrono.Dispose();

        logger.WrittenEvents.Should().HaveCount(3);

        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Started test operation"));
        logger.WrittenEvents.Should().Contain(m => m.message.Contains("Finished test operation"));
        logger.WrittenEvents.Should().Contain(
            m => m.message.Contains("Long running operation 1567 detected"));
    }

    [TestMethod]
    public async Task TestOnStartAndOnEndAction()
    {
        var (chronograph, _) = GetChronographAndLogger();

        IReadOnlyList<object> onStartParameters = null;

        IReadOnlyList<object> onEndParameters = null;
        Stopwatch onEndStopwatch = null;

        var chrono = chronograph
            .For("test operation {IntParameter}", 42)
            .Report("Operation result {IntParameter}", () => 1567)
            .WithOnStartAction((parameters) => { onStartParameters = parameters; })
            .WithOnEndAction(
                (sw, parameters) =>
                {
                    onEndStopwatch = sw;
                    onEndParameters = parameters;
                })
            .Start();

        await Task.Delay(TimeSpan.FromMilliseconds(2));

        chrono.Dispose();

        onStartParameters.Should().NotBeNull();
        onStartParameters.Count.Should().Be(1);
        onStartParameters[0].Should().Be(42);

        onEndStopwatch.Should().NotBeNull();
        onEndStopwatch.Elapsed.TotalMilliseconds.Should().BeGreaterThan(0);

        onEndParameters.Should().NotBeNull();
        onEndParameters.Count.Should().Be(3); // +1 from start action parameters and +1 from elapsed
        onEndParameters[0].Should().Be(42);
        onEndParameters[1].Should().Be(1567);
        onEndParameters[2].Should().Be(onEndStopwatch.Elapsed.ToString("g"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestSampling_SampleNone_ReportLongRunningOperations(bool shouldLogLongRunningOperations)
    {
        var (chronograph, logger) = GetChronographAndLogger();

        var chrono = chronograph
            .For("test operation {IntParameter}", 42)
            .WithLongRunningOperationReport(
                TimeSpan.FromMilliseconds(1),
                "Long running operation {LongParameter} detected",
                // ReSharper disable once AccessToModifiedClosure | Justification - intended capture
                () => 42)
            .WithSampling(0, shouldAlwaysReportLongRunningOperations: shouldLogLongRunningOperations)
            .Start();

        await Task.Delay(TimeSpan.FromMilliseconds(2));

        chrono.Dispose();

        if (shouldLogLongRunningOperations == false)
        {
            logger.WrittenEvents.Should().HaveCount(0);
        }
        else
        {
            logger.WrittenEvents.Should().HaveCount(1);
            logger.WrittenEvents.Should().Contain(
                m => m.message.Contains("Long running operation 42 detected"));
        }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestSampling(bool shouldLogLongRunningOperations)
    {
        int writeMessageCount = 0;
        int runCount = 200;
        
        for (int i = 0; i < runCount; i++)
        {
            var (chronograph, logger) = GetChronographAndLogger();

            var chrono = chronograph
                .For("test operation {IntParameter}", 42)
                .WithLongRunningOperationReport(
                    TimeSpan.FromMilliseconds(1),
                    "Long running operation {LongParameter} detected",
                    // ReSharper disable once AccessToModifiedClosure | Justification - intended capture
                    () => 42)
                .WithSampling(50, shouldAlwaysReportLongRunningOperations: shouldLogLongRunningOperations)
                .Start();

            await Task.Delay(TimeSpan.FromMilliseconds(2));

            chrono.Dispose();

            if (logger.WrittenEvents.Count > 0)
            {
                writeMessageCount++;
            }
        }

        if (shouldLogLongRunningOperations)
        {
            writeMessageCount.Should().Be(runCount);
        }
        else
        {
            writeMessageCount.Should().BeLessThanOrEqualTo((runCount / 2)+20);
        }
    }
}
