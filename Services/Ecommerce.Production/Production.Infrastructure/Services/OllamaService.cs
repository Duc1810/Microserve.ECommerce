using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Production.Application.Commons.Interfaces;
using System.Net.Http.Json;

namespace Production.Infrastructure.Services;
public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private  readonly ILogger<OllamaService> _logger;
    private const string ModelName = "nomic-embed-text";

    public OllamaService(HttpClient httpClient, ILogger<OllamaService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    public async Task<float[]> GetVectorAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                model = ModelName,
                prompt = text
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/embeddings",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Ollama API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return Array.Empty<float>();
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully retrieved embedding from Ollama for text: {Text}", text);
            return result?.Embedding ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vector from Ollama for text: {Text}", text);
            return Array.Empty<float>();
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            _logger.LogError("Ollama service health check failed");
            return false;
        }
    }
}
