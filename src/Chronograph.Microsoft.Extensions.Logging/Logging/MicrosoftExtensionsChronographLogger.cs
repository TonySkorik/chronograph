using Chronograph.Core.Logging;

using Microsoft.Extensions.Logging;

namespace Chronograph.Microsoft.Extensions.Logging.Logging;

internal class MicrosoftExtensionsChronographLogger : IChronographLogger
{
	private readonly ILogger _logger;

	internal MicrosoftExtensionsChronographLogger(ILogger logger)
	{
		_logger = logger;
	}

	public void Write(ChronographLoggerEventLevel level, string message)
	{
		// ReSharper disable once TemplateIsNotCompileTimeConstantProblem
		_logger.Log(ToTargetEventLevel(level), message);
	}

	public void Write(ChronographLoggerEventLevel level, string messageTemplate, params object[] propertyValues)
	{
		// ReSharper disable once TemplateIsNotCompileTimeConstantProblem
		_logger.Log(ToTargetEventLevel(level), messageTemplate, propertyValues);
	}

	public IDisposable PushProperty(string propertyName, object propertyValue)
		// ReSharper disable once TemplateIsNotCompileTimeConstantProblem
		=> _logger.BeginScope(propertyName, propertyValue);

	public static LogLevel ToTargetEventLevel(ChronographLoggerEventLevel abstractEventLevel) =>
		abstractEventLevel switch
		{
			ChronographLoggerEventLevel.Information => LogLevel.Information,
			ChronographLoggerEventLevel.Warning => LogLevel.Warning,
			ChronographLoggerEventLevel.Error => LogLevel.Error,
			ChronographLoggerEventLevel.Verbose => LogLevel.Trace,
			ChronographLoggerEventLevel.Debug => LogLevel.Debug,
			ChronographLoggerEventLevel.Fatal => LogLevel.Critical,
			ChronographLoggerEventLevel.None => LogLevel.None,
			_ => throw new ArgumentOutOfRangeException(nameof(abstractEventLevel), abstractEventLevel, null)
		};

	public static ChronographLoggerEventLevel ToAbstractEventLevel(LogLevel targetEventLevel) =>
		targetEventLevel switch
		{
			LogLevel.Information => ChronographLoggerEventLevel.Information,
			LogLevel.Warning => ChronographLoggerEventLevel.Warning,
			LogLevel.Error => ChronographLoggerEventLevel.Error,
			LogLevel.Trace => ChronographLoggerEventLevel.Verbose,
			LogLevel.Debug => ChronographLoggerEventLevel.Debug,
			LogLevel.Critical => ChronographLoggerEventLevel.Fatal,
			LogLevel.None => ChronographLoggerEventLevel.None,
			_ => throw new ArgumentOutOfRangeException(nameof(targetEventLevel), targetEventLevel, null)
		};
}