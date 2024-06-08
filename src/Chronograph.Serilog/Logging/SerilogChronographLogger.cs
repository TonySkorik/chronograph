using Chronograph.Core.Logging;

using Serilog;
using Serilog.Context;
using Serilog.Events;

namespace Chronograph.Serilog.Logging;

internal class SerilogChronographLogger : IChronographLogger
{
	private readonly ILogger _logger;

	internal SerilogChronographLogger(ILogger logger)
	{
		_logger = logger;
	}

	public void Write(ChronographLoggerEventLevel level, string message)
	{
		// ReSharper disable once TemplateIsNotCompileTimeConstantProblem
		_logger.Write(ToTargetEventLevel(level), message);
	}

	public void Write(ChronographLoggerEventLevel level, string messageTemplate, params object[] propertyValues)
	{
		// ReSharper disable once TemplateIsNotCompileTimeConstantProblem
		_logger.Write(ToTargetEventLevel(level), messageTemplate, propertyValues);
	}

	public IDisposable PushProperty(string propertyName, object propertyValue)
		=> LogContext.PushProperty(propertyName, propertyValue);

	public static LogEventLevel ToTargetEventLevel(ChronographLoggerEventLevel abstractEventLevel) =>
		abstractEventLevel switch
		{
			ChronographLoggerEventLevel.Information => LogEventLevel.Information,
			ChronographLoggerEventLevel.Warning => LogEventLevel.Warning,
			ChronographLoggerEventLevel.Error => LogEventLevel.Error,
			ChronographLoggerEventLevel.Verbose => LogEventLevel.Verbose,
			ChronographLoggerEventLevel.Debug => LogEventLevel.Debug,
			ChronographLoggerEventLevel.Fatal => LogEventLevel.Fatal,
			ChronographLoggerEventLevel.None => LogEventLevel.Verbose,
			_ => throw new ArgumentOutOfRangeException(nameof(abstractEventLevel), abstractEventLevel, null)
		};

	public static ChronographLoggerEventLevel ToAbstractEventLevel(LogEventLevel targetEventLevel) =>
		targetEventLevel switch
		{
			LogEventLevel.Information => ChronographLoggerEventLevel.Information,
			LogEventLevel.Warning => ChronographLoggerEventLevel.Warning,
			LogEventLevel.Error => ChronographLoggerEventLevel.Error,
			LogEventLevel.Verbose => ChronographLoggerEventLevel.Verbose,
			LogEventLevel.Debug => ChronographLoggerEventLevel.Debug,
			LogEventLevel.Fatal => ChronographLoggerEventLevel.Fatal,
			_ => throw new ArgumentOutOfRangeException(nameof(targetEventLevel), targetEventLevel, null)
		};
}