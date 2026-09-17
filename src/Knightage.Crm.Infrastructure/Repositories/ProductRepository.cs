using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public ProductRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        var products = await connection.QueryAsync<Product>("usp_Products_GetAll",
            new { OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
        return products.ToList();
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Product>("usp_Products_GetById",
            new { Id = id, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    public async Task<Product> CreateAsync(Product product)
    {
        product.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Products_Create", new
        {
            product.Id, product.OrganizationId, product.Name, product.Description, product.Category,
            product.Price, product.IsActive, product.CreatedByUserId, product.CreatedAtUtc
        }, commandType: CommandType.StoredProcedure);
        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        product.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Products_Update", new
        {
            product.Id, product.OrganizationId, product.Name, product.Description, product.Category,
            product.Price, product.UpdatedByUserId, product.UpdatedAtUtc
        }, commandType: CommandType.StoredProcedure);
    }

    public async Task SetActiveAsync(Guid id, bool isActive)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Products_SetActive",
            new { Id = id, IsActive = isActive, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }
}
