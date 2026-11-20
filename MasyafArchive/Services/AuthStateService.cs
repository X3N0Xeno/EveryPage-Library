using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MasyafArchive.Models;
using Microsoft.JSInterop;

namespace MasyafArchive.Services;

public class AuthStateService
{
    private readonly Supabase.Client _supabase;
    private readonly IJSRuntime _jsRuntime;
    private readonly ArchiveService _archiveService;

    public Scholar? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;
    public bool IsAuditor => CurrentUser?.Role?.Equals("auditor", StringComparison.OrdinalIgnoreCase) == true;
    public bool IsScribe => CurrentUser?.Role?.Equals("scribe", StringComparison.OrdinalIgnoreCase) == true;

    public event Action? OnChange;

    public AuthStateService(Supabase.Client supabase, IJSRuntime jsRuntime, ArchiveService archiveService)
    {
        _supabase = supabase;
        _jsRuntime = jsRuntime;
        _archiveService = archiveService;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var storedJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "masyaf_scholar");
            if (!string.IsNullOrWhiteSpace(storedJson))
            {
                var scholar = JsonSerializer.Deserialize<Scholar>(storedJson);
                if (scholar != null)
                {
                    CurrentUser = scholar;
                    NotifyStateChanged();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthStateService] Failed to restore session from localStorage: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> SignInAsync(string username, string passcode)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(passcode))
        {
            return (false, "Both scholar name and passcode are required to unlock the archives.");
        }

        try
        {
            var trimmedUsername = username.Trim();
            var response = await _supabase.From<Scholar>()
                .Where(s => s.Username == trimmedUsername && s.Passcode == passcode)
                .Get();

            var scholar = response.Models.FirstOrDefault();
            if (scholar == null)
            {
                return (false, "Unrecognized seal. Check your scholar moniker and passcode.");
            }

            CurrentUser = scholar;
            var json = JsonSerializer.Serialize(scholar);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "masyaf_scholar", json);

            await _archiveService.LogAuditAsync(
                actorName: scholar.Username,
                actorRole: scholar.Role,
                action: "SCHOLAR_SIGN_IN",
                targetType: "Sanctuary Gate",
                targetTitle: scholar.Role.ToUpperInvariant(),
                details: $"Scholar {scholar.Username} entered the library sanctuary with {scholar.Role} privileges."
            );

            NotifyStateChanged();
            return (true, $"Welcome, {scholar.Username} ({scholar.Role}).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthStateService] Sign-in error: {ex.Message}");
            return (false, $"Library gate verification failed: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> RegisterAsync(string username, string passcode)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(passcode))
        {
            return (false, "A moniker and passcode are required to inscribe a new scholar.");
        }

        var trimmedUsername = username.Trim();
        if (trimmedUsername.Length < 3)
        {
            return (false, "Scholar moniker must contain at least 3 characters.");
        }
        if (passcode.Length < 4)
        {
            return (false, "Passcode must contain at least 4 characters.");
        }

        try
        {
            var existingResponse = await _supabase.From<Scholar>()
                .Where(s => s.Username == trimmedUsername)
                .Get();

            if (existingResponse.Models.Any())
            {
                return (false, $"The scholar moniker '{trimmedUsername}' has already been inscribed.");
            }

            var newScholar = new Scholar
            {
                Username = trimmedUsername,
                Passcode = passcode,
                Role = "scribe",
                CreatedAt = DateTime.UtcNow
            };

            var insertResponse = await _supabase.From<Scholar>().Insert(newScholar);
            var created = insertResponse.Models.FirstOrDefault() ?? newScholar;

            CurrentUser = created;
            var json = JsonSerializer.Serialize(created);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "masyaf_scholar", json);

            await _archiveService.LogAuditAsync(
                actorName: created.Username,
                actorRole: created.Role,
                action: "SCHOLAR_REGISTER",
                targetType: "Sanctuary Registry",
                targetTitle: "New Scribe",
                details: $"New Scribe '{created.Username}' inscribed into the Masyaf Archive rolls."
            );

            NotifyStateChanged();
            return (true, $"Scribe {created.Username} inscribed successfully into the Masyaf rolls.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthStateService] Register error: {ex.Message}");
            return (false, $"Failed to inscribe new scholar: {ex.Message}");
        }
    }

    public async Task SignOutAsync()
    {
        if (CurrentUser != null)
        {
            await _archiveService.LogAuditAsync(
                actorName: CurrentUser.Username,
                actorRole: CurrentUser.Role,
                action: "SCHOLAR_SIGN_OUT",
                targetType: "Sanctuary Gate",
                targetTitle: CurrentUser.Role.ToUpperInvariant(),
                details: $"Scholar {CurrentUser.Username} closed their study session."
            );
        }

        CurrentUser = null;
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "masyaf_scholar");
        }
        catch
        {
            // ignore
        }
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
