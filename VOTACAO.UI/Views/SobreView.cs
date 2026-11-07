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
        BackColor = Color.FromArgb(248, 250, 252);
        Dock = DockStyle.Fill;
        Padding = new Padding(30);

        var panelCard = new Panel
        {
            BackColor = Color.White,
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(26)
        };

        var conteudo = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(71, 85, 105),
            Font = new Font("Segoe UI", 10F, FontStyle.Regular),
            DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            WordWrap = true,
            TabStop = false
        };

        AdicionarTexto(conteudo, "Sobre o Sistema\n\n", 18F, FontStyle.Bold, Color.FromArgb(30, 41, 59));

        AdicionarTexto(conteudo, "O ", 10F, FontStyle.Regular);
        AdicionarTexto(conteudo, "Sistema de Votação para Concursos", 10F, FontStyle.Bold, Color.FromArgb(30, 41, 59));
        AdicionarTexto(conteudo, " foi desenvolvido para facilitar, organizar e automatizar todo o processo de avaliação e classificação de participantes em concursos.\n\n");

        AdicionarTexto(conteudo, "A plataforma permite o cadastro e gerenciamento de participantes, jurados e critérios de avaliação, proporcionando um ambiente centralizado, prático e seguro para a realização das votações.\n\n");

        AdicionarTexto(conteudo, "Por meio do sistema, cada jurado pode registrar individualmente suas notas para todas as participantes, considerando os critérios previamente definidos pela organização do concurso.\n\n");

        AdicionarTexto(conteudo, "Após o lançamento das avaliações, o sistema realiza automaticamente o processamento das notas, permitindo gerar consultas, apurar resultados e emitir relatórios de classificação conforme as regras e os critérios estabelecidos para cada concurso.\n\n");

        AdicionarTexto(conteudo, "Principais funcionalidades:\n", 10F, FontStyle.Bold, Color.FromArgb(30, 41, 59));
        AdicionarTexto(conteudo,
            "• Cadastro e gerenciamento de participantes.\n" +
            "• Cadastro e gerenciamento de jurados.\n" +
            "• Configuração dos critérios de avaliação.\n" +
            "• Lançamento individual de notas por jurado.\n" +
            "• Consulta e acompanhamento das avaliações.\n" +
            "• Processamento automático das pontuações.\n" +
            "• Geração de relatórios de classificação e resultados.\n\n");

        AdicionarTexto(conteudo, "O objetivo é proporcionar ");
        AdicionarTexto(conteudo, "transparência, agilidade, organização e confiabilidade", 10F, FontStyle.Bold, Color.FromArgb(30, 41, 59));
        AdicionarTexto(conteudo, " em todas as etapas do processo de votação, contribuindo para uma gestão eficiente e uma apuração precisa dos resultados.\n\n");

        AdicionarTexto(conteudo, "Todos os direitos reservados.", 10F, FontStyle.Bold, Color.FromArgb(30, 41, 59));

        panelCard.Controls.Add(conteudo);
        Controls.Add(panelCard);
    }

    private static void AdicionarTexto(RichTextBox caixa, string texto, float tamanho = 10F,
        FontStyle estilo = FontStyle.Regular, Color? cor = null)
    {
        using var fonte = new Font("Segoe UI", tamanho, estilo);
        caixa.SelectionStart = caixa.TextLength;
        caixa.SelectionLength = 0;
        caixa.SelectionFont = fonte;
        caixa.SelectionColor = cor ?? Color.FromArgb(71, 85, 105);
        caixa.AppendText(texto);
    }
}
