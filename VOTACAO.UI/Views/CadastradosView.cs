using System.Drawing;
using System.Windows.Forms;
using VOTACAO.BO.Services;
using VOTACAO.Domain.DTOs;
using VOTACAO.Domain.Entities;
using VOTACAO.Domain.Enums;
using VOTACAO.UI.Navigation;

namespace VOTACAO.UI.Views;

public class CadastradosView : UserControl, IRefreshableView
{
    private readonly IParticipanteService _participanteService;

    private DataGridView dgvParticipantes = null!;
    private TextBox txtPesquisa = null!;
    private Button btnPesquisar = null!;
    private Button btnNovo = null!;
    private Button btnEditar = null!;
    private Button btnExcluir = null!;
    private Button btnAtualizar = null!;

    private List<Participante> _todosParticipantes = new();
    private int _paginaAtual = 1;
    private const int RegistrosPorPagina = 10;

    private Panel pnlPaginacao = null!;
    private Button btnPaginaAnterior = null!;
    private Button btnProximaPagina = null!;
    private Label lblInfoPagina = null!;

    public CadastradosView(IParticipanteService participanteService)
    {
        _participanteService = participanteService;
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
            Height = 65,
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
            Size = new Size(220, 25)
        };
        txtPesquisa.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
                await CarregarGridAsync(txtPesquisa.Text);
        };

        btnPesquisar = CriarBotao("Filtrar", Color.FromArgb(37, 99, 235), 305, 15, 80);
        btnPesquisar.Click += async (s, e) => await CarregarGridAsync(txtPesquisa.Text);

        btnNovo = CriarBotao("+ Novo Participante", Color.FromArgb(5, 150, 105), 400, 15, 160);
        btnNovo.Click += async (s, e) => await AbrirFormularioParaNovoAsync();

        btnEditar = CriarBotao("Editar", Color.FromArgb(217, 119, 6), 570, 15, 80);
        btnEditar.Click += async (s, e) => await AbrirFormularioParaEdicaoAsync();

        btnExcluir = CriarBotao("Excluir", Color.FromArgb(220, 38, 38), 660, 15, 80);
        btnExcluir.Click += async (s, e) => await ExcluirSelecionadoAsync();

        btnAtualizar = CriarBotao("Recarregar", Color.FromArgb(100, 116, 139), 750, 15, 100);
        btnAtualizar.Click += async (s, e) => await CarregarGridAsync();

        pnlAcoes.Controls.AddRange(new Control[] { lblPesq, txtPesquisa, btnPesquisar, btnNovo, btnEditar, btnExcluir, btnAtualizar });

        dgvParticipantes = new DataGridView
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
        dgvParticipantes.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
        dgvParticipantes.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvParticipantes.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        dgvParticipantes.ColumnHeadersHeight = 35;
        dgvParticipantes.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
        dgvParticipantes.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        dgvParticipantes.DefaultCellStyle.SelectionForeColor = Color.Black;
        dgvParticipantes.RowTemplate.Height = 28;

        ConfigurarColunasGrid();
        CriarPainelPaginacao();

        this.Controls.Add(dgvParticipantes);
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
        dgvParticipantes.Columns.Clear();
        dgvParticipantes.Columns.Add("Id", "ID");
        dgvParticipantes.Columns["Id"]!.Width = 60;
        dgvParticipantes.Columns.Add("Nome", "Nome do Participante");
        dgvParticipantes.Columns["Nome"]!.FillWeight = 250;
        dgvParticipantes.Columns.Add("DataNascimento", "Data de Nascimento");
        dgvParticipantes.Columns["DataNascimento"]!.FillWeight = 150;
        dgvParticipantes.Columns.Add("Categoria", "Categoria");
        dgvParticipantes.Columns["Categoria"]!.FillWeight = 120;
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
        if (_todosParticipantes.Count == 0) return 1;
        return (int)Math.Ceiling((double)_todosParticipantes.Count / RegistrosPorPagina);
    }

    private void ExibirPagina()
    {
        dgvParticipantes.Rows.Clear();

        int totalRegistros = _todosParticipantes.Count;
        int totalPaginas = TotalPaginas();

        if (_paginaAtual > totalPaginas) _paginaAtual = totalPaginas;
        if (_paginaAtual < 1) _paginaAtual = 1;

        int indiceInicio = (_paginaAtual - 1) * RegistrosPorPagina;
        int indiceFim = Math.Min(indiceInicio + RegistrosPorPagina, totalRegistros);

        for (int i = indiceInicio; i < indiceFim; i++)
        {
            var participante = _todosParticipantes[i];
            dgvParticipantes.Rows.Add(participante.Id, participante.Nome, participante.DataNascimento.ToString("dd/MM/yyyy"), participante.Categoria.ToString());
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
            _todosParticipantes = (await _participanteService.PesquisarAsync(termo))
                .OrderBy(p => p.Nome, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _paginaAtual = 1;
            ExibirPagina();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar participantes: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task AbrirFormularioParaNovoAsync()
    {
        var form = new Form
        {
            Text = "Novo Participante",
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(620, 260),
            BackColor = Color.FromArgb(248, 250, 252),
            FormBorderStyle = FormBorderStyle.FixedSingle,
            MaximizeBox = false,
            MinimizeBox = false,
            Padding = new Padding(15)
        };

        var lblTitulo = new Label { Text = "NOVO PARTICIPANTE", Font = new Font("Segoe UI", 11F, FontStyle.Bold), Location = new Point(20, 15), AutoSize = true };
        var lblNome = new Label { Text = "Nome Completo:", Location = new Point(20, 55), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        var txtNome = new TextBox { Location = new Point(20, 75), Size = new Size(240, 25), Font = new Font("Segoe UI", 9.5F) };
        var lblDataNasc = new Label { Text = "Data de Nascimento:", Location = new Point(280, 55), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        var dtpDataNascimento = new DateTimePicker
        {
            Location = new Point(280, 75),
            Size = new Size(150, 25),
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.5F),
            MaxDate = DateTime.Today
        };
        var lblCategoria = new Label { Text = "Categoria:", Location = new Point(450, 55), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        var cmbCategoria = new ComboBox
        {
            Location = new Point(450, 75),
            Size = new Size(130, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5F)
        };
        cmbCategoria.Items.AddRange(Enum.GetNames(typeof(CategoriaParticipante)));
        cmbCategoria.SelectedIndex = 0;

        var btnSalvar = new Button
        {
            Text = "Salvar",
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Location = new Point(280, 135),
            Size = new Size(120, 32),
            Cursor = Cursors.Hand
        };
        btnSalvar.Click += async (s, e) =>
        {
            try
            {
                await _participanteService.SalvarAsync(new ParticipanteDto
                {
                    Id = 0,
                    Nome = txtNome.Text.Trim(),
                    DataNascimento = dtpDataNascimento.Value.Date,
                    Categoria = Enum.Parse<CategoriaParticipante>(cmbCategoria.SelectedItem?.ToString() ?? "Adulto")
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
            Location = new Point(410, 135),
            Size = new Size(120, 32),
            Cursor = Cursors.Hand
        };
        btnCancelar.Click += (s, e) => form.Close();

        form.Controls.AddRange(new Control[] { lblTitulo, lblNome, txtNome, lblDataNasc, dtpDataNascimento, lblCategoria, cmbCategoria, btnSalvar, btnCancelar });

        var result = form.ShowDialog(this);
        if (result == DialogResult.OK)
        {
            await CarregarGridAsync(txtPesquisa.Text);
        }
    }

    private async Task AbrirFormularioParaEdicaoAsync()
    {
        if (dgvParticipantes.SelectedRows.Count == 0)
        {
            MessageBox.Show("Selecione um participante para editar.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var id = Convert.ToInt32(dgvParticipantes.SelectedRows[0].Cells["Id"].Value);
        var participante = await _participanteService.ObterPorIdAsync(id);
        if (participante == null)
        {
            MessageBox.Show("Participante não encontrado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var form = new Form
        {
            Text = $"Editar Participante (ID: {participante.Id})",
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(620, 260),
            BackColor = Color.FromArgb(248, 250, 252),
            FormBorderStyle = FormBorderStyle.FixedSingle,
            MaximizeBox = false,
            MinimizeBox = false,
            Padding = new Padding(15)
        };

        var lblTitulo = new Label { Text = "EDITAR PARTICIPANTE", Font = new Font("Segoe UI", 11F, FontStyle.Bold), Location = new Point(20, 15), AutoSize = true };
        var lblNome = new Label { Text = "Nome Completo:", Location = new Point(20, 55), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        var txtNome = new TextBox { Text = participante.Nome, Location = new Point(20, 75), Size = new Size(240, 25), Font = new Font("Segoe UI", 9.5F) };
        var lblDataNasc = new Label { Text = "Data de Nascimento:", Location = new Point(280, 55), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        var dtpDataNascimento = new DateTimePicker
        {
            Value = participante.DataNascimento,
            Location = new Point(280, 75),
            Size = new Size(150, 25),
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.5F),
            MaxDate = DateTime.Today
        };
        var lblCategoria = new Label { Text = "Categoria:", Location = new Point(450, 55), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        var cmbCategoria = new ComboBox
        {
            Location = new Point(450, 75),
            Size = new Size(130, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5F)
        };
        cmbCategoria.Items.AddRange(Enum.GetNames(typeof(CategoriaParticipante)));
        cmbCategoria.SelectedItem = participante.Categoria.ToString();

        var btnSalvar = new Button
        {
            Text = "Salvar",
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Location = new Point(280, 135),
            Size = new Size(120, 32),
            Cursor = Cursors.Hand
        };
        btnSalvar.Click += async (s, e) =>
        {
            try
            {
                await _participanteService.SalvarAsync(new ParticipanteDto
                {
                    Id = participante.Id,
                    Nome = txtNome.Text.Trim(),
                    DataNascimento = dtpDataNascimento.Value.Date,
                    Categoria = Enum.Parse<CategoriaParticipante>(cmbCategoria.SelectedItem?.ToString() ?? "Adulto")
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
            Location = new Point(410, 135),
            Size = new Size(120, 32),
            Cursor = Cursors.Hand
        };
        btnCancelar.Click += (s, e) => form.Close();

        form.Controls.AddRange(new Control[] { lblTitulo, lblNome, txtNome, lblDataNasc, dtpDataNascimento, lblCategoria, cmbCategoria, btnSalvar, btnCancelar });

        var result = form.ShowDialog(this);
        if (result == DialogResult.OK)
        {
            await CarregarGridAsync(txtPesquisa.Text);
        }
    }

    private async Task ExcluirSelecionadoAsync()
    {
        if (dgvParticipantes.SelectedRows.Count == 0)
        {
            MessageBox.Show("Selecione um participante para excluir.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var id = Convert.ToInt32(dgvParticipantes.SelectedRows[0].Cells["Id"].Value);
        var nome = dgvParticipantes.SelectedRows[0].Cells["Nome"].Value?.ToString() ?? "";

        var confirmacao = MessageBox.Show($"Deseja realmente excluir o participante '{nome}' (ID: {id})?",
            "Confirmação de Exclusão", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (confirmacao == DialogResult.Yes)
        {
            try
            {
                await _participanteService.ExcluirAsync(id);
                MessageBox.Show("Participante excluído com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await CarregarGridAsync(txtPesquisa.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao excluir participante: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
