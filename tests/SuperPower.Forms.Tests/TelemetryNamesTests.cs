using Xunit;

namespace SuperPower.Forms.Tests;

public class TelemetryNamesTests
{
    [Fact]
    public void ActivitySource_and_Meter_use_package_name()
    {
        Assert.Equal("SuperPower.Forms", SuperPowerFormsTelemetry.ActivitySource.Name);
        Assert.Equal("SuperPower.Forms", SuperPowerFormsTelemetry.Meter.Name);
    }
}
