using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface IPipelineStageRepository
{
    Task<IReadOnlyList<PipelineStage>> GetActiveAsync();
    Task<PipelineStage?> GetByIdAsync(Guid id);
    Task<PipelineStage> CreateAsync(PipelineStage stage);
    Task UpdateAsync(PipelineStage stage);
}
