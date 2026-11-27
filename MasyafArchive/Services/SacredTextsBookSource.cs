using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EverydayLibrary.Models;

namespace EverydayLibrary.Services;

public class SacredTextsBookSource
{
    private const string SearchEndpoint = "https://archive.org/advancedsearch.php";
    private const string SourceName = "Sacred Texts";

    private readonly HttpClient _http;

    public SacredTextsBookSource(HttpClient http)
    {
        _http = http;
    }

    public Task<List<Book>> GetPopularAsync(int limit = 16, int page = 1, CancellationToken cancellationToken = default)
    {
        return FetchAsync("(collection:sacred-texts OR subject:\"Sacred Texts\" OR subject:\"mythology\" OR subject:\"folklore\") AND mediatype:texts", null, limit, page, cancellationToken);
    }

    public Task<List<Book>> SearchAsync(string query, string? genre, int limit = 16, int page = 1, CancellationToken cancellationToken = default)
    {
        var safeQuery = query.Replace("\"", " ").Replace("(", " ").Replace(")", " ").Trim();
        var q = $"(collection:sacred-texts OR subject:\"Sacred Texts\") AND ({safeQuery}) AND mediatype:texts";
        return FetchAsync(q, genre, limit, page, cancellationToken);
    }

    public Task<List<Book>> BrowseSubjectAsync(string genre, int limit = 16, int page = 1, CancellationToken cancellationToken = default)
    {
        var keyword = BookGenres.ToSearchKeyword(genre);
        var q = $"(collection:sacred-texts OR subject:\"Sacred Texts\") AND ({keyword}) AND mediatype:texts";
        return FetchAsync(q, genre, limit, page, cancellationToken);
    }

    private async Task<List<Book>> FetchAsync(string q, string? genre, int limit, int page, CancellationToken cancellationToken)
    {
        var url = $"{SearchEndpoint}?q={Uri.EscapeDataString(q)}" +
                  "&fl[]=identifier&fl[]=title&fl[]=creator&fl[]=date&fl[]=description&fl[]=downloads" +
                  $"&sort[]=downloads+desc&rows={limit}&page={Math.Max(1, page)}&output=json";

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new List<Book>();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("response", out var respElem) ||
                !respElem.TryGetProperty("docs", out var docsElem) ||
                docsElem.ValueKind != JsonValueKind.Array)
            {
                return new List<Book>();
            }

            var results = new List<Book>();
            foreach (var item in docsElem.EnumerateArray())
            {
                var identifier = ReadFirstString(item, "identifier");
                var title = ReadFirstString(item, "title");
                if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(title))
                    continue;

                var creator = ReadFirstString(item, "creator") ?? "Sacred Texts Archive";
                var date = ReadFirstString(item, "date") ?? string.Empty;
                var year = new string(date.Where(char.IsDigit).Take(4).ToArray());
                var description = ReadFirstString(item, "description");

                results.Add(new Book
                {
                    Title = title.Trim(),
                    Author = creator.Trim(),
                    Category = BookGenres.ToStorage(genre ?? "Humanities-Arts"),
                    Synopsis = !string.IsNullOrWhiteSpace(description)
                        ? StripHtml(description)
                        : "Public domain text on religion, mythology, folklore, or comparative philosophy from Sacred Texts.",
                    PublicationYear = year,
                    CoverUrl = $"https://archive.org/services/img/{identifier}",
                    PdfUrl = $"https://archive.org/download/{identifier}/{identifier}.pdf",
                    EpubUrl = $"https://archive.org/download/{identifier}/{identifier}.epub",
                    FileUrl = $"https://archive.org/details/{identifier}",
                    SourceName = SourceName,
                    ExternalId = $"st-{identifier}",
                    UploadedBy = SourceName,
                    DownloadCount = ReadInt(item, "downloads"),
                    ContentText = string.Empty
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SacredTextsBookSource] Request failed: {ex.Message}");
            return new List<Book>();
        }
    }

    private static string StripHtml(string text)
    {
        var chars = new char[text.Length];
        int idx = 0;
        bool inside = false;
        foreach (char c in text)
        {
            if (c == '<') { inside = true; continue; }
            if (c == '>') { inside = false; continue; }
            if (!inside) chars[idx++] = c;
        }
        return new string(chars, 0, idx).Trim();
    }

    private static string? ReadFirstString(JsonElement obj, string prop)
    {
        if (!obj.TryGetProperty(prop, out var el)) return null;
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Array => el.EnumerateArray().FirstOrDefault(e => e.ValueKind == JsonValueKind.String).GetString(),
            JsonValueKind.Number => el.ToString(),
            _ => null
        };
    }

    private static int ReadInt(JsonElement obj, string prop)
    {
        if (!obj.TryGetProperty(prop, out var el)) return 0;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var num)) return num;
        if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var parsed)) return parsed;
        return 0;
    }
}
