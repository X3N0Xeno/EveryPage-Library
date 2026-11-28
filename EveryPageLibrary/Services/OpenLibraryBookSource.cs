using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EveryPageLibrary.Models;

namespace EveryPageLibrary.Services;

public class OpenLibraryBookSource
{
    private const string SearchUrl = "https://openlibrary.org/search.json";
    private const string Fields = "key,title,author_name,first_publish_year,edition_count,cover_i,ia,subject";
    private const string SourceName = "Open Library";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public const string ArchiveEmbedBaseUrl = "https://archive.org/embed/";

    private readonly HttpClient _http;

    public OpenLibraryBookSource(HttpClient http)
    {
        _http = http;
    }

    public Task<List<Book>> GetPopularAsync(int limit = 28, int page = 1, CancellationToken cancellationToken = default)
    {
        var url = $"{SearchUrl}?q=subject:classics&sort=editions&has_fulltext=true&fields={Fields}&limit={limit}&page={Math.Max(1, page)}";
        return FetchAsync(url, "Classics", limit, cancellationToken);
    }

    public Task<List<Book>> SearchAsync(string query, string? genre, int limit = 28, int page = 1, CancellationToken cancellationToken = default)
    {
        var fullQuery = !string.IsNullOrWhiteSpace(genre) && !genre.Equals("All", StringComparison.OrdinalIgnoreCase)
            ? $"{query} subject:{BookGenres.ToOpenLibrarySubject(genre)}"
            : query;

        var url = $"{SearchUrl}?q={Uri.EscapeDataString(fullQuery)}&fields={Fields}&limit={limit}&page={Math.Max(1, page)}";
        return FetchAsync(url, genre, limit, cancellationToken);
    }

    public Task<List<Book>> BrowseSubjectAsync(string genre, int limit = 28, int page = 1, CancellationToken cancellationToken = default)
    {
        var subject = BookGenres.ToOpenLibrarySubject(genre);
        var url = $"{SearchUrl}?q={Uri.EscapeDataString($"subject:{subject}")}&sort=editions&fields={Fields}&limit={limit}&page={Math.Max(1, page)}";
        return FetchAsync(url, genre, limit, cancellationToken);
    }

    private async Task<List<Book>> FetchAsync(string url, string? genre, int limit, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await _http.GetFromJsonAsync<OpenLibrarySearchResponse>(url, JsonOptions, cancellationToken);
            if (payload?.Docs == null)
                return new List<Book>();

            return payload.Docs
                .Select(doc => Map(doc, genre))
                .Where(book => book != null)
                .Select(book => book!)
                .Take(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OpenLibraryBookSource] Request failed ({url}): {ex.Message}");
            return new List<Book>();
        }
    }

    private static Book? Map(OpenLibraryDoc doc, string? genre)
    {
        if (string.IsNullOrWhiteSpace(doc.Title))
            return null;

        var archiveId = doc.InternetArchiveIdentifiers?
            .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));

        var subjects = doc.Subject?
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Take(4)
            .ToList() ?? new List<string>();

        var book = new Book
        {
            Title = doc.Title.Trim(),
            Author = doc.AuthorName?.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "Unknown author",
            Category = BookGenres.ToStorage(genre ?? InferGenre(doc.Subject)),
            Synopsis = subjects.Count > 0
                ? string.Join(" \u00B7 ", subjects)
                : "Cataloged edition from the Open Library & Internet Archive collection.",
            PublicationYear = doc.FirstPublishYear?.ToString() ?? string.Empty,
            CoverUrl = doc.CoverId.HasValue
                ? $"https://covers.openlibrary.org/b/id/{doc.CoverId.Value}-M.jpg"
                : null,
            SourceName = SourceName,
            UploadedBy = SourceName,
            DownloadCount = doc.EditionCount,
            ContentText = string.Empty
        };

        if (!string.IsNullOrWhiteSpace(archiveId))
        {
            book.PdfUrl = $"https://archive.org/download/{archiveId}/{archiveId}.pdf";
            book.EpubUrl = $"https://archive.org/download/{archiveId}/{archiveId}.epub";
            book.FileUrl = book.PdfUrl;
            book.ExternalId = $"ol-{archiveId}";
        }
        else if (!string.IsNullOrWhiteSpace(doc.WorkKey))
        {
            book.ExternalId = $"ol-{doc.WorkKey!.TrimStart('/')}";
            book.FileUrl = $"https://openlibrary.org{doc.WorkKey}";
        }

        return book;
    }

    private static string InferGenre(IEnumerable<string>? subjects)
    {
        if (subjects == null) return "Literature";
        var haystack = string.Join(' ', subjects).ToLowerInvariant();

        return haystack switch
        {
            _ when haystack.Contains("science fiction") => "Science Fiction",
            _ when haystack.Contains("fantasy") => "Fantasy",
            _ when haystack.Contains("mystery") || haystack.Contains("detective") => "Mystery & Detective",
            _ when haystack.Contains("horror") || haystack.Contains("gothic") => "Horror & Gothic",
            _ when haystack.Contains("romance") => "Romance",
            _ when haystack.Contains("adventure") => "Adventure",
            _ when haystack.Contains("philosoph") => "Philosophy",
            _ when haystack.Contains("history") => "History",
            _ when haystack.Contains("science") => "Science",
            _ when haystack.Contains("psycholog") => "Psychology",
            _ when haystack.Contains("poetry") => "Poetry",
            _ when haystack.Contains("drama") => "Drama",
            _ when haystack.Contains("biograph") => "Biography",
            _ when haystack.Contains("technology") => "Technology",
            _ => "Literature"
        };
    }
}
