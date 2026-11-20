using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MasyafArchive.Models;
using static Supabase.Postgrest.Constants;

namespace MasyafArchive.Services;

public class ArchiveService
{
    private readonly Supabase.Client _supabase;

    public ArchiveService(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<Book>> GetBooksAsync(string? category = null, string? searchQuery = null, int limit = 50, int offset = 0)
    {
        try
        {
            var query = _supabase.From<Book>()
                .Where(b => b.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(b => b.Category == category);
            }

            var response = await query
                .Order(b => b.CreatedAt, Ordering.Descending)
                .Range(offset, offset + limit - 1)
                .Get();

            var books = response.Models;

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var term = searchQuery.Trim();
                books = books.Where(b =>
                    (b.Title != null && b.Title.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (b.Author != null && b.Author.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (b.Synopsis != null && b.Synopsis.Contains(term, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            return books;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveService] Error fetching books: {ex.Message}");
            return new List<Book>();
        }
    }

    public async Task<Book?> GetBookByIdAsync(long id)
    {
        try
        {
            var response = await _supabase.From<Book>()
                .Where(b => b.Id == id)
                .Single();
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveService] Error getting book #{id}: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> IncrementDownloadCountAsync(long bookId)
    {
        try
        {
            var book = await GetBookByIdAsync(bookId);
            if (book == null) return false;

            book.DownloadCount += 1;
            await _supabase.From<Book>()
                .Where(b => b.Id == bookId)
                .Update(book);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveService] Error incrementing download count for #{bookId}: {ex.Message}");
            return false;
        }
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
            Console.WriteLine($"[ArchiveService] Error fetching comments for book #{bookId}: {ex.Message}");
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
                actorName: authorName,
                actorRole: authorRole,
                action: "INSCRIBE_MARGIN_NOTE",
                targetType: "Codex Note",
                targetTitle: bookTitle,
                details: $"Margin note inscribed into Tome #{bookId}: \"{(body.Length > 60 ? body[..60] + "..." : body)}\""
            );

            return inserted;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveService] Error adding comment: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> SoftDeleteCommentAsync(long commentId, long bookId, string bookTitle, string actorName, string actorRole)
    {
        try
        {
            var response = await _supabase.From<BookComment>()
                .Where(c => c.Id == commentId)
                .Single();

            if (response == null) return false;

            response.DeletedAt = DateTime.UtcNow;
            await _supabase.From<BookComment>()
                .Where(c => c.Id == commentId)
                .Update(response);

            await LogAuditAsync(
                actorName: actorName,
                actorRole: actorRole,
                action: "SEAL_MARGIN_NOTE",
                targetType: "Codex Note",
                targetTitle: bookTitle,
                details: $"Margin Note #{commentId} sealed and removed from public view in Tome #{bookId}."
            );

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveService] Error soft-deleting comment #{commentId}: {ex.Message}");
            return false;
        }
    }

    public async Task<Book?> InscribeBookAsync(Book book, byte[]? fileBytes, string? fileName, string actorName, string actorRole)
    {
        try
        {
            if (fileBytes != null && !string.IsNullOrWhiteSpace(fileName))
            {
                var cleanFileName = Path.GetFileName(fileName).Replace(" ", "_");
                var storagePath = $"{DateTime.UtcNow.Ticks}_{cleanFileName}";
                
                await _supabase.Storage.From("tomes").Upload(
                    fileBytes,
                    storagePath,
                    new Supabase.Storage.FileOptions { Upsert = true }
                );

                book.FileUrl = _supabase.Storage.From("tomes").GetPublicUrl(storagePath);
            }

            book.UploadedBy = actorName;
            book.CreatedAt = DateTime.UtcNow;
            book.DownloadCount = 0;
            book.DeletedAt = null;

            var response = await _supabase.From<Book>().Insert(book);
            var inserted = response.Models.FirstOrDefault() ?? book;

            await LogAuditAsync(
                actorName: actorName,
                actorRole: actorRole,
                action: "INSCRIBE_TOME",
                targetType: "Book",
                targetTitle: book.Title,
                details: $"New tome inscribed under category '{book.Category}' by {book.Author}."
            );

            return inserted;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveService] Error inscribing book: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateBookAsync(Book book, string actorName, string actorRole)
    {
        try
        {
            await _supabase.From<Book>()
                .Where(b => b.Id == book.Id)
                .Update(book);

            await LogAuditAsync(
                actorName: actorName,
                actorRole: actorRole,
                action: "EDIT_TOME_METADATA",
                targetType: "Book",
                targetTitle: book.Title,
                details: $"Metadata updated for Tome #{book.Id} ({book.Category})."
            );

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveService] Error updating book #{book.Id}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SoftDeleteBookAsync(long bookId, string actorName, string actorRole, string reason)
    {
        try
        {
            var book = await GetBookByIdAsync(bookId);
            if (book == null) return false;

            book.DeletedAt = DateTime.UtcNow;
            await _supabase.From<Book>()
                .Where(b => b.Id == bookId)
                .Update(book);

            await LogAuditAsync(
                actorName: actorName,
                actorRole: actorRole,
                action: "SEAL_TOME",
                targetType: "Book",
                targetTitle: book.Title,
                details: $"Tome #{bookId} sealed by Auditor. Reason: {(string.IsNullOrWhiteSpace(reason) ? "No reason specified" : reason)}"
            );

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveService] Error soft-deleting book #{bookId}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RestoreBookAsync(long bookId, string actorName, string actorRole)
    {
        try
        {
            var book = await GetBookByIdAsync(bookId);
            if (book == null) return false;

            book.DeletedAt = null;
            await _supabase.From<Book>()
                .Where(b => b.Id == bookId)
                .Update(book);

            await LogAuditAsync(
                actorName: actorName,
                actorRole: actorRole,
                action: "UNSEAL_TOME",
                targetType: "Book",
                targetTitle: book.Title,
                details: $"Tome #{bookId} unsealed and restored to public archive shelves."
            );

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveService] Error restoring book #{bookId}: {ex.Message}");
            return false;
        }
    }

    public async Task<List<Book>> GetSealedBooksAsync(int limit = 50, int offset = 0)
    {
        try
        {
            var response = await _supabase.From<Book>()
                .Where(b => b.DeletedAt != null)
                .Order(b => b.Id, Ordering.Descending)
                .Range(offset, offset + limit - 1)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveService] Error fetching sealed books: {ex.Message}");
            return new List<Book>();
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
            Console.WriteLine($"[ArchiveService] Error fetching audit logs: {ex.Message}");
            return new List<AuditLog>();
        }
    }

    public async Task LogAuditAsync(string actorName, string actorRole, string action, string targetType, string targetTitle, string details)
    {
        try
        {
            var entry = new AuditLog
            {
                ActorName = string.IsNullOrWhiteSpace(actorName) ? "Anonymous Traveler" : actorName,
                ActorRole = string.IsNullOrWhiteSpace(actorRole) ? "traveler" : actorRole,
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
            Console.WriteLine($"[ArchiveService] Error recording audit log: {ex.Message}");
        }
    }

    public async Task<(int TotalBooks, int ActiveCodices, int TotalDownloads, int TotalLogs)> GetArchiveStatsAsync()
    {
        try
        {
            var activeBooks = await _supabase.From<Book>()
                .Where(b => b.DeletedAt == null)
                .Get();

            int totalBooks = activeBooks.Models.Count;
            int totalDownloads = activeBooks.Models.Sum(b => b.DownloadCount);
            int codexCount = activeBooks.Models.Count(b => b.Category.Equals("Codex", StringComparison.OrdinalIgnoreCase));

            return (totalBooks, codexCount, totalDownloads, 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveService] Error getting stats: {ex.Message}");
            return (0, 0, 0, 0);
        }
    }
}
