using System.Data;
using System.Globalization;
using System.Text;

namespace VOTACAO.UI.Helpers;

internal static class PdfTabelaExporter
{
    private const float Margem = 30;
    private const float FonteDados = 8;
    private const float AlturaLinhaTexto = 10;
    private const float LarguraMinimaColuna = 76;

    internal static byte[] Gerar(DataTable tabela, string titulo)
    {
        bool paisagem = tabela.Columns.Count > 4;
        float larguraPagina = paisagem ? 842 : 595;
        float alturaPagina = paisagem ? 595 : 842;
        float larguraUtil = larguraPagina - (Margem * 2);
        int colunasPorPagina = Math.Max(1, (int)(larguraUtil / LarguraMinimaColuna));
        var colunas = Enumerable.Range(0, tabela.Columns.Count).ToList();
        var grupos = colunas.Count == 0
            ? new List<List<int>> { new() }
            : colunas.Chunk(colunasPorPagina).Select(grupo => grupo.ToList()).ToList();
        var valores = tabela.Rows.Cast<DataRow>()
            .Select(linha => linha.ItemArray.Select(valor => valor == DBNull.Value ? string.Empty : Convert.ToString(valor, CultureInfo.CurrentCulture) ?? string.Empty).ToArray())
            .ToList();

        var paginas = new List<PdfPagina>();
        foreach (var grupo in grupos)
        {
            var larguras = CalcularLarguras(tabela, grupo, larguraUtil);
            var quebrasCabecalho = grupo.Select((coluna, indice) => QuebrarTexto(tabela.Columns[coluna].ColumnName, Capacidade(larguras[indice], FonteDados))).ToList();
            float alturaCabecalho = Math.Max(28, (quebrasCabecalho.Count == 0 ? 1 : quebrasCabecalho.Max(linhas => linhas.Count)) * AlturaLinhaTexto + 10);
            float topoDados = alturaPagina - Margem - 25 - alturaCabecalho;
            float alturaDisponivel = topoDados - Margem - 20;
            var linhasPagina = new List<int>();
            float alturaAtual = 0;

            for (int indiceLinha = 0; indiceLinha < valores.Count; indiceLinha++)
            {
                var quebras = grupo.Select((coluna, indiceColuna) => QuebrarTexto(valores[indiceLinha][coluna], Capacidade(larguras[indiceColuna], FonteDados))).ToList();
                float alturaLinha = Math.Max(24, quebras.Max(linhas => linhas.Count) * AlturaLinhaTexto + 8);
                if (linhasPagina.Count > 0 && alturaAtual + alturaLinha > alturaDisponivel)
                {
                    paginas.Add(new PdfPagina(grupo, linhasPagina, larguras, quebrasCabecalho, alturaCabecalho));
                    linhasPagina = new List<int>();
                    alturaAtual = 0;
                }
                linhasPagina.Add(indiceLinha);
                alturaAtual += alturaLinha;
            }

            if (linhasPagina.Count > 0 || valores.Count == 0)
                paginas.Add(new PdfPagina(grupo, linhasPagina, larguras, quebrasCabecalho, alturaCabecalho));
        }

        var objetos = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            string.Empty,
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"
        };
        var referenciasPaginas = new List<string>();

        for (int indicePagina = 0; indicePagina < paginas.Count; indicePagina++)
        {
            var pagina = paginas[indicePagina];
            int numeroObjetoPagina = 4 + indicePagina * 2;
            int numeroObjetoConteudo = numeroObjetoPagina + 1;
            referenciasPaginas.Add($"{numeroObjetoPagina} 0 R");

            var conteudo = DesenharPagina(tabela, valores, titulo, pagina, indicePagina, paginas.Count, larguraPagina, alturaPagina);
            objetos.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {larguraPagina} {alturaPagina}] /Resources << /Font << /F1 3 0 R >> >> /Contents {numeroObjetoConteudo} 0 R >>");
            objetos.Add($"<< /Length {Encoding.ASCII.GetByteCount(conteudo)} >>\nstream\n{conteudo}endstream");
        }

        objetos[1] = $"<< /Type /Pages /Kids [{string.Join(" ", referenciasPaginas)}] /Count {paginas.Count} >>";
        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (int i = 0; i < objetos.Count; i++)
        {
            offsets.Add(pdf.Length);
            pdf.Append($"{i + 1} 0 obj\n{objetos[i]}\nendobj\n");
        }

        long xrefPos = pdf.Length;
        pdf.Append($"xref\n0 {objetos.Count + 1}\n0000000000 65535 f \n");
        for (int i = 1; i < offsets.Count; i++)
            pdf.Append($"{offsets[i]:D10} 00000 n \n");
        pdf.Append($"trailer\n<< /Size {objetos.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefPos}\n%%EOF");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static string DesenharPagina(DataTable tabela, IReadOnlyList<string[]> valores, string titulo, PdfPagina pagina,
        int indicePagina, int totalPaginas, float larguraPagina, float alturaPagina)
    {
        var pdf = new StringBuilder();
        float larguraUtil = larguraPagina - Margem * 2;
        Texto(pdf, Margem, alturaPagina - Margem - 14, 15, titulo);
        Texto(pdf, Margem, alturaPagina - Margem - 32, 8, $"{valores.Count:N0} registro(s)  |  {pagina.Colunas.Count:N0} coluna(s)");

        float yTopo = alturaPagina - Margem - 45;
        float yCabecalho = yTopo - pagina.AlturaCabecalho;
        float x = Margem;
        for (int i = 0; i < pagina.Colunas.Count; i++)
        {
            Caixa(pdf, x, yCabecalho, pagina.Larguras[i], pagina.AlturaCabecalho, 30, 41, 59);
            Bordas(pdf, x, yCabecalho, pagina.Larguras[i], pagina.AlturaCabecalho);
            DesenharLinhas(pdf, pagina.QuebrasCabecalho[i], x + 5, yTopo - 14, FonteDados, AlturaLinhaTexto, branco: true);
            x += pagina.Larguras[i];
        }

        float y = yCabecalho;
        if (pagina.Linhas.Count == 0)
        {
            Texto(pdf, Margem + 5, y - 18, FonteDados, "Nenhum registro retornado.");
        }
        else
        {
            for (int indiceRegistroNaPagina = 0; indiceRegistroNaPagina < pagina.Linhas.Count; indiceRegistroNaPagina++)
            {
                int indiceRegistro = pagina.Linhas[indiceRegistroNaPagina];
                var quebrasCelulas = pagina.Colunas.Select((coluna, indiceColuna) =>
                    QuebrarTexto(valores[indiceRegistro][coluna], Capacidade(pagina.Larguras[indiceColuna], FonteDados))).ToList();
                float alturaLinha = Math.Max(24, quebrasCelulas.Max(linhas => linhas.Count) * AlturaLinhaTexto + 8);
                float yLinha = y - alturaLinha;
                byte tom = indiceRegistroNaPagina % 2 == 0 ? (byte)255 : (byte)241;
                x = Margem;

                for (int i = 0; i < pagina.Colunas.Count; i++)
                {
                    Caixa(pdf, x, yLinha, pagina.Larguras[i], alturaLinha, tom, (byte)Math.Min(255, tom + 2), 255);
                    Bordas(pdf, x, yLinha, pagina.Larguras[i], alturaLinha);
                    DesenharLinhas(pdf, quebrasCelulas[i], x + 5, y - 13, FonteDados, AlturaLinhaTexto);
                    x += pagina.Larguras[i];
                }
                y = yLinha;
            }
        }

        Texto(pdf, larguraUtil + Margem - 65, 16, 8, $"Pagina {indicePagina + 1} de {totalPaginas}");
        return pdf.ToString();
    }

    private static float[] CalcularLarguras(DataTable tabela, IReadOnlyList<int> colunas, float larguraTotal)
    {
        if (colunas.Count == 0)
            return Array.Empty<float>();

        var pesos = colunas.Select(coluna => Math.Clamp(tabela.Columns[coluna].ColumnName.Length, 8, 24)).ToArray();
        float totalPesos = pesos.Sum();
        return pesos.Select(peso => larguraTotal * peso / totalPesos).ToArray();
    }

    private static int Capacidade(float largura, float tamanhoFonte) => Math.Max(4, (int)((largura - 10) / (tamanhoFonte * 0.52f)));

    private static List<string> QuebrarTexto(string valor, int capacidade)
    {
        var linhas = new List<string>();
        foreach (var linhaOriginal in valor.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var restante = linhaOriginal;
            if (restante.Length == 0)
            {
                linhas.Add(string.Empty);
                continue;
            }
            while (restante.Length > capacidade)
            {
                int corte = restante.LastIndexOf(' ', capacidade);
                if (corte <= 0) corte = capacidade;
                linhas.Add(restante[..corte]);
                restante = restante[corte..].TrimStart();
            }
            linhas.Add(restante);
        }
        return linhas;
    }

    private static void DesenharLinhas(StringBuilder pdf, IReadOnlyList<string> linhas, float x, float y, float fonte, float passo, bool branco = false)
    {
        foreach (var linha in linhas)
        {
            Texto(pdf, x, y, fonte, linha, branco);
            y -= passo;
        }
    }

    private static void Texto(StringBuilder pdf, float x, float y, float fonte, string texto, bool branco = false)
    {
        pdf.AppendLine(branco ? "1 1 1 rg" : "0.12 0.16 0.23 rg");
        pdf.AppendLine("BT");
        pdf.AppendLine($"/F1 {Numero(fonte)} Tf");
        pdf.AppendLine($"{Numero(x)} {Numero(y)} Td");
        pdf.AppendLine($"({Sanitizar(texto)}) Tj");
        pdf.AppendLine("ET");
    }

    private static void Caixa(StringBuilder pdf, float x, float y, float largura, float altura, byte r, byte g, byte b)
    {
        pdf.AppendLine($"{Numero(r / 255f)} {Numero(g / 255f)} {Numero(b / 255f)} rg");
        pdf.AppendLine($"{Numero(x)} {Numero(y)} {Numero(largura)} {Numero(altura)} re f");
    }

    private static void Bordas(StringBuilder pdf, float x, float y, float largura, float altura)
    {
        pdf.AppendLine("0.72 0.76 0.81 RG");
        pdf.AppendLine("0.5 w");
        pdf.AppendLine($"{Numero(x)} {Numero(y)} {Numero(largura)} {Numero(altura)} re S");
    }

    private static string Numero(float valor) => valor.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Sanitizar(string texto)
    {
        var resultado = new StringBuilder(texto.Length);
        foreach (char caractere in texto.Normalize(NormalizationForm.FormD))
        {
            if (char.GetUnicodeCategory(caractere) == UnicodeCategory.NonSpacingMark)
                continue;
            switch (caractere)
            {
                case '\\': resultado.Append("\\\\"); break;
                case '(': resultado.Append("\\("); break;
                case ')': resultado.Append("\\)"); break;
                default: resultado.Append(caractere is >= ' ' and <= '~' ? caractere : '?'); break;
            }
        }
        return resultado.ToString();
    }

    private sealed record PdfPagina(IReadOnlyList<int> Colunas, IReadOnlyList<int> Linhas, float[] Larguras,
        IReadOnlyList<List<string>> QuebrasCabecalho, float AlturaCabecalho);
}
