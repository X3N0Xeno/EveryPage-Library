using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EveryPageLibrary.Models;

namespace EveryPageLibrary.Services;

public class OapenBookSource
{
    private readonly HttpClient _http;

    public OapenBookSource(HttpClient http) => _http = http;

    public Task<List<Book>> SearchAsync(string query, string? genre, int limit = 12, int page = 1, CancellationToken ct = default) =>
        FetchAsync($"https://library.oapen.org/rest/search?query={Uri.EscapeDataString(query)}&expand=metadata,bitstreams&limit={limit}&offset={(page - 1) * limit}", genre, ct);

    public Task<List<Book>> BrowseSubjectAsync(string genre, int limit = 12, int page = 1, CancellationToken ct = default) =>
        FetchAsync($"https://library.oapen.org/rest/search?query={Uri.EscapeDataString(genre)}&expand=metadata,bitstreams&limit={limit}&offset={(page - 1) * limit}", genre, ct);

    public Task<List<Book>> GetPopularAsync(int limit = 12, int page = 1, CancellationToken ct = default) =>
        FetchAsync($"https://library.oapen.org/rest/search?query=literature&expand=metadata,bitstreams&limit={limit}&offset={(page - 1) * limit}", null, ct);

    private async Task<List<Book>> FetchAsync(string endpoint, string? genre, CancellationToken ct)
    {
        try
        {
            using var res = await _http.GetAsync(endpoint, ct);
            if (!res.IsSuccessStatusCode) return new List<Book>();

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return new List<Book>();

            var list = new List<Book>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var handle = item.TryGetProperty("handle", out var hEl) ? hEl.GetString() : null;
                var name = item.TryGetProperty("name", out var nEl) ? nEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(handle) || string.IsNullOrWhiteSpace(name)) continue;

                string? pdfUrl = null;
                if (item.TryGetProperty("bitstreams", out var bsArr) && bsArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var bs in bsArr.EnumerateArray())
                    {
                        var retrieve = bs.TryGetProperty("retrieveLink", out var rl) ? rl.GetString() : null;
                        var mime = bs.TryGetProperty("mimeType", out var mt) ? mt.GetString() : null;
                        if (!string.IsNullOrWhiteSpace(retrieve) &&
                            (mime?.Contains("pdf", StringComparison.OrdinalIgnoreCase) == true || retrieve.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)))
                        {
                            pdfUrl = $"https://library.oapen.org{retrieve}";
                            break;
                        }
                    }
                }

                var fallbackUrl = $"https://library.oapen.org/handle/{handle}";
                list.Add(new Book
                {
                    ExternalId = $"oapen-{handle.Replace('/', '-')}",
                    Title = name.Trim(),
                    Author = "OAPEN Library",
                    Category = BookGenres.ToStorage(genre ?? name),
                    Synopsis = "Peer-reviewed open-access book from the OAPEN Digital Library.",
                    SourceName = "OAPEN Library",
                    FileUrl = pdfUrl ?? fallbackUrl,
                    PdfUrl = pdfUrl
                });
            }
            return list;
        }
        catch { return new List<Book>(); }
    }
}