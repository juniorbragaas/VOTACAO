using System.Data;
using System.Data.OleDb;
using VOTACAO.Domain.Entities;
using VOTACAO.Domain.Enums;
using VOTACAO.Domain.Interfaces;
using VOTACAO.Infrastructure.Database;

namespace VOTACAO.Data.Repositories;

public class ParticipanteRepository : IParticipanteRepository
{
    private readonly MdbConnectionFactory _connectionFactory;

    public ParticipanteRepository(MdbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static Participante MapFromReader(OleDbDataReader reader)
    {
        return new Participante
        {
            Id = Convert.ToInt32(reader["Id"]),
            Nome = reader["Nome"].ToString() ?? string.Empty,
            DataNascimento = Convert.ToDateTime(reader["DataNascimento"]),
            Categoria = (CategoriaParticipante)Convert.ToInt32(reader["Categoria"])
        };
    }

    public async Task<IEnumerable<Participante>> ObterTodosAsync()
    {
        var lista = new List<Participante>();
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT Id, Nome, DataNascimento, Categoria FROM Participantes ORDER BY Nome ASC";
        using var cmd = new OleDbCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(MapFromReader((OleDbDataReader)reader));
        }
        return lista;
    }

    public async Task<Participante?> ObterPorIdAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT Id, Nome, DataNascimento, Categoria FROM Participantes WHERE Id = ?";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = id;

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapFromReader((OleDbDataReader)reader);
        }
        return null;
    }

    public async Task<IEnumerable<Participante>> PesquisarAsync(string termo)
    {
        var lista = new List<Participante>();
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT Id, Nome, DataNascimento, Categoria FROM Participantes WHERE Nome LIKE ? ORDER BY Nome ASC";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.VarWChar, 150).Value = $"%{termo}%";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(MapFromReader((OleDbDataReader)reader));
        }
        return lista;
    }

    public async Task<int> InserirAsync(Participante participante)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "INSERT INTO Participantes (Nome, DataNascimento, Categoria) VALUES (?, ?, ?)";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.VarWChar, 150).Value = participante.Nome;
        cmd.Parameters.Add("?", OleDbType.Date).Value = participante.DataNascimento;
        cmd.Parameters.Add("?", OleDbType.Integer).Value = (int)participante.Categoria;

        await cmd.ExecuteNonQueryAsync();

        using var cmdId = new OleDbCommand("SELECT @@IDENTITY", conn);
        var idGerado = await cmdId.ExecuteScalarAsync();
        return Convert.ToInt32(idGerado);
    }

    public async Task<bool> AtualizarAsync(Participante participante)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "UPDATE Participantes SET Nome = ?, DataNascimento = ?, Categoria = ? WHERE Id = ?";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.VarWChar, 150).Value = participante.Nome;
        cmd.Parameters.Add("?", OleDbType.Date).Value = participante.DataNascimento;
        cmd.Parameters.Add("?", OleDbType.Integer).Value = (int)participante.Categoria;
        cmd.Parameters.Add("?", OleDbType.Integer).Value = participante.Id;

        int rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "DELETE FROM Participantes WHERE Id = ?";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = id;

        int rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<int> ObterTotalCadastradosAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT COUNT(*) FROM Participantes";
        using var cmd = new OleDbCommand(sql, conn);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
}
