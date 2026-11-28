using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EveryPageLibrary.Models;

namespace EveryPageLibrary.Services;

public class WikibooksBookSource
{
    private const string ApiEndpoint = "https://en.wikibooks.org/w/api.php";
    private const string SourceName = "Wikibooks";

    private readonly HttpClient _http;

    public WikibooksBookSource(HttpClient http)
    {
        _http = http;
    }

    public Task<List<Book>> GetPopularAsync(int limit = 16, int page = 1, CancellationToken cancellationToken = default)
    {
        return FetchAsync("textbook programming mathematics science history", null, limit, page, cancellationToken);
    }

    public Task<List<Book>> SearchAsync(string query, string? genre, int limit = 16, int page = 1, CancellationToken cancellationToken = default)
    {
        var q = !string.IsNullOrWhiteSpace(genre) && !genre.Equals("All", StringComparison.OrdinalIgnoreCase)
            ? $"{query} {BookGenres.ToSearchKeyword(genre)}"
            : query;
        return FetchAsync(q, genre, limit, page, cancellationToken);
    }

    public Task<List<Book>> BrowseSubjectAsync(string genre, int limit = 16, int page = 1, CancellationToken cancellationToken = default)
    {
        return FetchAsync(BookGenres.ToSearchKeyword(genre), genre, limit, page, cancellationToken);
    }

    private async Task<List<Book>> FetchAsync(string searchTerm, string? genre, int limit, int page, CancellationToken cancellationToken)
    {
        int offset = Math.Max(0, (page - 1) * limit);
        var url = $"{ApiEndpoint}?action=query&list=search&srsearch={Uri.EscapeDataString(searchTerm)}" +
                  $"&srnamespace=0&srlimit={limit}&sroffset={offset}&format=json&origin=*";

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new List<Book>();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("query", out var queryEl) ||
                !queryEl.TryGetProperty("search", out var searchArr) ||
                searchArr.ValueKind != JsonValueKind.Array)
            {
                return new List<Book>();
            }

            var results = new List<Book>();
            var seenRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in searchArr.EnumerateArray())
            {
                var rawTitle = item.TryGetProperty("title", out var tEl) ? tEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(rawTitle))
                    continue;

                var rootTitle = rawTitle.Split('/')[0].Trim();
                if (!seenRoots.Add(rootTitle))
                    continue;

                var pageId = item.TryGetProperty("pageid", out var idEl) ? idEl.GetInt64() : 0;
                var wordCount = item.TryGetProperty("wordcount", out var wcEl) ? wcEl.GetInt32() : 0;
                var snippet = item.TryGetProperty("snippet", out var snEl) ? StripHtml(snEl.GetString() ?? "") : "";
                var slug = Uri.EscapeDataString(rootTitle.Replace(' ', '_'));

                results.Add(new Book
                {
                    Title = rootTitle,
                    Author = "Wikibooks Contributors",
                    Category = BookGenres.ToStorage(genre ?? $"{rootTitle} {snippet}"),
                    Synopsis = !string.IsNullOrWhiteSpace(snippet)
                        ? $"{snippet}..."
                        : "Open-content textbook and instructional guide from Wikibooks, exportable as PDF or EPUB.",
                    PublicationYear = "2024",
                    PdfUrl = $"https://en.wikibooks.org/api/rest_v1/page/pdf/{slug}",
                    FileUrl = $"https://en.wikibooks.org/wiki/{slug}",
                    SourceName = SourceName,
                    ExternalId = $"wb-{pageId}",
                    UploadedBy = SourceName,
                    DownloadCount = Math.Max(120, wordCount),
                    ContentText = string.Empty
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WikibooksBookSource] Request failed: {ex.Message}");
            return new List<Book>();
        }
    }

    private static string StripHtml(string html)
    {
        var decoded = WebUtility.HtmlDecode(html);
        var chars = new char[decoded.Length];
        int idx = 0;
        bool inside = false;
        foreach (char c in decoded)
        {
            if (c == '<') { inside = true; continue; }
            if (c == '>') { inside = false; continue; }
            if (!inside) chars[idx++] = c;
        }
        return new string(chars, 0, idx).Trim();
    }
}
