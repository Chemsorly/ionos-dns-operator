using System.Text.Json.Serialization;

namespace IonosDns.Internal;

internal class RecordUpdateRequest
{
    [JsonPropertyName("content")]
    public required string Content { get; init; }
    
    [JsonPropertyName("ttl")]
    public required int Ttl { get; init; }
    
    [JsonPropertyName("prio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Prio { get; init; }
    
    [JsonPropertyName("disabled")]
    public bool Disabled { get; init; }
}
