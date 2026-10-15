using Xunit;
using SuperPower.Web.Telemetry;

namespace SuperPower.Web.Tests;

public class PageTypeTagTests
{
    [Fact]
    public void Alias_PassesThrough() => Assert.Equal("event", PageTypeTag.From("event"));

    [Fact]
    public void Null_IsNone() => Assert.Equal("none", PageTypeTag.From(null));

    [Fact]
    public void Empty_IsNone() => Assert.Equal("none", PageTypeTag.From(""));
}
