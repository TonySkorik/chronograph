using Diagnostics.Chronograph.Core.Logging;
using Diagnostics.Chronograph.Serilog.Logging;
using Serilog;

namespace Diagnostics.Chronograph.Serilog.Helpers;

/// <summary>
/// Helper class for creating chronographs for Serilog logger.
/// </summary>
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
	/// <remarks>The returned chronograph is <c>not started by default</c> so you need to call <c>Start()</c> method explicitly upon build completion.</remarks>
	public static global::Diagnostics.Chronograph.Core.Chronograph Chrono(this ILogger targetLogger)
	{
		var serilogChronoLogger = new SerilogChronographLogger(targetLogger);

		return global::Diagnostics.Chronograph.Core.Chronograph.Create(serilogChronoLogger)
			.WithEventLevel(DefaultChronographEventLevel);
	}

	/// <summary>
	/// Creates the simple chronograph for a non-parameterized acton description.
	/// </summary>
	/// <param name="targetLogger">The target logger.</param>
	/// <param name="atcionDescription">The action description.</param>
	/// <remarks>The returned chronograph is <c>started by default</c>.</remarks>
	public static global::Diagnostics.Chronograph.Core.Chronograph Chrono(this ILogger targetLogger, string atcionDescription)
	{
		var serilogChronoLogger = new SerilogChronographLogger(targetLogger);

		return global::Diagnostics.Chronograph.Core.Chronograph.Create(serilogChronoLogger)
			.WithEventLevel(DefaultChronographEventLevel)
			.For(atcionDescription)
			.Start();
	}
}