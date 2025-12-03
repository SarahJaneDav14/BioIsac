using Microsoft.AspNetCore.Mvc;
using BioIsac.Models;
using BioIsac.Services;
using MySql.Data.MySqlClient;

namespace BioIsac.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactsController : ControllerBase
{
    private readonly DatabaseService _dbService;
    private readonly AuthService _authService;

    public ContactsController(DatabaseService dbService, AuthService authService)
    {
        _dbService = dbService;
        _authService = authService;
    }

    private bool IsAuthenticated()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return false;

        var token = authHeader.Substring(7);
        return _authService.ValidateSession(token);
    }

    [HttpGet]
    public IActionResult GetContacts()
    {
        if (!IsAuthenticated()) return Unauthorized();

        var contacts = new List<Contact>();
        using var connection = _dbService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Email, WorkField, CreatedAt FROM Contacts ORDER BY WorkField, Name";

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

        return Ok(contacts);
    }

    [HttpGet("categories")]
    public IActionResult GetCategories()
    {
        if (!IsAuthenticated()) return Unauthorized();

        var categories = new List<string>();
        using var connection = _dbService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT WorkField FROM Contacts ORDER BY WorkField";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            categories.Add(reader.GetString(0));
        }

        return Ok(categories);
    }

    [HttpPost]
    public IActionResult CreateContact([FromBody] ContactRequest request)
    {
        if (!IsAuthenticated()) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.WorkField))
        {
            return BadRequest(new { message = "Name, Email, and WorkField are required" });
        }

        using var connection = _dbService.GetConnection();
        connection.Open();
        using var command = (MySqlCommand)connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Contacts (Name, Email, WorkField) 
            VALUES (@name, @email, @workField)";
        command.Parameters.AddWithValue("@name", request.Name);
        command.Parameters.AddWithValue("@email", request.Email);
        command.Parameters.AddWithValue("@workField", request.WorkField);
        command.ExecuteNonQuery();

        return Ok(new { message = "Contact created successfully" });
    }

    [HttpPut("{id}")]
    public IActionResult UpdateContact(int id, [FromBody] ContactRequest request)
    {
        if (!IsAuthenticated()) return Unauthorized();

        using var connection = _dbService.GetConnection();
        connection.Open();
        using var command = (MySqlCommand)connection.CreateCommand();
        command.CommandText = @"
            UPDATE Contacts 
            SET Name = @name, Email = @email, WorkField = @workField 
            WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@name", request.Name);
        command.Parameters.AddWithValue("@email", request.Email);
        command.Parameters.AddWithValue("@workField", request.WorkField);
        
        var rowsAffected = command.ExecuteNonQuery();
        if (rowsAffected == 0)
            return NotFound();

        return Ok(new { message = "Contact updated successfully" });
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteContact(int id)
    {
        if (!IsAuthenticated()) return Unauthorized();

        using var connection = _dbService.GetConnection();
        connection.Open();
        using var command = (MySqlCommand)connection.CreateCommand();
        command.CommandText = "DELETE FROM Contacts WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);
        
        var rowsAffected = command.ExecuteNonQuery();
        if (rowsAffected == 0)
            return NotFound();

        return Ok(new { message = "Contact deleted successfully" });
    }
}

