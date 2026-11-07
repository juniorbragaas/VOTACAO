using System.Data.OleDb;
using VOTACAO.Infrastructure.Configuration;

namespace VOTACAO.Infrastructure.Database;

public class MdbConnectionFactory
{
    private readonly AppSettings _config;
    private string _activeConnectionString = string.Empty;

    public MdbConnectionFactory(AppSettings config)
    {
        _config = config;
    }

    public string GetDatabaseFilePath()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string dbDir = Path.Combine(baseDir, _config.Database.Directory);
        if (!Directory.Exists(dbDir))
        {
            Directory.CreateDirectory(dbDir);
        }
        return Path.Combine(dbDir, _config.Database.FileName);
    }

    public OleDbConnection CreateConnection()
    {
        if (!string.IsNullOrEmpty(_activeConnectionString))
        {
            return new OleDbConnection(_activeConnectionString);
        }

        string dbPath = GetDatabaseFilePath();
        
        string connStr1 = $"Provider={_config.Database.Provider};Data Source={dbPath};Persist Security Info=False;";
        try
        {
            var conn = new OleDbConnection(connStr1);
            conn.Open();
            conn.Close();
            _activeConnectionString = connStr1;
            return new OleDbConnection(_activeConnectionString);
        }
        catch
        {
            string connStr2 = $"Provider={_config.Database.FallbackProvider};Data Source={dbPath};Persist Security Info=False;";
            _activeConnectionString = connStr2;
            return new OleDbConnection(_activeConnectionString);
        }
    }
}
