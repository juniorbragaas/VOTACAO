using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using VOTACAO.Infrastructure.Database;
using VOTACAO.UI.Navigation;

namespace VOTACAO.UI.Views;

public class DiculgacaoView : UserControl, IRefreshableView
{
    private readonly MdbConnectionFactory _connectionFactory;
    private ComboBox cmbConsultas = null!;
    private Label lblTituloConsulta = null!;
    private DataGridView dgvResultado = null!;
    private Button btnExecutar = null!;
    private Button btnSalvarPdf = null!;
    private Label lblMensagem = null!;

    public DiculgacaoView(MdbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        InitializeComponent();
    }

    public void OnNavigatedTo()
    {
        GarantirTabelaConsultas();
        CarregarConsultas();
    }

    private void GarantirTabelaConsultas()
    {
        try
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            using var cmd = new OleDbCommand("SELECT COUNT(*) FROM Consultas", conn);
            cmd.ExecuteScalar();
        }
        catch
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            using var cmd = new OleDbCommand("CREATE TABLE Consultas (Id AUTOINCREMENT PRIMARY KEY, Titulo TEXT(255) NOT NULL, [SQL] MEMO NOT NULL)", conn);
            cmd.ExecuteNonQuery();
        }
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
            Text = "DICULGAÇÃO",
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        pnlTop.Controls.Add(lblTitulo);

        var pnlCampos = new Panel
        {
            Dock = DockStyle.Top,
            Height = 120,
            Padding = new Padding(20, 15, 20, 15),
            BackColor = Color.White
        };

        var lblConsulta = new Label
        {
            Text = "Consulta salva:",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            AutoSize = true,
            Location = new Point(20, 20)
        };

        cmbConsultas = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10F),
            Width = 540,
            Location = new Point(170, 15)
        };
        cmbConsultas.SelectedIndexChanged += (s, e) => CarregarConsultaSelecionada();

        btnExecutar = new Button
        {
            Text = "Carregar resultado",
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
            AutoSize = true,
            Location = new Point(20, 65)
        };

        lblMensagem = new Label
        {
            Text = string.Empty,
            ForeColor = Color.FromArgb(55, 65, 81),
            Font = new Font("Segoe UI", 9F, FontStyle.Italic),
            AutoSize = true,
            Location = new Point(170, 65)
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

            var sql = "SELECT Id, Titulo, [SQL] AS Sql FROM Consultas ORDER BY Titulo";
            using var cmd = new OleDbCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            var itens = new List<(int Id, string Titulo, string Sql)>();
            while (reader.Read())
            {
                itens.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
            }

            cmbConsultas.Items.Clear();
            foreach (var item in itens)
            {
                cmbConsultas.Items.Add(new ComboBoxItem(item.Id, item.Titulo, item.Sql));
            }

            if (cmbConsultas.Items.Count > 0)
            {
                cmbConsultas.SelectedIndex = 0;
            }
            else
            {
                lblTituloConsulta.Text = "Título da consulta";
                lblMensagem.Text = "Nenhuma consulta salva encontrada.";
                dgvResultado.DataSource = null;
                btnSalvarPdf.Enabled = false;
            }
        }
        catch (Exception ex)
        {
            lblMensagem.Text = "Erro ao carregar consultas.";
            MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CarregarConsultaSelecionada()
    {
        if (cmbConsultas.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        lblTituloConsulta.Text = item.Titulo;
        lblMensagem.Text = "Consultando resultado...";
        btnSalvarPdf.Enabled = false;
        dgvResultado.DataSource = null;

        try
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();

            using var cmd = new OleDbCommand(item.Sql, conn);
            using var reader = cmd.ExecuteReader();

            var table = new DataTable();
            table.Load(reader);
            dgvResultado.DataSource = table;

            lblMensagem.Text = $"Consulta carregada: {table.Rows.Count} registro(s)";
            btnSalvarPdf.Enabled = table.Rows.Count > 0;
        }
        catch (Exception ex)
        {
            lblMensagem.Text = "Erro ao executar a consulta salva.";
            MessageBox.Show(ex.Message, "Erro na consulta", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void SalvarPdfAsync()
    {
        if (dgvResultado.DataSource is not DataTable table || table.Rows.Count == 0)
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
        var linhas = new List<string>
        {
            $"TITULO: {titulo}",
            string.Empty,
            table.Columns.Count > 0 ? string.Join(" | ", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName)) : "Sem colunas"
        };

        foreach (DataRow row in table.Rows)
        {
            linhas.Add(string.Join(" | ", row.ItemArray.Select(item => item?.ToString() ?? string.Empty)));
        }

        var builder = new StringBuilder();
        var y = 790;

        foreach (var linha in linhas)
        {
            if (y < 40)
            {
                break;
            }

            var texto = SanitizarTextoPdf(linha);
            if (!string.IsNullOrEmpty(texto))
            {
                builder.AppendLine("BT");
                builder.AppendLine("/F1 10 Tf");
                builder.AppendLine($"50 {y} Td");
                builder.AppendLine($"({texto}) Tj");
                builder.AppendLine("ET");
            }

            y -= 18;
        }

        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(builder.ToString())} >>\nstream\n{builder}endstream"
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

    private sealed class ComboBoxItem
    {
        public int Id { get; }
        public string Titulo { get; }
        public string Sql { get; }

        public ComboBoxItem(int id, string titulo, string sql)
        {
            Id = id;
            Titulo = titulo;
            Sql = sql;
        }

        public override string ToString() => Titulo;
    }
}
