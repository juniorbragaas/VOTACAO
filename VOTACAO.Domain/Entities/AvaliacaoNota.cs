namespace VOTACAO.Domain.Entities;

public class AvaliacaoNota
{
    public int Id { get; set; }
    public int IdJurado { get; set; }
    public int IdParticipante { get; set; }
    public int IdCriterio { get; set; }
    public int Nota { get; set; }
}
