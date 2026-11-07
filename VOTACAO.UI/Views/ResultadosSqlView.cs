using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using VOTACAO.Infrastructure.Database;
using VOTACAO.UI.Helpers;
using VOTACAO.UI.Navigation;

namespace VOTACAO.UI.Views;

public class ResultadosSqlView : UserControl, IRefreshableView
{
    private readonly MdbConnectionFactory _connectionFactory;
    private TextBox txtTitulo = null!;
    private RichTextBox txtSql = null!;
    private Button btnExecutar = null!;
    private Button btnSalvarConsulta = null!;
    private Button btnSalvarPdf = null!;
    private Button btnLimparBanco = null!;
    private DataGridView dgvResultado = null!;
    private Label lblMensagem = null!;
    private Label lblResultado = null!;

    private const string DefaultTitulo = "Resultados do Banco";
    private const string DefaultSql = "SELECT * FROM Participante;";

    public ResultadosSqlView(MdbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        InitializeComponent();
    }

    public void OnNavigatedTo()
    {
        if (string.IsNullOrWhiteSpace(txtTitulo.Text))
        {
            txtTitulo.Text = DefaultTitulo;
        }

        if (string.IsNullOrWhiteSpace(txtSql.Text))
        {
            txtSql.Text = DefaultSql;
        }
    }

    private static bool EhConsulta(string sql)
    {
        var texto = sql.TrimStart();
        return texto.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            || texto.StartsWith("WITH", StringComparison.OrdinalIgnoreCase);
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(248, 250, 252);

        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 110,
            BackColor = Color.FromArgb(30, 41, 59),
            Padding = new Padding(20, 18, 20, 18)
        };

        var lblTituloTela = new Label
        {
            Text = "RESULTADOS SQL",
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        txtTitulo = new TextBox
        {
            Font = new Font("Segoe UI", 11F),
            Width = 600,
            Height = 32,
            Location = new Point(20, 58),
            Text = DefaultTitulo
        };

        pnlTop.Controls.Add(lblTituloTela);
        pnlTop.Controls.Add(txtTitulo);

        var pnlConsulta = new Panel
        {
            Dock = DockStyle.Top,
            Height = 270,
            BackColor = Color.White,
            Padding = new Padding(20, 15, 20, 15)
        };

        var lblSql = new Label
        {
            Text = "SQL:",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            AutoSize = true,
            Location = new Point(20, 15)
        };

        txtSql = new RichTextBox
        {
            MaxLength = int.MaxValue,
            AcceptsTab = true,
            WordWrap = false,
            DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.Both,
            Font = new Font("Consolas", 10F),
            Location = new Point(20, 40),
            Size = new Size(980, 120),
            Text = DefaultSql
        };

        btnExecutar = new Button
        {
            Text = "Executar SQL",
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(150, 34),
            Location = new Point(20, 175),
            Cursor = Cursors.Hand
        };
        btnExecutar.Click += async (s, e) => await ExecutarSqlAsync();

        btnSalvarConsulta = new Button
        {
            Text = "Salvar",
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 34),
            Location = new Point(190, 175),
            Cursor = Cursors.Hand
        };
        btnSalvarConsulta.Click += async (s, e) => await SalvarConsultaAsync();

        btnSalvarPdf = new Button
        {
            Text = "Salvar PDF",
            BackColor = Color.FromArgb(220, 38, 127),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(150, 34),
            Location = new Point(330, 175),
            Cursor = Cursors.Hand
        };
        btnSalvarPdf.Click += async (s, e) => await SalvarPdfAsync();

        btnLimparBanco = new Button
        {
            Text = "Limpar banco",
            BackColor = Color.FromArgb(185, 28, 28),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(150, 34),
            Location = new Point(500, 175),
            Cursor = Cursors.Hand
        };
        btnLimparBanco.FlatAppearance.BorderColor = Color.FromArgb(153, 27, 27);
        btnLimparBanco.FlatAppearance.BorderSize = 1;
        btnLimparBanco.Click += async (s, e) => await LimparBancoAsync();

        lblMensagem = new Label
        {
            Text = "",
            ForeColor = Color.FromArgb(55, 65, 81),
            Font = new Font("Segoe UI", 9F, FontStyle.Italic),
            AutoSize = false,
            AutoEllipsis = true,
            Size = new Size(950, 24),
            Location = new Point(20, 218)
        };

        pnlConsulta.Controls.Add(lblSql);
        pnlConsulta.Controls.Add(txtSql);
        pnlConsulta.Controls.Add(btnExecutar);
        pnlConsulta.Controls.Add(btnSalvarConsulta);
        pnlConsulta.Controls.Add(btnSalvarPdf);
        pnlConsulta.Controls.Add(btnLimparBanco);
        pnlConsulta.Controls.Add(lblMensagem);

        dgvResultado = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoGenerateColumns = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle =
            {
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            }
        };

        var pnlGrid = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 10, 20, 20),
            BackColor = Color.FromArgb(248, 250, 252)
        };

        lblResultado = new Label
        {
            Text = "Resultado",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            AutoSize = true,
            Location = new Point(0, 0)
        };

        var panelResultadoTitulo = new Panel
        {
            Dock = DockStyle.Top,
            Height = 30,
            BackColor = Color.FromArgb(248, 250, 252)
        };
        panelResultadoTitulo.Controls.Add(lblResultado);

        pnlGrid.Controls.Add(dgvResultado);
        pnlGrid.Controls.Add(panelResultadoTitulo);

        this.Controls.Add(pnlGrid);
        this.Controls.Add(pnlConsulta);
        this.Controls.Add(pnlTop);
    }

    private async Task ExecutarSqlAsync()
    {
        var sql = txtSql.Text.Trim();
        if (string.IsNullOrWhiteSpace(sql))
        {
            MessageBox.Show("Informe um SQL para executar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnExecutar.Enabled = false;
        btnSalvarConsulta.Enabled = false;
        btnSalvarPdf.Enabled = false;
        lblMensagem.Text = "Executando SQL...";
        dgvResultado.DataSource = null;

        try
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new OleDbCommand(sql, conn);
            if (EhConsulta(sql))
            {
                using var reader = await cmd.ExecuteReaderAsync();
                var table = new DataTable();
                table.Load(reader);

                dgvResultado.DataSource = table;
                lblMensagem.Text = $"Total de registros encontrados: {table.Rows.Count:N0}.";
            }
            else
            {
                var linhasAfetadas = await cmd.ExecuteNonQueryAsync();
                dgvResultado.DataSource = null;
                lblMensagem.Text = $"Comando executado com sucesso. {linhasAfetadas} linha(s) afetada(s).";
            }

            var titulo = txtTitulo.Text.Trim();
            if (!string.IsNullOrWhiteSpace(titulo))
            {
                lblResultado.Text = titulo;
            }
        }
        catch (Exception ex)
        {
            dgvResultado.DataSource = null;
            lblMensagem.Text = "Erro ao executar SQL.";
            MessageBox.Show(ex.Message, "Erro na consulta", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnExecutar.Enabled = true;
            btnSalvarConsulta.Enabled = !string.IsNullOrWhiteSpace(txtSql.Text.Trim());
            btnSalvarPdf.Enabled = dgvResultado.DataSource is DataTable;
        }
    }

    private async Task LimparBancoAsync()
    {
        var confirmacao = MessageBox.Show(
            "Esta operação apagará todos os registros das tabelas Jurados, Participantes e Voto. Deseja continuar?",
            "Confirmar limpeza do banco",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (confirmacao != DialogResult.Yes)
            return;

        btnLimparBanco.Enabled = false;
        lblMensagem.Text = "Limpando as tabelas...";

        try
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();
            using var transacao = conn.BeginTransaction();

            int registrosRemovidos = 0;
            foreach (var tabela in new[] { "Voto", "Jurados", "Participantes" })
            {
                using var cmd = new OleDbCommand($"DELETE FROM [{tabela}]", conn, transacao);
                registrosRemovidos += await cmd.ExecuteNonQueryAsync();
            }

            transacao.Commit();
            dgvResultado.DataSource = null;
            lblResultado.Text = "Banco limpo";
            lblMensagem.Text = $"Limpeza concluída. {registrosRemovidos:N0} registro(s) removido(s).";
            MessageBox.Show("As tabelas Jurados, Participantes e Voto foram limpas.", "Limpeza concluída", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            lblMensagem.Text = "Não foi possível limpar o banco.";
            MessageBox.Show($"Erro ao limpar as tabelas: {ex.Message}", "Erro na limpeza", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnLimparBanco.Enabled = true;
        }
    }

    private async Task SalvarConsultaAsync()
    {
        var titulo = txtTitulo.Text.Trim();
        var sql = txtSql.Text.Trim();

        if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(sql))
        {
            MessageBox.Show("Informe o título e o SQL antes de salvar a consulta.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var textoParaContarLinhas = sql.Replace("\r\n", "\n").Replace('\r', '\n');
        var linhasSql = textoParaContarLinhas.Count(caractere => caractere == '\n') +
                        (textoParaContarLinhas.EndsWith('\n') ? 0 : 1);
        if (linhasSql > 40000)
        {
            MessageBox.Show("O SQL pode ter no máximo 40.000 linhas.", "Limite de linhas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();
            ConsultaSqlBanco.GarantirTabelaConsultas(conn);
            ConsultaSqlBanco.GarantirTabelaPartes(conn);

            using var transacao = conn.BeginTransaction();
            bool armazenarEmPartes = sql.Length > ConsultaSqlBanco.TamanhoParte;
            string valorSql = armazenarEmPartes ? ConsultaSqlBanco.SqlEmPartes : sql;

            using (var cmd = new OleDbCommand("INSERT INTO Consultas (Titulo, [SQL]) VALUES (?, ?)", conn, transacao))
            {
                cmd.Parameters.Add("?", OleDbType.VarWChar, 255).Value = titulo;
                cmd.Parameters.Add("?", OleDbType.LongVarWChar).Value = valorSql;
                await cmd.ExecuteNonQueryAsync();
            }

            int idConsulta;
            using (var cmdId = new OleDbCommand("SELECT @@IDENTITY", conn, transacao))
                idConsulta = Convert.ToInt32(await cmdId.ExecuteScalarAsync());

            if (armazenarEmPartes)
            {
                for (int inicio = 0; inicio < sql.Length; inicio += ConsultaSqlBanco.TamanhoParte)
                {
                    string parte = sql.Substring(inicio, Math.Min(ConsultaSqlBanco.TamanhoParte, sql.Length - inicio));
                    using var cmdParte = new OleDbCommand(
                        "INSERT INTO ConsultaSqlPartes (IdConsulta, NumeroParte, Conteudo) VALUES (?, ?, ?)", conn, transacao);
                    cmdParte.Parameters.Add("?", OleDbType.Integer).Value = idConsulta;
                    cmdParte.Parameters.Add("?", OleDbType.Integer).Value = inicio / ConsultaSqlBanco.TamanhoParte;
                    cmdParte.Parameters.Add("?", OleDbType.LongVarWChar).Value = parte;
                    await cmdParte.ExecuteNonQueryAsync();
                }
            }

            transacao.Commit();
            lblMensagem.Text = $"Consulta salva ({linhasSql:N0} linhas).";
            MessageBox.Show("Consulta salva com sucesso no banco de dados.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            lblMensagem.Text = "Erro ao salvar a consulta.";
            MessageBox.Show($"Erro ao salvar a consulta: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void GarantirTabelaConsultasLocal()
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();
        ConsultaSqlBanco.GarantirTabelaConsultas(conn);
        ConsultaSqlBanco.GarantirTabelaPartes(conn);
    }

    private async Task SalvarPdfAsync()
    {
        if (dgvResultado.DataSource is not DataTable table)
        {
            MessageBox.Show("Não há resultado para salvar em PDF.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var titulo = txtTitulo.Text.Trim();
        if (string.IsNullOrWhiteSpace(titulo))
        {
            titulo = DefaultTitulo;
            txtTitulo.Text = titulo;
        }

        using var saveDialog = new SaveFileDialog
        {
            Filter = "Arquivo PDF (*.pdf)|*.pdf",
            DefaultExt = "pdf",
            FileName = $"{RemoverCaracteresInvalidos(titulo)}.pdf"
        };

        if (saveDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        btnExecutar.Enabled = false;
        btnSalvarPdf.Enabled = false;

        try
        {
            var pdfBytes = GerarPdf(table, titulo);
            await File.WriteAllBytesAsync(saveDialog.FileName, pdfBytes);
            MessageBox.Show($"Arquivo salvo com sucesso em:\n{saveDialog.FileName}", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar o relatório PDF: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnExecutar.Enabled = true;
            btnSalvarPdf.Enabled = true;
        }
    }

    private static string RemoverCaracteresInvalidos(string valor)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(valor.Length);

        foreach (var caractere in valor)
        {
            if (invalidos.Contains(caractere) || char.IsControl(caractere))
            {
                builder.Append('_');
            }
            else
            {
                builder.Append(caractere);
            }
        }

        var resultado = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(resultado) ? "resultado" : resultado;
    }

    private static byte[] GerarPdf(DataTable table, string titulo)
    {
        return PdfTabelaExporter.Gerar(table, titulo);
    }

    private static byte[] GerarPdfTextoLegado(DataTable table, string titulo)
    {
        var linhas = new List<string>
        {
            $"TITULO: {titulo}",
            string.Empty
        };

        if (table.Columns.Count > 0)
        {
            linhas.Add(string.Join(" | ", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));
        }

        foreach (DataRow row in table.Rows)
        {
            var valores = row.ItemArray.Select(item => item?.ToString() ?? string.Empty).ToArray();
            linhas.Add(string.Join(" | ", valores));
        }

        var content = new StringBuilder();
        var y = 790;

        foreach (var linha in linhas)
        {
            if (y < 40)
            {
                break;
            }

            var texto = SanitizarTextoPdf(linha);
            if (string.IsNullOrEmpty(texto))
            {
                y -= 14;
                continue;
            }

            content.AppendLine("BT");
            content.AppendLine("/F1 10 Tf");
            content.AppendLine($"50 {y} Td");
            content.AppendLine($"({texto}) Tj");
            content.AppendLine("ET");
            y -= 18;
        }

        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content.ToString())} >>\nstream\n{content}endstream"
        };

        var pdf = new StringBuilder();
        var offsets = new List<long> { 0 };
        pdf.Append("%PDF-1.4\n");

        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(pdf.Length);
            pdf.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xrefPos = pdf.Length;
        pdf.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
        {
            pdf.Append($"{offsets[i]:D10} 00000 n \n");
        }

        pdf.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefPos}\n%%EOF");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static string SanitizarTextoPdf(string texto)
    {
        var builder = new StringBuilder(texto.Length);

        foreach (var caractere in texto)
        {
            if (caractere == '\\')
            {
                builder.Append("\\\\");
            }
            else if (caractere == '(')
            {
                builder.Append("\\(");
            }
            else if (caractere == ')')
            {
                builder.Append("\\)");
            }
            else if (caractere >= ' ' && caractere <= '~')
            {
                builder.Append(caractere);
            }
            else
            {
                builder.Append('?');
            }
        }

        return builder.ToString();
    }
}
