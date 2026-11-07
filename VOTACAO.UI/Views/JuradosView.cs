using System.Drawing;
using System.Windows.Forms;
using VOTACAO.BO.Services;
using VOTACAO.Domain.Entities;
using VOTACAO.UI.Navigation;

namespace VOTACAO.UI.Views;

public class JuradosView : UserControl, IRefreshableView
{
    private readonly IJuradoService _juradoService;

    private DataGridView dgvJurados = null!;
    private TextBox txtPesquisa = null!;
    private Button btnPesquisar = null!;
    private Button btnNovo = null!;
    private Button btnEditar = null!;
    private Button btnExcluir = null!;
    private Button btnAtualizar = null!;

    private List<Jurado> _todosJurados = new();
    private int _paginaAtual = 1;
    private const int RegistrosPorPagina = 10;

    private Panel pnlPaginacao = null!;
    private Button btnPaginaAnterior = null!;
    private Button btnProximaPagina = null!;
    private Label lblInfoPagina = null!;

    public JuradosView(IJuradoService juradoService)
    {
        _juradoService = juradoService;
        InitializeComponent();
    }

    public void OnNavigatedTo()
    {
        _ = CarregarGridAsync();
    }

    private void InitializeComponent()
    {
        this.BackColor = Color.FromArgb(248, 250, 252);
        this.Dock = DockStyle.Fill;

        var pnlAcoes = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.White,
            Padding = new Padding(15)
        };

        var lblPesq = new Label
        {
            Text = "Buscar:",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(15, 20),
            AutoSize = true
        };

        txtPesquisa = new TextBox
        {
            Font = new Font("Segoe UI", 10F),
            Location = new Point(75, 17),
            Size = new Size(250, 25)
        };
        txtPesquisa.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
                await CarregarGridAsync(txtPesquisa.Text);
        };

        btnPesquisar = CriarBotao("Filtrar", Color.FromArgb(37, 99, 235), 340, 15, 100);
        btnPesquisar.Click += async (s, e) => await CarregarGridAsync(txtPesquisa.Text);

        btnNovo = CriarBotao("+ Novo Jurado", Color.FromArgb(5, 150, 105), 450, 15, 150);
        btnNovo.Click += async (s, e) => await AbrirFormularioParaNovoAsync();

        btnEditar = CriarBotao("Editar", Color.FromArgb(217, 119, 6), 610, 15, 90);
        btnEditar.Click += async (s, e) => await AbrirFormularioParaEdicaoAsync();

        btnExcluir = CriarBotao("Excluir", Color.FromArgb(220, 38, 38), 710, 15, 90);
        btnExcluir.Click += async (s, e) => await ExcluirSelecionadoAsync();

        btnAtualizar = CriarBotao("Recarregar", Color.FromArgb(100, 116, 139), 810, 15, 120);
        btnAtualizar.Click += async (s, e) => await CarregarGridAsync();

        pnlAcoes.Controls.AddRange(new Control[] { lblPesq, txtPesquisa, btnPesquisar, btnNovo, btnEditar, btnExcluir, btnAtualizar });

        dgvJurados = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            EnableHeadersVisualStyles = false
        };
        dgvJurados.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
        dgvJurados.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvJurados.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        dgvJurados.ColumnHeadersHeight = 35;
        dgvJurados.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
        dgvJurados.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        dgvJurados.DefaultCellStyle.SelectionForeColor = Color.Black;
        dgvJurados.RowTemplate.Height = 28;

        ConfigurarColunasGrid();
        CriarPainelPaginacao();

        this.Controls.Add(dgvJurados);
        this.Controls.Add(pnlPaginacao);
        this.Controls.Add(pnlAcoes);
    }

    private Button CriarBotao(string texto, Color cor, int x, int y, int width)
    {
        return new Button
        {
            Text = texto,
            BackColor = cor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Location = new Point(x, y),
            Size = new Size(width, 30),
            Cursor = Cursors.Hand
        };
    }

    private void ConfigurarColunasGrid()
    {
        dgvJurados.Columns.Clear();
        dgvJurados.Columns.Add("Id", "ID");
        dgvJurados.Columns["Id"]!.Width = 70;
        dgvJurados.Columns.Add("Nome", "Nome");
        dgvJurados.Columns["Nome"]!.FillWeight = 250;
        dgvJurados.Columns.Add("Descricao", "Descrição");
        dgvJurados.Columns["Descricao"]!.FillWeight = 400;
    }

    private void CriarPainelPaginacao()
    {
        pnlPaginacao = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            BackColor = Color.White,
            Padding = new Padding(15, 6, 15, 6)
        };

        btnPaginaAnterior = new Button
        {
            Text = "◀ Anterior",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            Size = new Size(100, 30),
            Cursor = Cursors.Hand,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(71, 85, 105),
            Dock = DockStyle.Left
        };
        btnPaginaAnterior.Click += (s, e) =>
        {
            if (_paginaAtual > 1)
            {
                _paginaAtual--;
                ExibirPagina();
            }
        };

        btnProximaPagina = new Button
        {
            Text = "Próxima ▶",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            Size = new Size(100, 30),
            Cursor = Cursors.Hand,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(71, 85, 105),
            Dock = DockStyle.Right
        };
        btnProximaPagina.Click += (s, e) =>
        {
            if (_paginaAtual < TotalPaginas())
            {
                _paginaAtual++;
                ExibirPagina();
            }
        };

        lblInfoPagina = new Label
        {
            Text = "Página 1 de 1",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(71, 85, 105),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };

        pnlPaginacao.Controls.AddRange(new Control[] { btnPaginaAnterior, lblInfoPagina, btnProximaPagina });
    }

    private int TotalPaginas()
    {
        if (_todosJurados.Count == 0) return 1;
        return (int)Math.Ceiling((double)_todosJurados.Count / RegistrosPorPagina);
    }

    private void ExibirPagina()
    {
        dgvJurados.Rows.Clear();

        int totalRegistros = _todosJurados.Count;
        int totalPaginas = TotalPaginas();

        if (_paginaAtual > totalPaginas) _paginaAtual = totalPaginas;
        if (_paginaAtual < 1) _paginaAtual = 1;

        int indiceInicio = (_paginaAtual - 1) * RegistrosPorPagina;
        int indiceFim = Math.Min(indiceInicio + RegistrosPorPagina, totalRegistros);

        for (int i = indiceInicio; i < indiceFim; i++)
        {
            var jurado = _todosJurados[i];
            dgvJurados.Rows.Add(jurado.Id, jurado.Nome, jurado.Descricao);
        }

        int primeiroReg = totalRegistros == 0 ? 0 : indiceInicio + 1;
        lblInfoPagina.Text = $"Página {_paginaAtual} de {totalPaginas}  •  {primeiroReg}-{indiceFim} de {totalRegistros} registros";

        btnPaginaAnterior.Enabled = _paginaAtual > 1;
        btnProximaPagina.Enabled = _paginaAtual < totalPaginas;
    }

    private async Task CarregarGridAsync(string termo = "")
    {
        try
        {
            _todosJurados = (string.IsNullOrWhiteSpace(termo)
                ? await _juradoService.ObterTodosAsync()
                : await _juradoService.PesquisarAsync(termo))
                .OrderBy(j => j.Nome, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _paginaAtual = 1;
            ExibirPagina();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar jurados: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task AbrirFormularioParaNovoAsync()
    {
        var form = new Form
        {
            Text = "Novo Jurado",
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(560, 240),
            BackColor = Color.FromArgb(248, 250, 252),
            FormBorderStyle = FormBorderStyle.FixedSingle,
            MaximizeBox = false,
            MinimizeBox = false,
            Padding = new Padding(15)
        };

        var lblTitulo = new Label { Text = "NOVO JURADO", Font = new Font("Segoe UI", 11F, FontStyle.Bold), Location = new Point(20, 15), AutoSize = true };
        var lblNome = new Label { Text = "Nome:", Location = new Point(20, 55), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        var txtNome = new TextBox { Location = new Point(20, 75), Size = new Size(220, 25), Font = new Font("Segoe UI", 9.5F) };
        var lblDescricao = new Label { Text = "Descrição:", Location = new Point(260, 55), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        var txtDescricao = new TextBox { Location = new Point(260, 75), Size = new Size(250, 25), Font = new Font("Segoe UI", 9.5F) };

        var btnSalvar = new Button
        {
            Text = "Salvar",
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Location = new Point(260, 130),
            Size = new Size(120, 32),
            Cursor = Cursors.Hand
        };
        btnSalvar.Click += async (s, e) =>
        {
            try
            {
                await _juradoService.SalvarAsync(new Jurado
                {
                    Nome = txtNome.Text.Trim(),
                    Descricao = txtDescricao.Text.Trim()
                });

                form.DialogResult = DialogResult.OK;
                form.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        var btnCancelar = new Button
        {
            Text = "Cancelar",
            BackColor = Color.FromArgb(148, 163, 184),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Location = new Point(390, 130),
            Size = new Size(120, 32),
            Cursor = Cursors.Hand
        };
        btnCancelar.Click += (s, e) => form.Close();

        form.Controls.AddRange(new Control[] { lblTitulo, lblNome, txtNome, lblDescricao, txtDescricao, btnSalvar, btnCancelar });

        var result = form.ShowDialog(this);
        if (result == DialogResult.OK)
        {
            await CarregarGridAsync(txtPesquisa.Text);
        }
    }

    private async Task AbrirFormularioParaEdicaoAsync()
    {
        if (dgvJurados.SelectedRows.Count == 0)
        {
            MessageBox.Show("Selecione um jurado para editar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var id = Convert.ToInt32(dgvJurados.SelectedRows[0].Cells["Id"].Value);
        var jurado = await _juradoService.ObterPorIdAsync(id);
        if (jurado == null)
        {
            MessageBox.Show("Jurado não encontrado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var form = new Form
        {
            Text = $"Editar Jurado (ID: {jurado.Id})",
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(560, 240),
            BackColor = Color.FromArgb(248, 250, 252),
            FormBorderStyle = FormBorderStyle.FixedSingle,
            MaximizeBox = false,
            MinimizeBox = false,
            Padding = new Padding(15)
        };

        var lblTitulo = new Label { Text = "EDITAR JURADO", Font = new Font("Segoe UI", 11F, FontStyle.Bold), Location = new Point(20, 15), AutoSize = true };
        var lblNome = new Label { Text = "Nome:", Location = new Point(20, 55), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        var txtNome = new TextBox { Text = jurado.Nome, Location = new Point(20, 75), Size = new Size(220, 25), Font = new Font("Segoe UI", 9.5F) };
        var lblDescricao = new Label { Text = "Descrição:", Location = new Point(260, 55), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        var txtDescricao = new TextBox { Text = jurado.Descricao, Location = new Point(260, 75), Size = new Size(250, 25), Font = new Font("Segoe UI", 9.5F) };

        var btnSalvar = new Button
        {
            Text = "Salvar",
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Location = new Point(260, 130),
            Size = new Size(120, 32),
            Cursor = Cursors.Hand
        };
        btnSalvar.Click += async (s, e) =>
        {
            try
            {
                jurado.Nome = txtNome.Text.Trim();
                jurado.Descricao = txtDescricao.Text.Trim();
                await _juradoService.SalvarAsync(jurado);

                form.DialogResult = DialogResult.OK;
                form.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        var btnCancelar = new Button
        {
            Text = "Cancelar",
            BackColor = Color.FromArgb(148, 163, 184),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Location = new Point(390, 130),
            Size = new Size(120, 32),
            Cursor = Cursors.Hand
        };
        btnCancelar.Click += (s, e) => form.Close();

        form.Controls.AddRange(new Control[] { lblTitulo, lblNome, txtNome, lblDescricao, txtDescricao, btnSalvar, btnCancelar });

        var result = form.ShowDialog(this);
        if (result == DialogResult.OK)
        {
            await CarregarGridAsync(txtPesquisa.Text);
        }
    }

    private async Task ExcluirSelecionadoAsync()
    {
        if (dgvJurados.SelectedRows.Count == 0)
        {
            MessageBox.Show("Selecione um jurado para excluir.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var id = Convert.ToInt32(dgvJurados.SelectedRows[0].Cells["Id"].Value);
        var nome = dgvJurados.SelectedRows[0].Cells["Nome"].Value?.ToString() ?? "";

        var confirm = MessageBox.Show($"Deseja excluir o jurado '{nome}'?", "Confirmar exclusão", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
            return;

        try
        {
            var ok = await _juradoService.ExcluirAsync(id);
            if (ok)
            {
                await CarregarGridAsync(txtPesquisa.Text);
                MessageBox.Show("Jurado excluído com sucesso.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao excluir jurado", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
