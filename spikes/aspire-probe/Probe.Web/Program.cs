using System.Net;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddHttpClient("api", c => c.BaseAddress = new Uri("https+http://probe-api"));

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/", async (IHttpClientFactory factory, string? slow, string? fail) =>
{
    var query = new List<string>();
    if (!string.IsNullOrEmpty(slow)) query.Add("slow=" + Uri.EscapeDataString(slow));
    if (!string.IsNullOrEmpty(fail)) query.Add("fail=" + Uri.EscapeDataString(fail));
    var url = "/visits" + (query.Count > 0 ? "?" + string.Join('&', query) : "");

    string body;
    try
    {
        var resp = await factory.CreateClient("api").GetAsync(url);
        body = resp.IsSuccessStatusCode
            ? $"<p id=\"count\">Visit count: {WebUtility.HtmlEncode(await resp.Content.ReadAsStringAsync())}</p>"
            : $"<p id=\"error\" style=\"color:#b00\">Error: API returned {(int)resp.StatusCode} {WebUtility.HtmlEncode(resp.ReasonPhrase)}</p>";
    }
    catch (Exception ex)
    {
        body = $"<p id=\"error\" style=\"color:#b00\">Error: {WebUtility.HtmlEncode(ex.GetType().Name)}: {WebUtility.HtmlEncode(ex.Message)}</p>";
    }

    return Results.Content($"""
        <!doctype html><html><body>
        <h1>Probe</h1>
        {body}
        <ul>
        <li><a href="/">Normal</a></li>
        <li><a href="/?slow=2000">Slow (2 s)</a></li>
        <li><a href="/?fail=true">Failing</a></li>
        </ul>
        </body></html>
        """, "text/html");
});

app.Run();
