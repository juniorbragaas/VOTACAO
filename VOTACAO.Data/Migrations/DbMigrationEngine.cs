using System.Data.OleDb;
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

    private void GarantirTabelaVoto()
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();

        try
        {
            using var cmd = new OleDbCommand("SELECT COUNT(*) FROM Voto", conn);
            cmd.ExecuteScalar();
            return;
        }
        catch
        {
            string sql = @"CREATE TABLE Voto (
                Id AUTOINCREMENT PRIMARY KEY,
                IdJurado INT NOT NULL,
                IdParticipante INT NOT NULL,
                IdCriterio INT NOT NULL,
                Nota INT NOT NULL
            );";

            try
            {
                using var cmd = new OleDbCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
            catch
            {
            }
        }
    }

    private void ExecutarMigracoes()
    {
        GarantirTabelaVoto();

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

                @"CREATE TABLE CandidatosOpcoes (
                    Id AUTOINCREMENT PRIMARY KEY,
                    Numero INT NOT NULL,
                    Nome TEXT(150) NOT NULL,
                    Cargo TEXT(100) NOT NULL,
                    Foto TEXT(255),
                    Ativo YESNO NOT NULL
                );",
                @"CREATE UNIQUE INDEX UX_Candidatos_Num ON CandidatosOpcoes (Numero);",

                @"CREATE TABLE Eleicoes (
                    Id AUTOINCREMENT PRIMARY KEY,
                    Titulo TEXT(150) NOT NULL,
                    Descricao MEMO,
                    DataInicio DATETIME NOT NULL,
                    DataFim DATETIME,
                    Status INT NOT NULL,
                    PermiteVotoBrancoNulo YESNO NOT NULL
                );",

                @"CREATE TABLE Votos (
                    Id AUTOINCREMENT PRIMARY KEY,
                    EleicaoId INT NOT NULL,
                    CandidatoOpcaoId INT,
                    IsBranco YESNO NOT NULL,
                    IsNulo YESNO NOT NULL,
                    DataHora DATETIME NOT NULL,
                    HashIntegridade TEXT(128) NOT NULL
                );",

                @"CREATE TABLE RegistroParticipacoes (
                    Id AUTOINCREMENT PRIMARY KEY,
                    EleicaoId INT NOT NULL,
                    ParticipanteId INT NOT NULL,
                    DataHoraVoto DATETIME NOT NULL
                );",
                @"CREATE UNIQUE INDEX UX_Part_Eleicao ON RegistroParticipacoes (EleicaoId, ParticipanteId);",

                @"CREATE TABLE Auditoria (
                    Id AUTOINCREMENT PRIMARY KEY,
                    Acao TEXT(100) NOT NULL,
                    Detalhes MEMO NOT NULL,
                    Usuario TEXT(50) NOT NULL,
                    DataHora DATETIME NOT NULL
                );"
            }, false),

            (2, "Carga Inicial (Seed) de Eleicao e Candidatos Demonstrativos", new List<string>
            {
                @"INSERT INTO Eleicoes (Titulo, Descricao, DataInicio, Status, PermiteVotoBrancoNulo)
                  VALUES ('Eleição Diretoria & Conselho 2026', 'Processo eleitoral oficial para mandato 2026-2028', NOW(), 1, TRUE);",

                @"INSERT INTO CandidatosOpcoes (Numero, Nome, Cargo, Ativo) VALUES (10, 'Chapa 1 - Inovação e Transparência', 'Diretoria Geral', TRUE);",
                @"INSERT INTO CandidatosOpcoes (Numero, Nome, Cargo, Ativo) VALUES (20, 'Chapa 2 - Renovação e Participação', 'Diretoria Geral', TRUE);",
                @"INSERT INTO CandidatosOpcoes (Numero, Nome, Cargo, Ativo) VALUES (30, 'Chapa 3 - Integração e Futuro', 'Diretoria Geral', TRUE);",

                @"INSERT INTO Participantes (Nome, DataNascimento, Categoria) VALUES ('Ana Carolina Mendes', #1990-05-15#, 1);",
                @"INSERT INTO Participantes (Nome, DataNascimento, Categoria) VALUES ('Bruno Henrique Lima', #1992-08-22#, 1);",
                @"INSERT INTO Participantes (Nome, DataNascimento, Categoria) VALUES ('Carlos Eduardo Silva', #1988-03-10#, 1);",
                @"INSERT INTO Participantes (Nome, DataNascimento, Categoria) VALUES ('Daniela Freitas Ramos', #2008-11-30#, 2);",
                @"INSERT INTO Participantes (Nome, DataNascimento, Categoria) VALUES ('Eduardo Moreira Santos', #1991-07-18#, 1);"
            }, false),

            (3, "Reestruturacao da Tabela Participantes (Categoria ADULTO/TEEN)", new List<string>
            {
                @"ALTER TABLE Participantes ADD COLUMN DataNascimento DATETIME;",
                @"ALTER TABLE Participantes ADD COLUMN Categoria INT;",
                @"UPDATE Participantes SET DataNascimento = #2000-01-01#, Categoria = 1 WHERE DataNascimento IS NULL;",
                @"DROP INDEX UX_Participantes_Doc ON Participantes;",
                @"DROP INDEX UX_Participantes_Mat ON Participantes;",
                @"ALTER TABLE Participantes DROP COLUMN Documento;",
                @"ALTER TABLE Participantes DROP COLUMN Matricula;",
                @"ALTER TABLE Participantes DROP COLUMN Situacao;",
                @"ALTER TABLE Participantes DROP COLUMN DataCadastro;"
            }, true),

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
}
