using System;

namespace EveryPageLibrary.Services;

public class CatalogStateService
{
    public string SearchQuery { get; set; } = "";
    public string SelectedCategory { get; set; } = "All";
    public string SelectedForm { get; set; } = "All";
    public string SelectedFactuality { get; set; } = "All";
    public string SelectedAudience { get; set; } = "All";
    public string SelectedEra { get; set; } = "All";
    public string SelectedRights { get; set; } = "All";

    public int TotalOnShelf { get; private set; }
    public bool IsLoading { get; private set; }

    public event Func<Task>? OnQueryChanged;
    public event Action? OnFacetChanged;
    public event Action? OnUploadRequested;
    public event Action? OnStatsUpdated;

    public async Task SetCategoryAsync(string category)
    {
        SelectedCategory = category;
        OnFacetChanged?.Invoke();
        if (OnQueryChanged is not null)
            await OnQueryChanged.Invoke();
    }

    public async Task ApplySearchAsync()
    {
        OnFacetChanged?.Invoke();
        if (OnQueryChanged is not null)
            await OnQueryChanged.Invoke();
    }

    public async Task ClearSearchAsync()
    {
        SearchQuery = "";
        OnFacetChanged?.Invoke();
        if (OnQueryChanged is not null)
            await OnQueryChanged.Invoke();
    }

    public void NotifyFacetChanged()
    {
        OnFacetChanged?.Invoke();
    }

    public async Task ResetAllAsync()
    {
        SearchQuery = "";
        SelectedCategory = "All";
        SelectedForm = "All";
        SelectedFactuality = "All";
        SelectedAudience = "All";
        SelectedEra = "All";
        SelectedRights = "All";
        OnFacetChanged?.Invoke();
        if (OnQueryChanged is not null)
            await OnQueryChanged.Invoke();
    }

    public void RequestUpload()
    {
        OnUploadRequested?.Invoke();
    }

    public void UpdateShelfStats(int count, bool isLoading)
    {
        TotalOnShelf = count;
        IsLoading = isLoading;
        OnStatsUpdated?.Invoke();
    }
}