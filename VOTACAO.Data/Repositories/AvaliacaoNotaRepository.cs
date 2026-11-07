using System.Data.OleDb;
using VOTACAO.Domain.Entities;
using VOTACAO.Domain.Interfaces;
using VOTACAO.Infrastructure.Database;

namespace VOTACAO.Data.Repositories;

public class AvaliacaoNotaRepository : IAvaliacaoNotaRepository
{
    private readonly MdbConnectionFactory _connectionFactory;

    public AvaliacaoNotaRepository(MdbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private async Task GarantirTabelaVotoAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        try
        {
            using var prova = new OleDbCommand("SELECT COUNT(*) FROM Voto", conn);
            await prova.ExecuteScalarAsync();
        }
        catch
        {
            try
            {
                using var cmd = new OleDbCommand(@"CREATE TABLE Voto (
                    Id AUTOINCREMENT PRIMARY KEY,
                    IdJurado INT NOT NULL,
                    IdParticipante INT NOT NULL,
                    IdCriterio INT NOT NULL,
                    Nota INT NOT NULL
                );", conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
            }
        }

        try
        {
            using var cmd = new OleDbCommand("CREATE UNIQUE INDEX UX_Voto_Jurado_Participante_Criterio ON Voto (IdJurado, IdParticipante, IdCriterio);", conn);
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            await RemoverDuplicatasVotoAsync(conn);
            try
            {
                using var cmd = new OleDbCommand("CREATE UNIQUE INDEX UX_Voto_Jurado_Participante_Criterio ON Voto (IdJurado, IdParticipante, IdCriterio);", conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
            }
        }

        var indiceSqls = new[]
        {
            "CREATE INDEX IX_Voto_Jurado ON Voto (IdJurado);",
            "CREATE INDEX IX_Voto_Jurado_Participante ON Voto (IdJurado, IdParticipante);",
            "CREATE INDEX IX_Voto_Participante ON Voto (IdParticipante);",
            "CREATE INDEX IX_Voto_Criterio ON Voto (IdCriterio);"
        };

        foreach (var indiceSql in indiceSqls)
        {
            try
            {
                using var cmd = new OleDbCommand(indiceSql, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
            }
        }
    }

    private static async Task RemoverDuplicatasVotoAsync(OleDbConnection conn)
    {
        var sql = @"SELECT IdJurado, IdParticipante, IdCriterio, MIN(Id) AS PrimeiroId
                    FROM Voto
                    GROUP BY IdJurado, IdParticipante, IdCriterio
                    HAVING COUNT(*) > 1";

        using var cmd = new OleDbCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var idJurado = Convert.ToInt32(reader["IdJurado"]);
            var idParticipante = Convert.ToInt32(reader["IdParticipante"]);
            var idCriterio = Convert.ToInt32(reader["IdCriterio"]);
            var primeiroId = Convert.ToInt32(reader["PrimeiroId"]);

            using var deleteCmd = new OleDbCommand(
                "DELETE FROM Voto WHERE IdJurado = ? AND IdParticipante = ? AND IdCriterio = ? AND Id <> ?",
                conn);
            deleteCmd.Parameters.Add("?", OleDbType.Integer).Value = idJurado;
            deleteCmd.Parameters.Add("?", OleDbType.Integer).Value = idParticipante;
            deleteCmd.Parameters.Add("?", OleDbType.Integer).Value = idCriterio;
            deleteCmd.Parameters.Add("?", OleDbType.Integer).Value = primeiroId;
            await deleteCmd.ExecuteNonQueryAsync();
        }
    }

    private static AvaliacaoNota MapFromReader(OleDbDataReader reader)
    {
        return new AvaliacaoNota
        {
            Id = Convert.ToInt32(reader["Id"]),
            IdJurado = Convert.ToInt32(reader["IdJurado"]),
            IdParticipante = Convert.ToInt32(reader["IdParticipante"]),
            IdCriterio = Convert.ToInt32(reader["IdCriterio"]),
            Nota = Convert.ToInt32(reader["Nota"])
        };
    }

    private static async Task<List<int>> ObterIdsAsync(OleDbConnection conn, string tabela)
    {
        var ids = new List<int>();
        using var cmd = new OleDbCommand($"SELECT Id FROM {tabela} ORDER BY Id", conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            ids.Add(Convert.ToInt32(reader["Id"]));
        }

        return ids;
    }

    private static async Task InserirZeroParaTodosAsync(OleDbConnection conn, IReadOnlyCollection<int> idsJurado, IReadOnlyCollection<int> idsParticipante, IReadOnlyCollection<int> idsCriterio)
    {
        foreach (var idJurado in idsJurado)
        {
            foreach (var idParticipante in idsParticipante)
            {
                foreach (var idCriterio in idsCriterio)
                {
                    using var cmd = new OleDbCommand(
                        "INSERT INTO Voto (IdJurado, IdParticipante, IdCriterio, Nota) VALUES (?, ?, ?, 0)",
                        conn);
                    cmd.Parameters.Add("?", OleDbType.Integer).Value = idJurado;
                    cmd.Parameters.Add("?", OleDbType.Integer).Value = idParticipante;
                    cmd.Parameters.Add("?", OleDbType.Integer).Value = idCriterio;
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }

    public async Task<int> ZerarTodasNotasAsync()
    {
        await GarantirTabelaVotoAsync();

        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var idsJurado = await ObterIdsAsync(conn, "Jurado");
        var idsParticipante = await ObterIdsAsync(conn, "Participante");
        var idsCriterio = await ObterIdsAsync(conn, "Criterio");

        using (var deleteCmd = new OleDbCommand("DELETE FROM Voto", conn))
        {
            await deleteCmd.ExecuteNonQueryAsync();
        }

        if (!idsJurado.Any() || !idsParticipante.Any() || !idsCriterio.Any())
        {
            return 0;
        }

        await InserirZeroParaTodosAsync(conn, idsJurado, idsParticipante, idsCriterio);
        return idsJurado.Count * idsParticipante.Count * idsCriterio.Count;
    }

    public async Task<IEnumerable<AvaliacaoNota>> ObterPorJuradoAsync(int idJurado)
    {
        await GarantirTabelaVotoAsync();

        var lista = new List<AvaliacaoNota>();
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var sql = "SELECT Id, IdJurado, IdParticipante, IdCriterio, Nota FROM Voto WHERE IdJurado = ? ORDER BY IdParticipante, IdCriterio";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = idJurado;

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(MapFromReader((OleDbDataReader)reader));
        }

        return lista;
    }

    public async Task<int> SalvarNotasAsync(int idJurado, IReadOnlyCollection<(int IdParticipante, int IdCriterio, int Nota)> notas)
    {
        if (notas == null || notas.Count == 0)
            return 0;

        await GarantirTabelaVotoAsync();

        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();

        var total = 0;
        foreach (var nota in notas)
        {
            var consulta = "SELECT Id FROM Voto WHERE IdJurado = ? AND IdParticipante = ? AND IdCriterio = ?";
            using var selectCmd = new OleDbCommand(consulta, conn, transaction);
            selectCmd.Parameters.Add("?", OleDbType.Integer).Value = idJurado;
            selectCmd.Parameters.Add("?", OleDbType.Integer).Value = nota.IdParticipante;
            selectCmd.Parameters.Add("?", OleDbType.Integer).Value = nota.IdCriterio;

            var existente = await selectCmd.ExecuteScalarAsync();
            if (existente == null || existente == DBNull.Value)
            {
                using var insertCmd = new OleDbCommand(
                    "INSERT INTO Voto (IdJurado, IdParticipante, IdCriterio, Nota) VALUES (?, ?, ?, ?)",
                    conn,
                    transaction);
                insertCmd.Parameters.Add("?", OleDbType.Integer).Value = idJurado;
                insertCmd.Parameters.Add("?", OleDbType.Integer).Value = nota.IdParticipante;
                insertCmd.Parameters.Add("?", OleDbType.Integer).Value = nota.IdCriterio;
                insertCmd.Parameters.Add("?", OleDbType.Integer).Value = nota.Nota;
                await insertCmd.ExecuteNonQueryAsync();
            }
            else
            {
                using var updateCmd = new OleDbCommand(
                    "UPDATE Voto SET Nota = ? WHERE IdJurado = ? AND IdParticipante = ? AND IdCriterio = ?",
                    conn,
                    transaction);
                updateCmd.Parameters.Add("?", OleDbType.Integer).Value = nota.Nota;
                updateCmd.Parameters.Add("?", OleDbType.Integer).Value = idJurado;
                updateCmd.Parameters.Add("?", OleDbType.Integer).Value = nota.IdParticipante;
                updateCmd.Parameters.Add("?", OleDbType.Integer).Value = nota.IdCriterio;
                await updateCmd.ExecuteNonQueryAsync();
            }

            total++;
        }

        transaction.Commit();
        return total;
    }

    public async Task<AvaliacaoNota?> ObterPorJuradoParticipanteCriterioAsync(int idJurado, int idParticipante, int idCriterio)
    {
        await GarantirTabelaVotoAsync();

        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var sql = "SELECT Id, IdJurado, IdParticipante, IdCriterio, Nota FROM Voto WHERE IdJurado = ? AND IdParticipante = ? AND IdCriterio = ?";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = idJurado;
        cmd.Parameters.Add("?", OleDbType.Integer).Value = idParticipante;
        cmd.Parameters.Add("?", OleDbType.Integer).Value = idCriterio;

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapFromReader((OleDbDataReader)reader);
        }

        return null;
    }

    public async Task<IEnumerable<AvaliacaoNota>> ObterPorJuradoEParticipanteAsync(int idJurado, int idParticipante)
    {
        await GarantirTabelaVotoAsync();

        var lista = new List<AvaliacaoNota>();
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var sql = "SELECT Id, IdJurado, IdParticipante, IdCriterio, Nota FROM Voto WHERE IdJurado = ? AND IdParticipante = ? ORDER BY IdCriterio";
        using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = idJurado;
        cmd.Parameters.Add("?", OleDbType.Integer).Value = idParticipante;

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(MapFromReader((OleDbDataReader)reader));
        }

        return lista;
    }

    public async Task<int> InserirAsync(AvaliacaoNota nota)
    {
        await GarantirTabelaVotoAsync();

        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new OleDbCommand("INSERT INTO Voto (IdJurado, IdParticipante, IdCriterio, Nota) VALUES (?, ?, ?, ?)", conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = nota.IdJurado;
        cmd.Parameters.Add("?", OleDbType.Integer).Value = nota.IdParticipante;
        cmd.Parameters.Add("?", OleDbType.Integer).Value = nota.IdCriterio;
        cmd.Parameters.Add("?", OleDbType.Integer).Value = nota.Nota;

        await cmd.ExecuteNonQueryAsync();

        using var selectCmd = new OleDbCommand("SELECT @@IDENTITY", conn);
        var result = await selectCmd.ExecuteScalarAsync();
        return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }

    public async Task<bool> AtualizarAsync(AvaliacaoNota nota)
    {
        await GarantirTabelaVotoAsync();

        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new OleDbCommand("UPDATE Voto SET Nota = ? WHERE Id = ?", conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = nota.Nota;
        cmd.Parameters.Add("?", OleDbType.Integer).Value = nota.Id;

        var linhas = await cmd.ExecuteNonQueryAsync();
        return linhas > 0;
    }

    public async Task<bool> ExcluirPorJuradoAsync(int idJurado)
    {
        await GarantirTabelaVotoAsync();

        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new OleDbCommand("DELETE FROM Voto WHERE IdJurado = ?", conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = idJurado;

        var linhas = await cmd.ExecuteNonQueryAsync();
        return linhas > 0;
    }

    public async Task<bool> ExcluirPorJuradoParticipanteAsync(int idJurado, int idParticipante)
    {
        await GarantirTabelaVotoAsync();

        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new OleDbCommand("DELETE FROM Voto WHERE IdJurado = ? AND IdParticipante = ?", conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = idJurado;
        cmd.Parameters.Add("?", OleDbType.Integer).Value = idParticipante;

        var linhas = await cmd.ExecuteNonQueryAsync();
        return linhas > 0;
    }

    public async Task<bool> SalvarOuAtualizarAsync(AvaliacaoNota nota)
    {
        await GarantirTabelaVotoAsync();

        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var existente = await ObterPorJuradoParticipanteCriterioAsync(nota.IdJurado, nota.IdParticipante, nota.IdCriterio);
        if (existente == null)
        {
            using var insert = new OleDbCommand("INSERT INTO Voto (IdJurado, IdParticipante, IdCriterio, Nota) VALUES (?, ?, ?, ?)", conn);
            insert.Parameters.Add("?", OleDbType.Integer).Value = nota.IdJurado;
            insert.Parameters.Add("?", OleDbType.Integer).Value = nota.IdParticipante;
            insert.Parameters.Add("?", OleDbType.Integer).Value = nota.IdCriterio;
            insert.Parameters.Add("?", OleDbType.Integer).Value = nota.Nota;
            var linhas = await insert.ExecuteNonQueryAsync();
            return linhas > 0;
        }

        using var update = new OleDbCommand("UPDATE Voto SET Nota = ? WHERE IdJurado = ? AND IdParticipante = ? AND IdCriterio = ?", conn);
        update.Parameters.Add("?", OleDbType.Integer).Value = nota.Nota;
        update.Parameters.Add("?", OleDbType.Integer).Value = nota.IdJurado;
        update.Parameters.Add("?", OleDbType.Integer).Value = nota.IdParticipante;
        update.Parameters.Add("?", OleDbType.Integer).Value = nota.IdCriterio;
        var linhasAtualizadas = await update.ExecuteNonQueryAsync();
        return linhasAtualizadas > 0;
    }

    public async Task<int> GarantirNotasEmBrancoAsync(int idJurado, IReadOnlyCollection<int> idsParticipantes, IReadOnlyCollection<int> idsCriterios)
    {
        await GarantirTabelaVotoAsync();

        if (idsParticipantes == null || !idsParticipantes.Any() || idsCriterios == null || !idsCriterios.Any())
            return 0;

        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var linhasCriadas = 0;
        foreach (var idParticipante in idsParticipantes)
        {
            foreach (var idCriterio in idsCriterios)
            {
                if (await ObterPorJuradoParticipanteCriterioAsync(idJurado, idParticipante, idCriterio) is not null)
                    continue;

                using var cmd = new OleDbCommand("INSERT INTO Voto (IdJurado, IdParticipante, IdCriterio, Nota) VALUES (?, ?, ?, 5)", conn);
                cmd.Parameters.Add("?", OleDbType.Integer).Value = idJurado;
                cmd.Parameters.Add("?", OleDbType.Integer).Value = idParticipante;
                cmd.Parameters.Add("?", OleDbType.Integer).Value = idCriterio;
                await cmd.ExecuteNonQueryAsync();
                linhasCriadas++;
            }
        }

        return linhasCriadas;
    }
}
