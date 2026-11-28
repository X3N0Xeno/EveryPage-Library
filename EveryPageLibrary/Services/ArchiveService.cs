using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EveryPageLibrary.Models;
using static Supabase.Postgrest.Constants;

namespace EveryPageLibrary.Services;

/// <summary>
/// All persistence for the library: catalog reads, comment threads, community PDF
/// uploads, soft-delete moderation and the audit trail.
/// </summary>
public class ArchiveService
{
    private const string StorageBucket = "tomes";
    private const string CommunitySource = "Community PDF";

    private readonly Supabase.Client _supabase;
    private readonly ArchiveModerationService _moderation;

    public ArchiveService(Supabase.Client supabase, ArchiveModerationService moderation)
    {
        _supabase = supabase;
        _moderation = moderation;
    }

    // -------------------------------------------------------------------------
    // Catalog reads
    // -------------------------------------------------------------------------

    public async Task<List<Book>> GetBooksAsync(string? category = null, string? searchQuery = null, int limit = 60, int offset = 0)
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
                    Contains(b.Title, term) ||
                    Contains(b.Author, term) ||
                    Contains(b.Synopsis, term)
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
            return await _supabase.From<Book>()
                .Where(b => b.Id == id)
                .Single();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveService] Error getting book #{id}: {ex.Message}");
            return null;
        }
    }

    public async Task<Book?> FindByExternalIdAsync(string externalId)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return null;
        }

        try
        {
            var response = await _supabase.From<Book>()
                .Where(b => b.ExternalId == externalId && b.DeletedAt == null)
                .Limit(1)
                .Get();

            return response.Models.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveService] Error looking up external book '{externalId}': {ex.Message}");
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // Federation bookkeeping
    // -------------------------------------------------------------------------

    /// <summary>
    /// Makes sure an external search result exists in public.books so comments and
    /// download counts can be attached to it. Returns the stored row (with a real
    /// catalog id) or the original book when it could not be saved.
    /// </summary>
    public async Task<Book> EnsureIndexedAsync(Book book)
    {
        if (book.IsIndexed)
        {
            return book;
        }

        if (!string.IsNullOrWhiteSpace(book.ExternalId))
        {
            var existing = await FindByExternalIdAsync(book.ExternalId);
            if (existing != null)
            {
                return existing;
            }
        }

        try
        {
            var payload = BuildIndexableCopy(book);
            var response = await _supabase.From<Book>().Insert(payload);
            return response.Models.FirstOrDefault() ?? payload;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveService] Could not index '{book.Title}': {ex.Message}");
            return book;
        }
    }

    /// <summary>
    /// Records a download. External books are inserted with download_count = 1,
    /// already-indexed books are incremented. Returns the persisted book.
    /// </summary>
    public async Task<Book> RegisterDownloadAsync(Book book, string actorName, string actorRole)
    {
        if (!book.IsIndexed)
        {
            var indexed = await InsertWithDownloadCountAsync(book, 1);
            await LogAuditAsync(
                actorName: actorName,
                actorRole: actorRole,
                action: "DOWNLOAD_BOOK",
                targetType: "Book",
                targetTitle: book.Title,
                details: $"Downloaded '{book.Title}' from {book.SourceBadge} and added it to the library catalog."
            );

            return indexed;
        }

        var stored = await GetBookByIdAsync(book.Id) ?? book;
        stored.DownloadCount += 1;

        try
        {
            await _supabase.From<Book>()
                .Where(b => b.Id == stored.Id)
                .Update(stored);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveService] Error incrementing download count for #{book.Id}: {ex.Message}");
        }

        await LogAuditAsync(
            actorName: actorName,
            actorRole: actorRole,
            action: "DOWNLOAD_BOOK",
            targetType: "Book",
            targetTitle: stored.Title,
            details: $"Downloaded '{stored.Title}' ({stored.SourceBadge}). Total downloads: {stored.DownloadCount}."
        );

        return stored;
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

    private async Task<Book> InsertWithDownloadCountAsync(Book book, int downloadCount)
    {
        if (!string.IsNullOrWhiteSpace(book.ExternalId))
        {
            var existing = await FindByExternalIdAsync(book.ExternalId);
            if (existing != null)
            {
                existing.DownloadCount += downloadCount;
                await _supabase.From<Book>().Where(b => b.Id == existing.Id).Update(existing);
                return existing;
            }
        }

        try
        {
            var payload = BuildIndexableCopy(book);
            payload.DownloadCount = downloadCount;
            var response = await _supabase.From<Book>().Insert(payload);
            return response.Models.FirstOrDefault() ?? payload;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveService] Could not store download record for '{book.Title}': {ex.Message}");
            return book;
        }
    }

    private static Book BuildIndexableCopy(Book book) => new()
    {
        Title = book.Title,
        Author = book.Author,
        Category = BookGenres.ToStorage(book.Category),
        Synopsis = book.Synopsis,
        ContentText = book.ContentText ?? string.Empty,
        FileUrl = book.FileUrl,
        PdfUrl = book.PdfUrl,
        EpubUrl = book.EpubUrl,
        CoverUrl = book.CoverUrl,
        PublicationYear = book.PublicationYear,
        SourceName = book.SourceBadge,
        ExternalId = book.ExternalId,
        UploadedBy = string.IsNullOrWhiteSpace(book.UploadedBy) ? "Open Web" : book.UploadedBy,
        DownloadCount = book.DownloadCount,
        DeletedAt = null
    };

    private static bool Contains(string? haystack, string term) =>
        haystack != null && haystack.Contains(term, StringComparison.OrdinalIgnoreCase);

    // -------------------------------------------------------------------------
    // Reader comments (delegated to ArchiveModerationService)
    // -------------------------------------------------------------------------

    public Task<List<BookComment>> GetCommentsForBookAsync(long bookId) => _moderation.GetCommentsForBookAsync(bookId);
    public Task<BookComment?> AddCommentAsync(long bookId, string bookTitle, string authorName, string authorRole, string body) => _moderation.AddCommentAsync(bookId, bookTitle, authorName, authorRole, body);
    public Task<bool> SoftDeleteCommentAsync(long commentId, long bookId, string bookTitle, string actorName, string actorRole) => _moderation.SoftDeleteCommentAsync(commentId, bookId, bookTitle, actorName, actorRole);

    // -------------------------------------------------------------------------
    // Community PDF uploads and metadata editing
    // -------------------------------------------------------------------------

    /// <summary>
    /// Stores a community PDF (and optional cover image) in the "tomes" bucket, then
    /// inserts the catalog record. Exceptions are surfaced to the caller so the
    /// upload form can show the real error message.
    /// </summary>
    public async Task<Book> UploadPdfBookAsync(
        Book book,
        byte[] pdfBytes,
        string pdfFileName,
        byte[]? coverBytes,
        string? coverFileName,
        string actorName,
        string actorRole)
    {
        if (pdfBytes == null || pdfBytes.Length == 0)
        {
            throw new InvalidOperationException("The selected PDF file is empty.");
        }

        if (!pdfFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only .pdf files can be uploaded to the library.");
        }

        var storage = _supabase.Storage.From(StorageBucket);

        var pdfPath = $"{Guid.NewGuid():N}_{SanitizeStorageName(pdfFileName, "book.pdf")}";
        await storage.Upload(pdfBytes, pdfPath, new Supabase.Storage.FileOptions { Upsert = true });
        var publicPdfUrl = storage.GetPublicUrl(pdfPath);

        string? publicCoverUrl = null;
        if (coverBytes is { Length: > 0 } && !string.IsNullOrWhiteSpace(coverFileName))
        {
            var coverPath = $"{Guid.NewGuid():N}_{SanitizeStorageName(coverFileName, "cover.jpg")}";
            await storage.Upload(coverBytes, coverPath, new Supabase.Storage.FileOptions { Upsert = true });
            publicCoverUrl = storage.GetPublicUrl(coverPath);
        }

        var record = BuildIndexableCopy(book);
        record.Category = BookGenres.ToStorage(book.Category);
        record.SourceName = CommunitySource;
        record.ExternalId = null;
        record.PdfUrl = publicPdfUrl;
        record.FileUrl = publicPdfUrl;
        record.EpubUrl = null;
        record.CoverUrl = publicCoverUrl ?? book.CoverUrl;
        record.UploadedBy = actorName;
        record.DownloadCount = 0;
        record.DeletedAt = null;

        var response = await _supabase.From<Book>().Insert(record);
        var inserted = response.Models.FirstOrDefault() ?? record;

        await LogAuditAsync(
            actorName: actorName,
            actorRole: actorRole,
            action: "UPLOAD_BOOK",
            targetType: "Book",
            targetTitle: inserted.Title,
            details: $"Uploaded community PDF '{inserted.Title}' by {inserted.Author} ({inserted.GenreLabel})."
        );

        return inserted;
    }

    /// <summary>Auditor metadata edit — bibliographic fields only, no book text.</summary>
    public async Task<bool> UpdateBookAsync(Book book, string actorName, string actorRole)
    {
        try
        {
            var stored = await GetBookByIdAsync(book.Id) ?? book;
            stored.Title = book.Title;
            stored.Author = book.Author;
            stored.Category = BookGenres.ToStorage(book.Category);
            stored.Synopsis = book.Synopsis;
            stored.PublicationYear = book.PublicationYear;

            await _supabase.From<Book>()
                .Where(b => b.Id == stored.Id)
                .Update(stored);

            await LogAuditAsync(
                actorName: actorName,
                actorRole: actorRole,
                action: "EDIT_BOOK",
                targetType: "Book",
                targetTitle: stored.Title,
                details: $"Metadata updated for book #{stored.Id} ({stored.GenreLabel})."
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
                action: "DELETE_BOOK",
                targetType: "Book",
                targetTitle: book.Title,
                details: $"Book #{bookId} removed from the catalog. Reason: {(string.IsNullOrWhiteSpace(reason) ? "No reason specified" : reason)}"
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
                action: "RESTORE_BOOK",
                targetType: "Book",
                targetTitle: book.Title,
                details: $"Book #{bookId} restored to the public catalog."
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
            Console.WriteLine($"[ArchiveService] Error fetching deleted books: {ex.Message}");
            return new List<Book>();
        }
    }

    // -------------------------------------------------------------------------
    // Audit trail and statistics
    // -------------------------------------------------------------------------

    public Task<List<AuditLog>> GetAuditLogsAsync(string? filterAction = null, int limit = 100, int offset = 0) => _moderation.GetAuditLogsAsync(filterAction, limit, offset);
    public Task LogAuditAsync(string actorName, string actorRole, string action, string targetType, string targetTitle, string details) => _moderation.LogAuditAsync(actorName, actorRole, action, targetType, targetTitle, details);

    public async Task<(int TotalBooks, int CommunityPdfs, int TotalDownloads, int TotalLogs)> GetArchiveStatsAsync()
    {
        try
        {
            var activeBooks = await _supabase.From<Book>()
                .Where(b => b.DeletedAt == null)
                .Get();

            int totalBooks = activeBooks.Models.Count;
            int totalDownloads = activeBooks.Models.Sum(b => b.DownloadCount);
            int communityPdfs = activeBooks.Models.Count(b =>
                b.SourceName.Equals(CommunitySource, StringComparison.OrdinalIgnoreCase));

            return (totalBooks, communityPdfs, totalDownloads, 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArchiveService] Error getting stats: {ex.Message}");
            return (0, 0, 0, 0);
        }
    }

    /// <summary>Keeps only characters that are safe inside a storage object key.</summary>
    private static string SanitizeStorageName(string fileName, string fallback)
    {
        var name = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(name))
        {
            return fallback;
        }

        var cleaned = new string(name
            .Select(ch => char.IsLetterOrDigit(ch) || ch == '.' || ch == '-' || ch == '_' ? ch : '_')
            .ToArray());

        return cleaned.Length == 0 ? fallback : cleaned;
    }
}
