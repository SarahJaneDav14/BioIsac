namespace BioIsac.Models;

public class EmailRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int? ContactId { get; set; }
}

