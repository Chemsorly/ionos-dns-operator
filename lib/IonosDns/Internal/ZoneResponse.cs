using System.Text.Json.Serialization;

namespace IonosDns.Internal;

internal class ZoneResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
    
    [JsonPropertyName("records")]
    public List<RecordResponse>? Records { get; init; }
}
