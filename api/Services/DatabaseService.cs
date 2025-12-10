using MySql.Data.MySqlClient;
using System.Data;

namespace BioIsac.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        // Priority: Environment variable (Heroku) > appsettings.json (local dev)
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") 
            ?? configuration["DATABASE_URL"];
        
        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            throw new InvalidOperationException(
                "DATABASE_URL is not set. " +
                "Please set it in appsettings.json for local development or as an environment variable in your deployment environment."
            );
        }

        // Parse DATABASE_URL into MySQL connection string
        _connectionString = DatabaseConnectionHelper.ParseDatabaseUrl(databaseUrl);
        
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            Console.WriteLine("MySQL connection successful");

        // Admin users table
        var createUsersTable = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INT PRIMARY KEY AUTO_INCREMENT,
                Username VARCHAR(255) UNIQUE NOT NULL,
                PasswordHash VARCHAR(255) NOT NULL,
                TwoFactorSecret VARCHAR(255),
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            )";

        // Contacts table
        var createContactsTable = @"
            CREATE TABLE IF NOT EXISTS Contacts (
                Id INT PRIMARY KEY AUTO_INCREMENT,
                Name VARCHAR(255) NOT NULL,
                Email VARCHAR(255) NOT NULL,
                WorkField VARCHAR(255) NOT NULL,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            )";

        // 2FA sessions table
        var createSessionsTable = @"
            CREATE TABLE IF NOT EXISTS TwoFactorSessions (
                Id INT PRIMARY KEY AUTO_INCREMENT,
                UserId INT NOT NULL,
                Token VARCHAR(255) NOT NULL,
                ExpiresAt DATETIME NOT NULL,
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
            )";

        using (var command = new MySqlCommand(createUsersTable, connection))
        {
            command.ExecuteNonQuery();
        }

        using (var command = new MySqlCommand(createContactsTable, connection))
        {
            command.ExecuteNonQuery();
        }

        using (var command = new MySqlCommand(createSessionsTable, connection))
        {
            command.ExecuteNonQuery();
        }

        // Create default admin if doesn't exist
        CreateDefaultAdmin(connection);
        Console.WriteLine("Database initialization completed");
        }
        catch (Exception ex)
        {
            // Log error but don't crash - database might not be ready yet
            Console.WriteLine($"Database initialization error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private void CreateDefaultAdmin(MySqlConnection connection)
    {
        try
        {
            var checkAdmin = "SELECT COUNT(*) FROM Users WHERE Username = 'admin'";
            using var checkCmd = new MySqlCommand(checkAdmin, connection);
            var exists = Convert.ToInt64(checkCmd.ExecuteScalar()) > 0;

            if (!exists)
            {
                // Default password: admin123
                // Using SHA256 hash (simple approach for this app)
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var bytes = System.Text.Encoding.UTF8.GetBytes("admin123");
                var hash = sha256.ComputeHash(bytes);
                var hashString = Convert.ToBase64String(hash);

                var insertAdmin = @"
                    INSERT INTO Users (Username, PasswordHash) 
                    VALUES ('admin', @hash)";
                using var insertCmd = new MySqlCommand(insertAdmin, connection);
                insertCmd.Parameters.AddWithValue("@hash", hashString);
                insertCmd.ExecuteNonQuery();
                Console.WriteLine("Default admin user created: admin / admin123");
            }
            else
            {
                Console.WriteLine("Admin user already exists");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating admin user: {ex.Message}");
        }
    }

    public IDbConnection GetConnection() => new MySqlConnection(_connectionString);
}
