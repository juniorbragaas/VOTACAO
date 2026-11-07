using VOTACAO.Domain.Enums;

namespace VOTACAO.Domain.Entities;

public class Eleicao
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public StatusEleicao Status { get; set; } = StatusEleicao.Aberta;
    public bool PermiteVotoBrancoNulo { get; set; } = true;
}
