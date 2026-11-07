using System.Drawing;
using System.Windows.Forms;

namespace VOTACAO.UI.Views;

public sealed class ProgressoModal : Form
{
    private readonly Label _lblMensagem;
    private readonly ProgressBar _progressBar;

    public ProgressoModal(string titulo, string mensagem)
    {
        Text = titulo;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ControlBox = false;
        TopMost = true;
        Size = new Size(430, 150);
        BackColor = Color.FromArgb(248, 250, 252);
        Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        Opacity = 0.98;

        _progressBar = new ProgressBar
        {
            Style = ProgressBarStyle.Marquee,
            Dock = DockStyle.Top,
            Height = 22,
            MarqueeAnimationSpeed = 40
        };

        _lblMensagem = new Label
        {
            Text = mensagem,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(30, 41, 59),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Padding = new Padding(20, 10, 20, 10)
        };

        Controls.Add(_lblMensagem);
        Controls.Add(_progressBar);
    }

    public new void Show(IWin32Window owner)
    {
        if (owner is not null)
        {
            StartPosition = FormStartPosition.CenterParent;
            Owner = owner.Handle == IntPtr.Zero ? null : Control.FromHandle(owner.Handle) as Form;
        }

        base.Show(owner);
        CenterToParent();
        Activate();
        Application.DoEvents();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        CenterToParent();
        Application.DoEvents();
    }
}
