﻿namespace Chronograph.Core.Logging;

/// <summary>
/// Chronograph logger-agnostic abstract event levels.
/// </summary>
public enum ChronographLoggerEventLevel
{
	/// <summary>
	/// The verbose event (aka. TRACE).
	/// </summary>
	Verbose,

	/// <summary>
	/// The debug event.
	/// </summary>
	Debug,

	/// <summary>
	/// The informational event.
	/// </summary>
	Information,

	/// <summary>
	/// The warning event.
	/// </summary>
	Warning,

	/// <summary>
	/// The error event.
	/// </summary>
	Error,

	/// <summary>
	/// The fatal event (aka. Critical).
	/// </summary>
	Fatal,
	
	/// <summary>
	/// The event level indicating that event should not be written at all.
	/// Applies only to Microsoft.Extensions.Logging logger.
	/// </summary>
	None
}