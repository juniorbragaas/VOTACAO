using VOTACAO.Domain.Enums;

namespace VOTACAO.Domain.Entities;

public class Participante
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataNascimento { get; set; }
    public CategoriaParticipante Categoria { get; set; } = CategoriaParticipante.Adulto;
}
