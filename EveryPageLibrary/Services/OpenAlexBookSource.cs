using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EveryPageLibrary.Models;

namespace EveryPageLibrary.Services;

public class OpenAlexBookSource
{
    private readonly HttpClient _http;

    public OpenAlexBookSource(HttpClient http) => _http = http;

    public Task<List<Book>> SearchAsync(string query, string? genre, int limit = 15, int page = 1, CancellationToken ct = default) =>
        FetchAsync($"https://api.openalex.org/works?search={Uri.EscapeDataString(query)}&filter=has_fulltext:true,is_oa:true,type:book&per-page={limit}&page={page}", genre, ct);

    public Task<List<Book>> BrowseSubjectAsync(string genre, int limit = 15, int page = 1, CancellationToken ct = default) =>
        FetchAsync($"https://api.openalex.org/works?search={Uri.EscapeDataString(genre)}&filter=has_fulltext:true,is_oa:true,type:book&per-page={limit}&page={page}", genre, ct);

    public Task<List<Book>> GetPopularAsync(int limit = 15, int page = 1, CancellationToken ct = default) =>
        FetchAsync($"https://api.openalex.org/works?filter=has_fulltext:true,is_oa:true,type:book&sort=cited_by_count:desc&per-page={limit}&page={page}", null, ct);

    private async Task<List<Book>> FetchAsync(string endpoint, string? genre, CancellationToken ct)
    {
        try
        {
            using var res = await _http.GetAsync(endpoint, ct);
            if (!res.IsSuccessStatusCode) return new List<Book>();

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            if (!doc.RootElement.TryGetProperty("results", out var results)) return new List<Book>();

            var list = new List<Book>();
            foreach (var w in results.EnumerateArray())
            {
                var id = w.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                var title = w.TryGetProperty("title", out var tEl) ? tEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title)) continue;

                string? oaUrl = null;
                if (w.TryGetProperty("open_access", out var oa) && oa.TryGetProperty("oa_url", out var uEl))
                    oaUrl = uEl.GetString();
                if (string.IsNullOrWhiteSpace(oaUrl)) continue;

                var authors = new List<string>();
                if (w.TryGetProperty("authorships", out var authArr))
                {
                    foreach (var a in authArr.EnumerateArray().Take(3))
                    {
                        if (a.TryGetProperty("author", out var au) && au.TryGetProperty("display_name", out var dn))
                        {
                            var name = dn.GetString();
                            if (!string.IsNullOrWhiteSpace(name)) authors.Add(name);
                        }
                    }
                }

                var year = w.TryGetProperty("publication_year", out var yr) && yr.ValueKind == JsonValueKind.Number
                    ? yr.GetInt32().ToString() : "";
                var isPdf = oaUrl.Contains(".pdf", StringComparison.OrdinalIgnoreCase);

                list.Add(new Book
                {
                    ExternalId = $"oa-{id.Split('/').Last()}",
                    Title = title.Trim(),
                    Author = authors.Count > 0 ? string.Join(", ", authors) : "OpenAlex Scholar",
                    Category = BookGenres.ToStorage(genre ?? title),
                    Synopsis = $"Open-access scholarly book indexed via OpenAlex ({year}). Direct full-text edition.",
                    SourceName = "OpenAlex Books",
                    FileUrl = oaUrl,
                    PdfUrl = isPdf ? oaUrl : null
                });
            }
            return list;
        }
        catch { return new List<Book>(); }
    }
}