using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EveryPageLibrary.Models;

namespace EveryPageLibrary.Services;

public class FederatedBookService
{
    private readonly GutendexBookSource _gutendex;
    private readonly OpenLibraryBookSource _openLibrary;
    private readonly InternetArchiveBookSource _internetArchive;
    private readonly StandardEbooksBookSource _standardEbooks;
    private readonly WikibooksBookSource _wikibooks;
    private readonly SacredTextsBookSource _sacredTexts;
    private readonly DoabBookSource _doab;

    private readonly ConcurrentDictionary<string, List<Book>> _pageCache = new(StringComparer.OrdinalIgnoreCase);

    public FederatedBookService(
        GutendexBookSource gutendex,
        OpenLibraryBookSource openLibrary,
        InternetArchiveBookSource internetArchive,
        StandardEbooksBookSource standardEbooks,
        WikibooksBookSource wikibooks,
        SacredTextsBookSource sacredTexts,
        DoabBookSource doab)
    {
        _gutendex = gutendex;
        _openLibrary = openLibrary;
        _internetArchive = internetArchive;
        _standardEbooks = standardEbooks;
        _wikibooks = wikibooks;
        _sacredTexts = sacredTexts;
        _doab = doab;
    }

    private static string BuildCacheKey(string? query, string? genre, int page) =>
        $"{(query ?? "").Trim().ToLowerInvariant()}|{(genre ?? "All").Trim().ToLowerInvariant()}|{page}";

    public bool IsCached(string? query, string? genre, int page) =>
        _pageCache.ContainsKey(BuildCacheKey(query, genre, page));

    public async Task PrefetchPageAsync(string? query, string? genre, int page, CancellationToken cancellationToken = default)
    {
        var key = BuildCacheKey(query, genre, page);
        if (_pageCache.ContainsKey(key))
            return;

        try
        {
            await SearchPagedAsync(query, genre, page, cancellationToken: cancellationToken);
        }
        catch
        {
            // Background prefetch ignores transient cancellations
        }
    }

    public async Task<List<Book>> SearchPagedAsync(
        string? query,
        string? genre,
        int page = 1,
        Action<List<Book>>? onPartialBatchReady = null,
        CancellationToken cancellationToken = default)
    {
        var key = BuildCacheKey(query, genre, page);
        if (_pageCache.TryGetValue(key, out var cached))
            return cached;

        bool hasQuery = !string.IsNullOrWhiteSpace(query);
        bool hasGenre = !string.IsNullOrWhiteSpace(genre) && !genre.Equals("All", StringComparison.OrdinalIgnoreCase);

        var tasks = new List<Task<List<Book>>>();

        if (hasQuery)
        {
            tasks.Add(_standardEbooks.SearchAsync(query!, genre, 12, page, cancellationToken));
            tasks.Add(_gutendex.SearchAsync(query!, genre, 20, page, cancellationToken));
            tasks.Add(_openLibrary.SearchAsync(query!, genre, 20, page, cancellationToken));
            tasks.Add(_internetArchive.SearchAsync(query!, genre, 20, page, cancellationToken));
            tasks.Add(_wikibooks.SearchAsync(query!, genre, 12, page, cancellationToken));
            tasks.Add(_sacredTexts.SearchAsync(query!, genre, 12, page, cancellationToken));
            tasks.Add(_doab.SearchAsync(query!, genre, 12, page, cancellationToken));
        }
        else if (hasGenre)
        {
            tasks.Add(_standardEbooks.BrowseSubjectAsync(genre!, 12, page, cancellationToken));
            tasks.Add(_gutendex.BrowseSubjectAsync(genre!, 20, page, cancellationToken));
            tasks.Add(_openLibrary.BrowseSubjectAsync(genre!, 20, page, cancellationToken));
            tasks.Add(_internetArchive.BrowseSubjectAsync(genre!, 20, page, cancellationToken));
            tasks.Add(_wikibooks.BrowseSubjectAsync(genre!, 12, page, cancellationToken));
            tasks.Add(_sacredTexts.BrowseSubjectAsync(genre!, 12, page, cancellationToken));
            tasks.Add(_doab.BrowseSubjectAsync(genre!, 12, page, cancellationToken));
        }
        else
        {
            tasks.Add(_standardEbooks.GetPopularAsync(12, page, cancellationToken));
            tasks.Add(_gutendex.GetPopularAsync(20, page, cancellationToken));
            tasks.Add(_openLibrary.GetPopularAsync(20, page, cancellationToken));
            tasks.Add(_internetArchive.GetPopularAsync(20, page, cancellationToken));
            tasks.Add(_wikibooks.GetPopularAsync(12, page, cancellationToken));
            tasks.Add(_sacredTexts.GetPopularAsync(12, page, cancellationToken));
            tasks.Add(_doab.GetPopularAsync(12, page, cancellationToken));
        }

        var accumulated = new List<Book>();
        var pending = new List<Task<List<Book>>>(tasks);

        while (pending.Count > 0)
        {
            var finished = await Task.WhenAny(pending);
            pending.Remove(finished);

            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var batch = await finished;
                if (batch.Count > 0)
                {
                    accumulated.AddRange(batch);
                    if (pending.Count > 0 && onPartialBatchReady != null)
                    {
                        onPartialBatchReady(RankAndDeduplicate(accumulated, query));
                    }
                }
            }
            catch
            {
                // Individual source failure should not halt other sources
            }
        }

        var finalMerged = RankAndDeduplicate(accumulated, query);
        if (!cancellationToken.IsCancellationRequested && finalMerged.Count > 0)
        {
            _pageCache[key] = finalMerged;
        }

        return finalMerged;
    }

    private static List<Book> RankAndDeduplicate(List<Book> raw, string? query)
    {
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenTitleAuthor = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unique = new List<Book>(raw.Count);

        foreach (var b in raw)
        {
            if (!string.IsNullOrWhiteSpace(b.ExternalId) && !seenIds.Add(b.ExternalId))
                continue;

            var sig = $"{NormalizeKey(b.Title)}|{NormalizeKey(b.Author)}";
            if (!seenTitleAuthor.Add(sig))
                continue;

            unique.Add(b);
        }

        if (string.IsNullOrWhiteSpace(query))
            return unique;

        var q = query.Trim().ToLowerInvariant();
        return unique
            .OrderByDescending(b => ComputeRelevanceScore(b, q))
            .ThenByDescending(b => b.DownloadCount)
            .ToList();
    }

    private static int ComputeRelevanceScore(Book book, string q)
    {
        var title = book.Title.ToLowerInvariant();
        var author = book.Author.ToLowerInvariant();
        int score = 0;

        if (title == q) score += 1000;
        else if (title.StartsWith(q)) score += 700;
        else if (title.Contains(q)) score += 450;

        if (author.Contains(q)) score += 300;
        if (!string.IsNullOrWhiteSpace(book.PdfUrl) || !string.IsNullOrWhiteSpace(book.EpubUrl)) score += 80;
        if (book.SourceName == "Standard Ebooks") score += 40;

        return score;
    }

    private static string NormalizeKey(string text) =>
        new string(text.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
}
