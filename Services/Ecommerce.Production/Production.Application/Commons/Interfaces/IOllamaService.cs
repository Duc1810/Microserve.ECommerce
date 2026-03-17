using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Production.Application.Commons.Interfaces;
public interface IOllamaService
{
    Task<float[]> GetVectorAsync(string text, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

public class OllamaEmbeddingResponse
{
    public float[] Embedding { get; set; } = Array.Empty<float>();
}

public class OllamaEmbeddingRequest
{
    public string Model { get; set; } = "nomic-embed-text"; 
    public string Prompt { get; set; } = string.Empty;
}