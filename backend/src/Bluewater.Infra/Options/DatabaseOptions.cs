using Npgsql;

namespace Bluewater.Infra.Options;

public class DatabaseOptions
{
    public string Host { get; set; } = null!;
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;

    public string BuildConnectionString() => new NpgsqlConnectionStringBuilder
    {
        Host = Host,
        Port = Port,
        Database = Database,
        Username = Username,
        Password = Password
    }.ConnectionString;
}