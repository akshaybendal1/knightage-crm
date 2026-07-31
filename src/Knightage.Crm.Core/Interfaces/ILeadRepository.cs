using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface ILeadRepository
{
    Task<IReadOnlyList<Lead>> GetAllAsync(Guid? pipelineStageId = null);
    Task<Lead?> GetByIdAsync(Guid id);
    Task<Lead> CreateAsync(Lead lead);
    Task UpdateAsync(Lead lead);
    Task<IReadOnlyList<Lead>> CreateManyAsync(IEnumerable<Lead> leads);
}
