using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MasyafArchive.Models;

namespace MasyafArchive.Services;

/// <summary>
/// Queries the two open-access book catalogs (Gutendex / Project Gutenberg and
/// Open Library) in parallel and returns one merged, de-duplicated shelf.
/// </summary>
public class FederatedBookService
{
    private const int SearchLimitPerSource = 15;
    private const int FeaturedLimitPerTopic = 5;

    /// <summary>Topics pulled in parallel to fill the shelf before the reader searches.</summary>
    private static readonly string[] FeaturedTopics = { "Philosophy", "History", "Science", "Fiction" };

    private readonly GutendexBookSource _gutendex;
    private readonly OpenLibraryBookSource _openLibrary;

    public FederatedBookService(GutendexBookSource gutendex, OpenLibraryBookSource openLibrary)
    {
        _gutendex = gutendex;
        _openLibrary = openLibrary;
    }

    /// <summary>The default shelf: a spread of real public-domain books across genres.</summary>
    public async Task<List<Book>> GetFeaturedBooksAsync(CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task<List<Book>>>
        {
            _gutendex.GetPopularAsync("Literature", FeaturedLimitPerTopic, cancellationToken),
            _openLibrary.BrowseSubjectAsync("literature", "Literature", FeaturedLimitPerTopic, cancellationToken)
        };

        foreach (var topic in FeaturedTopics)
        {
            tasks.Add(_gutendex.BrowseTopicAsync(topic, topic, FeaturedLimitPerTopic, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);
        return Merge(results);
    }

    /// <summary>Unified search across both catalogs, optionally narrowed to a genre.</summary>
    public async Task<List<Book>> SearchAsync(string query, string? genre, CancellationToken cancellationToken = default)
    {
        var trimmed = (query ?? string.Empty).Trim();
        var hasGenre = !string.IsNullOrWhiteSpace(genre)
                       && !genre.Equals("All", StringComparison.OrdinalIgnoreCase);

        if (trimmed.Length == 0 && !hasGenre)
        {
            return await GetFeaturedBooksAsync(cancellationToken);
        }

        var tasks = new List<Task<List<Book>>>();

        if (trimmed.Length > 0)
        {
            tasks.Add(_gutendex.SearchAsync(trimmed, hasGenre ? genre : null, SearchLimitPerSource, cancellationToken));
            tasks.Add(_openLibrary.SearchAsync(trimmed, hasGenre ? genre : null, SearchLimitPerSource, cancellationToken));
        }

        if (hasGenre)
        {
            tasks.Add(_gutendex.BrowseTopicAsync(genre!, genre, SearchLimitPerSource, cancellationToken));
            tasks.Add(_openLibrary.BrowseSubjectAsync(genre!.ToLowerInvariant(), genre, SearchLimitPerSource, cancellationToken));
        }

        if (tasks.Count == 0)
        {
            return new List<Book>();
        }

        var results = await Task.WhenAll(tasks);
        return Merge(results);
    }

    /// <summary>Fetches a single genre's shelf from both catalogs.</summary>
    public async Task<List<Book>> BrowseGenreAsync(string genre, CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(
            _gutendex.BrowseTopicAsync(genre, genre, SearchLimitPerSource, cancellationToken),
            _openLibrary.BrowseSubjectAsync(genre.ToLowerInvariant(), genre, SearchLimitPerSource, cancellationToken));

        return Merge(results);
    }

    /// <summary>Flattens the batches, drops duplicates and books with no usable download.</summary>
    private static List<Book> Merge(IEnumerable<List<Book>> batches)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var merged = new List<Book>();

        foreach (var book in batches.SelectMany(batch => batch))
        {
            var key = BuildKey(book);
            if (!seen.Add(key))
            {
                continue;
            }

            merged.Add(book);
        }

        return merged;
    }

    private static string BuildKey(Book book)
    {
        if (!string.IsNullOrWhiteSpace(book.ExternalId))
        {
            return book.ExternalId!;
        }

        return $"{book.Title}|{book.Author}".ToLowerInvariant();
    }
}
