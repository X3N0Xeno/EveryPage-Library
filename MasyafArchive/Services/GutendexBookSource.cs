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
/// Reads the public-domain Project Gutenberg catalog through the Gutendex API.
/// </summary>
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

    /// <summary>Searches Gutenberg by free-text query.</summary>
    public Task<List<Book>> SearchAsync(string query, string? genre, int limit, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/?search={Uri.EscapeDataString(query)}";
        return FetchAsync(url, genre, limit, cancellationToken);
    }

    /// <summary>Fetches works tagged with a subject / topic, e.g. "Philosophy".</summary>
    public Task<List<Book>> BrowseTopicAsync(string topic, string? genre, int limit, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/?topic={Uri.EscapeDataString(topic)}";
        return FetchAsync(url, genre ?? topic, limit, cancellationToken);
    }

    /// <summary>Most downloaded works â€” used to fill the shelf on first load.</summary>
    public Task<List<Book>> GetPopularAsync(string? genre, int limit, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/?sort=popular";
        return FetchAsync(url, genre, limit, cancellationToken);
    }

    private async Task<List<Book>> FetchAsync(string url, string? genre, int limit, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await _http.GetFromJsonAsync<GutendexResponse>(url, JsonOptions, cancellationToken);
            if (payload?.Results == null)
            {
                return new List<Book>();
            }

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
        {
            return null;
        }

        var subjects = result.Subjects
            .Where(subject => !string.IsNullOrWhiteSpace(subject))
            .Take(4)
            .ToList();

        return new Book
        {
            Title = result.Title.Trim(),
            Author = FormatAuthor(result.Authors.FirstOrDefault()?.Name),
            Category = BookGenres.ToStorage(genre ?? InferGenre(result.Subjects, result.Bookshelves)),
            Synopsis = subjects.Count > 0
                ? string.Join(" \u00B7 ", subjects)
                : "A public-domain work from the Project Gutenberg collection.",
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
        {
            return exact;
        }

        foreach (var pair in formats)
        {
            if (pair.Key.StartsWith(key, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(pair.Value))
            {
                return pair.Value;
            }
        }

        return null;
    }

    /// <summary>Gutenberg gives "Carroll, Lewis" â€” flip it to "Lewis Carroll".</summary>
    private static string FormatAuthor(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "Unknown author";
        }

        var name = rawName.Trim();
        var commaIndex = name.IndexOf(',');
        if (commaIndex <= 0)
        {
            return name;
        }

        var last = name[..commaIndex].Trim();
        var first = name[(commaIndex + 1)..].Trim();
        return string.IsNullOrWhiteSpace(first) ? last : $"{first} {last}";
    }

    private static string InferGenre(IEnumerable<string> subjects, IEnumerable<string> bookshelves)
    {
        var haystack = string.Join(' ', subjects.Concat(bookshelves)).ToLowerInvariant();

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

