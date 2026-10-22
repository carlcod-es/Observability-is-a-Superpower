using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using SuperPower.Web.Events;
using Xunit;

namespace SuperPower.Web.Tests;

public class BookingCountsTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastUri = request.RequestUri;
            return Task.FromResult(respond(request));
        }
    }

    private static BookingCountsClient Client(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://api") }, NullLogger<BookingCountsClient>.Instance);

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [Theory]
    [InlineData(null, null)]
    [InlineData(0, null)]
    [InlineData(1, "1 person has booked this event")]
    [InlineData(3, "3 people have booked this event")]
    public void Text(int? count, string? expected) => Assert.Equal(expected, BookingCountText.Format(count));

    [Fact]
    public async Task Ok_ParsesCount()
    {
        var handler = new StubHandler(_ => Json("{\"count\":3}"));
        Assert.Equal(3, await Client(handler).GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ServerError_ReturnsNull() =>
        Assert.Null(await Client(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError))).GetAsync(Guid.NewGuid()));

    [Fact]
    public async Task ThrowingHandler_ReturnsNull() =>
        Assert.Null(await Client(new StubHandler(_ => throw new HttpRequestException("down"))).GetAsync(Guid.NewGuid()));

    [Fact]
    public async Task EventKey_InRequestPath()
    {
        var key = Guid.NewGuid();
        var handler = new StubHandler(_ => Json("{\"count\":1}"));
        await Client(handler).GetAsync(key);
        Assert.Equal($"/events/{key}/bookings", handler.LastUri!.AbsolutePath);
    }

    [Fact]
    public async Task NoBookingCounts_ReturnsNull() => Assert.Null(await new NoBookingCounts().GetAsync(Guid.NewGuid()));
}
