using VOTACAO.Domain.Entities;

namespace VOTACAO.Domain.Interfaces;

public interface IAuditoriaRepository
{
    Task InserirLogAsync(string acao, string detalhes, string usuario = "Sistema");
}
