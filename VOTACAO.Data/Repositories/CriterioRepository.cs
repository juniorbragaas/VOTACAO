using System.Data.OleDb;
using VOTACAO.Domain.Entities;
using VOTACAO.Domain.Interfaces;
using VOTACAO.Infrastructure.Database;

namespace VOTACAO.Data.Repositories;

public class CriterioRepository : ICriterioRepository
{
    private readonly MdbConnectionFactory _connectionFactory;

    public CriterioRepository(MdbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static Criterio MapFromReader(OleDbDataReader reader)
    {
        return new Criterio
        {
            Id = Convert.ToInt32(reader["Id"]),
            Nome = reader["Nome"].ToString() ?? string.Empty
        };
    }

    public async Task<IEnumerable<Criterio>> ObterTodosAsync()
    {
        var lista = new List<Criterio>();
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT Id, Nome FROM Criterios ORDER BY Nome ASC";
        using var cmd = new OleDbCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(MapFromReader((OleDbDataReader)reader));
        }
        return lista;
    }

    public async Task<Criterio?> ObterPorIdAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT Id, Nome FROM Criterios WHERE Id = ?";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = id;

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapFromReader((OleDbDataReader)reader);
        }
        return null;
    }

    public async Task<Criterio?> ObterPorNomeAsync(string nome)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT Id, Nome FROM Criterios WHERE Nome = ?";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.VarWChar, 150).Value = nome;

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapFromReader((OleDbDataReader)reader);
        }
        return null;
    }

    public async Task<IEnumerable<Criterio>> PesquisarAsync(string termo)
    {
        var lista = new List<Criterio>();
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT Id, Nome FROM Criterios WHERE Nome LIKE ? ORDER BY Nome ASC";
        using var cmd = new OleDbCommand(sql, conn);
        string wildcard = $"%{termo}%";
        cmd.Parameters.Add("?", OleDbType.VarWChar, 150).Value = wildcard;

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(MapFromReader((OleDbDataReader)reader));
        }
        return lista;
    }

    public async Task<int> InserirAsync(Criterio criterio)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "INSERT INTO Criterios (Nome) VALUES (?)";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.VarWChar, 150).Value = criterio.Nome;

        await cmd.ExecuteNonQueryAsync();

        using var cmdId = new OleDbCommand("SELECT @@IDENTITY", conn);
        return Convert.ToInt32(await cmdId.ExecuteScalarAsync());
    }

    public async Task<bool> AtualizarAsync(Criterio criterio)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "UPDATE Criterios SET Nome = ? WHERE Id = ?";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.VarWChar, 150).Value = criterio.Nome;
        cmd.Parameters.Add("?", OleDbType.Integer).Value = criterio.Id;

        int rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "DELETE FROM Criterios WHERE Id = ?";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = id;

        int rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<int> ObterTotalAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT COUNT(*) FROM Criterios";
        using var cmd = new OleDbCommand(sql, conn);
        var result = await cmd.ExecuteScalarAsync();
        return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }
}
