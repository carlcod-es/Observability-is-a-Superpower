using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using SuperPower.Web.Events;
using Xunit;

namespace SuperPower.Web.Tests;

public class BookingEventsTests
{
    private sealed class Provider(Func<object> resolve) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == typeof(IConnection) ? resolve() : null;
    }

    private static RabbitBookingEvents Events(Func<object> resolve) =>
        new(new Provider(resolve), NullLogger<RabbitBookingEvents>.Instance);

    [Fact]
    public async Task ThrowingConnection_DoesNotThrow()
    {
        var watch = Stopwatch.StartNew();
        await Events(() => throw new InvalidOperationException("broker down")).PublishStoredAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task ThrowingConnection_RecordsErrorPublishSpan()
    {
        var stopped = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "Acme.Forms",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => { lock (stopped) stopped.Add(a); }
        };
        ActivitySource.AddActivityListener(listener);

        await Events(() => throw new InvalidOperationException("broker down")).PublishStoredAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        var span = Assert.Single(stopped, a => a.OperationName == "acme.forms.publish");
        Assert.Equal(ActivityStatusCode.Error, span.Status);
    }

    [Fact]
    public async Task BlockingConnection_ReturnsWithinCap()
    {
        var gate = new ManualResetEventSlim();
        var watch = Stopwatch.StartNew();
        await Events(() => { gate.Wait(TimeSpan.FromSeconds(30)); return null!; }).PublishStoredAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        gate.Set();
        Assert.InRange(watch.Elapsed, TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(3.5));
    }

    [Fact]
    public async Task NoBookingEvents_Completes() => await new NoBookingEvents().PublishStoredAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
}
