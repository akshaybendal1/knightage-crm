using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface IMessageTemplateRepository
{
    Task<IReadOnlyList<MessageTemplate>> GetAllAsync();
    Task<MessageTemplate?> GetByIdAsync(Guid id);
    Task<MessageTemplate> CreateAsync(MessageTemplate template);
    Task UpdateAsync(MessageTemplate template);
    Task SetActiveAsync(Guid id, bool isActive);
}
