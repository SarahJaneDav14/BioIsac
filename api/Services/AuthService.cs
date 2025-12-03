using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
using OtpNet;
using BioIsac.Models;

namespace BioIsac.Services;

public class AuthService
{
    private readonly DatabaseService _dbService;
    private readonly IConfiguration _configuration;

    public AuthService(DatabaseService dbService, IConfiguration configuration)
    {
        _dbService = dbService;
        _configuration = configuration;
    }

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }

    public string GenerateTwoFactorSecret()
    {
        var secret = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(secret);
    }

    public bool VerifyTwoFactorCode(string secret, string code)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(code))
            return false;

        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code, out _, new VerificationWindow(2, 2));
    }

    public string? GetTwoFactorSecret(string username)
    {
        using var connection = _dbService.GetConnection();
        connection.Open();
        using var command = new MySqlCommand("SELECT TwoFactorSecret FROM Users WHERE Username = @username", (MySqlConnection)connection);
        command.Parameters.AddWithValue("@username", username);
        var result = command.ExecuteScalar();
        return result?.ToString();
    }

    public bool SetTwoFactorSecret(string username, string secret)
    {
        using var connection = _dbService.GetConnection();
        connection.Open();
        using var command = new MySqlCommand("UPDATE Users SET TwoFactorSecret = @secret WHERE Username = @username", (MySqlConnection)connection);
        command.Parameters.AddWithValue("@secret", secret);
        command.Parameters.AddWithValue("@username", username);
        return command.ExecuteNonQuery() > 0;
    }

    public bool ValidateUser(string username, string password)
    {
        try
        {
            using var connection = _dbService.GetConnection();
            connection.Open();
            using var command = new MySqlCommand("SELECT PasswordHash FROM Users WHERE Username = @username", (MySqlConnection)connection);
            command.Parameters.AddWithValue("@username", username);
            var hash = command.ExecuteScalar()?.ToString();
            
            if (string.IsNullOrEmpty(hash))
            {
                Console.WriteLine($"User '{username}' not found in database");
                return false;
            }

            var isValid = VerifyPassword(password, hash);
            if (!isValid)
            {
                Console.WriteLine($"Invalid password for user '{username}'");
            }
            return isValid;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error validating user: {ex.Message}");
            return false;
        }
    }

    public string GenerateSessionToken()
    {
        return Guid.NewGuid().ToString();
    }

    public void SaveSession(string username, string token)
    {
        using var connection = _dbService.GetConnection();
        connection.Open();
        
        // Get user ID
        int userId;
        using (var cmd = new MySqlCommand("SELECT Id FROM Users WHERE Username = @username", (MySqlConnection)connection))
        {
            cmd.Parameters.AddWithValue("@username", username);
            var result = cmd.ExecuteScalar();
            userId = Convert.ToInt32(result);
        }

        // Delete old sessions
        using (var cmd = new MySqlCommand("DELETE FROM TwoFactorSessions WHERE UserId = @userId", (MySqlConnection)connection))
        {
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.ExecuteNonQuery();
        }

        // Insert new session
        using (var cmd = new MySqlCommand(@"
            INSERT INTO TwoFactorSessions (UserId, Token, ExpiresAt) 
            VALUES (@userId, @token, @expiresAt)", (MySqlConnection)connection))
        {
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@token", token);
            cmd.Parameters.AddWithValue("@expiresAt", DateTime.UtcNow.AddHours(24));
            cmd.ExecuteNonQuery();
        }
    }

    public bool ValidateSession(string token)
    {
        try
        {
            using var connection = _dbService.GetConnection();
            connection.Open();
            using var command = new MySqlCommand(@"
                SELECT COUNT(*) FROM TwoFactorSessions 
                WHERE Token = @token AND ExpiresAt > @now", (MySqlConnection)connection);
            command.Parameters.AddWithValue("@token", token);
            command.Parameters.AddWithValue("@now", DateTime.UtcNow);
            var count = Convert.ToInt64(command.ExecuteScalar());
            return count > 0;
        }
        catch
        {
            return false;
        }
    }
}

