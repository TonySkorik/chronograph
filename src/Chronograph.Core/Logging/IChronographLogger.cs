namespace Chronograph.Core.Logging;

/// <summary>
/// Common interface for all Chronograph underlying loggers
/// </summary>
public interface IChronographLogger
{
	/// <summary>
	/// Writes the specified <paramref name="message"/> as event of specified <paramref name="level"/>.
	/// </summary>
	/// <param name="level">The event level.</param>
	/// <param name="message">The message to write.</param>
	void Write(ChronographLoggerEventLevel level, string message);

	/// <summary>
	/// Renders the specified <paramref name="messageTemplate"/> as event of specified <paramref name="level"/> using provided <paramref name="propertyValues"/>.
	/// </summary>
	/// <param name="level">The event level.</param>
	/// <param name="messageTemplate">The message template to render a message.</param>
	/// <param name="propertyValues">The property values for a message template.</param>
	void Write(ChronographLoggerEventLevel level, string messageTemplate, params object[] propertyValues);

	/// <summary>
	/// Pushes the property to an underlying logger context.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="propertyValue">The value of the property.</param>
	/// <remarks>Pushing properties may not be supported by an underlying logger.</remarks>
	IDisposable PushProperty(string propertyName, object propertyValue);
}