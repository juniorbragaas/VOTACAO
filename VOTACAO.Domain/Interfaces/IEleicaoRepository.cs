using VOTACAO.Domain.Entities;

namespace VOTACAO.Domain.Interfaces;

public interface IEleicaoRepository
{
    Task<Eleicao?> ObterEleicaoAtivaAsync();
    Task<Eleicao?> ObterPorIdAsync(int id);
    Task<int> InserirAsync(Eleicao eleicao);
}
