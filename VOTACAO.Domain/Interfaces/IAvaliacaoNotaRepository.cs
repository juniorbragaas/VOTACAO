using VOTACAO.Domain.Entities;

namespace VOTACAO.Domain.Interfaces;

public interface IAvaliacaoNotaRepository
{
    Task<IEnumerable<AvaliacaoNota>> ObterPorJuradoAsync(int idJurado);
    Task<AvaliacaoNota?> ObterPorJuradoParticipanteCriterioAsync(int idJurado, int idParticipante, int idCriterio);
    Task<IEnumerable<AvaliacaoNota>> ObterPorJuradoEParticipanteAsync(int idJurado, int idParticipante);
    Task<int> InserirAsync(AvaliacaoNota nota);
    Task<bool> AtualizarAsync(AvaliacaoNota nota);
    Task<bool> ExcluirPorJuradoAsync(int idJurado);
    Task<bool> ExcluirPorJuradoParticipanteAsync(int idJurado, int idParticipante);
    Task<bool> SalvarOuAtualizarAsync(AvaliacaoNota nota);
    Task<int> SalvarNotasAsync(int idJurado, IReadOnlyCollection<(int IdParticipante, int IdCriterio, int Nota)> notas);
    Task<int> GarantirNotasEmBrancoAsync(int idJurado, IReadOnlyCollection<int> idsParticipantes, IReadOnlyCollection<int> idsCriterios);
    Task<int> ZerarTodasNotasAsync();
}



























































































































