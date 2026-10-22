// End-to-end smoke check (add-demo-foundation task 9.4, ADR-0012). .NET 10 file-based app.
//
//   dotnet run tests/smoke/smoke.cs                 target "apphost": AppHost freshly started (aspire run), SQL Server, `lgtm` not started;
//                                                   run it before any page is loaded (its first request is the first Home load)
//   dotnet run tests/smoke/smoke.cs -- standalone   target "standalone": `dotnet run --project src/SuperPower.Web`, SQLite
//
// Run from inside the repo. Exit code is non-zero if any check FAILs. Submissions are fake and never deleted.
// apphost: SQL Server login comes from the AppHost user secrets (Parameters:sql-password, Resources:sql:tcp:port);
//   override with env SMOKE_SQL_CONNECTION. smtp4dev is reached through the `aspire` CLI (web UI URL) and its HTTP API.
//   Telemetry is read through Grafana (http://localhost:3000, admin/admin), the same path the agent uses.
//   Grafana (`lgtm`) is on demand (ADR-0022): the check requests the unknown page /smoke-pre/?smoke-pre=<marker>
//   (404 but logged; a real page such as About us warms the content cache and hides the Home DB spans), starts `lgtm` via the
//   aspire CLI, waits for Grafana health and grafana-mcp (port 8091), then runs the checks, including "no pre-start
//   telemetry in Grafana". If `lgtm` is already running the pre-check is reported "not applicable".
//   add-demo-dashboard (design D6): dashboards provisioned (/api/search: demo-site, 19924), acme.page_type on the Home metric (Prometheus),
//   Home span and service-enquiry post span (Tempo), and /assets/rubber-duck.svg excluded from tracing.
//   These checks read Grafana, so `lgtm` must be running (the telemetry section starts it).
// standalone: SQLite file src/SuperPower.Web/umbraco/Data/Umbraco.sqlite.db, opened read-only. Pages and forms only.
//   add-form-emails task 5.1: the success page shows "Your reference is ACME-nnnn" matching the stored ReferenceNumber (both targets);
//   apphost: smtp4dev holds the owner notification (to owner@acme-agency.example, subject ending in that reference) for each valid submission.
//   standalone sends no email.
//   add-booking-api (design D10): apphost: booking count on the first upcoming event page rises by one within 5 s of a booking, Tempo has
//   `api` spans with acme.event.key and a booking trace with `site` acme.forms.submit and `api` deliver booking-stored (with acme.event.key) (needs `lgtm`); standalone: no event-bookings element,
//   a booking still returns a reference.
// Not scripted (PO 2026-10-14): Event booking post to a past event; check it manually (verification.md).
// Passing both targets also proves the raw FieldsJson is identical on SQL Server and SQLite.
#:package Microsoft.Data.SqlClient
#:package Microsoft.Data.Sqlite

using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;

const string Site = "https://localhost:44368";
const string Grafana = "http://localhost:3000";
const string Success = "Thank you, we'll be in touch. Your reference is";
var submitted = new List<(string Label, string Reference)>(); // valid submissions, for the notification email check
// SuperPower.Web.Forms.AcmeFormKeys
var contactKey = Guid.Parse("a1c0f3d2-5b7e-4c81-9a2d-0e6f4b8c1a01");
var serviceKey = Guid.Parse("a1c0f3d2-5b7e-4c81-9a2d-0e6f4b8c1a02");
var eventKey = Guid.Parse("a1c0f3d2-5b7e-4c81-9a2d-0e6f4b8c1a03");
// Raw stored JSON for the fixed Contact values below: quote (backslash-escaped), non-ASCII (stored unescaped), newline (backslash n).
const string ExpectedContactJson = "{\"Name\":\"Smoke Test\",\"Email\":\"smoke@example.com\",\"Message\":\"Line one: \\\"quoted\\\" caf\u00e9 \u00fc\\nLine two\"}";

var standalone = args.Contains("standalone");
var failures = 0;
void Report(bool ok, string name, string detail)
{
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {name} - {detail}");
    if (!ok) failures++;
}

var root = Directory.GetCurrentDirectory();
while (!File.Exists(Path.Combine(root, "SuperPower.slnx")))
    root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("Run from inside the repo.");
var appHostProject = Path.Combine(root, "src", "SuperPower.AppHost", "SuperPower.AppHost.csproj");

Console.WriteLine($"Target: {(standalone ? "standalone (SQLite)" : "apphost (SQL Server)")}, site {Site}");

DbConnection OpenDb()
{
    if (standalone)
    {
        var path = Path.Combine(root, "src", "SuperPower.Web", "umbraco", "Data", "Umbraco.sqlite.db");
        var c = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
        c.Open();
        return c;
    }
    var cs = Environment.GetEnvironmentVariable("SMOKE_SQL_CONNECTION");
    if (string.IsNullOrEmpty(cs))
    {
        var secrets = Run("dotnet", "user-secrets", "list", "--project", Path.Combine(root, "src", "SuperPower.AppHost"))
            .Split('\n').Select(l => l.Split(" = ", 2)).Where(p => p.Length == 2).ToDictionary(p => p[0].Trim(), p => p[1].Trim());
        cs = $"Server=127.0.0.1,{secrets["Resources:sql:tcp:port"]};User Id=sa;Password={secrets["Parameters:sql-password"]};Database=umbraco;TrustServerCertificate=True";
    }
    var s = new SqlConnection(cs);
    s.Open();
    return s;
}

// Returns the rows of acmeFormSubmission. Tiny table; filtering happens here to keep the SQL identical on both databases.
List<Row> ReadRows()
{
    using var c = OpenDb();
    using var cmd = c.CreateCommand();
    cmd.CommandText = "SELECT Id, FormKey, PageKey, SubmittedUtc, FieldsJson, ReferenceNumber FROM acmeFormSubmission ORDER BY Id";
    using var r = cmd.ExecuteReader();
    var rows = new List<Row>();
    while (r.Read())
    {
        var ts = r.GetValue(3);
        var utc = ts is DateTime dt ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            : DateTime.Parse(ts.ToString()!, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        rows.Add(new Row(Convert.ToInt32(r.GetValue(0)), Guid.Parse(r.GetValue(1).ToString()!), Guid.Parse(r.GetValue(2).ToString()!), utc, r.GetString(4), Convert.ToInt32(r.GetValue(5))));
    }
    return rows;
}

Guid PageKey(string url)
{
    var segment = url.Trim('/').Split('/').Last();
    using var c = OpenDb();
    using var cmd = c.CreateCommand();
    cmd.CommandText = "SELECT DISTINCT d.uniqueId FROM umbracoDocumentUrl d JOIN umbracoNode n ON n.uniqueId = d.uniqueId WHERE d.isDraft = 0 AND n.trashed = 0 AND d.urlSegment = @s"; // skips recycle-bin leftovers
    var p = cmd.CreateParameter(); p.ParameterName = "@s"; p.Value = segment; cmd.Parameters.Add(p);
    return Guid.Parse(cmd.ExecuteScalar()!.ToString()!);
}

HttpClient NewClient() => new(new HttpClientHandler
{
    AllowAutoRedirect = false,
    UseCookies = true,
    CookieContainer = new CookieContainer(),
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true, // dev certificate
}) { BaseAddress = new Uri(Site), Timeout = TimeSpan.FromSeconds(30) };

// GET the page, read the form's hidden fields (antiforgery token, ufprt, checkbox false-twin), POST visible + hidden fields.
async Task<(int Status, string? Location, string Body, HttpClient Client)> Post(string url, List<KeyValuePair<string, string>> fields)
{
    var client = NewClient();
    var page = await client.GetStringAsync(url);
    var form = Regex.Match(page, "<form[^>]*>.*?</form>", RegexOptions.Singleline).Value;
    var action = WebUtility.HtmlDecode(Regex.Match(form, "action=\"([^\"]*)\"").Groups[1].Value);
    var all = new List<KeyValuePair<string, string>>(fields);
    foreach (Match m in Regex.Matches(form, "<input[^>]*type=\"hidden\"[^>]*>"))
        all.Add(new(Attr(m.Value, "name"), Attr(m.Value, "value"))); // after the visible fields, like a browser
    var resp = await client.PostAsync(action, new FormUrlEncodedContent(all));
    return ((int)resp.StatusCode, resp.Headers.Location?.ToString(), await resp.Content.ReadAsStringAsync(), client);
}
static string Attr(string tag, string name) => WebUtility.HtmlDecode(Regex.Match(tag, name + "=\"([^\"]*)\"").Groups[1].Value);

// Valid post expectations: 302, success text after the redirect, exactly one new row with the expected keys and fields.
async Task ValidSubmission(string label, string url, Guid formKey, List<KeyValuePair<string, string>> fields, string? expectedJson)
{
    try
    {
        var before = ReadRows().Select(r => r.Id).DefaultIfEmpty(0).Max();
        var (status, location, _, client) = await Post(url, fields);
        var after = status == 302 ? WebUtility.HtmlDecode(await client.GetStringAsync(location)) : "";
        var added = ReadRows().Where(r => r.Id > before).ToList();
        var row = added.FirstOrDefault();
        var pageKey = PageKey(url);
        var problems = new List<string>();
        if (status != 302) problems.Add($"status {status}");
        if (!after.Contains(Success)) problems.Add("no success text");
        var reference = Regex.Match(after, @"ACME-\d+").Value;
        if (added.Count != 1) problems.Add($"{added.Count} new rows");
        if (row is not null)
        {
            var expectedRef = $"ACME-{row.ReferenceNumber:D4}";
            if (reference != expectedRef) problems.Add($"reference on page '{reference}' != stored {expectedRef}");
            else submitted.Add((label, reference));
            if (row.FormKey != formKey) problems.Add($"FormKey {row.FormKey}");
            if (row.PageKey != pageKey) problems.Add($"PageKey {row.PageKey} != {pageKey}");
            if (Math.Abs((DateTime.UtcNow - row.SubmittedUtc).TotalMinutes) > 5) problems.Add($"SubmittedUtc {row.SubmittedUtc:O}");
            if (expectedJson is not null && !Encoding.UTF8.GetBytes(row.FieldsJson).SequenceEqual(Encoding.UTF8.GetBytes(expectedJson)))
                problems.Add($"FieldsJson differs: {row.FieldsJson}");
            if (expectedJson is null && !fields.All(f => JsonDocument.Parse(row.FieldsJson).RootElement.TryGetProperty(f.Key, out var v) && v.GetString() == f.Value))
                problems.Add($"FieldsJson lacks posted values: {row.FieldsJson}");
        }
        Report(problems.Count == 0, label, problems.Count == 0 ? $"302, success text, reference {reference}, row {row!.Id} {row.SubmittedUtc:HH:mm:ss}Z page {pageKey}" : string.Join("; ", problems));
    }
    catch (Exception e) { Report(false, label, e.Message); }
}

// ---- 0. first request of the run: Home with a unique marker. On a freshly started AppHost this is the first Home load
// (later loads are served from Umbraco's cache without database calls); TelemetryCheck inspects its trace.
var marker = Guid.NewGuid().ToString("N");
var homeSince = DateTime.UtcNow.AddSeconds(-5);
var preMarker = Guid.NewGuid().ToString("N");
var preStart = false; // true when this run requested a page before lgtm was started
if (!standalone)
{
    try
    {
        var lgtmJson = Run("aspire", "describe", "lgtm", "--apphost", appHostProject, "--non-interactive", "--nologo", "--format", "json");
        var lgtmState = JsonDocument.Parse(lgtmJson[lgtmJson.IndexOf('{')..]).RootElement.GetProperty("resources")[0].GetProperty("state").GetString();
        if (lgtmState == "NotStarted")
        {
            await NewClient().GetAsync("/smoke-pre/?smoke-pre=" + preMarker);
            Run("aspire", "resource", "lgtm", "start", "--apphost", appHostProject, "--non-interactive", "--nologo");
            preStart = true;
            homeSince = DateTime.UtcNow.AddSeconds(-5);
            using var probe = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var ready = false;
            for (var i = 0; i < 60 && !ready; i++)
            {
                try
                {
                    ready = (await probe.GetAsync(Grafana + "/api/health")).IsSuccessStatusCode;
                    if (ready) { using var tcp = new TcpClient(); await tcp.ConnectAsync("localhost", 8091); }
                }
                catch { ready = false; }
                if (!ready) await Task.Delay(2000);
            }
            Console.WriteLine(ready ? "lgtm started: Grafana healthy, grafana-mcp port 8091 open" : "note: lgtm started but Grafana / grafana-mcp not ready after 120 s");
        }
        else Console.WriteLine($"note: lgtm is {lgtmState}, not started by this run");
    }
    catch (Exception e) { Console.WriteLine($"note: lgtm pre-start failed ({e.Message})"); }
    try { await NewClient().GetAsync("/?smoke=" + marker); }
    catch (Exception e) { Console.WriteLine($"note: first Home request failed ({e.Message})"); }
}

// ---- 1. pages
var http = NewClient();
var urls = new SortedSet<string> { "/", "/about-us/", "/events/", "/services/", "/contact-us/" };
string upcomingEvent = "", service = "";
try
{
    foreach (var section in new[] { "/events/", "/services/" })
        foreach (Match m in Regex.Matches(await http.GetStringAsync(section), "href=\"(" + section + "[^\"/]+/)\""))
            urls.Add(m.Groups[1].Value);
    var bad = new List<string>();
    foreach (var u in urls)
    {
        var resp = await http.GetAsync(u);
        if (resp.StatusCode != HttpStatusCode.OK) bad.Add($"{u} {(int)resp.StatusCode}");
        else if (u.StartsWith("/events/") && u != "/events/" && upcomingEvent == "" && (await resp.Content.ReadAsStringAsync()).Contains("ufprt")) upcomingEvent = u;
        else if (u.StartsWith("/services/") && u != "/services/" && service == "") service = u;
    }
    Report(bad.Count == 0, "pages 200", bad.Count == 0 ? $"{urls.Count} pages" : string.Join(", ", bad));
}
catch (Exception e) { Report(false, "pages 200", e.Message); }

// ---- 2. valid submissions
await ValidSubmission("contact submission", "/contact-us/", contactKey, [
    new("Name", "Smoke Test"), new("Email", "smoke@example.com"),
    new("Message", "Line one: \"quoted\" café ü\nLine two")], ExpectedContactJson);
if (!standalone)
{
    await ValidSubmission("service enquiry submission", service, serviceKey, [
        new("Name", "Smoke Test"), new("Email", "smoke@example.com"), new("Company", "Smoke Ltd"),
        new("Website", "https://example.com"), new("Message", "Smoke test enquiry")], null);
    await ValidSubmission("event booking submission", upcomingEvent, eventKey, [
        new("Name", "Smoke Test"), new("Email", "smoke@example.com"), new("Notes", "Smoke test booking"), new("JoinMailingList", "true")], null);
}

// ---- 3. invalid submission
try
{
    var before = ReadRows().Count;
    var (status, _, body, _) = await Post("/contact-us/", [new("Name", ""), new("Email", "not-an-email"), new("Message", "x")]);
    var errors = Regex.Matches(body, "field-validation-error[^>]*>[^<]+<").Count;
    var stored = ReadRows().Count - before;
    Report(status == 200 && errors >= 2 && stored == 0, "invalid submission", $"status {status}, {errors} field errors, {stored} new rows");
}
catch (Exception e) { Report(false, "invalid submission", e.Message); }

if (!standalone)
{
    await TelemetryCheck();
    await DashboardCheck();
    await NotificationCheck();
    await BookingCheck();
}
else
{
    // add-booking-api: without api, cache and queue the page shows no count and booking still works.
    try
    {
        var page = await http.GetStringAsync(upcomingEvent);
        Report(!page.Contains("event-bookings"), "standalone: no booking count", "event page has no event-bookings element");
    }
    catch (Exception e) { Report(false, "standalone: no booking count", e.Message); }
    await ValidSubmission("standalone: event booking returns reference", upcomingEvent, eventKey, [
        new("Name", "Smoke Test"), new("Email", "smoke@example.com"), new("Notes", "Smoke test booking"), new("JoinMailingList", "true")], null);
}

Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
return failures == 0 ? 0 : 1;

// ---- 4. Home trace in Grafana (Tempo + Loki)
async Task TelemetryCheck()
{
    try
    {
        using var g = new HttpClient { BaseAddress = new Uri(Grafana), Timeout = TimeSpan.FromSeconds(20) };
        g.DefaultRequestHeaders.Authorization = new("Basic", Convert.ToBase64String("admin:admin"u8.ToArray()));
        async Task<JsonElement> Get(string path) => JsonDocument.Parse(await g.GetStringAsync(path)).RootElement;
        // Newest site log lines (with trace id) matching a filter; polls for exporter delay.
        async Task<List<string>> TraceIds(string logql, DateTime since)
        {
            for (var i = 0; i < 15; i++)
            {
                var start = new DateTimeOffset(since).ToUnixTimeMilliseconds() * 1_000_000;
                var j = await Get("/api/datasources/proxy/uid/loki/loki/api/v1/query_range?limit=20&start=" + start + "&query=" + Uri.EscapeDataString(logql));
                var ids = j.GetProperty("data").GetProperty("result").EnumerateArray()
                    .Select(s => s.GetProperty("stream").TryGetProperty("trace_id", out var t) ? t.GetString()! : "").Where(t => t != "").Distinct().ToList();
                if (ids.Count > 0) return ids;
                await Task.Delay(2000);
            }
            return [];
        }
        // Counts SqlClient spans and server spans of a trace; null if Tempo does not have it yet.
        async Task<(int Db, int Server)?> Spans(string traceId)
        {
            for (var i = 0; i < 15; i++)
            {
                var resp = await g.GetAsync("/api/datasources/proxy/uid/tempo/api/traces/" + traceId);
                if (resp.IsSuccessStatusCode)
                {
                    var spans = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement.GetProperty("batches").EnumerateArray()
                        .SelectMany(b => b.GetProperty("scopeSpans").EnumerateArray()).SelectMany(s => s.GetProperty("spans").EnumerateArray()).ToList();
                    return (spans.Count(s => s.TryGetProperty("attributes", out var a) && a.EnumerateArray().Any(x => x.GetProperty("key").GetString() == "db.system.name")),
                        spans.Count(s => s.GetProperty("kind").GetString() == "SPAN_KIND_SERVER"));
                }
                await Task.Delay(2000);
            }
            return null;
        }

        // The first request of the run (section 0): its log lines carry the trace id.
        var homeIds = await TraceIds($$"""{service_name="site"} |= "smoke={{marker}}" | trace_id != "" """, homeSince);
        var home = homeIds.Count > 0 ? await Spans(homeIds[0]) : null;
        var detail = home is null ? "trace not found in Tempo/Loki"
            : $"trace {homeIds[0]}: {home.Value.Server} server span, {home.Value.Db} database spans, request log line found with that trace_id";
        if (home is { Db: 0 })
            detail = "Home trace has no database spans: run the smoke check right after starting the AppHost (first Home load after start); " + detail;
        Report(home is { Server: >= 1, Db: > 0 }, "home trace (first load): database spans + linked logs", detail);

        // Request made before lgtm started must not appear; only meaningful once the pipeline is proven live (home lines found).
        const string preName = "no pre-start telemetry in Grafana";
        if (!preStart) Console.WriteLine($"N/A  {preName} - not applicable: lgtm was already running");
        else if (homeIds.Count == 0) Report(false, preName, "pipeline not proven live (no Home log lines in Loki)");
        else
        {
            var since = new DateTimeOffset(homeSince.AddMinutes(-2)).ToUnixTimeMilliseconds() * 1_000_000;
            var j = await Get("/api/datasources/proxy/uid/loki/loki/api/v1/query_range?limit=20&start=" + since + "&query="
                + Uri.EscapeDataString($$"""{service_name="site"} |= "smoke-pre={{preMarker}}" """));
            var lines = j.GetProperty("data").GetProperty("result").EnumerateArray().Sum(s => s.GetProperty("values").GetArrayLength());
            Report(lines == 0, preName, lines == 0 ? $"no Loki line with smoke-pre={preMarker}" : $"{lines} Loki line(s) with smoke-pre={preMarker}");
        }
    }
    catch (Exception e) { Report(false, "telemetry", e.Message); }
}

// ---- 4b. Dashboards and page-type label (add-demo-dashboard, design D6)
async Task DashboardCheck()
{
    try
    {
        using var g = new HttpClient { BaseAddress = new Uri(Grafana), Timeout = TimeSpan.FromSeconds(20) };
        g.DefaultRequestHeaders.Authorization = new("Basic", Convert.ToBase64String("admin:admin"u8.ToArray()));
        var from = new DateTimeOffset(homeSince.AddMinutes(-1)).ToUnixTimeSeconds();
        // Tempo TraceQL search: (trace id, selected span attributes) per matching span.
        async Task<List<(string Trace, Dictionary<string, string> Attrs)>> Tempo(string q, bool strict = false)
        {
            var url = "/api/datasources/proxy/uid/tempo/api/search?limit=50&start=" + from + "&end=" + DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds() + "&q=" + Uri.EscapeDataString(q);
            var resp = await g.GetAsync(url);
            var list = new List<(string, Dictionary<string, string>)>();
            if (!resp.IsSuccessStatusCode) return strict ? throw new Exception($"Tempo search {(int)resp.StatusCode}") : list; // non-strict: Tempo can answer 404/5xx while ingesting; callers retry
            var root = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
            if (!root.TryGetProperty("traces", out var traces)) return list;
            foreach (var t in traces.EnumerateArray())
                foreach (var set in t.TryGetProperty("spanSets", out var ss) ? ss.EnumerateArray().ToList() : t.TryGetProperty("spanSet", out var one) ? [one] : new List<JsonElement>())
                    foreach (var sp in set.GetProperty("spans").EnumerateArray())
                        list.Add((t.GetProperty("traceID").GetString()!, sp.TryGetProperty("attributes", out var at)
                            ? at.EnumerateArray().ToDictionary(a => a.GetProperty("key").GetString()!, a => a.GetProperty("value").EnumerateObject().First().Value.ToString()) : []));
            return list;
        }
        async Task<List<(string Trace, Dictionary<string, string> Attrs)>> TempoWait(string q, Func<List<(string Trace, Dictionary<string, string> Attrs)>, bool> done)
        {
            var r = new List<(string, Dictionary<string, string>)>();
            for (var i = 0; i < 15; i++)
            {
                r = await Tempo(q);
                if (done(r)) break;
                await Task.Delay(2000);
            }
            return r;
        }

        // Explicit asset request: must never become a span. Checked last, after a sentinel request made right after it
        // has shown up in Tempo (spans export in order, so the asset span would be there too); else the check could pass vacuously.
        var assetStatus = (int)(await NewClient().GetAsync("/assets/rubber-duck.svg")).StatusCode;
        var sentinelPath = "/smoke-asset-" + Guid.NewGuid().ToString("N") + "/";
        await NewClient().GetAsync(sentinelPath);

        var search = JsonDocument.Parse(await g.GetStringAsync("/api/search")).RootElement.EnumerateArray().Select(d => d.GetProperty("uid").GetString()).ToList();
        var missing = new[] { "demo-site", "KdDACDp4z" }.Where(u => !search.Contains(u)).ToList();
        Report(missing.Count == 0, "dashboards provisioned", missing.Count == 0 ? "demo-site and 19924 (KdDACDp4z) listed" : $"missing {string.Join(", ", missing)}; found {string.Join(", ", search)}");

        var metric = "";
        for (var i = 0; i < 15 && metric == ""; i++)
        {
            var q = Uri.EscapeDataString("""http_server_request_duration_seconds_count{job="site",acme_page_type="home"}""");
            var resp = await g.GetAsync("/api/datasources/proxy/uid/prometheus/api/v1/query?query=" + q);
            if (resp.IsSuccessStatusCode)
            {
                var res = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("result");
                if (res.GetArrayLength() > 0) { metric = res[0].GetProperty("value")[1].GetString()!; break; }
            }
            await Task.Delay(2000);
        }
        Report(metric != "", "page-type label on metric", metric != "" ? $"acme_page_type=home count {metric}" : "no series after 30 s");

        var select = " | select(span.acme.page_type, span.url.path)";
        var home = await TempoWait("""{resource.service.name="site" && span.acme.page_type="home"}""" + select, r => r.Count > 0);
        Report(home.Count > 0, "page-type label on span", home.Count > 0 ? $"{home.Count} span(s) with acme.page_type=home, e.g. trace {home[0].Trace}" : "no Home span with acme.page_type=home");

        var posts = await TempoWait($$"""{resource.service.name="site" && kind=server && span.http.request.method="POST" && span.url.path=~"{{service.TrimEnd('/')}}/?"}""" + select, r => r.Any(s => s.Attrs.GetValueOrDefault("span.acme.page_type") == "service" || s.Attrs.GetValueOrDefault("acme.page_type") == "service"));
        var types = posts.Select(s => s.Attrs.GetValueOrDefault("span.acme.page_type") ?? s.Attrs.GetValueOrDefault("acme.page_type") ?? "(missing)").ToList();
        Report(types.Contains("service"), "page-type label on form post", $"POST server spans: {(types.Count == 0 ? "none found" : string.Join(", ", types.GroupBy(t => t).Select(t => $"{t.Key} x{t.Count()}")))}");

        var sentinel = await TempoWait($$"""{resource.service.name="site" && span.url.path="{{sentinelPath}}"}""" + select, r => r.Count > 0);
        if (sentinel.Count == 0) { Report(false, "assets excluded", "pipeline not proven live (sentinel request span not in Tempo after 30 s)"); return; }
        var assets = await Tempo("""{resource.service.name="site" && span.url.path=~"/assets/.*"}""" + select, strict: true);
        Report(assets.Count == 0 && assetStatus == 200, "assets excluded", $"GET /assets/rubber-duck.svg {assetStatus}; {assets.Count} span(s) with an /assets url.path");
    }
    catch (Exception e) { Report(false, "dashboard checks", e.Message); }
}

// ---- 5. Owner notification in smtp4dev (add-form-emails task 5.1)
async Task NotificationCheck()
{
    string inbox;
    try
    {
        var json = Run("aspire", "describe", "smtp4dev", "--apphost", appHostProject, "--non-interactive", "--nologo", "--format", "json");
        inbox = JsonDocument.Parse(json[json.IndexOf('{')..]).RootElement.GetProperty("resources")[0].GetProperty("urls")
            .EnumerateArray().First(u => u.GetProperty("name").GetString() == "http").GetProperty("url").GetString()!;
    }
    catch (Exception e) { Report(false, "notification email", $"smtp4dev inbox URL not found ({e.Message})"); return; }

    using var h = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
    foreach (var (label, reference) in submitted)
    {
        try
        {
            string? found = null;
            for (var i = 0; i < 10 && found is null; i++)
            {
                var results = JsonDocument.Parse(await h.GetStringAsync(inbox + "/api/messages?pageSize=500")).RootElement.GetProperty("results");
                foreach (var m in results.EnumerateArray())
                {
                    var subject = m.GetProperty("subject").GetString() ?? "";
                    var to = m.GetProperty("to").EnumerateArray().Select(t => t.GetString() ?? "");
                    if (subject.EndsWith(reference) && to.Any(t => t.Contains("owner@acme-agency.example"))) { found = subject; break; }
                }
                if (found is null) await Task.Delay(1000);
            }
            Report(found is not null, $"notification email {label}", found is not null ? $"smtp4dev has '{found}' to owner@acme-agency.example" : $"no email to owner@acme-agency.example with subject ending {reference}");
        }
        catch (Exception e) { Report(false, $"notification email {label}", e.Message); }
    }
    if (submitted.Count == 0) Report(false, "notification email", "no valid submission to check");
}

// ---- 6. Booking count and api telemetry (add-booking-api, design D10)
async Task BookingCheck()
{
    var since = new DateTimeOffset(DateTime.UtcNow.AddMinutes(-1)).ToUnixTimeSeconds();
    // Count shown on the event page; absent element = 0.
    async Task<int> Count()
    {
        var m = Regex.Match(await NewClient().GetStringAsync(upcomingEvent), @"event-bookings"">\s*(\d+)");
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }
    try
    {
        var before = await Count();
        var (status, _, _, _) = await Post(upcomingEvent, [new("Name", "Smoke Test"), new("Email", "smoke@example.com"), new("Notes", "Smoke test booking"), new("JoinMailingList", "true")]);
        var now = before;
        for (var i = 0; i < 10 && now != before + 1; i++)
        {
            await Task.Delay(500);
            now = await Count();
        }
        Report(status == 302 && now == before + 1, "booking count rises", $"post {status}, count {before} -> {now}");
    }
    catch (Exception e) { Report(false, "booking count rises", e.Message); }

    try
    {
        using var g = new HttpClient { BaseAddress = new Uri(Grafana), Timeout = TimeSpan.FromSeconds(20) };
        g.DefaultRequestHeaders.Authorization = new("Basic", Convert.ToBase64String("admin:admin"u8.ToArray()));
        // Tempo TraceQL search; polls for exporter delay. Returns the number of matching traces and the first trace id.
        async Task<(int Traces, string First)> Tempo(string q)
        {
            var n = 0; var first = "";
            for (var i = 0; i < 15 && n == 0; i++)
            {
                var resp = await g.GetAsync("/api/datasources/proxy/uid/tempo/api/search?limit=50&start=" + since + "&end=" + DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds() + "&q=" + Uri.EscapeDataString(q));
                if (resp.IsSuccessStatusCode && JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement.TryGetProperty("traces", out var t) && t.GetArrayLength() > 0)
                    (n, first) = (t.GetArrayLength(), t[0].GetProperty("traceID").GetString()!);
                else await Task.Delay(2000);
            }
            return (n, first);
        }
        var api = await Tempo("""{resource.service.name="api" && span.acme.event.key != nil}""");
        Report(api.Traces > 0, "api spans with acme.event.key", api.Traces > 0 ? $"{api.Traces} trace(s), e.g. {api.First}" : "none found in Tempo after 30 s");
        var both = await Tempo("""{resource.service.name="site" && name="acme.forms.submit"} && {resource.service.name="api" && name="deliver booking-stored" && span.acme.event.key != nil}""");
        Report(both.Traces > 0, "booking trace spans site and api", both.Traces > 0 ? $"{both.Traces} trace(s), e.g. {both.First}" : "no trace with spans from both services after 30 s");
    }
    catch (Exception e) { Report(false, "booking telemetry", e.Message); }
}

static string Run(string file, params string[] arguments)
{
    var psi = new ProcessStartInfo(file) { RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var a in arguments) psi.ArgumentList.Add(a);
    using var p = Process.Start(psi)!;
    var stdout = p.StandardOutput.ReadToEndAsync();
    var stderr = p.StandardError.ReadToEndAsync();
    p.WaitForExit();
    if (p.ExitCode != 0) throw new InvalidOperationException($"{file} {arguments[0]} exited {p.ExitCode}");
    return stdout.Result;
}

record Row(int Id, Guid FormKey, Guid PageKey, DateTime SubmittedUtc, string FieldsJson, int ReferenceNumber);
