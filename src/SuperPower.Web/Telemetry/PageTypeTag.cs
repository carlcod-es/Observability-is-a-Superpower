namespace SuperPower.Web.Telemetry;

/// <summary>ADR-0024: page-type label added to HTTP server metrics and spans.</summary>
public static class PageTypeTag
{
    public const string Name = "acme.page_type";
    public const string None = "none";

    public static string From(string? alias) => string.IsNullOrEmpty(alias) ? None : alias;
}
