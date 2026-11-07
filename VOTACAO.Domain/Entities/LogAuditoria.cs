namespace VOTACAO.Domain.Entities;

public class LogAuditoria
{
    public int Id { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string Detalhes { get; set; } = string.Empty;
    public string Usuario { get; set; } = "Sistema";
    public DateTime DataHora { get; set; } = DateTime.Now;
}
