using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MasyafArchive.Models;

[Table("audit_logs")]
public class AuditLog : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("actor_name")]
    public string ActorName { get; set; } = string.Empty;

    [Column("actor_role")]
    public string ActorRole { get; set; } = string.Empty;

    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [Column("target_type")]
    public string TargetType { get; set; } = string.Empty;

    [Column("target_title")]
    public string TargetTitle { get; set; } = string.Empty;

    [Column("details")]
    public string Details { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
