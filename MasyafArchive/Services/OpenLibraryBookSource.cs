using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MasyafArchive.Models;

namespace MasyafArchive.Services;

/// <summary>
/// Reads the Open Library search index and turns Internet Archive scans into
/// direct PDF / EPUB download links.
/// </summary>
public class OpenLibraryBookSource
{
    private const string SearchUrl = "https://openlibrary.org/search.json";
    private const string SourceName = "Open Library";
    private const int ResultLimit = 15;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Base URL of the Internet Archive item viewer (used by the inline reader).</summary>
    public const string ArchiveEmbedBaseUrl = "https://archive.org/embed/";

    private readonly HttpClient _http;

    public OpenLibraryBookSource(HttpClient http)
    {
        _http = http;
    }

    public Task<List<Book>> SearchAsync(string query, string? genre, int limit, CancellationToken cancellationToken = default)
    {
        var url = $"{SearchUrl}?q={Uri.EscapeDataString(query)}&has_fulltext=true&limit={ResultLimit}";
        return FetchAsync(url, genre, limit, cancellationToken);
    }

    public Task<List<Book>> BrowseSubjectAsync(string subject, string? genre, int limit, CancellationToken cancellationToken = default)
    {
        var url = $"{SearchUrl}?q={Uri.EscapeDataString($"subject:{subject}")}&has_fulltext=true&limit={ResultLimit}";
        return FetchAsync(url, genre ?? subject, limit, cancellationToken);
    }

    private async Task<List<Book>> FetchAsync(string url, string? genre, int limit, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await _http.GetFromJsonAsync<OpenLibrarySearchResponse>(url, JsonOptions, cancellationToken);
            if (payload?.Docs == null)
            {
                return new List<Book>();
            }

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
        {
            return null;
        }

        var archiveId = doc.InternetArchiveIdentifiers?
            .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));

        var subjects = doc.Subject?
            .Where(subject => !string.IsNullOrWhiteSpace(subject))
            .Take(4)
            .ToList() ?? new List<string>();

        var book = new Book
        {
            Title = doc.Title.Trim(),
            Author = doc.AuthorName?.FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
                     ?? "Unknown author",
            Category = BookGenres.ToStorage(genre ?? InferGenre(doc.Subject)),
            Synopsis = subjects.Count > 0
                ? string.Join(" \u00B7 ", subjects)
                : "A full-text scanned work from the Open Library collection.",
            PublicationYear = doc.FirstPublishYear?.ToString() ?? string.Empty,
            CoverUrl = doc.CoverId.HasValue
                ? $"https://covers.openlibrary.org/b/id/{doc.CoverId.Value}-M.jpg"
                : null,
            SourceName = SourceName,
            UploadedBy = SourceName,
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
            // No downloadable scan, but the record can still be indexed for comments.
            book.ExternalId = $"ol-{doc.WorkKey!.TrimStart('/')}";
        }

        return book;
    }

    private static string InferGenre(IEnumerable<string>? subjects)
    {
        if (subjects == null)
        {
            return "Literature";
        }

        var haystack = string.Join(' ', subjects).ToLowerInvariant();

        return haystack switch
        {
            _ when haystack.Contains("philosoph") => "Philosophy",
            _ when haystack.Contains("history") || haystack.Contains("historical") => "History",
            _ when haystack.Contains("science") || haystack.Contains("physics")
                   || haystack.Contains("chemistry") || haystack.Contains("biology") => "Science",
            _ when haystack.Contains("biograph") || haystack.Contains("autobiograph") => "Biography",
            _ when haystack.Contains("technology") || haystack.Contains("engineering")
                   || haystack.Contains("mechanic") => "Technology",
            _ when haystack.Contains("fiction") || haystack.Contains("novel")
                   || haystack.Contains("short stories") => "Fiction",
            _ => "Literature"
        };
    }
}

