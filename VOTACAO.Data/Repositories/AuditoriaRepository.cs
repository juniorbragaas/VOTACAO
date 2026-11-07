using System.Data.OleDb;
using VOTACAO.Domain.Interfaces;
using VOTACAO.Infrastructure.Database;

namespace VOTACAO.Data.Repositories;

public class AuditoriaRepository : IAuditoriaRepository
{
    private readonly MdbConnectionFactory _connectionFactory;

    public AuditoriaRepository(MdbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InserirLogAsync(string acao, string detalhes, string usuario = "Sistema")
    {
        try
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            string sql = "INSERT INTO Auditoria (Acao, Detalhes, Usuario, DataHora) VALUES (?, ?, ?, NOW())";
            using var cmd = new OleDbCommand(sql, conn);
            cmd.Parameters.Add("?", OleDbType.VarWChar, 100).Value = acao;
            cmd.Parameters.Add("?", OleDbType.LongVarWChar).Value = detalhes;
            cmd.Parameters.Add("?", OleDbType.VarWChar, 50).Value = usuario;

            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
        }
    }
}
