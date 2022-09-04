using Diagnostics.Chronograph.Core.Logging;
using Microsoft.Extensions.Logging;

namespace Diagnostics.Chronograph.Microsoft.Extensions.Logging.Logging;

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

	private static LogLevel ToTargetEventLevel(ChronographLoggerEventLevel abastractEventLevel) =>
		abastractEventLevel switch
		{
			ChronographLoggerEventLevel.Information => LogLevel.Information,
			ChronographLoggerEventLevel.Warning => LogLevel.Warning,
			ChronographLoggerEventLevel.Error => LogLevel.Error,
			ChronographLoggerEventLevel.Verbose => LogLevel.Trace,
			ChronographLoggerEventLevel.Debug => LogLevel.Debug,
			ChronographLoggerEventLevel.Fatal => LogLevel.Critical,
			_ => throw new ArgumentOutOfRangeException(nameof(abastractEventLevel), abastractEventLevel, null)
		};
}