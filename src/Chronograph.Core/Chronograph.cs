using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Chronograph.Core.Infrastructure;
using Chronograph.Core.Logging;

namespace Chronograph.Core;

/// <summary>
/// Represents chronograph which writes operation timing information to serilog.
/// </summary>
/// <remarks>Uses <c>using()</c> scope Dispose pattern. Uses scope-modified closures to get timed operation results. May require some ReSharper "Access to modified closure" messages suppression attributes or comments.</remarks>
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "MemberCanBeInternal")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public class Chronograph : IDisposable
{
    /// <summary>
    /// The name of the structured logging parameter that contains timed operation run time in milliseconds. 
    /// </summary>
    public const string OperationDurationMillisecondsLoggerParameterName = "OperationDurationMilliseceonds";
    
    /// <summary>
    /// The name of the structured logging parameter that indicates that the timed operation is considered long-running.
    /// </summary>
    public const string IsLongRunningOperationLoggerParameterName = "IsLongRunningOperation";
    
    private readonly Stopwatch _stopwatch;
    private readonly IChronographLogger _logger;
    private readonly Dictionary<string, object> _parameters = new();
    private readonly List<object> _actionDescriptionParameters = [];

    private Action<Stopwatch,  IReadOnlyList<object>> _onEndAction;
    private Action<IReadOnlyList<object>> _onStartAction;

    private ChronographLoggerEventLevel _eventLevel;
    private string _actionDescription;
    private string _endActionMessageTemplate;
    private Func<object>[] _countProviders;

    private readonly Random _random = new (DateTime.Now.Millisecond);
    private bool _shouldWriteMessagesToLogger = true;
    private bool _shouldAlwaysReportLongRunningOperations = true;

    private TimeSpan? _longRunningOperationThreshold;
    private string _longRunningOperationReportMessage;
    private object[] _longRunningOperationReportMessageParameters;
    private Func<object>[] _longRunningOperationReportMessageParameterProviders;

    private bool _wasEverStarted;

    /// <summary>
    /// Gets the total elapsed time measured by the current chronograph instance's stopwatch.
    /// </summary>
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    /// <summary>
    /// Gets the total elapsed time measured by the current chronograph instance's stopwatch, in milliseconds.
    /// </summary>
    public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

    /// <summary>
    /// Gets the total elapsed time measured by the current chronograph instance's stopwatch, in timer ticks.
    /// </summary>
    public long ElapsedTicks => _stopwatch.ElapsedTicks;

    /// <summary>
    /// Gets a value indicating whether the current chronograph instance's stopwatch timer is running.
    /// </summary>
    public bool IsRunning => _stopwatch.IsRunning;

    /// <summary>
    /// Gets the exclusive threshold after which the timed operation considered long-running and reported upon chronograph instance disposal.
    /// <c>null</c> value indicates that long-running operation reporting is not performed.
    /// </summary>
    public TimeSpan? LongRunningOperationThreshold => _longRunningOperationThreshold;

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
    ///		OperationToTime();
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

        _actionDescription = actionDescription.LowercaseFirstChar();

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
    /// using(Log.CreateChronograph("operation description", "got dictionary entries from server {countParameter}", ()=>targetDictionary.Count)){
    ///		targetDictionary = GetDictionaryEntries();
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

    #region Methods for setting up chronograph instance

    /// <summary>
    /// Creates empty and not started chronograph. Used for Chronograph fluent builder methods.
    /// </summary>
    /// <param name="logger">The logger associated with this chronograph instance.</param>
    public static Chronograph Create(IChronographLogger logger) => new(logger);

    /// <summary>
    /// Sets the chronograph <see cref="LongRunningOperationThreshold"/> to the specified value and enables long-running operation reporting.
    /// </summary>
    /// <param name="longRunningOperationThreshold">The exclusive threshold after which the operation is considered to be long-running.</param>
    /// <param name="longRunningOperationReportMessage">The optional long-running operation report message. If not provided, the default message will be used.</param>
    public Chronograph WithLongRunningOperationReport(
        TimeSpan longRunningOperationThreshold,
        string longRunningOperationReportMessage = null)
    {
        _longRunningOperationThreshold = longRunningOperationThreshold;
        _longRunningOperationReportMessage = longRunningOperationReportMessage;

        return this;
    }
    
    /// <summary>
    /// Sets the chronograph <see cref="LongRunningOperationThreshold"/> to the specified value and enables long-running operation reporting.
    /// </summary>
    /// <param name="longRunningOperationThreshold">The exclusive threshold after which the operation is considered to be long-running.</param>
    /// <param name="longRunningOperationReportMessage">The optional long-running operation report message. If not provided, the default message will be used.</param>
    /// <param name="longRunningOperationReportMessageParameters">The optional long-running operation report message parameters.</param>
    public Chronograph WithLongRunningOperationReport(
        TimeSpan longRunningOperationThreshold, 
        string longRunningOperationReportMessage = null,
        params object[] longRunningOperationReportMessageParameters)
    {
        WithLongRunningOperationReport(longRunningOperationThreshold, longRunningOperationReportMessage);
        
        _longRunningOperationReportMessageParameters = longRunningOperationReportMessageParameters;
        
        return this;
    }

    /// <summary>
    /// Sets the chronograph <see cref="LongRunningOperationThreshold"/> to the specified value and enables long-running operation reporting.
    /// </summary>
    /// <param name="longRunningOperationThreshold">The exclusive threshold after which the operation is considered to be long-running.</param>
    /// <param name="longRunningOperationReportMessage">The optional long-running operation report message. If not provided, the default message will be used.</param>
    /// <param name="longRunningOperationReportMessageParameterProviders">The optional long-running operation report message parameter providers.</param>
    public Chronograph WithLongRunningOperationReport(
        TimeSpan longRunningOperationThreshold,
        string longRunningOperationReportMessage = null,
        params Func<object>[] longRunningOperationReportMessageParameterProviders)
    {
        WithLongRunningOperationReport(longRunningOperationThreshold, longRunningOperationReportMessage);

        _longRunningOperationReportMessageParameterProviders = longRunningOperationReportMessageParameterProviders;

        return this;
    }

    /// <summary>
    /// Sets the <see cref="ChronographLoggerEventLevel"/> of the logger to the specified value.
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
    /// <param name="parameters">The parameter dictionary.</param>
    public Chronograph WithParameters(IEnumerable<KeyValuePair<string, object>> parameters)
    {
        foreach (var parameterKv in parameters)
        {
            _parameters[parameterKv.Key] = parameterKv.Value;
        }

        return this;
    }

    /// <summary>
    /// Registers the action to be called on timed operation start.
    /// </summary>
    /// <param name="onStartAction">
    /// The action to be called on internal stopwatch start before writing the start message to the logger.
    /// The action is provided with rendered start action parameters.
    /// </param>
    public Chronograph WithOnStartAction(Action<IReadOnlyList<object>> onStartAction)
    {
        _onStartAction = onStartAction;
        return this;
    }
    
    /// <summary>
    /// Registers the action to be called on timed operation end.
    /// </summary>
    /// <param name="onEndAction">
    /// The action to be called on chronograph instance disposal before writing out the end message to the logger.
    /// The action is provided with internal stopwatch instance and rendered end action parameters.
    /// </param>
    public Chronograph WithOnEndAction(Action<Stopwatch, IReadOnlyList<object>> onEndAction)
    {
        _onEndAction = onEndAction; 
        return this;
    }

    /// <summary>
    /// Add output messages sampling.
    /// </summary>
    /// <param name="samplingFactor">
    /// The sampling factor between 0 and 100.
    /// 0 disables all message writes.
    /// 100 means that every message will be written to the logger.
    /// 50 means that roughly the 50% of messages will be written to the logger.
    /// Any value greater than 100 is interpreted as 100.
    /// </param>
    /// <param name="shouldAlwaysReportLongRunningOperations">
    /// Is set to <c>true</c> - long-running operations will be reported regardless of the <paramref name="samplingFactor"/>.
    /// Is set to <c>false</c> - <paramref name="samplingFactor"/> should also be applied to the long-running operations report.
    /// </param>
    public Chronograph WithSampling(uint samplingFactor, bool shouldAlwaysReportLongRunningOperations = true)
    {
        _shouldWriteMessagesToLogger = samplingFactor switch
        {
            0 => false,
            >= 100 => true,
            _ => _random.Next(1, 101) <= samplingFactor
        };

        _shouldAlwaysReportLongRunningOperations = shouldAlwaysReportLongRunningOperations;

        return this;
    }

    /// <summary>
    /// Adds action description which will be used to report start and an end. Optionally specifies action description serilog template parameters.
    /// </summary>
    /// <param name="actionDescriptionTemplate">The action description message template.</param>
    /// <param name="parameters">The action description message template serilog parameters.</param>
    public Chronograph For(string actionDescriptionTemplate, params object[] parameters)
    {
        _actionDescription = actionDescriptionTemplate.LowercaseFirstChar();

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
    
    #endregion

    #region Methods for controlling chronograph running state
    
    /// <summary>
    /// Starts the chronograph with specified action description template and optional serilog parameters.
    /// </summary>
    /// <param name="actionDescriptionTemplate">The action description message template.</param>
    /// <param name="parameters">The action description message template serilog parameters.</param>
    public Chronograph Start(string actionDescriptionTemplate, params object[] parameters)
        =>
            For(actionDescriptionTemplate, parameters).Start();

    /// <summary>
    /// Starts the chronograph.
    /// </summary>
    public Chronograph Start()
    {
        _wasEverStarted = true;

        _onStartAction?.Invoke(_actionDescriptionParameters);

        if (_shouldWriteMessagesToLogger)
        {
            if (_actionDescriptionParameters is {Count: > 0})
            {
                _logger.Write(
                    _eventLevel,
                    $"Started {_actionDescription}.",
                    _actionDescriptionParameters.ToArray());
            }
            else
            {
                _logger.Write(_eventLevel, $"Started {_actionDescription}.");
            }
        }

        _stopwatch.Start();

        return this;
    }

    /// <summary>
    /// Stops the current chronograph instance's stopwatch.
    /// </summary>
    public void Pause() => _stopwatch.Stop();

    /// <summary>
    /// Resumes the current chronograph instance's stopwatch.
    /// </summary>
    public void Resume() => _stopwatch.Start();
    
    #endregion

    #region Dispose pattern logic

    /// <summary>
    /// Disposes this chronograph instance, stopping the underlying Stopwatch and reporting action duration and writing down the specified end action message.
    /// </summary>
    /// <remarks>
    /// If the end action message was configured by <see cref="Report(string, Func{object}[])"/> method 
    /// or via corresponding ctor, it will be overridden.
    /// If the count providers array was configured by <see cref="Report(string, Func{object}[])"/> method 
    /// or via corresponding ctor, and <paramref name="countProviders"/> parameter specified, it will override previously configured count providers.    
    /// </remarks>
    public void Dispose(string endMessageTemplate, params Func<object>[] countProviders)
    {
        if (_shouldWriteMessagesToLogger)
        {
            if (_endActionMessageTemplate is not null)
            {
                _logger.Write(
                    ChronographLoggerEventLevel.Warning,
                    $"Looks like the end message template for operation '{_actionDescription}' was previously configured to '{_endActionMessageTemplate}', it will be overridden by specified '{endMessageTemplate}' message");
            }

            if (_countProviders is {Length: > 0})
            {
                _logger.Write(
                    ChronographLoggerEventLevel.Warning,
                    $"Looks like the {_countProviders.Length} parameter provider functions for end message template were previously configured, they will be overridden by specified {countProviders.Length} functions");
            }
        }

        _countProviders = countProviders;
        _endActionMessageTemplate = endMessageTemplate;

        Dispose();
    }

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
        try
        {
            _stopwatch.Stop();
            
            if (!_wasEverStarted)
            {
                _logger.Write(
                    ChronographLoggerEventLevel.Warning,
                    $"Looks like chronograph for operation '{_actionDescription}' was not properly initialized or already disposed or stopped. Reported results may be incorrect");
            }

            _actionDescription = _actionDescription.EscapeCurlyBraces();

            string finalTemplate = (string.IsNullOrWhiteSpace(_endActionMessageTemplate)
                ? $"Finished {_actionDescription}. [{{operationDuration}}]"
                : $"Finished {_actionDescription}. {_endActionMessageTemplate}. [{{operationDuration}}]").EscapeCurlyBraces();

            WithParameter(OperationDurationMillisecondsLoggerParameterName, _stopwatch.Elapsed.TotalMilliseconds);

            bool isLongRunningOperation = 
                _longRunningOperationThreshold is { } longRunningOperationThreshold
                && _stopwatch.Elapsed > longRunningOperationThreshold;

            if (isLongRunningOperation)
            {
                WithParameter(IsLongRunningOperationLoggerParameterName, true);
            }

            using (new DisposablesWrapper(PushParameters()))
            {
                var elapsedString = _stopwatch.Elapsed.ToString("g");
                var actionDescriptionParameters = _actionDescriptionParameters.ToList(); // defensive copy

                if (_countProviders is {Length: > 0})
                {
                    var counts = _countProviders
                        .Select(TryInvokeCountProvider)
                        .Where(c => c != null);
                    
                    actionDescriptionParameters.AddRange(counts);
                }
                
                actionDescriptionParameters.Add(elapsedString);

                var actionDescriptionParameterArray = actionDescriptionParameters.ToArray();

                _onEndAction?.Invoke(_stopwatch, actionDescriptionParameterArray);

                if (_shouldWriteMessagesToLogger)
                {
                    _logger.Write(
                        _eventLevel,
                        finalTemplate,
                        actionDescriptionParameterArray);
                }

                if (isLongRunningOperation 
                    && (_shouldAlwaysReportLongRunningOperations || _shouldWriteMessagesToLogger))
                {
                    var longRunningOperationReportTemplate =
                        string.IsNullOrEmpty(_longRunningOperationReportMessage)
                            ? $"{_actionDescription} took a long time to finish >({_longRunningOperationThreshold.Value!}) : [{Elapsed:g}]"
                            : _longRunningOperationReportMessage;
                        
                    _logger.Write(
                        _eventLevel,
                        longRunningOperationReportTemplate,
                        GetLongRunningOperationReportMessageParameters(actionDescriptionParameterArray));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Write(
                ChronographLoggerEventLevel.Error,
                "An exception happened during chronograph disposal. Details: {Exception}",
                ex);
        }
    }

    private object[] GetLongRunningOperationReportMessageParameters(object[] actionDescriptionParameterArray)
    {
        return _longRunningOperationReportMessageParameters is {Length: > 0}
            ? _longRunningOperationReportMessageParameters
            : _longRunningOperationReportMessageParameterProviders is {Length: > 0}
                ? _longRunningOperationReportMessageParameterProviders
                    .Select(TryInvokeCountProvider)
                    .Where(c => c != null).ToArray()
                : actionDescriptionParameterArray;
    }

    #endregion

    #region Service methods

    private object TryInvokeCountProvider(Func<object> provider)
    {
        try
        {
            return provider?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.Write(
                ChronographLoggerEventLevel.Error,
                $"Error happened during the '{_actionDescription}' count provider invocation.{Environment.NewLine}Exception: {ex}");

            return int.MinValue; // means invoking count provider threw an exception
        }
    }

    private List<IDisposable> PushParameters()
    {
        var ret = new List<IDisposable>();

        if (_parameters.Count == 0)
        {
            return ret;
        }

        foreach (var paramKv in _parameters)
        {
            ret.Add(_logger.PushProperty(paramKv.Key, paramKv.Value));
        }

        return ret;
    }

    #endregion
}