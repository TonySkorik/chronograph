using System.Diagnostics;
using System.Text;

using Chronograph.Core.Infrastructure;
using Chronograph.Core.Logging;

namespace Chronograph.Core;

/// <summary>
/// Represents chronograph which writes operation timing information to serilog.
/// </summary>
/// <remarks>Uses <c>using()</c> scope Dispose pattern. Uses scope-modified closures to get timed operation results. May require some ReSharper "Access to modified closure" messages suppression attributes or comments.</remarks>
public class Chronograph : IDisposable
{
	#region Private

	private readonly Stopwatch _stopwatch;
	private readonly IChronographLogger _logger;
	private readonly Dictionary<string, object> _parameters = new();
	private readonly List<object> _actionDescriptionParameters = new();

	private ChronographLoggerEventLevel _eventLevel;
	private string _actionDescription;
	private string _endActionMessageTemplate;
	private Func<object>[] _countProviders;

	#endregion

	#region Ctor

	/// <summary>
	/// Initializes a new instance of the <see cref="Chronograph"/> class.
	/// </summary>
	/// <param name="logger">The logger.</param>
	/// <param name="eventLevel">The event level.</param>
	protected internal Chronograph(IChronographLogger logger, ChronographLoggerEventLevel eventLevel)
	{
		_stopwatch = Stopwatch.StartNew();
		_logger = logger;
		_eventLevel = eventLevel;
	}

	/// <summary>
	/// Creates empty and not started chronograph. Used for Chronograph fluent builder methods.
	/// </summary>
	/// <param name="logger">The target logger chronograph will log to.</param>
	protected internal Chronograph(IChronographLogger logger)
	{
		_logger = logger;
		_stopwatch = new Stopwatch();
		_eventLevel = ChronographLoggerEventLevel.Information;
		_actionDescription = string.Empty;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Chronograph"/> class.
	/// </summary>
	/// <param name="logger">The logger.</param>
	/// <param name="actionDescription">The action description.</param>
	/// <param name="eventLevel">The event level.</param>
	/// <param name="actionDescriptionParameters">The action description parameters.</param>
	/// <remarks>Use this to log information about operation duration with parameters that you want to be extracted by serilog.</remarks>
	/// <example>
	/// <c>
	/// using(Log.CreateChronograph("operation description {withParameter}", operationDescriptionParameter)){
	///		OpertaionToTime();
	/// }
	/// </c>
	/// </example>
	protected internal Chronograph(
		IChronographLogger logger,
		string actionDescription,
		ChronographLoggerEventLevel eventLevel,
		params object[] actionDescriptionParameters) : this(logger, eventLevel)
	{
		if (actionDescriptionParameters != null)
		{
			_actionDescriptionParameters.AddRange(actionDescriptionParameters);
		}

		_actionDescription = PrepareActionDescription(actionDescription);

		if (actionDescriptionParameters != null)
		{
			_logger.Write(_eventLevel, $"Started {_actionDescription}", _actionDescriptionParameters);
		}
		else
		{
			_logger.Write(_eventLevel, $"Started {_actionDescription}");
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Chronograph"/> class.
	/// </summary>
	/// <param name="logger">The logger.</param>
	/// <param name="actionDescription">The action description.</param>
	/// <param name="eventLevel">The logger event level.</param>
	/// <param name="endActionMessageTemplate">The end action message template.</param>
	/// <param name="countProviders">The count provider closures. Invoked upon operation completion.</param>
	/// <example>
	/// <c>
	/// var targetDictionary = new Dictionary&lt;int,int&gt;();
	/// using(Log.CreateChronograph("operation description", "got dictionary entries from server {countParamter}", ()=>targetDictionary.Count)){
	///		targetDictionary = GetDicitonaryEntries();
	/// }
	/// </c>
	/// </example>
	protected internal Chronograph(
		IChronographLogger logger,
		string actionDescription,
		ChronographLoggerEventLevel eventLevel,
		string endActionMessageTemplate = null,
		params Func<object>[] countProviders) : this(logger, actionDescription, eventLevel)
	{
		_endActionMessageTemplate = endActionMessageTemplate;
		_countProviders = countProviders; // closures for getting objects count from within the chronograph scope
	}

	#endregion

	#region Factory methods

	/// <summary>
	/// Creates empty and not started chronograph. Used for Chronograph fluent builder methods.
	/// </summary>
	/// <param name="logger">The logger associated with this chronograph instance.</param>
	public static Chronograph Create(IChronographLogger logger) => new(logger);

	#endregion

	#region Methods for setting up chronograph instance

	/// <summary>
	/// Setts the <see cref="ChronographLoggerEventLevel"/> of the logger to the specified value.
	/// </summary>
	/// <param name="logEventLevel">The level to set.</param>
	public Chronograph WithEventLevel(ChronographLoggerEventLevel logEventLevel)
	{
		_eventLevel = logEventLevel;

		return this;
	}

	/// <summary>
	/// Adds parameter to a logging context. Parameters will be used to enrich context when chronograph writes action completion messages.
	/// </summary>
	/// <param name="name">The parameter name.</param>
	/// <param name="value">The parameter value.</param>
	public Chronograph WithParameter(string name, object value)
	{
		_parameters[name] = value;

		return this;
	}

	/// <summary>
	/// Adds parameters to a logging context. Parameters will be used to enrich context when chronograph writes action completion messages.
	/// </summary>
	/// <param name="parameters">The parameters dictionary.</param>
	public Chronograph WithParameters(IEnumerable<KeyValuePair<string, object>> parameters)
	{
		foreach (var parameterKv in parameters)
		{
			_parameters[parameterKv.Key] = parameterKv.Value;
		}

		return this;
	}

	/// <summary>
	/// Adds action description which will be used to report start and an end. Optionally specifies action description serilog template parameters.
	/// </summary>
	/// <param name="actionDescriptionTemplate">The action description message template.</param>
	/// <param name="parameters">The action description message template serilog parameters.</param>
	public Chronograph For(string actionDescriptionTemplate, params object[] parameters)
	{
		_actionDescription = PrepareActionDescription(actionDescriptionTemplate);
		if (parameters != null)
		{
			_actionDescriptionParameters.AddRange(parameters);
		}

		return this;
	}

	/// <summary>
	/// Adds end action report message that will be logged when action completes. Optionally specifies message template serilog parameters.
	/// </summary>
	/// <param name="endMessageTemplate">The end action message template.</param>
	/// <param name="countProviders">The end action message template serilog parameters.</param>
	public Chronograph Report(string endMessageTemplate, params Func<object>[] countProviders)
	{
		_endActionMessageTemplate = endMessageTemplate;
		_countProviders = countProviders;

		return this;
	}

	/// <summary>
	/// Starts the chronograph with specified action description template and optional serilog parameters.
	/// </summary>
	/// <param name="actionDescriptionTemplate">The action description message template.</param>
	/// <param name="parameters">The action description message template serilog parameters.</param>
	public Chronograph Start(string actionDescriptionTemplate, params object[] parameters)
		=> For(actionDescriptionTemplate, parameters).Start();

	/// <summary>
	/// Starts the chronograph.
	/// </summary>
	public Chronograph Start()
	{
		if (_actionDescriptionParameters != null && _actionDescriptionParameters.Any())
		{
			_logger.Write(_eventLevel, $"Started {_actionDescription}.", _actionDescriptionParameters.ToArray());
		}
		else
		{
			_logger.Write(_eventLevel, $"Started {_actionDescription}.");
		}

		_stopwatch.Start();

		return this;
	}

	#endregion

	#region Dispose pattern logic

	/// <summary>
	/// Disposes this chronograph instance, stopping the underlying Stopwatch and reporting action duration and optionally writing down end action message.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Disposes this chronograph instance, stopping the underlying Stopwatch and reporting action duration and optionally writing down end action message.
	/// </summary>
	protected virtual void Dispose(bool disposing)
	{
		if (!_stopwatch.IsRunning)
		{
			_logger.Write(
				ChronographLoggerEventLevel.Warning,
				$"Looks like chronograph for operation {_actionDescription} was not properly initialized or already disposed or stopped. Reported results may be incorrect.");
		}

		_stopwatch.Stop();

		WithParameter("OperationDurationMilliseceonds", _stopwatch.Elapsed.TotalMilliseconds);

		using (new DisposablesWrapper(PushParameters()))
		{
			var elapsedString = _stopwatch.Elapsed.ToString("g");
			var actionDescriptionParameters = _actionDescriptionParameters.ToList(); // defensive copy

			if (_countProviders != null
				&& _countProviders.Length > 0)
			{
				var counts = _countProviders.Select(TryInvokeCountProvider).Where(c => c != null);
				actionDescriptionParameters.AddRange(counts);
			}

			actionDescriptionParameters.Add(elapsedString);

			string finalTemplate = string.IsNullOrWhiteSpace(_endActionMessageTemplate)
				? $"Finished {_actionDescription}. [{{operationDuration}}]"
				: $"Finished {_actionDescription}. {_endActionMessageTemplate}. [{{operationDuration}}]";

			_logger.Write(
				_eventLevel,
				finalTemplate,
				actionDescriptionParameters.ToArray());
		}
	}

	#endregion

	#region Service methods

	private object TryInvokeCountProvider(Func<object> provider)
	{
		try
		{
			return provider?.Invoke();
		}
		catch
		{
			return int.MinValue; // means invoking count provider threw an exception
		}
	}

	private List<IDisposable> PushParameters()
	{
		var ret = new List<IDisposable>();
		if (_parameters.Count != 0)
		{
			foreach (var paramKv in _parameters)
			{
				ret.Add(_logger.PushProperty(paramKv.Key, paramKv.Value));
			}
		}

		return ret;
	}

	private string PrepareActionDescription(string actionDescription)
	{
		if (string.IsNullOrEmpty(actionDescription))
		{
			return string.Empty;
		}

		if (!char.IsUpper(actionDescription[0]))
		{
			return actionDescription;
		}

		StringBuilder prepared = new(actionDescription)
		{
			[0] = char.ToUpper(actionDescription[0])
		};

		return prepared.ToString();
	}

	#endregion
}