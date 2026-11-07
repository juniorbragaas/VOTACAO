using System.Drawing;
using System.Windows.Forms;

namespace VOTACAO.UI.Views;

public class SobreView : UserControl
{
    public SobreView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.BackColor = Color.FromArgb(248, 250, 252);
        this.Dock = DockStyle.Fill;

        var panelCard = new Panel
        {
            BackColor = Color.White,
            Size = new Size(720, 480),
            Location = new Point(30, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblTitulo = new Label
        {
            Text = "SISTEMA DE AVALIAÇÃO DE MODELOS",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(25, 25),
            AutoSize = true
        };

        var lblVersao = new Label
        {
            Text = "Versão: 2026.1 Enterprise • Arquitetura .NET 8 LTS (C#)",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(37, 99, 235),
            Location = new Point(26, 65),
            AutoSize = true
        };

        var lblDescricao = new Label
        {
            Text = "O sistema AVALIAÇÃO DE MODELOS é uma solução corporativa de alta confiabilidade projetada para conduzir processos\n" +
                   "de avaliação institucional com transparência, segurança criptográfica e sigilo absoluto.\n\n" +
                   "Destaques da Arquitetura e Engenharia de Software:\n" +
                   "• Padrão em 5 Camadas (Domain, Infrastructure, Data, Business Objects e User Interface).\n" +
                   "• Inversão de Controle e Injeção de Dependências com Microsoft.Extensions.DependencyInjection.\n" +
                   "• Persistência em arquivo físico Microsoft Access (.MDB) via OleDb com transações ACID atômicas.\n" +
                   "• Motor próprio de versionamento incremental e migrações de banco de dados (Auto-Migrations).\n" +
                   "• Voto Secreto com dissociação estrutural entre o Eleitor e a Cédula Votada.\n" +
                   "• Hashing criptográfico SHA-256 para cada voto computado garantindo integridade.\n" +
                   "• Logging estruturado diário e trilha de auditoria administrativa completa.",
            Font = new Font("Segoe UI", 10F, FontStyle.Regular),
            ForeColor = Color.FromArgb(71, 85, 105),
            Location = new Point(26, 110),
            Size = new Size(660, 230)
        };

        var lblCopyright = new Label
        {
            Text = "© 2026 AVALIAÇÃO DE MODELOS – Todos os direitos reservados.\nDesenvolvido com padrões Clean Code e SOLID.",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(26, 380),
            AutoSize = true
        };

        panelCard.Controls.Add(lblTitulo);
        panelCard.Controls.Add(lblVersao);
        panelCard.Controls.Add(lblDescricao);
        panelCard.Controls.Add(lblCopyright);

        this.Controls.Add(panelCard);
    }
}
