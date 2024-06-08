# Chronograph

`Chronograph` is an instrumentation object which encapsulates `Stopwatch` instance and serilog parametrized messages for operation start and end reporting.

It implements `IDisposable` for convenient operation wrapping. 
When `Dispose` method is called on `Chronograph` instance the internal `Stopwatch` gets stopped and operation duration along with action description and optional end action message gets written to a logger which `Chronograph` was associated with upon creation.

The general pattern of the `Chronograph` output is :

```csharp
[{Time} {Log event level}] Started {action description}
[{Time} {Log event level}] Finished {action description} {optional end action report mesage} [{action duration}]
```

Following is the example output during the simple (without end action message) `Chronograph` lifetime.

```csharp
[16:28:38 INF] Started getting products.
[16:34:03 INF] Finished getting products. [0:05:25.3627059]
```

Where:

* The *action description* is `getting products`
* The *operation duration* is `[0:05:25.3627059]`

Following is the example of the extended `Chronograph` output.

```csharp
[16:28:38 INF] Started getting products with batch size of 100.
[16:34:03 INF] Finished getting products with batch size of 100. Got 65536 products. [0:05:25.3627059]
```

Where:

* The *action description* is `getting products with batch size of {batchSize}`.
* The *action description* template parameter is `100`.
* The *end action report message* is `Got {productsCount} products from`.
* The *end action report message parameter* is `65536`. These parameters got calculated after operation completes.
* The *operation duration* is `[0:05:25.3627059]`

There can be multiple action description and end action report message template parameters.

Action description lowercasing and punctuation is automatic, so there is no need to lower/upper-case action description message or write full stops in message templates.

## Default event level

The default event level for each concrete logger messages can be configured using static specific logger helper class `DefaultChronographEventLevel` property.

For `Microsoft.Extensions.Logging` this helper class is called `MicrosoftExtensionsLoggerHelper`.
For `Serilog` this helper class is called `SerilogLoggerHelper`.

The default event level for all of the concrete loggers is `ChronographLoggerEventLevel.Information`.

## Message enrichment

`Chronograph` enriches all end ('Finished xxx') messages with the following field:

* OperationDurationMilliseceonds - the measured operation duration in milliseconds - for later aggregation convenience;

In addition to default field, `Chronograph` can enrich end message with custom fields. To provide fields to enrich message with use `WithParameter` or `WithParameters` methods.

## Simple `Chronograph` creation

The main functionality of the chronograph is implemented in Dispose method, so it is recommended to use `Chronograph` in `using` statements.
Although it is not always convenient to do so. In this case, call `Dispose` method manually (or use C#8 using declaration syntax when full support of this feature lands).

`Chronograph` class provides `Create` factory method. This method is used to create empty (without any message) non-started chronograph for subsequent fluent configuration methods calls. To start chronograph, call one of the `Start` method overloads.

```csharp
using(Chronograph.Create(Logger).WithEventLevel(LogEventLevel.Information).Start("action description"))
{
    // operation to time
}
```

To shorten the call, use provided `Chrono` extension method on `ILogger` instance.

```csharp
using(Logger.Chrono().Start("action description"))
{
    // operation to time
}
```

The result of the both calls will be as follows (operation duration may vary depending on operation to time).

```csharp
[16:28:38 INF] Started action description.
[16:34:03 INF] Finished action description. [0:05:25.3627059]
```

`Chrono()` method automatically sets the `LogEventLevel` to `Information` (you can edit this settings by modifying `LoggerHelper.ChronographEventLevel` property)

## Specifying parameters

To specify action description parameters, use either of the following.

```csharp
Logger.Chrono()
    .Start(
        "{parametrized} action description with {parameterCount} parameters", 
        "first_parameter_value", 
        2
    )
```

```csharp
Logger.Chrono()
    .For(
        "{parametrized} action description with {parameterCount} parameters", 
        "first_parameter_value", 
        2
    )
    .Start()
```

The result of the both calls will be as follows (operation duration may vary depending on operation to time).

```csharp
[16:28:38 INF] Started first_parameter_value action description with 2 parameters.
[16:34:03 INF] Finished first_parameter_value action description with 2 parameters. [0:03:44.3622359]
```

Parameter values are `params` and optional.

## Specifying end action report message

To specify end action report message, use either of the following.

```csharp
using(Logger.Chrono()
    .For("acton description")
    .Report(
        "Loaded {productsCount} products",
        () => loadedProducts.Count
    )
    .Start())
{
    loadedProducts.AddRange(await LoadProducts());
}
```

```csharp
using(Logger.Chrono()
    .Report(
        "Loaded {productsCount} products",
        () => loadedProducts.Count
    )
    .Start("acton description"))
{
    loadedProducts.AddRange(await LoadProducts());
}
```

The result of the both calls will be as follows (operation duration may vary depending on operation to time).

```csharp
[16:28:38 INF] Started acton description.
[16:34:03 INF] Finished acton description. Loaded 100 products. [0:01:44.3622359]
```

Note that end action report message also uses parameters, but to specify those you need to use lambda expressions.
This is because these parameters got calculated after operation completes and the chronograph instance is created before it.
Using lambda expressions allows us to use closures over some pararmeters that will be changed during the course of teh timed operation, so we can get actual final values of those changed parameters when the operation completes and chronograph message gets rendered.

__Important__: if you use ReSharper, the closure upon changed variable will trigger `AccessToModifiedClosure` diagnostic.
If you suppress this diagnostic for a method, please do watch for other places in your method where you might (unintentionally) use closures over modified values to avoid subtle bugs.

If you do not need to output counts or other dynamically changed (during timed operation) data, simply return a constant literal (this does not trigger ReSharper diagnostic).

You can combine both parametrized versions of end action report and action description.

End action report message parameters are `params` and optional.

## Specifying end action report message on manual chronograph object Dispose()

You can override the end action report message with another one. To do so - manually call `Dispose(string endMessageTemplate, params Func<object>[] countProviders)` on the desired `Chronograph` instance.

```csharp
 var chrono = logger.Chrono()
    .For("Some operation")
    .Report(
        "Teport count={CounterValue}, secondCount={CounterValue2}",
        () => 42,
        () => 1567
    )
    .Start();

// further in the code

chrono.Dispose(
    "Overridden end message with parameter={Parameter}", 
    () => 43
);
```

## Long running operation report

You can set up the chronograph to issue standardised or custom message when the operation is running longer than some set amount of time.

To do so use either of the `WithLongRunningOperationReport` builder method overloads.

```csharp
Chronograph WithLongRunningOperationReport(
    TimeSpan longRunningOperationThreshold, 
    string longRunningOperationReportMessage = null,
    params object[] longRunningOperationReportMessageParameters);

Chronograph WithLongRunningOperationReport(
    TimeSpan longRunningOperationThreshold,
    string longRunningOperationReportMessage = null,
    params Func<object>[] longRunningOperationReportMessageParameterProviders);
```

If you omit the `longRunningOperationReportMessage`, the standardised message will be written.

```csharp
$"{specified action description here} took a long time to finish >({specified long running operation threshold here}) : [{Elapsed:g}]"
```

The second overload `WithLongRunningOperationReport` with `Func<object>[]` formal parameter works the same as the similar `Report` oveload does. It evaluates the functions upon message rendering allowing for closures to report some values that will be changed during the timed opertaion run.

## On start and on end operation actions

Sometimes it is useful to call some code on start and/or on end of the operation chronograph.

To register such action for start operation use `WithOnStartAction(Action<IReadOnlyList<object>> onStartAction)` builder method.
This action has a single argument `IReadOnlyList<object>` that contains all the start action parameters if any.
This action gets called before chronograph writes the start action message.

To register such action for end operation use `WithOnEndAction(Action<Stopwatch, IReadOnlyList<object>> onEndAction)` builder method.
This action has two arguments : an internal `Stopwatch` instance (stopped by the time the action is called) and a `IReadOnlyList<object>` with all of the end action parameters that contain specfied start action parameters, end action parameters and a string, containing elapsed operation time.
This action gets called before chronograph writes the end action message.

```csharp
var chrono = chronograph
    .For("test operation {IntParameter}", 42)
    .Report("Operation result {IntParameter}", () => 1567)
    .WithOnStartAction((parameters) => { /*some code here*/ })
    .WithOnEndAction((sw, parameters) => { /*some code here*/ })
    .Start();
```

## Sampling

For cases when the chronograph wraps a frequently called code, it's beneficial to output only some of the start/end action messages and reports to the logger.
This is where sampling can be handty. To enable message smapling call the `WithSampling(uint samplingFactor, bool shouldAlwaysReportLongRunningOperations = true)` builder method.

This method have two parameters.

* `samplingFactor`, which is a value between 1 and 100 that indicates the rough percentage of the messages to output.
* `shouldAlwaysReportLongRunningOperations`, which indicates whether the sampling factor should appy to long running operation report if it is enabled.

The sampling is implemented by getting a random value between 1 and 100 at the chronograph creation and checking `samplingFactor` against this value.

---

> If you know how to improve this documentation, don't hesitate to suggest any changes by pull request.
