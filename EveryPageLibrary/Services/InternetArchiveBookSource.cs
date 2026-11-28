using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EveryPageLibrary.Models;

namespace EveryPageLibrary.Services;

public class InternetArchiveBookSource
{
    private const string SearchEndpoint = "https://archive.org/advancedsearch.php";
    private const string MetadataEndpoint = "https://archive.org/metadata";
    private const string SourceName = "Internet Archive";

    private readonly HttpClient _http;

    public InternetArchiveBookSource(HttpClient http)
    {
        _http = http;
    }

    public async Task<(string? PdfUrl, string? EpubUrl, bool IsRestricted)> ResolveDownloadFilesAsync(
        string identifier,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return (null, null, false);

        var url = $"{MetadataEndpoint}/{Uri.EscapeDataString(identifier)}";

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return (null, null, false);

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            bool itemAccessRestricted = false;
            if (doc.RootElement.TryGetProperty("metadata", out var metaObj) &&
                metaObj.ValueKind == JsonValueKind.Object)
            {
                var restrictedFlag = ReadFirstString(metaObj, "access-restricted-item");
                if (string.Equals(restrictedFlag, "true", StringComparison.OrdinalIgnoreCase))
                {
                    itemAccessRestricted = true;
                }

                var collections = ReadStringList(metaObj, "collection");
                if (collections.Any(c =>
                    c.Equals("inlibrary", StringComparison.OrdinalIgnoreCase) ||
                    c.Equals("lending", StringComparison.OrdinalIgnoreCase) ||
                    c.Equals("printdisabled", StringComparison.OrdinalIgnoreCase)))
                {
                    itemAccessRestricted = true;
                }
            }

            if (!doc.RootElement.TryGetProperty("files", out var filesArr) ||
                filesArr.ValueKind != JsonValueKind.Array)
            {
                return (null, null, itemAccessRestricted);
            }

            var publicPdfs = new List<string>();
            var publicEpubs = new List<string>();
            bool sawRestrictedFile = itemAccessRestricted;

            foreach (var file in filesArr.EnumerateArray())
            {
                var name = ReadFirstString(file, "name");
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var format = ReadFirstString(file, "format") ?? string.Empty;
                var isPrivate = string.Equals(ReadFirstString(file, "private"), "true", StringComparison.OrdinalIgnoreCase);

                bool isDrmLocked = name.Contains("_lcp.", StringComparison.OrdinalIgnoreCase) ||
                                   format.Contains("LCP", StringComparison.OrdinalIgnoreCase) ||
                                   format.Contains("ACS", StringComparison.OrdinalIgnoreCase) ||
                                   format.Contains("Encrypted", StringComparison.OrdinalIgnoreCase);

                bool isPdf = name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
                             format.Contains("PDF", StringComparison.OrdinalIgnoreCase);
                bool isEpub = name.EndsWith(".epub", StringComparison.OrdinalIgnoreCase) ||
                              format.Equals("EPUB", StringComparison.OrdinalIgnoreCase);

                if ((isPrivate || isDrmLocked || itemAccessRestricted) && (isPdf || isEpub))
                {
                    sawRestrictedFile = true;
                    continue;
                }

                if (isPdf)
                {
                    publicPdfs.Add(name);
                }
                else if (isEpub)
                {
                    publicEpubs.Add(name);
                }
            }

            var bestPdf = publicPdfs
                .OrderBy(n => n.EndsWith("_bw.pdf", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ThenBy(n => n.Length)
                .FirstOrDefault();

            var bestEpub = publicEpubs
                .OrderBy(n => n.Length)
                .FirstOrDefault();

            var encodedId = Uri.EscapeDataString(identifier);
            var pdfUrl = !string.IsNullOrWhiteSpace(bestPdf)
                ? $"https://archive.org/download/{encodedId}/{EncodeRelativePath(bestPdf)}"
                : null;
            var epubUrl = !string.IsNullOrWhiteSpace(bestEpub)
                ? $"https://archive.org/download/{encodedId}/{EncodeRelativePath(bestEpub)}"
                : null;

            bool isRestricted = pdfUrl == null && epubUrl == null && sawRestrictedFile;
            return (pdfUrl, epubUrl, isRestricted);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InternetArchiveBookSource] Metadata lookup failed ({identifier}): {ex.Message}");
            return (null, null, false);
        }
    }

    private static string EncodeRelativePath(string path) =>
        string.Join("/", path.Split('/').Select(Uri.EscapeDataString));

    public Task<List<Book>> GetPopularAsync(int limit = 28, int page = 1, CancellationToken cancellationToken = default)
    {
        var q = "mediatype:texts AND NOT collection:inlibrary AND NOT collection:printdisabled AND NOT access-restricted-item:true AND (subject:\"fiction\" OR subject:\"literature\" OR subject:\"classics\")";
        return FetchAsync(q, null, limit, page, cancellationToken);
    }

    public Task<List<Book>> SearchAsync(string query, string? genre, int limit = 28, int page = 1, CancellationToken cancellationToken = default)
    {
        var safeQuery = EscapeQuery(query);
        var q = !string.IsNullOrWhiteSpace(genre) && !genre.Equals("All", StringComparison.OrdinalIgnoreCase)
            ? $"({safeQuery}) AND subject:\"{EscapeQuery(BookGenres.ToInternetArchiveSubject(genre))}\" AND mediatype:texts AND NOT collection:inlibrary AND NOT collection:printdisabled AND NOT access-restricted-item:true"
            : $"({safeQuery}) AND mediatype:texts AND NOT collection:inlibrary AND NOT collection:printdisabled AND NOT access-restricted-item:true";

        return FetchAsync(q, genre, limit, page, cancellationToken);
    }

    public Task<List<Book>> BrowseSubjectAsync(string genre, int limit = 28, int page = 1, CancellationToken cancellationToken = default)
    {
        var subject = EscapeQuery(BookGenres.ToInternetArchiveSubject(genre));
        var q = $"subject:\"{subject}\" AND mediatype:texts AND NOT collection:inlibrary AND NOT collection:printdisabled AND NOT access-restricted-item:true";
        return FetchAsync(q, genre, limit, page, cancellationToken);
    }

    private async Task<List<Book>> FetchAsync(string q, string? genre, int limit, int page, CancellationToken cancellationToken)
    {
        var url = $"{SearchEndpoint}?q={Uri.EscapeDataString(q)}" +
                  "&fl[]=identifier&fl[]=title&fl[]=creator&fl[]=date&fl[]=description&fl[]=subject&fl[]=downloads" +
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
                var mapped = Map(item, genre);
                if (mapped != null)
                {
                    results.Add(mapped);
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InternetArchiveBookSource] Request failed: {ex.Message}");
            return new List<Book>();
        }
    }

    private static Book? Map(JsonElement item, string? genre)
    {
        var identifier = ReadFirstString(item, "identifier");
        var title = ReadFirstString(item, "title");
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(title))
            return null;

        var creator = ReadFirstString(item, "creator") ?? "Unknown author";
        var rawDate = ReadFirstString(item, "date") ?? string.Empty;
        var year = ExtractYear(rawDate);
        var subjects = ReadStringList(item, "subject");
        var description = ReadFirstString(item, "description");

        var synopsis = !string.IsNullOrWhiteSpace(description)
            ? StripHtml(description)
            : (subjects.Count > 0
                ? string.Join(" \u00B7 ", subjects.Take(4))
                : "Digitized full-text edition from the Internet Archive collection.");

        return new Book
        {
            Title = title.Trim(),
            Author = creator.Trim(),
            Category = BookGenres.ToStorage(genre ?? InferGenre(subjects, title)),
            Synopsis = synopsis,
            PublicationYear = year,
            CoverUrl = $"https://archive.org/services/img/{identifier}",
            PdfUrl = $"https://archive.org/download/{identifier}/{identifier}.pdf",
            EpubUrl = $"https://archive.org/download/{identifier}/{identifier}.epub",
            FileUrl = $"https://archive.org/details/{identifier}",
            SourceName = SourceName,
            ExternalId = $"ia-{identifier}",
            UploadedBy = SourceName,
            DownloadCount = ReadInt(item, "downloads"),
            ContentText = string.Empty
        };
    }

    private static string EscapeQuery(string input) =>
        input.Replace("\"", " ").Replace("(", " ").Replace(")", " ").Trim();

    private static string ExtractYear(string date)
    {
        if (string.IsNullOrWhiteSpace(date)) return string.Empty;
        for (int i = 0; i <= date.Length - 4; i++)
        {
            if (char.IsDigit(date[i]) && char.IsDigit(date[i + 1]) && char.IsDigit(date[i + 2]) && char.IsDigit(date[i + 3]))
                return date.Substring(i, 4);
        }
        return string.Empty;
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

    private static List<string> ReadStringList(JsonElement obj, string prop)
    {
        var list = new List<string>();
        if (!obj.TryGetProperty(prop, out var el)) return list;

        if (el.ValueKind == JsonValueKind.String)
        {
            var val = el.GetString();
            if (!string.IsNullOrWhiteSpace(val)) list.Add(val);
        }
        else if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in el.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.String)
                {
                    var val = entry.GetString();
                    if (!string.IsNullOrWhiteSpace(val)) list.Add(val);
                }
            }
        }
        return list;
    }

    private static int ReadInt(JsonElement obj, string prop)
    {
        if (!obj.TryGetProperty(prop, out var el)) return 0;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var num)) return num;
        if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var parsed)) return parsed;
        return 0;
    }

    private static string InferGenre(IEnumerable<string> subjects, string title)
    {
        var haystack = $"{title} {string.Join(' ', subjects)}".ToLowerInvariant();
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
