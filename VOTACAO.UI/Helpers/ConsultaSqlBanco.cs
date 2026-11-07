using System.Data;
using System.Data.OleDb;

namespace VOTACAO.UI.Helpers;

internal static class ConsultaSqlBanco
{
    internal const string SqlEmPartes = "[[SQL_ARMAZENADO_EM_PARTES]]";
    internal const int TamanhoParte = 50000;

    internal static void GarantirTabelaPartes(OleDbConnection conn)
    {
        var tabelas = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
        bool existe = tabelas?.Rows.Cast<DataRow>().Any(row =>
            string.Equals(Convert.ToString(row["TABLE_NAME"]), "ConsultaSqlPartes", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Convert.ToString(row["TABLE_TYPE"]), "TABLE", StringComparison.OrdinalIgnoreCase)) == true;

        if (!existe)
        {
            using var cmd = new OleDbCommand(
                "CREATE TABLE ConsultaSqlPartes (Id AUTOINCREMENT PRIMARY KEY, IdConsulta LONG NOT NULL, NumeroParte LONG NOT NULL, Conteudo MEMO NOT NULL)", conn);
            cmd.ExecuteNonQuery();
        }
    }

    internal static void GarantirTabelaConsultas(OleDbConnection conn)
    {
        var tabelas = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
        bool existe = tabelas?.Rows.Cast<DataRow>().Any(row =>
            string.Equals(Convert.ToString(row["TABLE_NAME"]), "Consultas", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Convert.ToString(row["TABLE_TYPE"]), "TABLE", StringComparison.OrdinalIgnoreCase)) == true;

        if (!existe)
        {
            using var cmd = new OleDbCommand(
                "CREATE TABLE Consultas (Id AUTOINCREMENT PRIMARY KEY, Titulo TEXT(255) NOT NULL, [SQL] MEMO NOT NULL)", conn);
            cmd.ExecuteNonQuery();
        }
    }

    internal static string CarregarSql(OleDbConnection conn, int idConsulta, string sqlSalvo)
    {
        if (!string.Equals(sqlSalvo, SqlEmPartes, StringComparison.Ordinal))
            return sqlSalvo;

        GarantirTabelaPartes(conn);
        using var cmd = new OleDbCommand(
            "SELECT Conteudo FROM ConsultaSqlPartes WHERE IdConsulta = ? ORDER BY NumeroParte", conn);
        cmd.Parameters.Add("?", OleDbType.Integer).Value = idConsulta;
        using var reader = cmd.ExecuteReader();

        var sql = new System.Text.StringBuilder();
        while (reader.Read())
        {
            if (!reader.IsDBNull(0))
                sql.Append(Convert.ToString(reader.GetValue(0)));
        }
        return sql.ToString();
    }
}
