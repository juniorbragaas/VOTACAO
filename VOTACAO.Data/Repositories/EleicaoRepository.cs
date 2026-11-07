using System.Data.OleDb;
using VOTACAO.Domain.Entities;
using VOTACAO.Domain.Enums;
using VOTACAO.Domain.Interfaces;
using VOTACAO.Infrastructure.Database;

namespace VOTACAO.Data.Repositories;

public class EleicaoRepository : IEleicaoRepository
{
    private readonly MdbConnectionFactory _connectionFactory;

    public EleicaoRepository(MdbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Eleicao?> ObterEleicaoAtivaAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT TOP 1 Id, Titulo, Descricao, DataInicio, DataFim, Status, PermiteVotoBrancoNulo " +
                     "FROM Eleicoes WHERE Status = ? ORDER BY Id DESC";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = (int)StatusEleicao.Aberta;

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Eleicao
            {
                Id = Convert.ToInt32(reader["Id"]),
                Titulo = reader["Titulo"].ToString() ?? string.Empty,
                Descricao = reader["Descricao"]?.ToString() ?? string.Empty,
                DataInicio = Convert.ToDateTime(reader["DataInicio"]),
                DataFim = reader["DataFim"] != DBNull.Value ? Convert.ToDateTime(reader["DataFim"]) : null,
                Status = (StatusEleicao)Convert.ToInt32(reader["Status"]),
                PermiteVotoBrancoNulo = Convert.ToBoolean(reader["PermiteVotoBrancoNulo"])
            };
        }
        return null;
    }

    public async Task<Eleicao?> ObterPorIdAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "SELECT Id, Titulo, Descricao, DataInicio, DataFim, Status, PermiteVotoBrancoNulo FROM Eleicoes WHERE Id = ?";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = id;

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Eleicao
            {
                Id = Convert.ToInt32(reader["Id"]),
                Titulo = reader["Titulo"].ToString() ?? string.Empty,
                Descricao = reader["Descricao"]?.ToString() ?? string.Empty,
                DataInicio = Convert.ToDateTime(reader["DataInicio"]),
                DataFim = reader["DataFim"] != DBNull.Value ? Convert.ToDateTime(reader["DataFim"]) : null,
                Status = (StatusEleicao)Convert.ToInt32(reader["Status"]),
                PermiteVotoBrancoNulo = Convert.ToBoolean(reader["PermiteVotoBrancoNulo"])
            };
        }
        return null;
    }

    public async Task<int> InserirAsync(Eleicao eleicao)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        string sql = "INSERT INTO Eleicoes (Titulo, Descricao, DataInicio, Status, PermiteVotoBrancoNulo) VALUES (?, ?, ?, ?, ?)";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.VarWChar, 150).Value = eleicao.Titulo;
        cmd.Parameters.Add("?", OleDbType.LongVarWChar).Value = eleicao.Descricao;
        cmd.Parameters.Add("?", OleDbType.Date).Value = eleicao.DataInicio;
        cmd.Parameters.Add("?", OleDbType.Integer).Value = (int)eleicao.Status;
        cmd.Parameters.Add("?", OleDbType.Boolean).Value = eleicao.PermiteVotoBrancoNulo;

        await cmd.ExecuteNonQueryAsync();

        using var cmdId = new OleDbCommand("SELECT @@IDENTITY", conn);
        return Convert.ToInt32(await cmdId.ExecuteScalarAsync());
    }
}
