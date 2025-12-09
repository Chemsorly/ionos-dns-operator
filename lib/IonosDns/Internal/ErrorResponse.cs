using System.Text.Json.Serialization;

namespace IonosDns.Internal;

internal class ErrorResponse
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }
    
    [JsonPropertyName("message")]
    public string? Message { get; init; }
}
