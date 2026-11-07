using VOTACAO.Domain.DTOs;
using VOTACAO.Domain.Entities;

namespace VOTACAO.BO.Services;

public interface IParticipanteService
{
    Task<IEnumerable<Participante>> ObterTodosAsync();
    Task<IEnumerable<Participante>> PesquisarAsync(string termo);
    Task<Participante?> ObterPorIdAsync(int id);
    Task<int> SalvarAsync(ParticipanteDto dto);
    Task<bool> ExcluirAsync(int id);
    Task<int> ObterTotalAsync();
}
