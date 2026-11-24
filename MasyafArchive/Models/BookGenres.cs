using System;
using System.Collections.Generic;
using System.Linq;

namespace MasyafArchive.Models;

/// <summary>
/// Central list of the genres the library filters by. Kept in one place so the
/// filter pills, upload form, edit form and badges never drift apart.
/// </summary>
public static class BookGenres
{
    /// <summary>The value stored in public.books.category for a book with no genre.</summary>
    public const string GeneralStorageValue = "Codex";

    public const string General = "General";

    /// <summary>Genre filter pills, in display order.</summary>
    public static readonly IReadOnlyList<string> Filters = new[]
    {
        "All",
        "Philosophy",
        "History",
        "Science",
        "Fiction",
        "Literature",
        "Technology",
        "Biography"
    };

    /// <summary>Selectable genres for the upload and edit forms (no "All").</summary>
    public static readonly IReadOnlyList<string> Selectable = Filters
        .Where(genre => !genre.Equals("All", StringComparison.OrdinalIgnoreCase))
        .ToArray();

    /// <summary>Turns a stored category value into the label shown to readers.</summary>
    public static string ToLabel(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return General;
        }

        return category.Equals(GeneralStorageValue, StringComparison.OrdinalIgnoreCase)
            ? General
            : category;
    }

    /// <summary>Turns a display genre into the value persisted in the database.</summary>
    public static string ToStorage(string? genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
        {
            return GeneralStorageValue;
        }

        return genre.Equals(General, StringComparison.OrdinalIgnoreCase)
            ? GeneralStorageValue
            : genre.Trim();
    }
}
