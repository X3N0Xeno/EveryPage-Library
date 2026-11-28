using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EveryPageLibrary.Models;

namespace EveryPageLibrary.Services;

public class OpenWebMirrorBookSource
{
    private readonly HttpClient _http;

    public OpenWebMirrorBookSource(HttpClient http) => _http = http;

    public async Task<List<Book>> SearchAsync(string query, string? genre, int limit = 15, int page = 1, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || page > 1)
            return new List<Book>();

        try
        {
            var url = $"/api/web-books?q={Uri.EscapeDataString(query.Trim())}&limit={limit}";
            using var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return new List<Book>();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!doc.RootElement.TryGetProperty("results", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return new List<Book>();

            var books = new List<Book>();
            foreach (var item in arr.EnumerateArray())
            {
                var fileUrl = item.TryGetProperty("url", out var u) ? u.GetString() : null;
                var title = item.TryGetProperty("title", out var t) ? t.GetString() : null;
                if (string.IsNullOrWhiteSpace(fileUrl) || string.IsNullOrWhiteSpace(title))
                    continue;

                var host = item.TryGetProperty("host", out var h) ? h.GetString() ?? "Open Web" : "Open Web";
                var snippet = item.TryGetProperty("snippet", out var s) ? s.GetString() : null;
                var format = item.TryGetProperty("format", out var f) ? f.GetString() : "pdf";
                var isPdf = string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase);

                books.Add(new Book
                {
                    ExternalId = $"web-{Math.Abs(fileUrl.GetHashCode())}",
                    Title = title.Trim(),
                    Author = host,
                    Category = BookGenres.ToStorage(genre ?? $"{title} {snippet}"),
                    Synopsis = string.IsNullOrWhiteSpace(snippet)
                        ? $"Direct open-web {format?.ToUpperInvariant()} file hosted on {host}."
                        : snippet,
                    SourceName = "Open Web Mirror",
                    FileUrl = fileUrl,
                    PdfUrl = isPdf ? fileUrl : null,
                    EpubUrl = !isPdf ? fileUrl : null
                });
            }
            return books;
        }
        catch
        {
            return new List<Book>();
        }
    }

    public Task<List<Book>> BrowseSubjectAsync(string genre, int limit = 12, int page = 1, CancellationToken ct = default) =>
        Task.FromResult(new List<Book>());

    public Task<List<Book>> GetPopularAsync(int limit = 12, int page = 1, CancellationToken ct = default) =>
        Task.FromResult(new List<Book>());
}