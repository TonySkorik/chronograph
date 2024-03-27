﻿using Microsoft.Extensions.Logging;

namespace Chronograph.Tests.Infrastructure;

internal class TestMicrosoftLogger : ILogger
{
    private class MicrosoftLoggerScopeMock<TState> : IDisposable
    {
        // ReSharper disable once MemberCanBePrivate.Local
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public TState ScopeState { get; }

        public MicrosoftLoggerScopeMock(TState scopeState)
        {
            ScopeState = scopeState;
        }

        public void Dispose()
        { }
    }

    public List<(LogLevel level, EventId eventId, Exception ex, string message)> WrittenEvents { set; get; } = [];

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        var rendered = formatter.Invoke(state, exception);
        WrittenEvents.Add((logLevel, eventId, exception, rendered));
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return new MicrosoftLoggerScopeMock<TState>(state);
    }
}