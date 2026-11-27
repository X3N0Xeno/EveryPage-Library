using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EverydayLibrary.Models;

[Table("book_comments")]
public class BookComment : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("book_id")]
    public long BookId { get; set; }

    [Column("author_name")]
    public string AuthorName { get; set; } = string.Empty;

    [Column("body")]
    public string Body { get; set; } = string.Empty;

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
