using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EveryPageLibrary.Models;
using static Supabase.Postgrest.Constants;

namespace EveryPageLibrary.Services;

/// <summary>
/// Single-responsibility persistence for reader margin notes (comments)
/// and the librarian audit trail.
/// </summary>
public class ArchiveModerationService
{
    private readonly Supabase.Client _supabase;

    public ArchiveModerationService(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<BookComment>> GetCommentsForBookAsync(long bookId)
    {
        try
        {
            var response = await _supabase.From<BookComment>()
                .Where(c => c.BookId == bookId && c.DeletedAt == null)
                .Order(c => c.CreatedAt, Ordering.Ascending)
                .Get();
            return response.Models;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveModerationService] Error fetching comments for book #{bookId}: {ex.Message}");
            return new List<BookComment>();
        }
    }

    public async Task<BookComment?> AddCommentAsync(long bookId, string bookTitle, string authorName, string authorRole, string body)
    {
        try
        {
            var comment = new BookComment
            {
                BookId = bookId,
                AuthorName = authorName,
                Body = body,
                CreatedAt = DateTime.UtcNow
            };

            var response = await _supabase.From<BookComment>().Insert(comment);
            var inserted = response.Models.FirstOrDefault() ?? comment;

            await LogAuditAsync(
                authorName, authorRole, "POST_COMMENT", "Comment", bookTitle,
                $"Comment added to '{bookTitle}': \"{(body.Length > 60 ? body[..60] + "..." : body)}\""
            );

            return inserted;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveModerationService] Error adding comment: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> SoftDeleteCommentAsync(long commentId, long bookId, string bookTitle, string actorName, string actorRole)
    {
        try
        {
            var comment = await _supabase.From<BookComment>().Where(c => c.Id == commentId).Single();
            if (comment == null) return false;

            comment.DeletedAt = DateTime.UtcNow;
            await _supabase.From<BookComment>().Where(c => c.Id == commentId).Update(comment);

            await LogAuditAsync(
                actorName, actorRole, "DELETE_COMMENT", "Comment", bookTitle,
                $"Comment #{commentId} was removed from public view on '{bookTitle}'."
            );

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveModerationService] Error soft-deleting comment #{commentId}: {ex.Message}");
            return false;
        }
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync(string? filterAction = null, int limit = 100, int offset = 0)
    {
        try
        {
            Supabase.Postgrest.Interfaces.IPostgrestTable<AuditLog> query = _supabase.From<AuditLog>();
            if (!string.IsNullOrWhiteSpace(filterAction) && !filterAction.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.Action == filterAction);
            }

            var response = await query
                .Order(a => a.CreatedAt, Ordering.Descending)
                .Range(offset, offset + limit - 1)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveModerationService] Error fetching audit logs: {ex.Message}");
            return new List<AuditLog>();
        }
    }

    public async Task LogAuditAsync(string actorName, string actorRole, string action, string targetType, string targetTitle, string details)
    {
        try
        {
            var entry = new AuditLog
            {
                ActorName = string.IsNullOrWhiteSpace(actorName) ? "Guest reader" : actorName,
                ActorRole = string.IsNullOrWhiteSpace(actorRole) ? "reader" : actorRole,
                Action = action,
                TargetType = targetType,
                TargetTitle = targetTitle,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };

            await _supabase.From<AuditLog>().Insert(entry);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveModerationService] Error recording audit log: {ex.Message}");
        }
    }
}