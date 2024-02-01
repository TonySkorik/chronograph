﻿using Chronograph.Core.Logging;
using Chronograph.Microsoft.Extensions.Logging.Helpers;
using Chronograph.Tests.TestLoggers;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chronograph.Tests;

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
}