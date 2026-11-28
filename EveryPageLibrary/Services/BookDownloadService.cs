using System;
using System.Linq;
using System.Threading.Tasks;
using EveryPageLibrary.Models;
using Microsoft.JSInterop;

namespace EveryPageLibrary.Services;

public record DownloadOutcome(bool Success, bool IsWarning, string Message);

public class BookDownloadService
{
    private readonly ArchiveService _archiveService;
    private readonly InternetArchiveBookSource _internetArchive;
    private readonly AuthStateService _authState;
    private readonly IJSRuntime _js;

    public BookDownloadService(
        ArchiveService archiveService,
        InternetArchiveBookSource internetArchive,
        AuthStateService authState,
        IJSRuntime js)
    {
        _archiveService = archiveService;
        _internetArchive = internetArchive;
        _authState = authState;
        _js = js;
    }

    public async Task<DownloadOutcome> DownloadAsync(Book book, string? preferredFormat = null)
    {
        try
        {
            var safeTitle = SanitizeFileName(book.Title);
            bool success;
            string toastText;

            if (!string.IsNullOrWhiteSpace(book.ContentText) &&
                string.IsNullOrWhiteSpace(book.PdfUrl) &&
                string.IsNullOrWhiteSpace(book.EpubUrl))
            {
                var manuscript = $"EVERYPAGE LIBRARY\nTITLE: {book.Title}\nAUTHOR: {book.Author}\n\n{book.Synopsis}\n\n{book.ContentText}";
                success = await _js.InvokeAsync<bool>("EveryPageLibrary.downloadTextFile", $"{safeTitle}.txt", manuscript);
                toastText = $"Saved \"{book.Title}.txt\" to your Downloads folder!";
            }
            else if (book.IsIndexed && !string.IsNullOrWhiteSpace(book.PdfUrl))
            {
                success = await _js.InvokeAsync<bool>("EveryPageLibrary.downloadDirectBlob", book.PdfUrl, $"{safeTitle}.pdf");
                if (!success)
                {
                    success = await _js.InvokeAsync<bool>("EveryPageLibrary.openSourcePage", book.PdfUrl);
                }
                toastText = $"Downloaded \"{book.Title}.pdf\"!";
            }
            else if (book.ExternalId?.StartsWith("wb-", StringComparison.OrdinalIgnoreCase) == true &&
                     !string.IsNullOrWhiteSpace(book.PdfUrl))
            {
                success = await _js.InvokeAsync<bool>("EveryPageLibrary.openSourcePage", book.PdfUrl);
                toastText = $"Started Wikibooks PDF export for \"{book.Title}\"!";
            }
            else if (book.ExternalId?.StartsWith("se-", StringComparison.OrdinalIgnoreCase) == true)
            {
                var targetUrl = book.FileUrl ?? book.EpubUrl;
                success = await _js.InvokeAsync<bool>("EveryPageLibrary.openSourcePage", targetUrl);
                toastText = $"Opened Standard Ebooks page for \"{book.Title}\" (1 click to save EPUB/AZW3/Kepub).";
            }
            else if (!string.IsNullOrWhiteSpace(ExtractArchiveIdentifier(book)))
            {
                var archiveId = ExtractArchiveIdentifier(book)!;
                var resolved = await _internetArchive.ResolveDownloadFilesAsync(archiveId);

                if (resolved.IsRestricted)
                {
                    return new DownloadOutcome(false, true,
                        $"\"{book.Title}\" is Controlled-Lending (borrow-only, not downloadable). Click \"Read Book\" to view online.");
                }

                book.PdfUrl = resolved.PdfUrl;
                book.EpubUrl = resolved.EpubUrl;

                var targetUrl = preferredFormat?.ToLowerInvariant() switch
                {
                    "pdf" when !string.IsNullOrWhiteSpace(resolved.PdfUrl) => resolved.PdfUrl!,
                    "epub" when !string.IsNullOrWhiteSpace(resolved.EpubUrl) => resolved.EpubUrl!,
                    _ => $"https://archive.org/details/{Uri.EscapeDataString(archiveId)}"
                };

                success = await _js.InvokeAsync<bool>("EveryPageLibrary.openSourcePage", targetUrl);
                toastText = $"Opened {book.SourceBadge} download page for \"{book.Title}\" (1 click to save).";
            }
            else if (book.ExternalId?.StartsWith("pg-", StringComparison.OrdinalIgnoreCase) == true)
            {
                var pgId = book.ExternalId["pg-".Length..];
                var targetUrl = !string.IsNullOrWhiteSpace(pgId)
                    ? $"https://www.gutenberg.org/ebooks/{Uri.EscapeDataString(pgId)}"
                    : book.FileUrl;

                success = await _js.InvokeAsync<bool>("EveryPageLibrary.openSourcePage", targetUrl);
                toastText = $"Opened Project Gutenberg page for \"{book.Title}\" (1 click to save).";
            }
            else
            {
                var targetUrl = preferredFormat?.ToLowerInvariant() switch
                {
                    "pdf" when !string.IsNullOrWhiteSpace(book.PdfUrl) => book.PdfUrl,
                    "epub" when !string.IsNullOrWhiteSpace(book.EpubUrl) => book.EpubUrl,
                    _ => book.PdfUrl ?? book.EpubUrl ?? book.FileUrl
                };

                if (string.IsNullOrWhiteSpace(targetUrl))
                {
                    return new DownloadOutcome(false, true,
                        $"\"{book.Title}\" is a catalog-only record with no downloadable file.");
                }

                success = await _js.InvokeAsync<bool>("EveryPageLibrary.openSourcePage", targetUrl);
                toastText = $"Opened download page for \"{book.Title}\" (1 click to save).";
            }

            if (success)
            {
                var actorName = _authState.CurrentUser?.Username ?? "Guest reader";
                var actorRole = _authState.CurrentUser?.Role ?? "reader";
                var persisted = await _archiveService.RegisterDownloadAsync(book, actorName, actorRole);
                book.Id = persisted.Id;
                book.DownloadCount = persisted.DownloadCount;
                return new DownloadOutcome(true, false, toastText);
            }

            return new DownloadOutcome(false, true, $"Could not start download for \"{book.Title}\".");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Download action failed: {ex.Message}");
            return new DownloadOutcome(false, true, $"Could not open download page for \"{book.Title}\".");
        }
    }

    private static string? ExtractArchiveIdentifier(Book book)
    {
        if (string.IsNullOrWhiteSpace(book.ExternalId)) return null;
        if (book.ExternalId.StartsWith("ia-", StringComparison.OrdinalIgnoreCase)) return book.ExternalId["ia-".Length..];
        if (book.ExternalId.StartsWith("st-", StringComparison.OrdinalIgnoreCase)) return book.ExternalId["st-".Length..];
        if (book.ExternalId.StartsWith("ol-", StringComparison.OrdinalIgnoreCase) &&
            !book.ExternalId.StartsWith("ol-works/", StringComparison.OrdinalIgnoreCase))
            return book.ExternalId["ol-".Length..];
        return null;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var cleaned = string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c)).Replace(" ", "_");
        return string.IsNullOrWhiteSpace(cleaned) ? "EveryPage_Book" : cleaned;
    }
}