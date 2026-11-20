using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MasyafArchive;
using MasyafArchive.Services;

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
builder.Services.AddScoped<ArchiveService>();
builder.Services.AddScoped<AuthStateService>();

await builder.Build().RunAsync();
