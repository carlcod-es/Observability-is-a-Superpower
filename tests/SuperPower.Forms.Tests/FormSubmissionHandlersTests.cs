using System.Reflection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Xunit;

namespace SuperPower.Forms.Tests;

public class FormSubmissionHandlersTests
{
    // IPublishedContent has dozens of members; a DispatchProxy stub throws for any call.
    private class ThrowingProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => throw new NotSupportedException(targetMethod?.Name);
    }

    private static IPublishedContent Content() => DispatchProxy.Create<IPublishedContent, ThrowingProxy>();

    private sealed class FakeHandler(string name, List<string> log, FormPreparation? prep = null) : IFormSubmissionHandler
    {
        public FormPreparation Prepare(IPublishedContent form, IPublishedContent page) => prep ?? FormPreparation.Open;

        public Task OnStoredAsync(FormSubmission submission, IPublishedContent page, CancellationToken cancellationToken)
        {
            log.Add(name);
            return Task.CompletedTask;
        }
    }

    private static FormSubmissionHandlers Sut(params IFormSubmissionHandler[] handlers) => new(handlers);

    [Fact]
    public void No_handlers_is_open()
    {
        var p = Sut().Prepare(Content(), Content());
        Assert.Null(p.ClosedMessage);
        Assert.Empty(p.FixedValues);
    }

    [Fact]
    public void Closed_wins_with_its_message()
    {
        var log = new List<string>();
        var sut = Sut(
            new FakeHandler("a", log, FormPreparation.WithFixedValues(("places", "1"))),
            new FakeHandler("b", log, FormPreparation.Closed("This event has ended")),
            new FakeHandler("c", log, FormPreparation.Closed("later")));
        var p = sut.Prepare(Content(), Content());
        Assert.Equal("This event has ended", p.ClosedMessage);
        Assert.True(p.IsClosed);
    }

    [Fact]
    public void Fixed_values_merge_and_later_handler_wins_on_same_alias()
    {
        var log = new List<string>();
        var sut = Sut(
            new FakeHandler("a", log, FormPreparation.WithFixedValues(("places", "1"), ("x", "a"))),
            new FakeHandler("b", log, FormPreparation.WithFixedValues(("places", "2"), ("y", "b"))),
            new FakeHandler("c", log));
        var p = sut.Prepare(Content(), Content());
        Assert.Null(p.ClosedMessage);
        Assert.Equal("2", p.FixedValues["places"]);
        Assert.Equal("a", p.FixedValues["x"]);
        Assert.Equal("b", p.FixedValues["y"]);
    }

    [Fact]
    public async Task OnStored_calls_every_handler_in_order()
    {
        var log = new List<string>();
        var sut = Sut(new FakeHandler("a", log), new FakeHandler("b", log), new FakeHandler("c", log));
        var submission = new FormSubmission(1, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, new Dictionary<string, string>());
        await sut.OnStoredAsync(submission, Content(), CancellationToken.None);
        Assert.Equal(["a", "b", "c"], log);
    }

    [Fact]
    public async Task OnStored_exception_propagates()
    {
        var sut = Sut(new ThrowingHandler());
        var submission = new FormSubmission(1, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, new Dictionary<string, string>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.OnStoredAsync(submission, Content(), CancellationToken.None));
    }

    private sealed class ThrowingHandler : IFormSubmissionHandler
    {
        public FormPreparation Prepare(IPublishedContent form, IPublishedContent page) => FormPreparation.Open;
        public Task OnStoredAsync(FormSubmission submission, IPublishedContent page, CancellationToken cancellationToken) =>
            throw new InvalidOperationException();
    }
}
