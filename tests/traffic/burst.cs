// Submission burst tool (add-form-emails task 4.1, design D6, ADR-0012). .NET 10 file-based app.
//
//   dotnet run tests/traffic/burst.cs [-- <site-url>] [--count 50] [--parallel 10]
//
// Default site https://localhost:44368 (the local site). Flags and the URL can come in any order.
// Posts REAL form submissions with made-up data (Burst Visitor <n>, burst<n>@example.com) round-robin to the
// Contact form, one Service enquiry form and one upcoming Event booking form. Submissions are stored and every one
// sends real notification emails, which land in the local mail catcher (smtp4dev). Never point it at anything else.
// Each submission uses its own HttpClient and cookie container: GET the page, read the form's action and hidden inputs,
// POST. 302 = success (the redirect is followed once to read the reference ACME-<n>); anything else = failure.
// Prints "ok=<n> failed=<n>", the references, the distinct reference count and each failure. Exit code 1 if any failure.
using System.Net;
using System.Text.RegularExpressions;

var site = "https://localhost:44368";
int count = 50, parallel = 10;
for (var a = 0; a < args.Length; a++)
{
    if (args[a] == "--count" && a + 1 < args.Length && int.TryParse(args[a + 1], out count) && count > 0) a++;
    else if (args[a] == "--parallel" && a + 1 < args.Length && int.TryParse(args[a + 1], out parallel) && parallel > 0) a++;
    else if (!args[a].StartsWith("--")) site = args[a];
    else
    {
        Console.WriteLine($"Unknown, incomplete or invalid argument {args[a]}");
        Console.WriteLine("Usage: dotnet run tests/traffic/burst.cs [-- <site-url>] [--count <n>] [--parallel <n>]  (n > 0)");
        return 2;
    }
}

HttpClient NewClient() => new(new HttpClientHandler
{
    AllowAutoRedirect = false,
    UseCookies = true,
    CookieContainer = new CookieContainer(),
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true, // dev certificate
}) { BaseAddress = new Uri(site), Timeout = TimeSpan.FromSeconds(60) };

// Page discovery, as the smoke check does it.
string service = "", upcomingEvent = "";
const string contact = "/contact-us/";
try
{
    using var http = NewClient();
    var contactHtml = await http.GetStringAsync(contact);
    if (!contactHtml.Contains("ufprt")) throw new Exception($"{contact} has no form");

    var serviceLinks = Regex.Matches(await http.GetStringAsync("/services/"), "href=\"(/services/[^\"/]+/)\"");
    service = serviceLinks.Select(m => m.Groups[1].Value).FirstOrDefault() ?? "";

    foreach (Match m in Regex.Matches(await http.GetStringAsync("/events/"), "href=\"(/events/[^\"/]+/)\""))
    {
        if ((await http.GetStringAsync(m.Groups[1].Value)).Contains("ufprt")) { upcomingEvent = m.Groups[1].Value; break; }
    }
}
catch (Exception e)
{
    Console.WriteLine($"Discovery failed on {site}: {e.Message}");
    return 1;
}
if (service == "") { Console.WriteLine("No service page found under /services/."); return 1; }
if (upcomingEvent == "") { Console.WriteLine("No upcoming event page with a form found under /events/."); return 1; }

string[] forms = ["Contact", "Service enquiry", "Event booking"];
string[] urls = [contact, service, upcomingEvent];
Console.WriteLine($"Burst to {site}: {count} submissions, {parallel} in parallel; {string.Join(' ', urls)}");

var references = new string[count];
var failures = new List<string>();
var okCount = 0;

await Parallel.ForEachAsync(Enumerable.Range(1, count), new ParallelOptions { MaxDegreeOfParallelism = parallel }, async (n, ct) =>
{
    var kind = (n - 1) % 3;
    try
    {
        using var client = NewClient();
        var page = await client.GetStringAsync(urls[kind], ct);
        var form = Regex.Matches(page, "<form[^>]*>.*?</form>", RegexOptions.Singleline).Select(m => m.Value).FirstOrDefault(f => f.Contains("ufprt"))
            ?? throw new Exception($"no form with ufprt on {urls[kind]}");
        var action = WebUtility.HtmlDecode(Regex.Match(form, "action=\"([^\"]*)\"").Groups[1].Value);

        var fields = new List<KeyValuePair<string, string>> { new("Name", $"Burst Visitor {n}"), new("Email", $"burst{n}@example.com") };
        switch (kind)
        {
            case 0:
                fields.Add(new("Message", $"Burst message {n}"));
                break;
            case 1:
                fields.Add(new("Company", $"Burst Ltd {n}"));
                fields.Add(new("Website", "https://example.com"));
                fields.Add(new("Message", $"Burst enquiry {n}"));
                break;
            default:
                fields.Add(new("Notes", $"Burst booking {n}"));
                fields.Add(new("JoinMailingList", "true"));
                break;
        }
        foreach (Match m in Regex.Matches(form, "<input[^>]*type=\"hidden\"[^>]*>")) // after the visible fields, like a browser
            fields.Add(new(Attr(m.Value, "name"), Attr(m.Value, "value")));

        using var resp = await client.PostAsync(action, new FormUrlEncodedContent(fields), ct);
        if (resp.StatusCode != HttpStatusCode.Found)
        {
            lock (failures) failures.Add($"{forms[kind]} #{n}: HTTP {(int)resp.StatusCode}");
            return;
        }
        var location = resp.Headers.Location!.ToString();
        var after = await client.GetStringAsync(location, ct);
        var reference = Regex.Match(after, @"ACME-\d+");
        if (!reference.Success)
        {
            lock (failures) failures.Add($"{forms[kind]} #{n}: no reference (ACME-<n>) on the redirect page");
            return;
        }
        references[n - 1] = reference.Value;
        Interlocked.Increment(ref okCount);
    }
    catch (Exception e)
    {
        lock (failures) failures.Add($"{forms[kind]} #{n}: {e.Message}");
    }
});

var refs = references.Where(r => r is not null).ToList();
Console.WriteLine($"ok={okCount} failed={failures.Count}");
Console.WriteLine("References: " + string.Join(' ', refs));
Console.WriteLine($"Distinct references: {refs.Distinct().Count()}");
foreach (var f in failures) Console.WriteLine("FAILED " + f);
return failures.Count == 0 ? 0 : 1;

static string Attr(string tag, string name) => WebUtility.HtmlDecode(Regex.Match(tag, name + "=\"([^\"]*)\"").Groups[1].Value);
