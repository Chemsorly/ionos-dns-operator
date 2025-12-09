using System.Text.Json.Serialization;

namespace IonosDns.Internal;

internal class RecordResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("rootName")]
    public string RootName { get; init; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;
    
    [JsonPropertyName("ttl")]
    public int Ttl { get; init; }
    
    [JsonPropertyName("prio")]
    public int? Prio { get; init; }
    
    [JsonPropertyName("disabled")]
    public bool Disabled { get; init; }
    
    [JsonPropertyName("changeDate")]
    public string? ChangeDate { get; init; }
}
