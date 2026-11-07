namespace VOTACAO.Domain.Entities;

public class CandidatoOpcao
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Foto { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}
