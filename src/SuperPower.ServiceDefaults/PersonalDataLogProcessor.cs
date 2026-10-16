using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Microsoft.Extensions.Hosting;

/// <summary>ADR-0028: masks personal form data in log records before any exporter sees them.</summary>
internal sealed class PersonalDataLogProcessor(byte[] key) : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord record)
    {
        if (record.Attributes is null && record.FormattedMessage is null) return;

        var (attributes, message) = LogRedaction.Redact(
            record.Attributes ?? [], record.FormattedMessage, key);

        record.Attributes = attributes.ToList();
        record.FormattedMessage = message;
    }
}
