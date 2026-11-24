using System;
using System.Collections.Generic;
using System.Linq;

namespace MasyafArchive.Models;

public static class BookGenres
{
    public const string GeneralStorageValue = "Codex";
    public const string General = "General";

    public static readonly IReadOnlyList<string> Filters = new[]
    {
        "All",
        "Literature",
        "Fiction",
        "Classics",
        "Science Fiction",
        "Fantasy",
        "Mystery & Detective",
        "Horror & Gothic",
        "Romance",
        "Adventure",
        "Philosophy",
        "History",
        "Biography",
        "Science",
        "Psychology",
        "Poetry",
        "Drama",
        "Mythology & Folklore",
        "Children's",
        "Religion & Spirituality",
        "Politics & Society",
        "Economics",
        "Art & Music",
        "Technology",
        "Travel"
    };

    public static readonly IReadOnlyList<string> Selectable = Filters
        .Where(genre => !genre.Equals("All", StringComparison.OrdinalIgnoreCase))
        .ToArray();

    public static string ToLabel(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return General;

        return category.Equals(GeneralStorageValue, StringComparison.OrdinalIgnoreCase)
            ? General
            : category;
    }

    public static string ToStorage(string? genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
            return GeneralStorageValue;

        return genre.Equals(General, StringComparison.OrdinalIgnoreCase)
            ? GeneralStorageValue
            : genre.Trim();
    }

    public static string ToGutendexTopic(string genre) => genre.ToLowerInvariant() switch
    {
        "classics" => "best books ever listings",
        "mystery & detective" => "detective",
        "horror & gothic" => "horror",
        "mythology & folklore" => "mythology",
        "children's" => "children",
        "religion & spirituality" => "religion",
        "politics & society" => "politics",
        "art & music" => "art",
        _ => genre.ToLowerInvariant()
    };

    public static string ToOpenLibrarySubject(string genre) => genre.ToLowerInvariant() switch
    {
        "science fiction" => "science_fiction",
        "mystery & detective" => "mystery_and_detective_stories",
        "horror & gothic" => "horror",
        "mythology & folklore" => "mythology",
        "children's" => "juvenile_literature",
        "religion & spirituality" => "religion",
        "politics & society" => "political_science",
        "art & music" => "art",
        _ => genre.ToLowerInvariant().Replace(' ', '_')
    };
}
