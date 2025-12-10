using System;

namespace BioIsac.Services;

public static class DatabaseConnectionHelper
{
    /// <summary>
    /// Parses a DATABASE_URL (typically from Heroku) into a MySQL connection string.
    /// Format: mysql://username:password@host:port/database
    /// </summary>
    public static string ParseDatabaseUrl(string? databaseUrl)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            throw new InvalidOperationException(
                "DATABASE_URL environment variable is not set. " +
                "Please set it in your .env file for local development or configure it in your deployment environment."
            );
        }

        try
        {
            // Parse the URL
            var uri = new Uri(databaseUrl);
            
            // Extract components
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 3306; // Default MySQL port
            var database = uri.AbsolutePath.TrimStart('/');
            var username = Uri.UnescapeDataString(uri.UserInfo.Split(':')[0]);
            var password = uri.UserInfo.Contains(':') 
                ? Uri.UnescapeDataString(uri.UserInfo.Split(':', 2)[1]) 
                : string.Empty;

            // Build MySQL connection string
            // Heroku MySQL requires SSL, so we enable it by default
            var connectionString = $"Server={host};" +
                                 $"Port={port};" +
                                 $"Database={database};" +
                                 $"User Id={username};" +
                                 $"Password={password};" +
                                 $"SslMode=Required;" +
                                 $"AllowUserVariables=True;";

            return connectionString;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse DATABASE_URL. Ensure it's in the format: mysql://username:password@host:port/database. " +
                $"Error: {ex.Message}",
                ex
            );
        }
    }
}

