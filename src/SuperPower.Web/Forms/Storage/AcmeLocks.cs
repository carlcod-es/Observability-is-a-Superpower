namespace SuperPower.Web.Forms.Storage;

/// <summary>Custom <c>umbracoLock</c> row ids (ADR-0026). Umbraco's own ids are negative.</summary>
public static class AcmeLocks
{
    public const int Submissions = 800001;
    public const string Name = "AcmeSite";
}
