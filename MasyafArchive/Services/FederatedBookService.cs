using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MasyafArchive.Models;

namespace MasyafArchive.Services;

public class FederatedBookService
{
    private const int PageSizePerSource = 28;
    private static readonly TimeSpan SourceTimeout = TimeSpan.FromMilliseconds(5000);

    private readonly InternetArchiveBookSource _internetArchive;
    private readonly OpenLibraryBookSource _openLibrary;
    private readonly GutendexBookSource _gutendex;
    private readonly WikisourceBookSource _wikisource;
    private readonly DoabBookSource _doab;

    private readonly Dictionary<string, List<Book>> _pageCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Task<List<Book>>> _inFlight = new(StringComparer.OrdinalIgnoreCase);

    public FederatedBookService(
        InternetArchiveBookSource internetArchive,
        OpenLibraryBookSource openLibrary,
        GutendexBookSource gutendex,
        WikisourceBookSource wikisource,
        DoabBookSource doab)
    {
        _internetArchive = internetArchive;
        _openLibrary = openLibrary;
        _gutendex = gutendex;
        _wikisource = wikisource;
        _doab = doab;
    }

    public bool IsCached(string query, string? genre, int page)
    {
        var key = BuildCacheKey(query, genre, page);
        return _pageCache.TryGetValue(key, out var list) && list.Count > 0;
    }

    public Task PrefetchPageAsync(string query, string? genre, int page, CancellationToken cancellationToken = default)
    {
        if (IsCached(query, genre, page))
            return Task.CompletedTask;

        return SearchPagedAsync(query, genre, page, onPartialBatchReady: null, cancellationToken);
    }

    public Task<List<Book>> SearchPagedAsync(
        string query,
        string? genre,
        int page = 1,
        Action<List<Book>>? onPartialBatchReady = null,
        CancellationToken cancellationToken = default)
    {
        var trimmed = (query ?? string.Empty).Trim();
        var normalizedGenre = string.IsNullOrWhiteSpace(genre) ? "All" : genre.Trim();
        var cacheKey = BuildCacheKey(trimmed, normalizedGenre, page);

        if (_pageCache.TryGetValue(cacheKey, out var cached) && cached.Count > 0)
        {
            onPartialBatchReady?.Invoke(cached);
            return Task.FromResult(cached);
        }

        if (_inFlight.TryGetValue(cacheKey, out var existingTask))
        {
            return AttachToInFlightAsync(existingTask, onPartialBatchReady);
        }

        var task = ExecuteFetchAsync(cacheKey, trimmed, normalizedGenre, page, onPartialBatchReady, cancellationToken);
        _inFlight[cacheKey] = task;
        return task;
    }

    private static async Task<List<Book>> AttachToInFlightAsync(
        Task<List<Book>> inFlightTask,
        Action<List<Book>>? onPartialBatchReady)
    {
        var result = await inFlightTask;
        if (result.Count > 0)
        {
            onPartialBatchReady?.Invoke(result);
        }
        return result;
    }

    private async Task<List<Book>> ExecuteFetchAsync(
        string cacheKey,
        string trimmed,
        string normalizedGenre,
        int page,
        Action<List<Book>>? onPartialBatchReady,
        CancellationToken cancellationToken)
    {
        try
        {
            var hasGenre = !normalizedGenre.Equals("All", StringComparison.OrdinalIgnoreCase);
            var pending = new List<Task<List<Book>>>();

            if (trimmed.Length > 0)
            {
                var g = hasGenre ? normalizedGenre : null;
                pending.Add(WithTimeout(_internetArchive.SearchAsync(trimmed, g, PageSizePerSource, page, cancellationToken), cancellationToken));
                pending.Add(WithTimeout(_openLibrary.SearchAsync(trimmed, g, PageSizePerSource, page, cancellationToken), cancellationToken));
                pending.Add(WithTimeout(_gutendex.SearchAsync(trimmed, g, PageSizePerSource, page, cancellationToken), cancellationToken));
                pending.Add(WithTimeout(_wikisource.SearchAsync(trimmed, g, 18, page, cancellationToken), cancellationToken));
                pending.Add(WithTimeout(_doab.SearchAsync(trimmed, g, 16, page, cancellationToken), cancellationToken));
            }
            else if (hasGenre)
            {
                pending.Add(WithTimeout(_internetArchive.BrowseSubjectAsync(normalizedGenre, PageSizePerSource, page, cancellationToken), cancellationToken));
                pending.Add(WithTimeout(_openLibrary.BrowseSubjectAsync(normalizedGenre, PageSizePerSource, page, cancellationToken), cancellationToken));
                pending.Add(WithTimeout(_gutendex.BrowseTopicAsync(normalizedGenre, PageSizePerSource, page, cancellationToken), cancellationToken));
                pending.Add(WithTimeout(_wikisource.BrowseSubjectAsync(normalizedGenre, 18, page, cancellationToken), cancellationToken));
                pending.Add(WithTimeout(_doab.BrowseSubjectAsync(normalizedGenre, 16, page, cancellationToken), cancellationToken));
            }
            else
            {
                pending.Add(WithTimeout(_internetArchive.GetPopularAsync(PageSizePerSource, page, cancellationToken), cancellationToken));
                pending.Add(WithTimeout(_openLibrary.GetPopularAsync(PageSizePerSource, page, cancellationToken), cancellationToken));
                pending.Add(WithTimeout(_gutendex.GetPopularAsync(PageSizePerSource, page, cancellationToken), cancellationToken));
                pending.Add(WithTimeout(_wikisource.GetPopularAsync(18, page, cancellationToken), cancellationToken));
                pending.Add(WithTimeout(_doab.GetPopularAsync(16, page, cancellationToken), cancellationToken));
            }

            var batches = new List<List<Book>>();
            while (pending.Count > 0)
            {
                var finished = await Task.WhenAny(pending);
                pending.Remove(finished);

                var batch = await finished;
                if (batch.Count > 0)
                {
                    batches.Add(batch);
                    onPartialBatchReady?.Invoke(InterleaveAndRank(batches, trimmed));
                }
            }

            var finalMerged = InterleaveAndRank(batches, trimmed);
            if (finalMerged.Count > 0)
            {
                _pageCache[cacheKey] = finalMerged;
            }

            return finalMerged;
        }
        finally
        {
            _inFlight.Remove(cacheKey);
        }
    }

    private static async Task<List<Book>> WithTimeout(Task<List<Book>> sourceTask, CancellationToken externalToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        var delayTask = Task.Delay(SourceTimeout, timeoutCts.Token);
        var winner = await Task.WhenAny(sourceTask, delayTask);
        if (winner == sourceTask)
        {
            timeoutCts.Cancel();
            return await sourceTask;
        }

        return new List<Book>();
    }

    private static string BuildCacheKey(string query, string? genre, int page)
    {
        var q = (query ?? string.Empty).Trim();
        var g = string.IsNullOrWhiteSpace(genre) ? "All" : genre.Trim();
        return $"{q}|{g}|p{page}";
    }

    private static List<Book> InterleaveAndRank(List<List<Book>> batches, string query)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var interleaved = new List<Book>();
        var maxLen = batches.Count == 0 ? 0 : batches.Max(b => b.Count);

        for (int i = 0; i < maxLen; i++)
        {
            foreach (var batch in batches)
            {
                if (i >= batch.Count) continue;
                var book = batch[i];
                var key = BuildKey(book);
                if (seen.Add(key))
                {
                    interleaved.Add(book);
                }
            }
        }

        if (string.IsNullOrWhiteSpace(query))
            return interleaved;

        // Bubble strong title matches (like "No Longer Human") to the very top across all sources
        return interleaved
            .OrderByDescending(b => ScoreTitleMatch(b.Title, query))
            .ToList();
    }

    private static int ScoreTitleMatch(string title, string query)
    {
        if (string.IsNullOrWhiteSpace(title)) return 0;
        if (title.Equals(query, StringComparison.OrdinalIgnoreCase)) return 3;
        if (title.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 2;
        if (title.Contains(query, StringComparison.OrdinalIgnoreCase)) return 1;
        return 0;
    }

    private static string BuildKey(Book book)
    {
        if (!string.IsNullOrWhiteSpace(book.ExternalId))
            return book.ExternalId!;

        return $"{book.Title}|{book.Author}".ToLowerInvariant();
    }
}
