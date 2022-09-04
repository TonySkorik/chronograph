using Diagnostics.Chronograph.Core.Logging;
using Serilog;
using Serilog.Context;
using Serilog.Events;

namespace Diagnostics.Chronograph.Serilog.Logging;

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

	private static LogEventLevel ToTargetEventLevel(ChronographLoggerEventLevel abastractEventLevel) =>
		abastractEventLevel switch
		{
			ChronographLoggerEventLevel.Information => LogEventLevel.Information,
			ChronographLoggerEventLevel.Warning => LogEventLevel.Warning,
			ChronographLoggerEventLevel.Error => LogEventLevel.Error,
			ChronographLoggerEventLevel.Verbose => LogEventLevel.Verbose,
			ChronographLoggerEventLevel.Debug => LogEventLevel.Debug,
			ChronographLoggerEventLevel.Fatal => LogEventLevel.Fatal,
			_ => throw new ArgumentOutOfRangeException(nameof(abastractEventLevel), abastractEventLevel, null)
		};
}