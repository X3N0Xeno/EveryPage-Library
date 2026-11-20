using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MasyafArchive.Models;

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
    public string UploadedBy { get; set; } = "Archivist";

    [Column("download_count")]
    public int DownloadCount { get; set; } = 0;

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
