using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MasyafArchive.Models;

namespace MasyafArchive.Services;

public class WikisourceBookSource
{
    private const string ApiUrl = "https://en.wikisource.org/w/api.php";
    private const string SourceName = "Wikisource";

    private readonly HttpClient _http;

    public WikisourceBookSource(HttpClient http)
    {
        _http = http;
    }

    public Task<List<Book>> GetPopularAsync(int limit = 20, int page = 1, CancellationToken cancellationToken = default)
    {
        return SearchInternalAsync("novel OR poetry OR philosophy", "Literature", limit, page, cancellationToken);
    }

    public Task<List<Book>> SearchAsync(string query, string? genre, int limit = 20, int page = 1, CancellationToken cancellationToken = default)
    {
        var q = !string.IsNullOrWhiteSpace(genre) && !genre.Equals("All", StringComparison.OrdinalIgnoreCase)
            ? $"{query} {genre}"
            : query;
        return SearchInternalAsync(q, genre, limit, page, cancellationToken);
    }

    public Task<List<Book>> BrowseSubjectAsync(string genre, int limit = 20, int page = 1, CancellationToken cancellationToken = default)
    {
        return SearchInternalAsync(genre, genre, limit, page, cancellationToken);
    }

    public async Task<string> FetchFullTextAsync(string title, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;

        var url = $"{ApiUrl}?action=query&prop=extracts&explaintext=1&titles={Uri.EscapeDataString(title)}&format=json&origin=*";
        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return string.Empty;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (doc.RootElement.TryGetProperty("query", out var queryEl) &&
                queryEl.TryGetProperty("pages", out var pagesEl))
            {
                foreach (var pageProp in pagesEl.EnumerateObject())
                {
                    if (pageProp.Value.TryGetProperty("extract", out var extractEl))
                    {
                        return extractEl.GetString() ?? string.Empty;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WikisourceBookSource] Extract fetch failed: {ex.Message}");
        }

        return string.Empty;
    }

    private async Task<List<Book>> SearchInternalAsync(string srSearch, string? genre, int limit, int page, CancellationToken cancellationToken)
    {
        var offset = (Math.Max(1, page) - 1) * limit;
        var url = $"{ApiUrl}?action=query&list=search&srsearch={Uri.EscapeDataString(srSearch)}&srnamespace=0&srlimit={limit}&sroffset={offset}&utf8=1&format=json&origin=*";

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

            var books = new List<Book>();
            foreach (var item in searchArr.EnumerateArray())
            {
                var title = item.TryGetProperty("title", out var tEl) ? tEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(title) || title.Contains('/'))
                    continue;

                var rawSnippet = item.TryGetProperty("snippet", out var sEl) ? sEl.GetString() ?? string.Empty : string.Empty;
                var cleanSnippet = Regex.Replace(rawSnippet, "<.*?>", string.Empty).Trim();
                var wordCount = item.TryGetProperty("wordcount", out var wEl) && wEl.TryGetInt32(out var wc) ? wc : 0;

                books.Add(new Book
                {
                    Title = title.Trim(),
                    Author = "Wikisource Community Edition",
                    Category = BookGenres.ToStorage(genre ?? "Literature"),
                    Synopsis = string.IsNullOrWhiteSpace(cleanSnippet)
                        ? "Proofread open-source text from the Wikisource digital library."
                        : $"{cleanSnippet}...",
                    PublicationYear = string.Empty,
                    FileUrl = $"https://en.wikisource.org/wiki/{Uri.EscapeDataString(title.Trim().Replace(' ', '_'))}",
                    SourceName = SourceName,
                    ExternalId = $"ws-{title.Trim()}",
                    UploadedBy = SourceName,
                    DownloadCount = wordCount,
                    ContentText = string.Empty
                });
            }

            return books;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WikisourceBookSource] Search failed: {ex.Message}");
            return new List<Book>();
        }
    }
}
