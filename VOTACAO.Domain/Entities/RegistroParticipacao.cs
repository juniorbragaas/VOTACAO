namespace VOTACAO.Domain.Entities;

public class RegistroParticipacao
{
    public int Id { get; set; }
    public int EleicaoId { get; set; }
    public int ParticipanteId { get; set; }
    public DateTime DataHoraVoto { get; set; } = DateTime.Now;
}
