using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MasyafArchive.Models;

[Table("scholars")]
public class Scholar : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("passcode")]
    public string Passcode { get; set; } = string.Empty;

    [Column("role")]
    public string Role { get; set; } = "scribe";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
