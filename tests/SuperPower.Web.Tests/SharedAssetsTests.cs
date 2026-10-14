using Xunit;

namespace SuperPower.Web.Tests;

// ADR-0020: the prototype is the source of the site's stylesheet and script.
public class SharedAssetsTests
{
    [Theory]
    [InlineData("css/site.css")]
    [InlineData("js/site.js")]
    public void SiteAssetMatchesPrototype(string asset)
    {
        var root = FindRepoRoot();
        var prototype = $"prototype/src/{asset}";
        var site = $"src/SuperPower.Web/wwwroot/{asset}";

        var prototypeText = Read(root, prototype);
        var siteText = Read(root, site);

        Assert.True(prototypeText == siteText,
            $"{site} differs from {prototype}; edit prototype/src and run `npm run sync` in prototype/");
    }

    private static string Read(string root, string relativePath)
    {
        var path = Path.Combine(root, Path.Combine(relativePath.Split('/')));
        Assert.True(File.Exists(path), $"{relativePath} is missing");
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SuperPower.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("SuperPower.slnx not found above " + AppContext.BaseDirectory);
    }
}
