using System.Data.OleDb;
using VOTACAO.Data;
using VOTACAO.Infrastructure.Database;
using VOTACAO.Infrastructure.Logging;

namespace VOTACAO.Data.Migrations;

public class DbMigrationEngine
{
    private readonly MdbConnectionFactory _connectionFactory;
    private readonly IAppLogger _logger;

    public DbMigrationEngine(MdbConnectionFactory connectionFactory, IAppLogger logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public void InitializeAndMigrate()
    {
        string dbPath = _connectionFactory.GetDatabaseFilePath();

        if (!File.Exists(dbPath))
        {
            _logger.LogInfo($"Criando arquivo físico MDB em: {dbPath}");
            CriarArquivoMdbFisico(dbPath);
        }

        GarantirTabelaVersionamento();
        ExecutarMigracoes();
        GarantirTabelaConsultas();
    }

    private void CriarArquivoMdbFisico(string caminho)
    {
        Type? adoxType = Type.GetTypeFromProgID("ADOX.Catalog");
        if (adoxType != null)
        {
            object? catalog = Activator.CreateInstance(adoxType);
            string connStr = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={caminho};";
            try
            {
                adoxType.InvokeMember("Create", System.Reflection.BindingFlags.InvokeMethod, null, catalog, new object[] { connStr });
                _logger.LogInfo("Arquivo .MDB criado com sucesso via Jet 4.0.");
                return;
            }
            catch
            {
                connStr = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={caminho};";
                adoxType.InvokeMember("Create", System.Reflection.BindingFlags.InvokeMethod, null, catalog, new object[] { connStr });
                _logger.LogInfo("Arquivo .MDB criado com sucesso via ACE 12.0.");
                return;
            }
        }
        throw new InvalidOperationException("Provedor COM ADOX.Catalog não encontrado no ambiente para criar o arquivo .MDB.");
    }

    private void GarantirTabelaVersionamento()
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();
        string sql = @"
            CREATE TABLE __DbVersionHistory (
                Id AUTOINCREMENT PRIMARY KEY,
                VersionNumber INT NOT NULL,
                Description TEXT(255) NOT NULL,
                AppliedOn DATETIME NOT NULL
            );";
        try
        {
            using var cmd = new OleDbCommand(sql, conn);
            cmd.ExecuteNonQuery();
            _logger.LogInfo("Tabela de versionamento __DbVersionHistory criada.");
        }
        catch
        {
        }
    }

    private void GarantirTabelaConsultas()
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();

        try
        {
            using var cmd = new OleDbCommand("SELECT COUNT(*) FROM Consultas", conn);
            cmd.ExecuteScalar();
            return;
        }
        catch
        {
            string sql = @"CREATE TABLE Consultas (
                Id AUTOINCREMENT PRIMARY KEY,
                Titulo TEXT(255) NOT NULL,
                [SQL] MEMO NOT NULL
            );";

            try
            {
                using var cmd = new OleDbCommand(sql, conn);
                cmd.ExecuteNonQuery();
                _logger.LogInfo("Tabela Consultas criada.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao criar tabela Consultas.", ex);
            }
        }
    }

    private int ObterVersaoAtual(OleDbConnection conn)
    {
        try
        {
            string sql = "SELECT MAX(VersionNumber) FROM __DbVersionHistory";
            using var cmd = new OleDbCommand(sql, conn);
            var result = cmd.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }
        }
        catch
        {
            return 0;
        }
        return 0;
    }

    

    private void ExecutarMigracoes()
    {
        

        using var conn = _connectionFactory.CreateConnection();
        conn.Open();
        int versaoAtual = ObterVersaoAtual(conn);

        var migracoes = new List<(int Versao, string Descricao, List<string> Scripts, bool SuprimirErros)>
        {
            (1, "Criacao Inicial das Tabelas e Indices", new List<string>
            {
                @"CREATE TABLE Participantes (
                    Id AUTOINCREMENT PRIMARY KEY,
                    Nome TEXT(150) NOT NULL,
                    DataNascimento DATETIME NOT NULL,
                    Categoria INT NOT NULL
                );",
                @"CREATE TABLE Auditoria (
                    Id AUTOINCREMENT PRIMARY KEY,
                    Acao TEXT(100) NOT NULL,
                    Detalhes MEMO NOT NULL,
                    Usuario TEXT(50) NOT NULL,
                    DataHora DATETIME NOT NULL
                );"
            }, false),
            (4, "Cadastro de Jurados", new List<string>
            {
                @"CREATE TABLE Jurados (
                    Id AUTOINCREMENT PRIMARY KEY,
                    Nome TEXT(150) NOT NULL,
                    Descricao MEMO
                );"
            }, false),

            (5, "Cadastro de Critérios", new List<string>
            {
                @"CREATE TABLE Criterios (
                    Id AUTOINCREMENT PRIMARY KEY,
                    Nome TEXT(150) NOT NULL
                );"
            }, false),

            (6, "Apuracao de Notas por Jurado", new List<string>
            {
                @"CREATE TABLE Voto (
                    Id AUTOINCREMENT PRIMARY KEY,
                    IdJurado INT NOT NULL,
                    IdParticipante INT NOT NULL,
                    IdCriterio INT NOT NULL,
                    Nota INT NOT NULL
                );",
                @"CREATE UNIQUE INDEX UX_Voto_Jurado_Participante_Criterio ON Voto (IdJurado, IdParticipante, IdCriterio);"
            }, false)
        };

        foreach (var mig in migracoes.OrderBy(m => m.Versao))
        {
            if (mig.Versao > versaoAtual)
            {
                _logger.LogInfo($"Aplicando Migração V{mig.Versao}: {mig.Descricao}");
                foreach (var sql in mig.Scripts)
                {
                    var instrucao = sql.TrimStart();
                    if (instrucao.StartsWith("CREATE TABLE ", StringComparison.OrdinalIgnoreCase))
                    {
                        var nomeTabela = instrucao["CREATE TABLE ".Length..].Split(new[] { ' ', '\r', '\n', '(' }, StringSplitOptions.RemoveEmptyEntries)[0];
                        if (TabelaExiste(conn, nomeTabela))
                        {
                            continue;
                        }
                    }

                    // A base pode ter sido criada antes do controle de versões. Nesse
                    // caso, a migração V6 precisa aceitar a tabela e o índice existentes.
                    if (mig.Versao == 6 &&
                        (TabelaExiste(conn, "Voto") && sql.TrimStart().StartsWith("CREATE TABLE Voto", StringComparison.OrdinalIgnoreCase) ||
                         IndiceExiste(conn, "Voto", "UX_Voto_Jurado_Participante_Criterio") && sql.TrimStart().StartsWith("CREATE UNIQUE INDEX UX_Voto_Jurado_Participante_Criterio", StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    using var cmd = new OleDbCommand(sql, conn);
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        if (mig.SuprimirErros)
                        {
                            _logger.LogInfo($"Instrução ignorada na migração {mig.Versao} (provavelmente já aplicada): {ex.Message}");
                        }
                        else
                        {
                            _logger.LogError($"Erro ao executar instrução SQL na migração {mig.Versao}: {sql}", ex);
                            throw;
                        }
                    }
                }

                string regSql = "INSERT INTO __DbVersionHistory (VersionNumber, Description, AppliedOn) VALUES (@v, @desc, NOW())";
                using var regCmd = new OleDbCommand(regSql, conn);
                regCmd.Parameters.AddWithValue("@v", mig.Versao);
                regCmd.Parameters.AddWithValue("@desc", mig.Descricao);
                regCmd.ExecuteNonQuery();

                _logger.LogInfo($"Migração V{mig.Versao} aplicada com sucesso.");
            }
        }
    }

    private static bool TabelaExiste(OleDbConnection conn, string nomeTabela)
    {
        var tabelas = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
        return tabelas?.Rows.Cast<System.Data.DataRow>().Any(row =>
            string.Equals(Convert.ToString(row["TABLE_NAME"]), nomeTabela, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Convert.ToString(row["TABLE_TYPE"]), "TABLE", StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static bool IndiceExiste(OleDbConnection conn, string nomeTabela, string nomeIndice)
    {
        var indices = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Indexes, null);
        return indices?.Rows.Cast<System.Data.DataRow>().Any(row =>
            string.Equals(Convert.ToString(row["TABLE_NAME"]), nomeTabela, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Convert.ToString(row["INDEX_NAME"]), nomeIndice, StringComparison.OrdinalIgnoreCase)) == true;
    }
}
