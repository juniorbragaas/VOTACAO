using System.Drawing;
using System.Windows.Forms;
using VOTACAO.UI.Navigation;
using VOTACAO.UI.Views;

namespace VOTACAO.UI.Forms;

public class Principal : Form
{
    private readonly INavigationService _navigationService;

    private Panel pnlCabecalho = null!;
    private Panel pnlMenu = null!;
    private Panel pnlConteudo = null!;
    private Panel pnlRodape = null!;

    private Button btnSobre = null!;
    private Button btnResultados = null!;
    private Button btnCadastros = null!;
    private Button btnApuracao = null!;

    public Principal(INavigationService navigationService)
    {
        _navigationService = navigationService;
        InitializeLayout();
    }

    private void InitializeLayout()
    {
        this.Text = "AVALIAÇÃO DE MODELOS – Sistema de Avaliação Corporativa";
        this.Size = new Size(1180, 760);
        this.MinimumSize = new Size(1000, 650);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        this.BackColor = Color.FromArgb(248, 250, 252);

        // 1. CABEÇALHO
        pnlCabecalho = new Panel
        {
            Dock = DockStyle.Top,
            Height = 78,
            BackColor = Color.FromArgb(27, 54, 93),
            Padding = Padding.Empty
        };

        var layoutCabecalho = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        layoutCabecalho.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layoutCabecalho.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        layoutCabecalho.RowStyles.Add(new RowStyle(SizeType.Percent, 42));

        var lblLogo = new Label
        {
            Text = "AVALIAÇÃO DE MODELOS",
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true,
            Margin = Padding.Empty
        };

        var lblSubtitulo = new Label
        {
            Text = "SISTEMA INTEGRADO DE GESTÃO E AVALIAÇÃO",
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(203, 213, 225),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true,
            Margin = Padding.Empty
        };

        layoutCabecalho.Controls.Add(lblLogo, 0, 0);
        layoutCabecalho.Controls.Add(lblSubtitulo, 0, 1);
        pnlCabecalho.Controls.Add(layoutCabecalho);

        // 2. RODAPÉ
        pnlRodape = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 35,
            BackColor = Color.FromArgb(30, 41, 59),
            Padding = new Padding(12, 0, 12, 0)
        };

        var lblRodape = new Label
        {
            Text = "© 2026 AVALIAÇÃO DE MODELOS – Todos os direitos reservados.   |   Banco de Dados: Microsoft Access (.MDB) Conectado",
            Font = new Font("Segoe UI", 8F, FontStyle.Regular),
            ForeColor = Color.FromArgb(148, 163, 184),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true,
            Margin = Padding.Empty
        };

        pnlRodape.Controls.Add(lblRodape);

        // 3. MENU (horizontal, abaixo do cabeçalho)
        pnlMenu = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.White
        };

        // Linha separadora inferior do menu
        pnlMenu.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
            e.Graphics.DrawLine(pen, 0, pnlMenu.Height - 1, pnlMenu.Width, pnlMenu.Height - 1);
        };

        btnSobre = CriarBotaoMenu("Sobre o Sistema", 15);
        btnSobre.Click += (s, e) => { AtivarBotaoMenu(btnSobre); _navigationService.NavigateTo<SobreView>(); };

       

        btnCadastros = CriarBotaoMenu("Cadastros ▾", 205);
        var menuCadastros = new ContextMenuStrip
        {
            Font = new Font("Segoe UI", 9.5F),
            BackColor = Color.White,
            ShowImageMargin = false
        };
        menuCadastros.Items.Add("Participantes", null, (s, e) =>
        {
            AtivarBotaoMenu(btnCadastros);
            _navigationService.NavigateTo<CadastradosView>();
        });
        menuCadastros.Items.Add("Jurados", null, (s, e) =>
        {
            AtivarBotaoMenu(btnCadastros);
            _navigationService.NavigateTo<JuradosView>();
        });
        menuCadastros.Items.Add("Critérios", null, (s, e) =>
        {
            AtivarBotaoMenu(btnCadastros);
            _navigationService.NavigateTo<CriteriosView>();
        });
        btnCadastros.Click += (s, e) =>
        {
            AtivarBotaoMenu(btnCadastros);
            menuCadastros.Show(btnCadastros, new Point(0, btnCadastros.Height));
        }; 

        btnApuracao = CriarBotaoMenu("Apuração de Notas", 395);
        btnApuracao.Click += (s, e) => { AtivarBotaoMenu(btnApuracao); _navigationService.NavigateTo<ApuracaoNotasView>(); };

        btnResultados = CriarBotaoMenu("Consultas",585);
        btnResultados.Click += (s, e) => { AtivarBotaoMenu(btnResultados); _navigationService.NavigateTo<ResultadosSqlView>(); };

        var btnDiculgacao = CriarBotaoMenu("Resultados Votação", 775);
        btnDiculgacao.Click += (s, e) => { AtivarBotaoMenu(btnDiculgacao); _navigationService.NavigateTo<DiculgacaoView>(); };

        pnlMenu.Controls.AddRange(new Control[] { btnSobre, btnResultados, btnCadastros, btnApuracao, btnDiculgacao });

        // 4. ÁREA CENTRAL
        pnlConteudo = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 250, 252)
        };

        this.Controls.Add(pnlConteudo);
        this.Controls.Add(pnlMenu);
        this.Controls.Add(pnlRodape);
        this.Controls.Add(pnlCabecalho);

        _navigationService.SetContainer(pnlConteudo);
        this.Load += (s, e) =>
        {
            AtivarBotaoMenu(btnSobre);
            _navigationService.NavigateTo<SobreView>();
        };
    }

    private Button CriarBotaoMenu(string texto, int left)
    {
        return new Button
        {
            Text = texto,
            Location = new Point(left, 7),
            Size = new Size(180, 38),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(71, 85, 105),
            BackColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
    }

    private void AtivarBotaoMenu(Button ativo)
    {
        foreach (Control c in pnlMenu.Controls)
        {
            if (c is Button b)
            {
                b.BackColor = Color.White;
                b.ForeColor = Color.FromArgb(71, 85, 105);
            }
        }
        ativo.BackColor = Color.FromArgb(238, 242, 255);
        ativo.ForeColor = Color.FromArgb(37, 99, 235);
    }
}
