using System.Text;
using Knightage.Crm.Api.Middleware;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;
using Knightage.Crm.Infrastructure.ExternalServices;
using Knightage.Crm.Infrastructure.Repositories;
using Knightage.Crm.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "wwwroot/browser"
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT bearer token issued by knightage-identity."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddMemoryCache();
builder.Services.AddScoped<CurrentTenantContext>();
builder.Services.AddScoped<DapperContext>();
builder.Services.AddScoped<ITenantDatabaseResolver, PlatformTenantDatabaseResolver>();
builder.Services.AddScoped<IPipelineStageRepository, PipelineStageRepository>();
builder.Services.AddScoped<ILeadRepository, LeadRepository>();
builder.Services.AddScoped<ILeadImportParser, CsvLeadImportParser>();

var platformBaseUrl = builder.Configuration["Services:PlatformBaseUrl"]
    ?? throw new InvalidOperationException("Services:PlatformBaseUrl is not configured.");
builder.Services.AddHttpClient("Platform", client =>
{
    client.BaseAddress = new Uri(platformBaseUrl);
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantResolutionMiddleware>();
app.MapControllers();

// Angular client-side routes (e.g. /leads) aren't real server routes -- fall back to
// index.html so the Angular router can handle them. Must come after MapControllers so API
// routes still resolve normally.
app.MapFallbackToFile("index.html");

app.Run();
