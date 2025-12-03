using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
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
        var emailSettings = _configuration.GetSection("EmailSettings");
        var smtpServer = emailSettings["SmtpServer"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
        var senderEmail = emailSettings["SenderEmail"] ?? "";
        var senderPassword = emailSettings["SenderPassword"] ?? "";

        if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
        {
            throw new Exception("Email configuration is missing. Please set EmailSettings in appsettings.json");
        }

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

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Admin", senderEmail));
        
        foreach (var recipient in recipients)
        {
            message.To.Add(new MailboxAddress(recipient.Name, recipient.Email));
        }

        message.Subject = request.Subject;
        message.Body = new TextPart("html")
        {
            Text = request.Body
        };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, senderPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

