namespace VOTACAO.Infrastructure.Configuration;

public class DatabaseConfig
{
    public string Directory { get; set; } = "Database";
    public string FileName { get; set; } = "Votacao.mdb";
    public string Provider { get; set; } = "Microsoft.ACE.OLEDB.12.0";
    public string FallbackProvider { get; set; } = "Microsoft.Jet.OLEDB.4.0";
}

public class LoggingConfig
{
    public string LogDirectory { get; set; } = "Logs";
}

public class AppSettings
{
    public DatabaseConfig Database { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
}
