using System.Text.Json.Serialization;

namespace BioIsac.Models;

public class EmailRequest
{
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;
    
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
    
    [JsonPropertyName("category")]
    public string? Category { get; set; }
    
    [JsonPropertyName("contactId")]
    public int? ContactId { get; set; }
}

