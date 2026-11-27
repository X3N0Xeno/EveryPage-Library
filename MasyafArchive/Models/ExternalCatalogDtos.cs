using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EveryPageLibrary.Models;

public class GutendexResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("results")]
    public List<GutendexBook> Results { get; set; } = new();
}

public class GutendexBook
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("authors")]
    public List<GutendexAuthor> Authors { get; set; } = new();

    [JsonPropertyName("subjects")]
    public List<string> Subjects { get; set; } = new();

    [JsonPropertyName("bookshelves")]
    public List<string> Bookshelves { get; set; } = new();

    [JsonPropertyName("formats")]
    public Dictionary<string, string> Formats { get; set; } = new();

    [JsonPropertyName("download_count")]
    public int DownloadCount { get; set; }
}

public class GutendexAuthor
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("birth_year")]
    public int? BirthYear { get; set; }

    [JsonPropertyName("death_year")]
    public int? DeathYear { get; set; }
}

public class OpenLibrarySearchResponse
{
    [JsonPropertyName("numFound")]
    public int NumFound { get; set; }

    [JsonPropertyName("docs")]
    public List<OpenLibraryDoc> Docs { get; set; } = new();
}

public class OpenLibraryDoc
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("author_name")]
    public List<string>? AuthorName { get; set; }

    [JsonPropertyName("first_publish_year")]
    public int? FirstPublishYear { get; set; }

    [JsonPropertyName("edition_count")]
    public int EditionCount { get; set; }

    [JsonPropertyName("cover_i")]
    public long? CoverId { get; set; }

    [JsonPropertyName("ia")]
    public List<string>? InternetArchiveIdentifiers { get; set; }

    [JsonPropertyName("subject")]
    public List<string>? Subject { get; set; }

    [JsonPropertyName("key")]
    public string? WorkKey { get; set; }
}
