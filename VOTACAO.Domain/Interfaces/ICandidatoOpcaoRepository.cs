using VOTACAO.Domain.Entities;

namespace VOTACAO.Domain.Interfaces;

public interface ICandidatoOpcaoRepository
{
    Task<IEnumerable<CandidatoOpcao>> ObterTodosAtivosAsync();
    Task<CandidatoOpcao?> ObterPorIdAsync(int id);
    Task<CandidatoOpcao?> ObterPorNumeroAsync(int numero);
    Task<int> InserirAsync(CandidatoOpcao opcao);
}
