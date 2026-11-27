using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EverydayLibrary.Models;

namespace EverydayLibrary.Services;

public class DoabBookSource
{
    private const string Endpoint = "https://directory.doabooks.org/rest/search";
    private const string SourceName = "DOAB Academic";

    private readonly HttpClient _http;

    public DoabBookSource(HttpClient http)
    {
        _http = http;
    }

    public Task<List<Book>> GetPopularAsync(int limit = 20, int page = 1, CancellationToken cancellationToken = default)
    {
        return FetchAsync("literature OR philosophy OR science", "Philosophy", limit, page, cancellationToken);
    }

    public Task<List<Book>> SearchAsync(string query, string? genre, int limit = 20, int page = 1, CancellationToken cancellationToken = default)
    {
        var q = !string.IsNullOrWhiteSpace(genre) && !genre.Equals("All", StringComparison.OrdinalIgnoreCase)
            ? $"({query}) AND ({genre})"
            : query;
        return FetchAsync(q, genre, limit, page, cancellationToken);
    }

    public Task<List<Book>> BrowseSubjectAsync(string genre, int limit = 20, int page = 1, CancellationToken cancellationToken = default)
    {
        return FetchAsync($"\"{genre}\"", genre, limit, page, cancellationToken);
    }

    private async Task<List<Book>> FetchAsync(string query, string? genre, int limit, int page, CancellationToken cancellationToken)
    {
        var offset = (Math.Max(1, page) - 1) * limit;
        var url = $"{Endpoint}?query={Uri.EscapeDataString(query)}&expand=metadata,bitstreams&limit={limit}&offset={offset}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");

            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new List<Book>();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return new List<Book>();

            var books = new List<Book>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var mapped = Map(item, genre);
                if (mapped != null)
                {
                    books.Add(mapped);
                }
            }

            return books;
        }
        catch
        {
            return new List<Book>();
        }
    }

    private static Book? Map(JsonElement item, string? genre)
    {
        string? title = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
        string? author = null;
        string? synopsis = null;
        string year = string.Empty;
        string? handle = item.TryGetProperty("handle", out var hEl) ? hEl.GetString() : null;

        if (item.TryGetProperty("metadata", out var metaArr) && metaArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var m in metaArr.EnumerateArray())
            {
                var key = m.TryGetProperty("key", out var kEl) ? kEl.GetString() : null;
                var val = m.TryGetProperty("value", out var vEl) ? vEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(val)) continue;

                if (key == "dc.title" && string.IsNullOrWhiteSpace(title)) title = val;
                else if (key == "dc.contributor.author" && string.IsNullOrWhiteSpace(author)) author = val;
                else if (key == "dc.description.abstract" && string.IsNullOrWhiteSpace(synopsis)) synopsis = val;
                else if (key == "dc.date.issued" && string.IsNullOrWhiteSpace(year) && val.Length >= 4) year = val[..4];
            }
        }

        if (string.IsNullOrWhiteSpace(title))
            return null;

        string? pdfUrl = null;
        if (item.TryGetProperty("bitstreams", out var bsArr) && bsArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var bs in bsArr.EnumerateArray())
            {
                var link = bs.TryGetProperty("retrieveLink", out var lEl) ? lEl.GetString() : null;
                var mime = bs.TryGetProperty("mimeType", out var mEl) ? mEl.GetString() : null;
                if (!string.IsNullOrWhiteSpace(link) && (mime?.Contains("pdf", StringComparison.OrdinalIgnoreCase) == true || link.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)))
                {
                    pdfUrl = link.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? link
                        : $"https://directory.doabooks.org{link}";
                    break;
                }
            }
        }

        return new Book
        {
            Title = title.Trim(),
            Author = string.IsNullOrWhiteSpace(author) ? "Academic Press" : author.Trim(),
            Category = BookGenres.ToStorage(genre ?? "Science"),
            Synopsis = string.IsNullOrWhiteSpace(synopsis)
                ? "Peer-reviewed open-access book from the Directory of Open Access Books (DOAB)."
                : synopsis.Trim(),
            PublicationYear = year,
            PdfUrl = pdfUrl,
            FileUrl = pdfUrl ?? (!string.IsNullOrWhiteSpace(handle) ? $"https://directory.doabooks.org/handle/{handle}" : null),
            SourceName = SourceName,
            ExternalId = $"doab-{handle ?? title.GetHashCode().ToString()}",
            UploadedBy = SourceName,
            DownloadCount = 1,
            ContentText = string.Empty
        };
    }
}
