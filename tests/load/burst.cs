// Concurrent form submissions to reproduce the Act 2 duplicate-reference race. Post pattern copied from tests/smoke/smoke.cs.
//   dotnet run burst.cs -- [rounds=5] [perRound=8]
using System.Net;
using System.Text.RegularExpressions;

const string Site = "https://localhost:44368";
var rounds = args.Length > 0 ? int.Parse(args[0]) : 5;
var perRound = args.Length > 1 ? int.Parse(args[1]) : 8;

HttpClient NewClient() => new(new HttpClientHandler
{
    AllowAutoRedirect = false, UseCookies = true, CookieContainer = new CookieContainer(),
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
}) { BaseAddress = new Uri(Site), Timeout = TimeSpan.FromSeconds(30) };
static string Attr(string tag, string name) => WebUtility.HtmlDecode(Regex.Match(tag, name + "=\"([^\"]*)\"").Groups[1].Value);

// Prepare (GET + token) first, then fire all POSTs together so they overlap on the server.
async Task<Func<Task<string>>> Prepare(string url, List<KeyValuePair<string, string>> fields)
{
    var client = NewClient();
    var page = await client.GetStringAsync(url);
    var form = Regex.Match(page, "<form[^>]*>.*?</form>", RegexOptions.Singleline).Value;
    var action = WebUtility.HtmlDecode(Regex.Match(form, "action=\"([^\"]*)\"").Groups[1].Value);
    var all = new List<KeyValuePair<string, string>>(fields);
    foreach (Match m in Regex.Matches(form, "<input[^>]*type=\"hidden\"[^>]*>"))
        all.Add(new(Attr(m.Value, "name"), Attr(m.Value, "value")));
    return async () =>
    {
        var resp = await client.PostAsync(action, new FormUrlEncodedContent(all));
        if ((int)resp.StatusCode != 302) return $"{url} {(int)resp.StatusCode}";
        var after = WebUtility.HtmlDecode(await client.GetStringAsync(resp.Headers.Location));
        var reference = Regex.Match(after, @"ACME-\d+").Value;
        return $"{url} 302 {(reference == "" ? "no reference" : reference)}";
    };
}

var http = NewClient();
string First(string html, string section) => Regex.Match(html, "href=\"(" + section + "[^\"/]+/)\"").Groups[1].Value;
var service = First(await http.GetStringAsync("/services/"), "/services/");
var upcomingEvent = "";
foreach (Match m in Regex.Matches(await http.GetStringAsync("/events/"), "href=\"(/events/[^\"/]+/)\""))
    if ((await http.GetStringAsync(m.Groups[1].Value)).Contains("ufprt")) { upcomingEvent = m.Groups[1].Value; break; }

var targets = new List<(string Url, List<KeyValuePair<string, string>> Fields)>
{
    ("/contact-us/", [new("Name", "Burst Test"), new("Email", "burst@example.com"), new("Message", "Burst contact")]),
    (service, [new("Name", "Burst Test"), new("Email", "burst@example.com"), new("Company", "Burst Ltd"), new("Website", "https://example.com"), new("Message", "Burst enquiry")]),
};
if (upcomingEvent != "") targets.Add((upcomingEvent, [new("Name", "Burst Test"), new("Email", "burst@example.com"), new("Notes", "Burst booking"), new("JoinMailingList", "true")]));

var results = new List<string>();
for (var r = 0; r < rounds; r++)
{
    var posts = await Task.WhenAll(Enumerable.Range(0, perRound).Select(i => Prepare(targets[i % targets.Count].Url, targets[i % targets.Count].Fields)));
    var res = await Task.WhenAll(posts.Select(p => p().ContinueWith(t => t.IsFaulted ? "error " + t.Exception!.GetBaseException().Message : t.Result)));
    Console.WriteLine($"round {r + 1}:"); foreach (var line in res) Console.WriteLine("  " + line);
    results.AddRange(res);
}

var refs = results.Select(l => Regex.Match(l, @"ACME-\d+").Value).Where(x => x != "").ToList();
var dupes = refs.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => $"{g.Key} x{g.Count()}").ToList();
Console.WriteLine($"{results.Count} posts, {refs.Count} with reference, {results.Count - refs.Count} without; duplicates: {(dupes.Count == 0 ? "none" : string.Join(", ", dupes))}");
