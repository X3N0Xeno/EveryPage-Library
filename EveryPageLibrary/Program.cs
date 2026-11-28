using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using EveryPageLibrary;
using EveryPageLibrary.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var supabaseUrl = builder.Configuration["Supabase:Url"] ?? "";
var supabaseKey = builder.Configuration["Supabase:Key"] ?? "";

// Handle PostgreSQL URI fallback if provided
if (supabaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
    supabaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
{
    var match = Regex.Match(supabaseUrl, @"postgres\.([a-z0-9]+)");
    if (match.Success)
    {
        supabaseUrl = $"https://{match.Groups[1].Value}.supabase.co";
    }
}

var supabaseOptions = new Supabase.SupabaseOptions
{
    AutoConnectRealtime = false
};

var supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey, supabaseOptions);
await supabaseClient.InitializeAsync();

builder.Services.AddSingleton(supabaseClient);
builder.Services.AddScoped<ArchiveModerationService>();
builder.Services.AddScoped<ArchiveService>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<CatalogStateService>();
builder.Services.AddScoped<BookDownloadService>();

// Federated open-web book catalog (Gutendex + Open Library). Each source gets its
// own client because the catalog URLs are absolute and slower than the app host.
builder.Services.AddScoped(sp => new GutendexBookSource(CreateCatalogClient()));
builder.Services.AddScoped(sp => new OpenLibraryBookSource(CreateCatalogClient()));
builder.Services.AddScoped(sp => new OpenAlexBookSource(CreateCatalogClient()));
builder.Services.AddScoped(sp => new OapenBookSource(CreateCatalogClient()));
builder.Services.AddScoped<OpenWebMirrorBookSource>();
builder.Services.AddScoped<FederatedBookService>();

static HttpClient CreateCatalogClient() => new() { Timeout = TimeSpan.FromSeconds(30) };

Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<EveryPageLibrary.Services.InternetArchiveBookSource>(builder.Services);
Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<EveryPageLibrary.Services.SacredTextsBookSource>(builder.Services);
Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<EveryPageLibrary.Services.StandardEbooksBookSource>(builder.Services);
Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<EveryPageLibrary.Services.WikibooksBookSource>(builder.Services);
Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<EveryPageLibrary.Services.DoabBookSource>(builder.Services);

await builder.Build().RunAsync();



