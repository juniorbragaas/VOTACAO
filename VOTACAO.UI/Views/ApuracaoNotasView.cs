using System.Drawing;
using System.Windows.Forms;
using VOTACAO.BO.Services;
using VOTACAO.Domain.Entities;
using VOTACAO.UI.Navigation;

namespace VOTACAO.UI.Views;

public class ApuracaoNotasView : UserControl, IRefreshableView
{
    private readonly IApuracaoNotasService _service;

    private ComboBox cmbJurados = null!;
    private DataGridView dgvNotas = null!;
    private Label lblStatus = null!;
    private Button btnZerar = null!;
    private Button btnSalvar = null!;

    private List<Jurado> _jurados = new();
    private List<Participante> _participantes = new();
    private List<Criterio> _criterios = new();
    private bool _carregandoGrid;
    private bool _gridConstruida;

    private void AjustarGridParaCriterios()
    {
        if (_gridConstruida)
            return;

        dgvNotas.Columns.Clear();

        dgvNotas.Columns.Add("Participante", "Participante");
        dgvNotas.Columns["Participante"].ReadOnly = true;
        dgvNotas.Columns["Participante"].Width = 220;

        foreach (var criterio in _criterios)
        {
            var col = new DataGridViewTextBoxColumn
            {
                Name = $"C_{criterio.Id}",
                HeaderText = criterio.Id.ToString()+ " - " + criterio.Nome,
                Width = 110,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter },
                ValueType = typeof(int)
            };
            dgvNotas.Columns.Add(col);
        }

        _gridConstruida = true;
    }

    public ApuracaoNotasView(IApuracaoNotasService service)
    {
        _service = service;
        InitializeComponent();
    }

    public void OnNavigatedTo()
    {
        _ = CarregarDadosAsync();
    }

    private void InitializeComponent()
    {
        this.BackColor = Color.FromArgb(248, 250, 252);
        this.Dock = DockStyle.Fill;
        this.AutoScroll = true;

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 90,
            BackColor = Color.FromArgb(30, 41, 59),
            Padding = new Padding(20, 12, 20, 12)
        };

        var lblTitulo = new Label
        {
            Text = "APURAÇÃO DE NOTAS",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 18),
            AutoSize = true
        };

        lblStatus = new Label
        {
            Text = "Carregando jurados e critérios...",
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(203, 213, 225),
            Location = new Point(20, 48),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitulo);
        pnlHeader.Controls.Add(lblStatus);

        var pnlFiltro = new Panel
        {
            Dock = DockStyle.Top,
            Height = 76,
            BackColor = Color.White,
            Padding = new Padding(20, 15, 20, 15)
        };

        var lblJurado = new Label
        {
            Text = "Selecione o jurado:",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(20, 25),
            AutoSize = true
        };

        cmbJurados = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10F),
            Location = new Point(170, 20),
            Size = new Size(380, 30)
        };
        cmbJurados.SelectedIndexChanged += async (s, e) =>
        {
            if (_carregandoGrid || !_gridConstruida)
                return;

            await CarregarGridPorJuradoAsync();
        };

        pnlFiltro.Controls.AddRange(new Control[] { lblJurado, cmbJurados });

        dgvNotas = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            MultiSelect = false,
            ReadOnly = false,
            EditMode = DataGridViewEditMode.EditOnEnter,
            AutoGenerateColumns = false,
            EnableHeadersVisualStyles = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect
        };
        dgvNotas.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
        dgvNotas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvNotas.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        dgvNotas.ColumnHeadersHeight = 35;
        dgvNotas.DefaultCellStyle.Font = new Font("Segoe UI", 9.1F);

        var pnlRodape = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 55,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };

        btnZerar = new Button
        {
            Text = "Zerar notas",
            BackColor = Color.FromArgb(148, 163, 184),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Right,
            Size = new Size(120, 32),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0)
        };
        btnZerar.Click += async (s, e) => await ZerarNotasAsync();

        btnSalvar = new Button
        {
            Text = "Salvar notas",
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Right,
            Size = new Size(140, 32),
            Cursor = Cursors.Hand
        };
        btnSalvar.Click += async (s, e) => await SalvarNotasAsync();

        pnlRodape.Controls.Add(btnSalvar);
        pnlRodape.Controls.Add(btnZerar);

        this.Controls.Add(dgvNotas);
        this.Controls.Add(pnlRodape);
        this.Controls.Add(pnlFiltro);
        this.Controls.Add(pnlHeader);
    }

    private async Task CarregarDadosAsync()
    {
        var modal = new ProgressoModal("Carregando apuração", "Preparando jurados, participantes e critérios...");

        try
        {
            var owner = this.ParentForm != null ? this.ParentForm : FindForm();
            if (owner != null)
                modal.Show(owner);
            else
                modal.Show(this);

            _jurados = (await _service.ObterJuradosAsync()).OrderBy(j => j.Nome, StringComparer.OrdinalIgnoreCase).ToList();
            _participantes = (await _service.ObterParticipantesOrdenadosAsync()).ToList();
            _criterios = (await _service.ObterCriteriosAsync()).OrderBy(c => c.Nome, StringComparer.OrdinalIgnoreCase).ToList();

            cmbJurados.Items.Clear();
            foreach (var jurado in _jurados)
            {
                cmbJurados.Items.Add(new ComboBoxItem(jurado.Id, jurado.Nome));
            }

            if (cmbJurados.Items.Count > 0)
            {
                _carregandoGrid = true;
                try
                {
                    cmbJurados.SelectedIndex = 0;
                }
                finally
                {
                    _carregandoGrid = false;
                }
            }

            AjustarGridParaCriterios();
            await _service.GarantirNotasEmBrancoAsync();
            lblStatus.Text = $"{_jurados.Count} jurados, {_participantes.Count} participantes e {_criterios.Count} critérios carregados.";
            await CarregarGridPorJuradoAsync();
        }
        catch (Exception ex)
        {
            lblStatus.Text = ex.Message;
            MessageBox.Show(ex.Message, "Erro na apuração", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            modal.Close();
            modal.Dispose();
        }
    }

    private async Task CarregarGridPorJuradoAsync()
    {
        if (cmbJurados.SelectedItem is not ComboBoxItem juradoSelecionado)
            return;

        if (!_gridConstruida || dgvNotas.Columns.Count == 0)
        {
            AjustarGridParaCriterios();
        }

        var idJurado = juradoSelecionado.Id;
        _carregandoGrid = true;
        try
        {
            dgvNotas.Rows.Clear();

            foreach (var participante in _participantes)
            {
                var row = new List<object> { participante.Nome };
                var notas = await _service.ObterNotasDoJuradoParaParticipanteAsync(idJurado, participante.Id);
                var dicionario = notas.ToDictionary(n => n.IdCriterio, n => n.Nota);

                foreach (var criterio in _criterios)
                {
                    var nota = dicionario.TryGetValue(criterio.Id, out var valor) ? valor : 0;
                    row.Add(nota);
                }

                dgvNotas.Rows.Add(row.ToArray());
            }
        }
        finally
        {
            _carregandoGrid = false;
        }

        lblStatus.Text = $"Avaliando: {juradoSelecionado.Nome} | {_participantes.Count} participantes | {_criterios.Count} critérios";
    }

    private async Task SalvarNotasAsync()
    {
        if (cmbJurados.SelectedItem is not ComboBoxItem juradoSelecionado)
        {
            MessageBox.Show("Selecione um jurado antes de salvar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var modal = new ProgressoModal("Salvando notas", "Gravando avaliações do jurado selecionado...");

        try
        {
            btnSalvar.Enabled = false;
            btnZerar.Enabled = false;

            var notasParaSalvar = new List<(int IdParticipante, int IdCriterio, int Nota)>();
            foreach (DataGridViewRow row in dgvNotas.Rows)
            {
                if (row.IsNewRow) continue;

                var participante = _participantes.FirstOrDefault(p => p.Nome == row.Cells[0].Value?.ToString());
                if (participante == null) continue;

                for (int i = 0; i < _criterios.Count; i++)
                {
                    var criterio = _criterios[i];
                    var cell = row.Cells[i + 1];
                    var valorTexto = cell.Value?.ToString()?.Trim();

                    int nota = 0;
                    if (!string.IsNullOrWhiteSpace(valorTexto) && int.TryParse(valorTexto, out var parsed))
                    {
                        nota = parsed;
                    }

                    notasParaSalvar.Add((participante.Id, criterio.Id, nota));
                }
            }

            var owner = this.ParentForm != null ? this.ParentForm : FindForm();
            if (owner != null)
                modal.Show(owner);
            else
                modal.Show(this);

            await _service.SalvarNotasAsync(juradoSelecionado.Id, notasParaSalvar);

            MessageBox.Show("Notas salvas com sucesso.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await CarregarGridPorJuradoAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao salvar notas", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            modal.Close();
            modal.Dispose();
            btnSalvar.Enabled = true;
            btnZerar.Enabled = true;
        }
    }

    private async Task ZerarNotasAsync()
    {
        var modal = new ProgressoModal("Zerando notas", "Apagando todas as avaliações e recriando o painel com zero...");

        try
        {
            btnSalvar.Enabled = false;
            btnZerar.Enabled = false;

            var owner = this.ParentForm != null ? this.ParentForm : FindForm();
            if (owner != null)
                modal.Show(owner);
            else
                modal.Show(this);

            await _service.ZerarTodasNotasAsync();
            await CarregarGridPorJuradoAsync();
            MessageBox.Show("Todas as notas foram zeradas com sucesso.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao zerar notas", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            modal.Close();
            modal.Dispose();
            btnSalvar.Enabled = true;
            btnZerar.Enabled = true;
        }
    }

    private sealed class ComboBoxItem
    {
        public ComboBoxItem(int id, string nome)
        {
            Id = id;
            Nome = nome;
        }

        public int Id { get; }
        public string Nome { get; }

        public override string ToString() => Nome;
    }
}
