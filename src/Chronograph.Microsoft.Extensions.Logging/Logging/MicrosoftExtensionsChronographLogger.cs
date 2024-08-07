﻿using System.Diagnostics.CodeAnalysis;
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

	[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
	public void Write(ChronographLoggerEventLevel level, string message)
	{
		_logger.Log(ToTargetEventLevel(level), message);
	}

	[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
	public void Write(ChronographLoggerEventLevel level, string messageTemplate, params object[] propertyValues)
	{
		_logger.Log(ToTargetEventLevel(level), messageTemplate, propertyValues);
	}

	[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
	public IDisposable PushProperty(string propertyName, object propertyValue)
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