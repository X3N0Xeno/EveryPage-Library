using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EveryPageLibrary.Models;

namespace EveryPageLibrary.Services;

public class GutendexBookSource
{
    public GutendexBookSource(HttpClient http) { }
    public Task<List<Book>> GetPopularAsync(int limit = 20, int page = 1, CancellationToken cancellationToken = default) => Task.FromResult(new List<Book>());
    public Task<List<Book>> SearchAsync(string query, string? genre, int limit = 20, int page = 1, CancellationToken cancellationToken = default) => Task.FromResult(new List<Book>());
    public Task<List<Book>> BrowseSubjectAsync(string genre, int limit = 20, int page = 1, CancellationToken cancellationToken = default) => Task.FromResult(new List<Book>());
}