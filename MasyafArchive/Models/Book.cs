using System.Text.Json.Serialization;
using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EveryPageLibrary.Models;

/// <summary>
/// A single record in the public library catalog (public.books).
/// Holds both community-uploaded PDFs and books indexed from open web catalogs
/// such as Project Gutenberg (via Gutendex) and Open Library.
/// </summary>
[Table("books")]
public class Book : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("author")]
    public string Author { get; set; } = string.Empty;

    [Column("category")]
    public string Category { get; set; } = "Codex";

    [Column("synopsis")]
    public string Synopsis { get; set; } = string.Empty;

    [Column("content_text")]
    public string ContentText { get; set; } = string.Empty;

    [Column("file_url")]
    public string? FileUrl { get; set; }

    [Column("uploaded_by")]
    public string UploadedBy { get; set; } = "Community";

    [Column("download_count")]
    public int DownloadCount { get; set; } = 0;

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("created_at", ignoreOnInsert: true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("publication_year")]
    public string PublicationYear { get; set; } = string.Empty;

    [Column("cover_url")]
    public string? CoverUrl { get; set; }

    [Column("source_name")]
    public string SourceName { get; set; } = "Community PDF";

    [Column("epub_url")]
    public string? EpubUrl { get; set; }

    [Column("pdf_url")]
    public string? PdfUrl { get; set; }

    [Column("external_id")]
    public string? ExternalId { get; set; }

    // ---- Derived, not persisted -------------------------------------------------

    /// <summary>True when this book exists in Supabase already (has a catalog id).</summary>
    public bool IsIndexed => Id > 0;

    /// <summary>Short label used by the source badge on every card and reader.</summary>
    public string SourceBadge => string.IsNullOrWhiteSpace(SourceName) ? "Community PDF" : SourceName;

    /// <summary>Category label shown to readers ("Codex" is the legacy value for General).</summary>
    public string GenreLabel => BookGenres.ToLabel(Category);

    /// <summary>Publication year, falling back to "Year unknown".</summary>
    public string YearLabel => string.IsNullOrWhiteSpace(PublicationYear) ? "Year unknown" : PublicationYear;

    [JsonIgnore]
    public BookFacetProfile FacetProfile => BookGenres.Classify(this);

    [JsonIgnore]
    public string FormTag => FacetProfile.Form;

    [JsonIgnore]
    public string FactualityTag => FacetProfile.Factuality;

    [JsonIgnore]
    public string DomainTag => FacetProfile.Domain;

    [JsonIgnore]
    public string AudienceTag => FacetProfile.Audience;

    [JsonIgnore]
    public string EraTag => FacetProfile.Era;

    [JsonIgnore]
    public string RightsTag => FacetProfile.Rights;
}
