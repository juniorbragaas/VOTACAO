using System.Data.OleDb;
using VOTACAO.Domain.Entities;
using VOTACAO.Domain.Interfaces;
using VOTACAO.Infrastructure.Database;

namespace VOTACAO.Data.Repositories;

public class CandidatoOpcaoRepository : ICandidatoOpcaoRepository
{
    private readonly MdbConnectionFactory _connectionFactory;

    public CandidatoOpcaoRepository(MdbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<CandidatoOpcao>> ObterTodosAtivosAsync()
    {
        var lista = new List<CandidatoOpcao>();
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT Id, Numero, Nome, Cargo, Foto, Ativo FROM CandidatosOpcoes WHERE Ativo = TRUE ORDER BY Numero ASC";
        using var cmd = new OleDbCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new CandidatoOpcao
            {
                Id = Convert.ToInt32(reader["Id"]),
                Numero = Convert.ToInt32(reader["Numero"]),
                Nome = reader["Nome"].ToString() ?? string.Empty,
                Cargo = reader["Cargo"].ToString() ?? string.Empty,
                Foto = reader["Foto"]?.ToString() ?? string.Empty,
                Ativo = Convert.ToBoolean(reader["Ativo"])
            });
        }
        return lista;
    }

    public async Task<CandidatoOpcao?> ObterPorIdAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT Id, Numero, Nome, Cargo, Foto, Ativo FROM CandidatosOpcoes WHERE Id = ?";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = id;

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new CandidatoOpcao
            {
                Id = Convert.ToInt32(reader["Id"]),
                Numero = Convert.ToInt32(reader["Numero"]),
                Nome = reader["Nome"].ToString() ?? string.Empty,
                Cargo = reader["Cargo"].ToString() ?? string.Empty,
                Foto = reader["Foto"]?.ToString() ?? string.Empty,
                Ativo = Convert.ToBoolean(reader["Ativo"])
            };
        }
        return null;
    }

    public async Task<CandidatoOpcao?> ObterPorNumeroAsync(int numero)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT Id, Numero, Nome, Cargo, Foto, Ativo FROM CandidatosOpcoes WHERE Numero = ?";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = numero;

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new CandidatoOpcao
            {
                Id = Convert.ToInt32(reader["Id"]),
                Numero = Convert.ToInt32(reader["Numero"]),
                Nome = reader["Nome"].ToString() ?? string.Empty,
                Cargo = reader["Cargo"].ToString() ?? string.Empty,
                Foto = reader["Foto"]?.ToString() ?? string.Empty,
                Ativo = Convert.ToBoolean(reader["Ativo"])
            };
        }
        return null;
    }

    public async Task<int> InserirAsync(CandidatoOpcao opcao)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "INSERT INTO CandidatosOpcoes (Numero, Nome, Cargo, Foto, Ativo) VALUES (?, ?, ?, ?, ?)";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = opcao.Numero;
        cmd.Parameters.Add("?", OleDbType.VarWChar, 150).Value = opcao.Nome;
        cmd.Parameters.Add("?", OleDbType.VarWChar, 100).Value = opcao.Cargo;
        cmd.Parameters.Add("?", OleDbType.VarWChar, 255).Value = opcao.Foto ?? string.Empty;
        cmd.Parameters.Add("?", OleDbType.Boolean).Value = opcao.Ativo;

        await cmd.ExecuteNonQueryAsync();

        using var cmdId = new OleDbCommand("SELECT @@IDENTITY", conn);
        return Convert.ToInt32(await cmdId.ExecuteScalarAsync());
    }
}
