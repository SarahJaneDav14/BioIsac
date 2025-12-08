using BioIsac.Models;
using MySql.Data.MySqlClient;

namespace BioIsac.Services;

public class EmailService
{
    private readonly DatabaseService _dbService;
    private readonly IConfiguration _configuration;

    public EmailService(DatabaseService dbService, IConfiguration configuration)
    {
        _dbService = dbService;
        _configuration = configuration;
    }

    public List<Contact> GetContactsByCategory(string? category)
    {
        var contacts = new List<Contact>();
        using var connection = _dbService.GetConnection();
        connection.Open();
        MySqlCommand command;

        if (string.IsNullOrEmpty(category))
        {
            command = new MySqlCommand("SELECT Id, Name, Email, WorkField, CreatedAt FROM Contacts", (MySqlConnection)connection);
        }
        else
        {
            command = new MySqlCommand("SELECT Id, Name, Email, WorkField, CreatedAt FROM Contacts WHERE WorkField = @category", (MySqlConnection)connection);
            command.Parameters.AddWithValue("@category", category);
        }

        using (command)
        {
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                contacts.Add(new Contact
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Email = reader.GetString(2),
                    WorkField = reader.GetString(3),
                    CreatedAt = reader.GetDateTime(4)
                });
            }
        }

        return contacts;
    }

    public Contact? GetContactById(int id)
    {
        using var connection = _dbService.GetConnection();
        connection.Open();
        using var command = (MySqlCommand)connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Email, WorkField, CreatedAt FROM Contacts WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new Contact
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                WorkField = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            };
        }

        return null;
    }

    public async Task<bool> SendEmailAsync(EmailRequest request)
    {
        // Simulate email sending - no actual SMTP connection needed
        List<Contact> recipients;

        if (request.ContactId.HasValue)
        {
            var contact = GetContactById(request.ContactId.Value);
            recipients = contact != null ? new List<Contact> { contact } : new List<Contact>();
        }
        else if (!string.IsNullOrEmpty(request.Category))
        {
            recipients = GetContactsByCategory(request.Category);
        }
        else
        {
            recipients = GetContactsByCategory(null);
        }

        if (recipients.Count == 0)
        {
            return false;
        }

        // Simulate async operation
        await Task.Delay(100);
        
        // Log simulated email (optional - can be removed)
        Console.WriteLine($"Simulated email sent to {recipients.Count} recipient(s)");
        Console.WriteLine($"Subject: {request.Subject}");
        
        return true;
    }
}

