namespace VOTACAO.Domain.DTOs;

public class VotoRegistroDto
{
    public int EleicaoId { get; set; }
    public int ParticipanteId { get; set; }
    public int? CandidatoOpcaoId { get; set; }
    public bool IsBranco { get; set; }
    public bool IsNulo { get; set; }
}
