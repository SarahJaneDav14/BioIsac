namespace BioIsac.Models;

public class Contact
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string WorkField { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ContactRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string WorkField { get; set; } = string.Empty;
}

