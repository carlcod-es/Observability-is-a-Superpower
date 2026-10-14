// End-to-end smoke check (add-demo-foundation task 9.4, ADR-0012). .NET 10 file-based app.
//
//   dotnet run tests/smoke/smoke.cs                 target "apphost": AppHost freshly started (aspire run), SQL Server;
//                                                   run it before any page is loaded (its first request is the first Home load)
//   dotnet run tests/smoke/smoke.cs -- standalone   target "standalone": `dotnet run --project src/SuperPower.Web`, SQLite
//
// Run from inside the repo. Exit code is non-zero if any check FAILs. Submissions are fake and never deleted.
// apphost: SQL Server login comes from the AppHost user secrets (Parameters:sql-password, Resources:sql:tcp:port);
//   override with env SMOKE_SQL_CONNECTION. smtp4dev and Toxiproxy are reached through the `aspire` CLI.
//   Telemetry is read through Grafana (http://localhost:3000, admin/admin), the same path the agent uses.
// standalone: SQLite file src/SuperPower.Web/umbraco/Data/Umbraco.sqlite.db, opened read-only. Pages and forms only.
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
const string Success = "Thank you, we'll be in touch.";
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
        cs = $"Server=localhost,{secrets["Resources:sql:tcp:port"]};User Id=sa;Password={secrets["Parameters:sql-password"]};Database=umbraco;TrustServerCertificate=True";
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
    cmd.CommandText = "SELECT Id, FormKey, PageKey, SubmittedUtc, FieldsJson FROM acmeFormSubmission ORDER BY Id";
    using var r = cmd.ExecuteReader();
    var rows = new List<Row>();
    while (r.Read())
    {
        var ts = r.GetValue(3);
        var utc = ts is DateTime dt ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            : DateTime.Parse(ts.ToString()!, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        rows.Add(new Row(Convert.ToInt32(r.GetValue(0)), Guid.Parse(r.GetValue(1).ToString()!), Guid.Parse(r.GetValue(2).ToString()!), utc, r.GetString(4)));
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
        if (added.Count != 1) problems.Add($"{added.Count} new rows");
        if (row is not null)
        {
            if (row.FormKey != formKey) problems.Add($"FormKey {row.FormKey}");
            if (row.PageKey != pageKey) problems.Add($"PageKey {row.PageKey} != {pageKey}");
            if (Math.Abs((DateTime.UtcNow - row.SubmittedUtc).TotalMinutes) > 5) problems.Add($"SubmittedUtc {row.SubmittedUtc:O}");
            if (expectedJson is not null && !Encoding.UTF8.GetBytes(row.FieldsJson).SequenceEqual(Encoding.UTF8.GetBytes(expectedJson)))
                problems.Add($"FieldsJson differs: {row.FieldsJson}");
            if (expectedJson is null && !fields.All(f => JsonDocument.Parse(row.FieldsJson).RootElement.TryGetProperty(f.Key, out var v) && v.GetString() == f.Value))
                problems.Add($"FieldsJson lacks posted values: {row.FieldsJson}");
        }
        Report(problems.Count == 0, label, problems.Count == 0 ? $"302, success text, row {row!.Id} {row.SubmittedUtc:HH:mm:ss}Z page {pageKey}" : string.Join("; ", problems));
    }
    catch (Exception e) { Report(false, label, e.Message); }
}

// ---- 0. first request of the run: Home with a unique marker. On a freshly started AppHost this is the first Home load
// (later loads are served from Umbraco's cache without database calls); TelemetryCheck inspects its trace.
var marker = Guid.NewGuid().ToString("N");
var homeSince = DateTime.UtcNow.AddSeconds(-5);
if (!standalone)
{
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
    await MailCheck();
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
    }
    catch (Exception e) { Report(false, "telemetry", e.Message); }
}

// ---- 5. Slow / Break / Heal on the SMTP path (localhost:2525 -> Toxiproxy -> smtp4dev)
async Task MailCheck()
{
    string? inbox = null;
    try
    {
        var json = Run("aspire", "describe", "smtp4dev", "--apphost", appHostProject, "--non-interactive", "--nologo", "--format", "json");
        inbox = JsonDocument.Parse(json[json.IndexOf('{')..]).RootElement.GetProperty("resources")[0].GetProperty("urls")
            .EnumerateArray().First(u => u.GetProperty("name").GetString() == "http").GetProperty("url").GetString();
    }
    catch (Exception e) { Console.WriteLine($"note: smtp4dev inbox URL not found ({e.Message}); delivery = SMTP 250 after DATA"); }

    // Same commands as the dashboard buttons (AppHost.cs).
    void Command(string name) => Run("aspire", "resource", "toxiproxy", name, "--apphost", appHostProject, "--non-interactive", "--nologo");

    async Task<(bool Ok, long Ms, string Detail)> Send(string subject)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var tcp = new TcpClient();
            tcp.ReceiveTimeout = tcp.SendTimeout = 60_000;
            await tcp.ConnectAsync("localhost", 2525);
            using var reader = new StreamReader(tcp.GetStream(), Encoding.ASCII);
            using var writer = new StreamWriter(tcp.GetStream(), Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
            async Task<string> Reply(string expect)
            {
                string line;
                do { line = await reader.ReadLineAsync() ?? throw new IOException("connection closed"); } while (line.Length > 3 && line[3] == '-');
                if (!line.StartsWith(expect)) throw new IOException($"expected {expect}, got {line}");
                return line;
            }
            async Task Cmd(string text, string expect) { await writer.WriteLineAsync(text); await Reply(expect); }
            await Reply("220");
            await Cmd("EHLO smoke.local", "250");
            await Cmd("MAIL FROM:<smoke@example.com>", "250");
            await Cmd("RCPT TO:<owner@example.com>", "250");
            await Cmd("DATA", "354");
            await Cmd($"Subject: {subject}\r\nFrom: smoke@example.com\r\nTo: owner@example.com\r\n\r\nSmoke test.\r\n.", "250");
            await writer.WriteLineAsync("QUIT");
            return (true, sw.ElapsedMilliseconds, "250 after DATA");
        }
        catch (Exception e) { return (false, sw.ElapsedMilliseconds, e.GetType().Name + ": " + e.Message); }
    }

    async Task<bool> Delivered(string subject)
    {
        if (inbox is null) return true; // fall back to the SMTP 250
        using var h = new HttpClient();
        for (var i = 0; i < 5; i++)
        {
            if ((await h.GetStringAsync(inbox + "/api/messages")).Contains(subject)) return true;
            await Task.Delay(1000);
        }
        return false;
    }

    async Task Step(string name, string? command, bool expectOk, Func<long, bool> timing, string timingText)
    {
        var subject = $"smoke {name} {Guid.NewGuid():N}";
        try
        {
            if (command is not null) Command(command);
            var (ok, ms, detail) = await Send(subject);
            var delivered = ok && await Delivered(subject);
            Report(ok == expectOk && delivered == expectOk && timing(ms), $"mail {name}",
                $"{ms} ms (expect {(expectOk ? "delivered" : "failure")}, {timingText}): {(ok ? "sent" : "failed")}, {(delivered ? "delivered" : "not delivered")} [{detail}]");
        }
        catch (Exception e) { Report(false, $"mail {name}", e.Message); }
    }

    try
    {
        Command("heal"); // known starting point
        await Step("baseline", null, true, ms => ms < 2000, "< 2000 ms");
        await Step("slow", "slow", true, ms => ms > 3000, "> 3000 ms");
        await Step("break", "break", false, ms => ms < 2000, "< 2000 ms");
    }
    catch (Exception e) { Report(false, "mail setup", e.Message); }
    finally
    {
        // Heal always runs, then must work again.
        try { await Step("heal", "heal", true, ms => ms < 2000, "< 2000 ms"); }
        catch { Console.WriteLine("heal failed"); }
    }
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

record Row(int Id, Guid FormKey, Guid PageKey, DateTime SubmittedUtc, string FieldsJson);
