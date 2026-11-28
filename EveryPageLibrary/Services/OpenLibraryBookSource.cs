using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EveryPageLibrary.Models;

namespace EveryPageLibrary.Services;

public class OpenLibraryBookSource
{
    private const string SearchUrl = "https://openlibrary.org/search.json";
    private const string Fields = "key,title,author_name,first_publish_year,edition_count,cover_i,ia,ia_collection_s,public_scan_b,subject";
    private const string SourceName = "Open Library";

    public const string ArchiveEmbedBaseUrl = "https://archive.org/embed/";
    private readonly HttpClient _http;

    public OpenLibraryBookSource(HttpClient http) => _http = http;

    public Task<List<Book>> GetPopularAsync(int limit = 24, int page = 1, CancellationToken cancellationToken = default)
    {
        var url = $"{SearchUrl}?q=subject:classics+public_scan_b:true&sort=editions&fields={Fields}&limit={limit}&page={Math.Max(1, page)}";
        return FetchAsync(url, "Classics", limit, cancellationToken);
    }

    public Task<List<Book>> SearchAsync(string query, string? genre, int limit = 24, int page = 1, CancellationToken cancellationToken = default)
    {
        var baseQ = !string.IsNullOrWhiteSpace(genre) && !genre.Equals("All", StringComparison.OrdinalIgnoreCase)
            ? $"{query} subject:{BookGenres.ToOpenLibrarySubject(genre)} public_scan_b:true"
            : $"{query} public_scan_b:true";

        var url = $"{SearchUrl}?q={Uri.EscapeDataString(baseQ)}&fields={Fields}&limit={limit}&page={Math.Max(1, page)}";
        return FetchAsync(url, genre, limit, cancellationToken);
    }

    public Task<List<Book>> BrowseSubjectAsync(string genre, int limit = 24, int page = 1, CancellationToken cancellationToken = default)
    {
        var subject = BookGenres.ToOpenLibrarySubject(genre);
        var url = $"{SearchUrl}?q={Uri.EscapeDataString($"subject:{subject} public_scan_b:true")}&sort=editions&fields={Fields}&limit={limit}&page={Math.Max(1, page)}";
        return FetchAsync(url, genre, limit, cancellationToken);
    }

    private async Task<List<Book>> FetchAsync(string url, string? genre, int limit, CancellationToken cancellationToken)
    {
        try
        {
            using var res = await _http.GetAsync(url, cancellationToken);
            if (!res.IsSuccessStatusCode) return new List<Book>();

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(cancellationToken));
            if (!doc.RootElement.TryGetProperty("docs", out var docs) || docs.ValueKind != JsonValueKind.Array)
                return new List<Book>();

            var results = new List<Book>();
            foreach (var el in docs.EnumerateArray())
            {
                var mapped = Map(el, genre);
                if (mapped != null) results.Add(mapped);
                if (results.Count >= limit) break;
            }
            return results;
        }
        catch
        {
            return new List<Book>();
        }
    }

    private static Book? Map(JsonElement el, string? genre)
    {
        var title = el.TryGetProperty("title", out var tEl) ? tEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(title)) return null;

        // Require verified public_scan_b == true
        if (!el.TryGetProperty("public_scan_b", out var scanEl) || scanEl.ValueKind != JsonValueKind.True)
            return null;

        // Reject any borrow-only / controlled lending collections
        if (el.TryGetProperty("ia_collection_s", out var colEl) && colEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in colEl.EnumerateArray())
            {
                var val = c.GetString() ?? "";
                if (val.Equals("inlibrary", StringComparison.OrdinalIgnoreCase) ||
                    val.Equals("printdisabled", StringComparison.OrdinalIgnoreCase) ||
                    val.Equals("lending", StringComparison.OrdinalIgnoreCase))
                    return null;
            }
        }

        string? archiveId = null;
        if (el.TryGetProperty("ia", out var iaArr) && iaArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in iaArr.EnumerateArray())
            {
                var candidate = item.GetString();
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    archiveId = candidate;
                    break;
                }
            }
        }

        // Never return catalog-only stubs that link to Open Library checkout pages
        if (string.IsNullOrWhiteSpace(archiveId))
            return null;

        var author = "Unknown author";
        if (el.TryGetProperty("author_name", out var authArr) && authArr.ValueKind == JsonValueKind.Array)
        {
            var first = authArr.EnumerateArray().FirstOrDefault().GetString();
            if (!string.IsNullOrWhiteSpace(first)) author = first;
        }

        var subjects = new List<string>();
        if (el.TryGetProperty("subject", out var subArr) && subArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in subArr.EnumerateArray().Take(4))
            {
                var sv = s.GetString();
                if (!string.IsNullOrWhiteSpace(sv)) subjects.Add(sv);
            }
        }

        var year = el.TryGetProperty("first_publish_year", out var yrEl) && yrEl.ValueKind == JsonValueKind.Number
            ? yrEl.GetInt32().ToString() : string.Empty;
        var editions = el.TryGetProperty("edition_count", out var edEl) && edEl.ValueKind == JsonValueKind.Number
            ? edEl.GetInt32() : 0;
        var coverId = el.TryGetProperty("cover_i", out var covEl) && covEl.ValueKind == JsonValueKind.Number
            ? covEl.GetInt32() : (int?)null;

        var pdfUrl = $"https://archive.org/download/{archiveId}/{archiveId}.pdf";
        return new Book
        {
            Title = title.Trim(),
            Author = author,
            Category = BookGenres.ToStorage(genre ?? (subjects.Count > 0 ? subjects[0] : "Literature")),
            Synopsis = subjects.Count > 0
                ? string.Join(" \u00B7 ", subjects)
                : "Verified public-domain full-text scan from Open Library & Internet Archive.",
            PublicationYear = year,
            CoverUrl = coverId.HasValue ? $"https://covers.openlibrary.org/b/id/{coverId.Value}-M.jpg" : null,
            SourceName = SourceName,
            UploadedBy = SourceName,
            DownloadCount = editions,
            PdfUrl = pdfUrl,
            EpubUrl = $"https://archive.org/download/{archiveId}/{archiveId}.epub",
            FileUrl = pdfUrl,
            ExternalId = $"ol-{archiveId}",
            ContentText = string.Empty
        };
    }
}