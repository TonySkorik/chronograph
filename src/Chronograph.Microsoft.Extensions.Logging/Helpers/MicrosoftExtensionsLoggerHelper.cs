using System.Diagnostics.CodeAnalysis;
using Chronograph.Core.Logging;
using Chronograph.Microsoft.Extensions.Logging.Logging;

using Microsoft.Extensions.Logging;

namespace Chronograph.Microsoft.Extensions.Logging.Helpers;

/// <summary>
/// Helper class for creating chronographs for Microsoft.Extensions.Logging <see cref="ILogger"/> logger.
/// </summary>
[SuppressMessage("ReSharper", "MemberCanBeInternal")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static class MicrosoftExtensionsLoggerHelper
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
		var microsoftExtensionsChronoLogger = new MicrosoftExtensionsChronographLogger(targetLogger);

		return Core.Chronograph.Create(microsoftExtensionsChronoLogger)
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
		var microsoftExtensionsChronoLogger = new MicrosoftExtensionsChronographLogger(targetLogger);

		return Core.Chronograph.Create(microsoftExtensionsChronoLogger)
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
		var microsoftExtensionsChronoLogger = new MicrosoftExtensionsChronographLogger(targetLogger);

		return Core.Chronograph.Create(microsoftExtensionsChronoLogger)
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
		var microsoftExtensionsChronoLogger = new MicrosoftExtensionsChronographLogger(targetLogger);

		return Core.Chronograph.Create(microsoftExtensionsChronoLogger)
			.WithEventLevel(chronographEventLevel)
			.For(actionDescription)
			.Start();
	}
}