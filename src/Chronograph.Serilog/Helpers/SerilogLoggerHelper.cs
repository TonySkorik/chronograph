﻿using System.Diagnostics.CodeAnalysis;
using Chronograph.Core.Logging;
using Chronograph.Serilog.Logging;

using Serilog;

namespace Chronograph.Serilog.Helpers;

/// <summary>
/// Helper class for creating chronographs for Serilog <see cref="ILogger"/>.
/// </summary>
[SuppressMessage("ReSharper", "MemberCanBeInternal")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static class SerilogLoggerHelper
{
	/// <summary>
	/// Gets or sets the default chronograph event level.
	/// </summary>
	public static ChronographLoggerEventLevel DefaultChronographEventLevel { get; set; } = ChronographLoggerEventLevel.Information;

	/// <summary>
	/// Creates the stopped chronograph. Used to build chronograph using fluent interface.
	/// </summary>
	/// <param name="targetLogger">The target logger.</param>
	/// <remarks>The returned chronograph is <c>not started by default</c>, so you need to call <c>Start()</c> method explicitly upon build completion.</remarks>
	public static Core.Chronograph Chrono(this ILogger targetLogger)
	{
		var serilogChronoLogger = new SerilogChronographLogger(targetLogger);

		return Core.Chronograph.Create(serilogChronoLogger)
			.WithEventLevel(DefaultChronographEventLevel);
	}

	/// <summary>
	/// Creates the stopped chronograph. Used to build chronograph using fluent interface.
	/// </summary>
	/// <param name="targetLogger">The target logger.</param>
	/// <param name="chronographEventLevel">The chronograph log messages event level.</param>
	/// <remarks>The returned chronograph is <c>not started by default</c>, so you need to call <c>Start()</c> method explicitly upon build completion.</remarks>
	public static Core.Chronograph Chrono(this ILogger targetLogger, ChronographLoggerEventLevel chronographEventLevel)
	{
		var serilogChronoLogger = new SerilogChronographLogger(targetLogger);

		return Core.Chronograph.Create(serilogChronoLogger)
			.WithEventLevel(chronographEventLevel);
	}

	/// <summary>
	/// Creates the running chronograph for a non-parameterized acton description.
	/// </summary>
	/// <param name="targetLogger">The target logger.</param>
	/// <param name="actionDescription">The action description.</param>
	/// <remarks>The returned chronograph is <c>started by default</c>.</remarks>
	public static Core.Chronograph Chrono(this ILogger targetLogger, string actionDescription)
	{
		var serilogChronoLogger = new SerilogChronographLogger(targetLogger);

		return Core.Chronograph.Create(serilogChronoLogger)
			.WithEventLevel(DefaultChronographEventLevel)
			.For(actionDescription)
			.Start();
	}

	/// <summary>
	/// Creates the running chronograph for a non-parameterized acton description.
	/// </summary>
	/// <param name="targetLogger">The target logger.</param>
	/// <param name="actionDescription">The action description.</param>
	/// <param name="chronographEventLevel">The chronograph log messages event level.</param>
	/// <remarks>The returned chronograph is <c>started by default</c>.</remarks>
	public static Core.Chronograph Chrono(
		this ILogger targetLogger, 
		string actionDescription, 
		ChronographLoggerEventLevel chronographEventLevel)
	{
		var serilogChronoLogger = new SerilogChronographLogger(targetLogger);

		return Core.Chronograph.Create(serilogChronoLogger)
			.WithEventLevel(chronographEventLevel)
			.For(actionDescription)
			.Start();
	}
}