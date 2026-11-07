using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using VOTACAO.Infrastructure.Database;
using VOTACAO.UI.Helpers;
using VOTACAO.UI.Navigation;

namespace VOTACAO.UI.Views;

public class DiculgacaoView : UserControl, IRefreshableView
{
    private readonly MdbConnectionFactory _connectionFactory;
    private ComboBox cmbConsultas = null!;
    private Label lblConsultaSelecionada = null!;
    private Label lblTituloConsulta = null!;
    private DataGridView dgvResultado = null!;
    private Button btnExecutar = null!;
    private Button btnSalvarPdf = null!;
    private Label lblMensagem = null!;
    private string _sqlSelecionado = string.Empty;

    public DiculgacaoView(MdbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        InitializeComponent();
    }

    public void OnNavigatedTo()
    {
        try
        {
            GarantirTabelaConsultas();
            CarregarConsultas();
        }
        catch (Exception ex)
        {
            cmbConsultas.DataSource = null;
            _sqlSelecionado = string.Empty;
            lblConsultaSelecionada.Text = "Consulta salva:";
            dgvResultado.DataSource = null;
            lblTituloConsulta.Text = "Resultados Votação";
            lblMensagem.Text = "Não foi possível carregar as consultas salvas.";
            btnSalvarPdf.Enabled = false;
            MessageBox.Show($"Não foi possível abrir os resultados: {ex.Message}", "Erro ao carregar resultados", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void GarantirTabelaConsultas()
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();
        ConsultaSqlBanco.GarantirTabelaConsultas(conn);
        ConsultaSqlBanco.GarantirTabelaPartes(conn);
    }

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(248, 250, 252);

        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 90,
            BackColor = Color.FromArgb(30, 41, 59),
            Padding = new Padding(20, 20, 20, 10)
        };

        var lblTitulo = new Label
        {
            Text = "Resultados Votação",
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        pnlTop.Controls.Add(lblTitulo);

        var pnlCampos = new Panel
        {
            Dock = DockStyle.Top,
            Height = 140,
            Padding = new Padding(20, 15, 20, 15),
            BackColor = Color.White
        };

        var lblConsulta = new Label
        {
            Text = "Consulta salva:",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            AutoSize = false,
            AutoEllipsis = true,
            Size = new Size(140, 28),
            Location = new Point(20, 18)
        };
        lblConsultaSelecionada = lblConsulta;

        cmbConsultas = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10F),
            Width = 540,
            Location = new Point(170, 15),
            DisplayMember = nameof(ConsultaSalva.Nome),
            ValueMember = nameof(ConsultaSalva.Id)
        };
        cmbConsultas.SelectedIndexChanged += (s, e) => AtualizarConsultaSelecionada();

        btnExecutar = new Button
        {
            Text = "Consultar",
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(160, 35),
            Location = new Point(745, 12),
            Cursor = Cursors.Hand
        };
        btnExecutar.Click += (s, e) => CarregarConsultaSelecionada();

        btnSalvarPdf = new Button
        {
            Text = "Salvar PDF",
            BackColor = Color.FromArgb(220, 38, 127),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(140, 35),
            Location = new Point(925, 12),
            Cursor = Cursors.Hand,
            Enabled = false
        };
        btnSalvarPdf.Click += (s, e) => SalvarPdfAsync();

        lblTituloConsulta = new Label
        {
            Text = "Título da consulta",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            AutoSize = false,
            AutoEllipsis = true,
            Size = new Size(950, 24),
            Location = new Point(20, 62)
        };

        lblMensagem = new Label
        {
            Text = string.Empty,
            ForeColor = Color.FromArgb(55, 65, 81),
            Font = new Font("Segoe UI", 9F, FontStyle.Italic),
            AutoSize = false,
            AutoEllipsis = true,
            Size = new Size(950, 22),
            Location = new Point(20, 91)
        };

        pnlCampos.Controls.Add(lblConsulta);
        pnlCampos.Controls.Add(cmbConsultas);
        pnlCampos.Controls.Add(btnExecutar);
        pnlCampos.Controls.Add(btnSalvarPdf);
        pnlCampos.Controls.Add(lblTituloConsulta);
        pnlCampos.Controls.Add(lblMensagem);

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

        pnlGrid.Controls.Add(dgvResultado);

        Controls.Add(pnlGrid);
        Controls.Add(pnlCampos);
        Controls.Add(pnlTop);
    }

    private void CarregarConsultas()
    {
        try
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();

            var sql = "SELECT [Id], [Titulo], [SQL] FROM [Consultas] ORDER BY [Titulo]";
            var registros = new List<(int Id, string Nome, string Sql)>();
            using (var cmd = new OleDbCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = Convert.ToInt32(reader.GetValue(0));
                    var nome = Convert.ToString(reader.GetValue(1));
                    var consultaSql = reader.IsDBNull(2) ? string.Empty : Convert.ToString(reader.GetValue(2)) ?? string.Empty;
                    registros.Add((id, string.IsNullOrWhiteSpace(nome) ? $"Consulta {id}" : nome.Trim(), consultaSql));
                }
            }

            var consultas = registros.Select(registro => new ConsultaSalva(
                registro.Id,
                registro.Nome,
                ConsultaSqlBanco.CarregarSql(conn, registro.Id, registro.Sql))).ToList();

            cmbConsultas.DataSource = null;
            cmbConsultas.DataSource = consultas;
            if (consultas.Count > 0)
            {
                cmbConsultas.SelectedIndex = 0;
                AtualizarConsultaSelecionada();
            }
            else
            {
                _sqlSelecionado = string.Empty;
                lblConsultaSelecionada.Text = "Consulta salva:";
                lblTituloConsulta.Text = "Resultados Votação";
                lblMensagem.Text = "Nenhuma consulta salva. Salve uma consulta na tela Consultas para exibir os resultados aqui.";
                dgvResultado.DataSource = null;
                btnSalvarPdf.Enabled = false;
            }
        }
        catch (Exception ex)
        {
            cmbConsultas.DataSource = null;
            _sqlSelecionado = string.Empty;
            lblConsultaSelecionada.Text = "Consulta salva:";
            dgvResultado.DataSource = null;
            btnSalvarPdf.Enabled = false;
            lblMensagem.Text = $"Erro ao carregar consultas: {ex.Message}";
            MessageBox.Show($"Não foi possível carregar a lista de consultas: {ex.Message}", "Erro ao carregar consultas", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AtualizarConsultaSelecionada()
    {
        if (cmbConsultas.SelectedItem is not ConsultaSalva consulta)
        {
            _sqlSelecionado = string.Empty;
            lblConsultaSelecionada.Text = "Consulta salva:";
            return;
        }

        // O SQL fica no estado privado da tela; o ComboBox exibe Nome e fornece Id.
        _sqlSelecionado = consulta.Sql;
        lblConsultaSelecionada.Text = consulta.Nome;
        lblTituloConsulta.Text = consulta.Nome;
        lblMensagem.Text = "Clique em Consultar para executar o SQL.";
        dgvResultado.DataSource = null;
        btnSalvarPdf.Enabled = false;
    }

    private void CarregarConsultaSelecionada()
    {
        if (cmbConsultas.SelectedItem is not ConsultaSalva consulta || cmbConsultas.SelectedValue == null)
        {
            lblMensagem.Text = "Selecione uma consulta salva.";
            return;
        }

        lblTituloConsulta.Text = consulta.Nome;
        if (string.IsNullOrWhiteSpace(_sqlSelecionado))
        {
            dgvResultado.DataSource = null;
            btnSalvarPdf.Enabled = false;
            lblMensagem.Text = "A consulta selecionada não contém um comando SQL.";
            return;
        }
        lblMensagem.Text = "Consultando resultado...";
        btnSalvarPdf.Enabled = false;
        dgvResultado.DataSource = null;

        try
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();

            using var cmd = new OleDbCommand(_sqlSelecionado, conn);
            using var reader = cmd.ExecuteReader();

            var table = new DataTable();
            table.Load(reader);
            dgvResultado.DataSource = table;

            lblMensagem.Text = $"Resultado da consulta: {table.Rows.Count} registro(s)";
            btnSalvarPdf.Enabled = true;
        }
        catch (Exception ex)
        {
            lblMensagem.Text = "Erro ao executar a consulta salva.";
            MessageBox.Show(ex.Message, "Erro na consulta", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void SalvarPdfAsync()
    {
        if (dgvResultado.DataSource is not DataTable table)
        {
            MessageBox.Show("Não há dados para exportar em PDF.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var titulo = lblTituloConsulta.Text.Trim();
        if (string.IsNullOrWhiteSpace(titulo))
        {
            titulo = "Consulta";
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "Arquivo PDF (*.pdf)|*.pdf",
            DefaultExt = "pdf",
            FileName = RemoverCaracteresInvalidos(titulo) + ".pdf"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var pdf = GerarPdf(table, titulo);
            await File.WriteAllBytesAsync(dialog.FileName, pdf);
            MessageBox.Show($"Arquivo salvo com sucesso:\n{dialog.FileName}", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao gerar o PDF: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string RemoverCaracteresInvalidos(string valor)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(valor.Length);

        foreach (var caractere in valor)
        {
            builder.Append(invalidos.Contains(caractere) || char.IsControl(caractere) ? '_' : caractere);
        }

        var resultado = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(resultado) ? "consulta" : resultado;
    }

    private static byte[] GerarPdf(DataTable table, string titulo)
    {
        return PdfTabelaExporter.Gerar(table, titulo);
    }

    private static byte[] GerarPdfTextoLegado(DataTable table, string titulo)
    {
        const int larguraLinha = 90;
        var cabecalho = table.Columns.Count > 0
            ? string.Join(" | ", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName))
            : "Sem colunas";
        var linhas = new List<string>();
        foreach (DataRow row in table.Rows)
        {
            var valores = row.ItemArray.Select(valor => valor == DBNull.Value ? string.Empty : Convert.ToString(valor) ?? string.Empty);
            linhas.AddRange(QuebrarLinha(string.Join(" | ", valores), larguraLinha));
        }
        if (linhas.Count == 0)
            linhas.Add("Nenhum registro retornado.");

        var paginas = new List<List<string>>();
        const int linhasPorPagina = 42;
        var indiceLinha = 0;
        do
        {
            var pagina = new List<string>();
            pagina.AddRange(QuebrarLinha(titulo, larguraLinha));
            pagina.AddRange(QuebrarLinha($"Colunas: {cabecalho}", larguraLinha));
            var limite = linhasPorPagina - pagina.Count;
            var restante = linhas.Count - indiceLinha;
            var quantidade = Math.Min(limite, restante);
            if (quantidade > 0)
                pagina.AddRange(linhas.Skip(indiceLinha).Take(quantidade));
            indiceLinha += quantidade;
            paginas.Add(pagina);
        } while (indiceLinha < linhas.Count);

        var objetos = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            string.Empty,
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"
        };
        var referenciasPaginas = new List<string>();

        for (var i = 0; i < paginas.Count; i++)
        {
            var numeroObjetoPagina = 4 + (i * 2);
            var numeroObjetoConteudo = numeroObjetoPagina + 1;
            referenciasPaginas.Add($"{numeroObjetoPagina} 0 R");

            var conteudo = new StringBuilder();
            var y = 790;
            foreach (var linha in paginas[i])
            {
                var texto = SanitizarTextoPdf(linha);
                conteudo.AppendLine("BT");
                conteudo.AppendLine(i == 0 && y == 790 ? "/F1 14 Tf" : "/F1 9 Tf");
                conteudo.AppendLine($"50 {y} Td");
                conteudo.AppendLine($"({texto}) Tj");
                conteudo.AppendLine("ET");
                y -= 17;
            }

            conteudo.AppendLine("BT");
            conteudo.AppendLine("/F1 8 Tf");
            conteudo.AppendLine("500 25 Td");
            conteudo.AppendLine($"(Pagina {i + 1} de {paginas.Count}) Tj");
            conteudo.AppendLine("ET");

            objetos.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {numeroObjetoConteudo} 0 R >>");
            objetos.Add($"<< /Length {Encoding.ASCII.GetByteCount(conteudo.ToString())} >>\nstream\n{conteudo}endstream");
        }

        objetos[1] = $"<< /Type /Pages /Kids [{string.Join(" ", referenciasPaginas)}] /Count {paginas.Count} >>";

        var pdf = new StringBuilder();
        var offsets = new List<long> { 0 };
        pdf.Append("%PDF-1.4\n");

        for (var i = 0; i < objetos.Count; i++)
        {
            offsets.Add(pdf.Length);
            pdf.Append($"{i + 1} 0 obj\n{objetos[i]}\nendobj\n");
        }

        var xrefPos = pdf.Length;
        pdf.Append($"xref\n0 {objetos.Count + 1}\n0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
        {
            pdf.Append($"{offsets[i]:D10} 00000 n \n");
        }

        pdf.Append($"trailer\n<< /Size {objetos.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefPos}\n%%EOF");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static IEnumerable<string> QuebrarLinha(string texto, int larguraMaxima)
    {
        var restante = texto.Trim();
        if (restante.Length == 0)
            return new[] { string.Empty };

        var linhas = new List<string>();
        while (restante.Length > larguraMaxima)
        {
            var corte = restante.LastIndexOf(' ', larguraMaxima);
            if (corte <= 0)
                corte = larguraMaxima;
            linhas.Add(restante[..corte]);
            restante = restante[corte..].TrimStart();
        }
        linhas.Add(restante);
        return linhas;
    }

    private static string SanitizarTextoPdf(string texto)
    {
        var builder = new StringBuilder(texto.Length);

        var normalizado = texto.Normalize(NormalizationForm.FormD);
        foreach (var caractere in normalizado)
        {
            if (char.GetUnicodeCategory(caractere) == System.Globalization.UnicodeCategory.NonSpacingMark)
                continue;

            switch (caractere)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '(':
                    builder.Append("\\(");
                    break;
                case ')':
                    builder.Append("\\)");
                    break;
                default:
                    if (caractere >= ' ' && caractere <= '~')
                    {
                        builder.Append(caractere);
                    }
                    else
                    {
                        builder.Append('?');
                    }
                    break;
            }
        }

        return builder.ToString();
    }

    private sealed class ConsultaSalva
    {
        public int Id { get; }
        public string Nome { get; }
        public string Sql { get; }

        public ConsultaSalva(int id, string nome, string sql)
        {
            Id = id;
            Nome = nome;
            Sql = sql;
        }
    }
}
