using System.Data.OleDb;
using VOTACAO.Domain.Entities;
using VOTACAO.Domain.Interfaces;
using VOTACAO.Infrastructure.Database;

namespace VOTACAO.Data.Repositories;

public class JuradoRepository : IJuradoRepository
{
    private readonly MdbConnectionFactory _connectionFactory;

    public JuradoRepository(MdbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static Jurado MapFromReader(OleDbDataReader reader)
    {
        return new Jurado
        {
            Id = Convert.ToInt32(reader["Id"]),
            Nome = reader["Nome"].ToString() ?? string.Empty,
            Descricao = reader["Descricao"].ToString() ?? string.Empty
        };
    }

    public async Task<IEnumerable<Jurado>> ObterTodosAsync()
    {
        var lista = new List<Jurado>();
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT Id, Nome, Descricao FROM Jurados ORDER BY Nome ASC";
        using var cmd = new OleDbCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(MapFromReader((OleDbDataReader)reader));
        }
        return lista;
    }

    public async Task<Jurado?> ObterPorIdAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT Id, Nome, Descricao FROM Jurados WHERE Id = ?";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = id;

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapFromReader((OleDbDataReader)reader);
        }
        return null;
    }

    public async Task<Jurado?> ObterPorNomeAsync(string nome)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT Id, Nome, Descricao FROM Jurados WHERE Nome = ?";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.VarWChar, 150).Value = nome;

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapFromReader((OleDbDataReader)reader);
        }
        return null;
    }

    public async Task<IEnumerable<Jurado>> PesquisarAsync(string termo)
    {
        var lista = new List<Jurado>();
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT Id, Nome, Descricao FROM Jurados WHERE Nome LIKE ? OR Descricao LIKE ? ORDER BY Nome ASC";
        using var cmd = new OleDbCommand(sql, conn);
        string wildcard = $"%{termo}%";
        cmd.Parameters.Add("?", OleDbType.VarWChar, 150).Value = wildcard;
        cmd.Parameters.Add("?", OleDbType.VarWChar, 255).Value = wildcard;

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(MapFromReader((OleDbDataReader)reader));
        }
        return lista;
    }

    public async Task<int> InserirAsync(Jurado jurado)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "INSERT INTO Jurados (Nome, Descricao) VALUES (?, ?)";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.VarWChar, 150).Value = jurado.Nome;
        cmd.Parameters.Add("?", OleDbType.VarWChar, 255).Value = jurado.Descricao;

        await cmd.ExecuteNonQueryAsync();

        using var cmdId = new OleDbCommand("SELECT @@IDENTITY", conn);
        return Convert.ToInt32(await cmdId.ExecuteScalarAsync());
    }

    public async Task<bool> AtualizarAsync(Jurado jurado)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "UPDATE Jurados SET Nome = ?, Descricao = ? WHERE Id = ?";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.VarWChar, 150).Value = jurado.Nome;
        cmd.Parameters.Add("?", OleDbType.VarWChar, 255).Value = jurado.Descricao;
        cmd.Parameters.Add("?", OleDbType.Integer).Value = jurado.Id;

        int rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "DELETE FROM Jurados WHERE Id = ?";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = id;

        int rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<int> ObterTotalAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT COUNT(*) FROM Jurados";
        using var cmd = new OleDbCommand(sql, conn);
        var result = await cmd.ExecuteScalarAsync();
        return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }
}
