// Demo traffic source (add-demo-dashboard task 3.1, ADR-0012). .NET 10 file-based app.
//
//   dotnet run tests/traffic/traffic.cs [-- <site-url>]     default https://localhost:44368
//
// GET only, never posts a form. Start after go-live. Stop with Ctrl+C.
// Fetches Home, Events list and Services list, collects their same-origin page links, then requests
// them round-robin at about 1 request/s. Prints "requests=<n> failures=<n>" every minute
// (failure = non-2xx response or exception).
using System.Text.RegularExpressions;

var site = new Uri(args.FirstOrDefault() ?? "https://localhost:44368");
using var client = new HttpClient(new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true, // dev certificate
}) { BaseAddress = site, Timeout = TimeSpan.FromSeconds(30) };

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var pages = new List<string> { "/", "/events/", "/services/" };
foreach (var start in pages.ToList())
{
    try
    {
        var html = await client.GetStringAsync(start, cts.Token);
        foreach (Match m in Regex.Matches(html, "href=\"([^\"]*)\""))
        {
            var href = System.Net.WebUtility.HtmlDecode(m.Groups[1].Value);
            if (!Uri.TryCreate(site, href, out var uri) || uri.Host != site.Host || uri.Port != site.Port) continue;
            var path = uri.AbsolutePath;
            if (href.Contains('#') || href.Contains('?') || path.StartsWith("/umbraco", StringComparison.OrdinalIgnoreCase)
                || Path.HasExtension(path) || pages.Contains(path)) continue;
            pages.Add(path);
        }
    }
    catch (Exception e) when (!cts.IsCancellationRequested)
    {
        Console.WriteLine($"Could not read {start}: {e.Message}");
    }
}

Console.WriteLine($"Traffic to {site}, {pages.Count} pages: {string.Join(' ', pages)}");

int requests = 0, failures = 0, i = 0;
var minute = DateTime.UtcNow;
try
{
    while (!cts.IsCancellationRequested)
    {
        requests++;
        try
        {
            using var resp = await client.GetAsync(pages[i++ % pages.Count], cts.Token);
            if (!resp.IsSuccessStatusCode) failures++;
        }
        catch (Exception) when (!cts.IsCancellationRequested) { failures++; }

        if (DateTime.UtcNow - minute >= TimeSpan.FromMinutes(1))
        {
            Console.WriteLine($"requests={requests} failures={failures}");
            requests = failures = 0;
            minute = DateTime.UtcNow;
        }
        await Task.Delay(1000, cts.Token);
    }
}
catch (OperationCanceledException) { }
Console.WriteLine($"Stopped. requests={requests} failures={failures}");
