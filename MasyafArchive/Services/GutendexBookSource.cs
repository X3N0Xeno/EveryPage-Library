using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EverydayLibrary.Models;

namespace EverydayLibrary.Services;

public class GutendexBookSource
{
    private const string BaseUrl = "https://gutendex.com/books";
    private const string SourceName = "Project Gutenberg";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public GutendexBookSource(HttpClient http)
    {
        _http = http;
    }

    public Task<List<Book>> SearchAsync(string query, string? genre, int limit = 32, int page = 1, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/?search={Uri.EscapeDataString(query)}&page={Math.Max(1, page)}";
        return FetchAsync(url, genre, limit, cancellationToken);
    }

    public Task<List<Book>> BrowseSubjectAsync(string genre, int limit = 32, int page = 1, CancellationToken cancellationToken = default)
        => BrowseTopicAsync(genre, limit, page, cancellationToken);

    public Task<List<Book>> BrowseTopicAsync(string genre, int limit = 32, int page = 1, CancellationToken cancellationToken = default)
    {
        var topic = BookGenres.ToGutendexTopic(genre);
        var url = $"{BaseUrl}/?topic={Uri.EscapeDataString(topic)}&sort=popular&page={Math.Max(1, page)}";
        return FetchAsync(url, genre, limit, cancellationToken);
    }

    public Task<List<Book>> GetPopularAsync(int limit = 32, int page = 1, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/?sort=popular&page={Math.Max(1, page)}";
        return FetchAsync(url, null, limit, cancellationToken);
    }

    private async Task<List<Book>> FetchAsync(string url, string? genre, int limit, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await _http.GetFromJsonAsync<GutendexResponse>(url, JsonOptions, cancellationToken);
            if (payload?.Results == null)
                return new List<Book>();

            return payload.Results
                .Select(result => Map(result, genre))
                .Where(book => book != null)
                .Select(book => book!)
                .Take(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GutendexBookSource] Request failed ({url}): {ex.Message}");
            return new List<Book>();
        }
    }

    private static Book? Map(GutendexBook result, string? genre)
    {
        if (string.IsNullOrWhiteSpace(result.Title))
            return null;

        var subjects = result.Subjects
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Take(4)
            .ToList();

        return new Book
        {
            Title = result.Title.Trim(),
            Author = FormatAuthor(result.Authors.FirstOrDefault()?.Name),
            Category = BookGenres.ToStorage(genre ?? InferGenre(result.Subjects, result.Bookshelves)),
            Synopsis = subjects.Count > 0
                ? string.Join(" \u00B7 ", subjects)
                : "A classic public-domain edition from the Project Gutenberg library.",
            PublicationYear = string.Empty,
            CoverUrl = PickFormat(result.Formats, "image/jpeg"),
            EpubUrl = PickFormat(result.Formats, "application/epub+zip"),
            FileUrl = PickFormat(result.Formats, "text/plain; charset=utf-8")
                      ?? PickFormat(result.Formats, "text/plain"),
            PdfUrl = null,
            SourceName = SourceName,
            ExternalId = $"gutendex-{result.Id}",
            UploadedBy = SourceName,
            DownloadCount = result.DownloadCount,
            ContentText = string.Empty
        };
    }

    private static string? PickFormat(Dictionary<string, string> formats, string key)
    {
        if (formats.TryGetValue(key, out var exact) && !string.IsNullOrWhiteSpace(exact))
            return exact;

        foreach (var pair in formats)
        {
            if (pair.Key.StartsWith(key, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(pair.Value))
                return pair.Value;
        }

        return null;
    }

    private static string FormatAuthor(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return "Unknown author";

        var name = rawName.Trim();
        var commaIndex = name.IndexOf(',');
        if (commaIndex <= 0)
            return name;

        var last = name[..commaIndex].Trim();
        var first = name[(commaIndex + 1)..].Trim();
        return string.IsNullOrWhiteSpace(first) ? last : $"{first} {last}";
    }

    private static string InferGenre(IEnumerable<string> subjects, IEnumerable<string> bookshelves)
    {
        var haystack = string.Join(' ', subjects.Concat(bookshelves)).ToLowerInvariant();

        return haystack switch
        {
            _ when haystack.Contains("science fiction") => "Science Fiction",
            _ when haystack.Contains("fantasy") => "Fantasy",
            _ when haystack.Contains("detective") || haystack.Contains("mystery") => "Mystery & Detective",
            _ when haystack.Contains("horror") || haystack.Contains("gothic") => "Horror & Gothic",
            _ when haystack.Contains("romance") || haystack.Contains("love stories") => "Romance",
            _ when haystack.Contains("adventure") => "Adventure",
            _ when haystack.Contains("philosoph") => "Philosophy",
            _ when haystack.Contains("history") || haystack.Contains("historical") => "History",
            _ when haystack.Contains("science") || haystack.Contains("physics") || haystack.Contains("biology") => "Science",
            _ when haystack.Contains("psycholog") => "Psychology",
            _ when haystack.Contains("poetry") || haystack.Contains("poems") => "Poetry",
            _ when haystack.Contains("drama") || haystack.Contains("plays") => "Drama",
            _ when haystack.Contains("mytholog") || haystack.Contains("folklore") => "Mythology & Folklore",
            _ when haystack.Contains("children") || haystack.Contains("juvenile") => "Children's",
            _ when haystack.Contains("biograph") || haystack.Contains("autobiograph") => "Biography",
            _ when haystack.Contains("technology") || haystack.Contains("engineering") => "Technology",
            _ when haystack.Contains("travel") || haystack.Contains("voyages") => "Travel",
            _ => "Literature"
        };
    }
}
