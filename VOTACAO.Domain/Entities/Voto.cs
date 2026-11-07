namespace VOTACAO.Domain.Entities;

public class Voto
{
    public int Id { get; set; }
    public int EleicaoId { get; set; }
    public int? CandidatoOpcaoId { get; set; }
    public bool IsBranco { get; set; }
    public bool IsNulo { get; set; }
    public DateTime DataHora { get; set; } = DateTime.Now;
    public string HashIntegridade { get; set; } = string.Empty;
}
