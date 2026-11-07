using VOTACAO.Domain.Entities;

namespace VOTACAO.BO.Services;

public interface IApuracaoNotasService
{
    Task<IEnumerable<Jurado>> ObterJuradosAsync();
    Task<IEnumerable<Participante>> ObterParticipantesOrdenadosAsync();
    Task<IEnumerable<Criterio>> ObterCriteriosAsync();
    Task<IEnumerable<AvaliacaoNota>> ObterNotasPorJuradoAsync(int idJurado);
    Task<int> SalvarNotasAsync(int idJurado, IReadOnlyCollection<(int IdParticipante, int IdCriterio, int Nota)> notas);
    Task SalvarNotaAsync(int idJurado, int idParticipante, int idCriterio, int nota);
    Task<IEnumerable<AvaliacaoNota>> ObterNotasDoJuradoParaParticipanteAsync(int idJurado, int idParticipante);
    Task GarantirNotasEmBrancoAsync();
    Task ZerarTodasNotasAsync();
}
