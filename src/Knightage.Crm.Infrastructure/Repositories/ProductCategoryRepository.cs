using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class ProductCategoryRepository : IProductCategoryRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public ProductCategoryRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<ProductCategory>> GetActiveAsync()
    {
        using var connection = _context.CreateConnection();
        var categories = await connection.QueryAsync<ProductCategory>("usp_ProductCategories_GetActive",
            new { OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
        return categories.ToList();
    }

    public async Task<ProductCategory?> GetByIdAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ProductCategory>("usp_ProductCategories_GetById",
            new { Id = id, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    public async Task<ProductCategory> CreateAsync(ProductCategory category)
    {
        category.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_ProductCategories_Create", category, commandType: CommandType.StoredProcedure);
        return category;
    }

    public async Task UpdateAsync(ProductCategory category)
    {
        category.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_ProductCategories_Update",
            new { category.Id, category.OrganizationId, category.Name, category.SortOrder }, commandType: CommandType.StoredProcedure);
    }

    public async Task SetActiveAsync(Guid id, bool isActive)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_ProductCategories_SetActive",
            new { Id = id, IsActive = isActive, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }
}
