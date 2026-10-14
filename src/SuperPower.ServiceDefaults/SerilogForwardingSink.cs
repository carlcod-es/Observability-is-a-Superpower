using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// ADR-0007: forwards Serilog events (Umbraco owns logging) to the ILoggerProviders registered by
/// ServiceDefaults (OpenTelemetry). Exporter-agnostic. Runs synchronously so Activity.Current is intact.
/// </summary>
internal sealed class SerilogForwardingSink(IServiceProvider services) : ILogEventSink
{
    private readonly Dictionary<string, ILogger[]> _loggers = new();
    private ILoggerProvider[]? _providers;

    [ThreadStatic] private static bool _forwarding;

    public void Emit(LogEvent logEvent)
    {
        if (_forwarding) return;
        _forwarding = true;
        try
        {
            // Same paths the trace filter excludes: their traces are never exported, so drop the logs too.
            if (logEvent.Properties.TryGetValue("RequestPath", out var rp)
                && rp is ScalarValue { Value: string requestPath }
                && TelemetryPaths.IsExcluded(requestPath))
                return;

            _providers ??= services.GetServices<ILoggerProvider>().OfType<OpenTelemetryLoggerProvider>()
                .Cast<ILoggerProvider>().ToArray();
            if (_providers.Length == 0) return;

            var category = logEvent.Properties.TryGetValue("SourceContext", out var sc) && sc is ScalarValue { Value: string s}
                ? s : "Serilog";

            ILogger[] loggers;
            lock (_loggers)
            {
                if (!_loggers.TryGetValue(category, out loggers!))
                    _loggers[category] = loggers = _providers.Select(p => p.CreateLogger(category)).ToArray();
            }

            var level = logEvent.Level switch
            {
                LogEventLevel.Verbose => MsLogLevel.Trace,
                LogEventLevel.Debug => MsLogLevel.Debug,
                LogEventLevel.Information => MsLogLevel.Information,
                LogEventLevel.Warning => MsLogLevel.Warning,
                LogEventLevel.Error => MsLogLevel.Error,
                _ => MsLogLevel.Critical,
            };

            var state = new LogValues(logEvent);
            foreach (var logger in loggers)
                logger.Log(level, default, state, logEvent.Exception, static (st, _) => st.ToString());
        }
        finally
        {
            _forwarding = false;
        }
    }

    // Template properties become separate attributes; {OriginalFormat} keeps the template.
    private sealed class LogValues(LogEvent e) : IReadOnlyList<KeyValuePair<string, object?>>
    {
        private readonly List<KeyValuePair<string, object?>> _items = Build(e);

        private static List<KeyValuePair<string, object?>> Build(LogEvent e)
        {
            var list = new List<KeyValuePair<string, object?>>();
            foreach (var (name, value) in e.Properties)
            {
                if (name == "SourceContext") continue;
                list.Add(new(name, Convert(value)));
            }
            list.Add(new("{OriginalFormat}", e.MessageTemplate.Text));
            return list;
        }

        private static object? Convert(LogEventPropertyValue v) => v switch
        {
            ScalarValue sv => sv.Value,
            _ => v.ToString(),
        };

        // Serilog's RenderMessage double-quotes strings; render tokens with raw strings instead.
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var token in e.MessageTemplate.Tokens)
            {
                if (token is not PropertyToken pt) { sb.Append(token.ToString()); continue; }
                if (!e.Properties.TryGetValue(pt.PropertyName, out var value)) { sb.Append(pt.ToString()); continue; }
                switch (value)
                {
                    case ScalarValue { Value: string str }: sb.Append(str); break;
                    case ScalarValue { Value: IFormattable f } when pt.Format is not null:
                        sb.Append(f.ToString(pt.Format, System.Globalization.CultureInfo.InvariantCulture)); break;
                    default: sb.Append(value.ToString()); break;
                }
            }
            return sb.ToString();
        }
        public int Count => _items.Count;
        public KeyValuePair<string, object?> this[int index] => _items[index];
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _items.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
