using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EverydayLibrary.Models;
using Microsoft.JSInterop;

namespace EverydayLibrary.Services;

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
            var storedJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "everyday_library_user");
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
            return (false, "Both username and password are required.");
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
                return (false, "Invalid username or password.");
            }

            CurrentUser = scholar;
            var json = JsonSerializer.Serialize(scholar);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "everyday_library_user", json);

            await _archiveService.LogAuditAsync(
                actorName: scholar.Username,
                actorRole: scholar.Role,
                action: "SCHOLAR_SIGN_IN",
                targetType: "Sanctuary Gate",
                targetTitle: scholar.Role.ToUpperInvariant(),
                details: $"User {scholar.Username} signed in as {scholar.Role}."
            );

            NotifyStateChanged();
            return (true, $"Welcome, {scholar.Username}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthStateService] Sign-in error: {ex.Message}");
            return (false, $"Sign-in failed: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> RegisterAsync(string username, string passcode)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(passcode))
        {
            return (false, "Both username and password are required.");
        }

        var trimmedUsername = username.Trim();
        if (trimmedUsername.Length < 3)
        {
            return (false, "Username must contain at least 3 characters.");
        }
        if (passcode.Length < 4)
        {
            return (false, "Password must contain at least 4 characters.");
        }

        try
        {
            var existingResponse = await _supabase.From<Scholar>()
                .Where(s => s.Username == trimmedUsername)
                .Get();

            if (existingResponse.Models.Any())
            {
                return (false, $"The username '{trimmedUsername}' is already taken.");
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
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "everyday_library_user", json);

            await _archiveService.LogAuditAsync(
                actorName: created.Username,
                actorRole: created.Role,
                action: "SCHOLAR_REGISTER",
                targetType: "Sanctuary Registry",
                targetTitle: "New User",
                details: $"New contributor account '{created.Username}' registered."
            );

            NotifyStateChanged();
            return (true, $"Account for {created.Username} created successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthStateService] Register error: {ex.Message}");
            return (false, $"Failed to create account: {ex.Message}");
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
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "everyday_library_user");
        }
        catch
        {
            // ignore
        }
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
