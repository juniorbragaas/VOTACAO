using VOTACAO.Domain.Enums;

namespace VOTACAO.Domain.DTOs;

public class ParticipanteDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataNascimento { get; set; }
    public CategoriaParticipante Categoria { get; set; }
    public bool JaVotou { get; set; }
}
