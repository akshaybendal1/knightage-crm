using System.Text;
using Dapper;
using Knightage.Crm.Api.Middleware;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;
using Knightage.Crm.Infrastructure.ExternalServices;
using Knightage.Crm.Infrastructure.Repositories;
using Knightage.Crm.Infrastructure.Services;
using Knightage.Crm.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

SqlMapper.AddTypeHandler(new UtcDateTimeHandler());
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

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
builder.Services.AddScoped<TenantContext>();
// Scoped, not Singleton -- DapperContext now reads TenantContext.DatabaseName per
// request (for a tenant migrated to its own database), which is itself scoped.
builder.Services.AddScoped<DapperContext>();
builder.Services.AddScoped<IPipelineStageRepository, PipelineStageRepository>();
builder.Services.AddScoped<IRoleScreenAccessRepository, RoleScreenAccessRepository>();
builder.Services.AddScoped<IScreenAccessService, ScreenAccessService>();
builder.Services.AddScoped<IMessageTemplateRepository, MessageTemplateRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ILeadRepository, LeadRepository>();
builder.Services.AddScoped<ILeadActivityRepository, LeadActivityRepository>();
builder.Services.AddScoped<ILeadTaskRepository, LeadTaskRepository>();
builder.Services.AddScoped<IMeetingRepository, MeetingRepository>();
builder.Services.AddScoped<ILeadProductRepository, LeadProductRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IReportsRepository, ReportsRepository>();
builder.Services.AddScoped<ILeadImportParser, CsvLeadImportParser>();
builder.Services.AddScoped<ITimelineRepository, TimelineRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IProductStageRepository, ProductStageRepository>();
builder.Services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
builder.Services.AddScoped<IDocumentPdfBuilder, DocumentPdfBuilder>();
builder.Services.AddScoped<ISavedCustomReportRepository, SavedCustomReportRepository>();
builder.Services.AddScoped<ICustomReportRepository, CustomReportRepository>();
builder.Services.AddScoped<IOrganizationBrandingRepository, OrganizationBrandingRepository>();
builder.Services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();

var identityBaseUrl = builder.Configuration["Client:IdentityBaseUrl"]
    ?? throw new InvalidOperationException("Client:IdentityBaseUrl is not configured.");
builder.Services.AddHttpClient("Identity", client =>
{
    client.BaseAddress = new Uri(identityBaseUrl);
});
builder.Services.AddScoped<IIdentityUserDirectory, IdentityUserDirectory>();
builder.Services.AddScoped<IIdentityOrganizationClient, IdentityOrganizationClient>();
builder.Services.AddHttpClient("IdentityCalendar", client =>
{
    client.BaseAddress = new Uri(identityBaseUrl);
    // This call chain is CRM -> Identity -> Google, and a user's "save meeting" click
    // must never hang on a Google round-trip -- kept longer than Identity's own ~5s
    // timeout on its outbound Google call so Identity usually finishes (success or
    // failure) before CRM gives up.
    client.Timeout = TimeSpan.FromSeconds(8);
});
builder.Services.AddScoped<IIdentityCalendarClient, IdentityCalendarClient>();
builder.Services.AddScoped<IReportExcelExporter, ReportExcelExporter>();
builder.Services.AddScoped<IReportsService, ReportsService>();

var platformBaseUrl = builder.Configuration["Services:PlatformBaseUrl"]
    ?? throw new InvalidOperationException("Services:PlatformBaseUrl is not configured.");
builder.Services.AddHttpClient("Platform", client =>
{
    client.BaseAddress = new Uri(platformBaseUrl);
    // Short timeout: this call now sits on every authenticated request via
    // TenantResolutionMiddleware, not just the once-a-day digest -- a hung Platform must
    // fail fast, not stall every CRM request behind it.
    client.Timeout = TimeSpan.FromSeconds(3);
});
builder.Services.AddScoped<ISubscriptionStatusProvider, PlatformSubscriptionStatusProvider>();

// Customers (won leads + field sales) and their copy in the Inventory portal. The portal's address is not
// configured here: InventoryCustomerSync reads it from Platform's per-tenant services list.
builder.Services.AddHttpClient("InventorySales", client =>
{
    // Runs inside the request that won a lead or added a customer -- a slow portal must not hold that up.
    client.Timeout = TimeSpan.FromSeconds(8);
});
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IInventoryCustomerSync, InventoryCustomerSync>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

// No SMTP host configured (the default for a fresh checkout, and for prod until real
// creds are set) -> log instead of attempting a real connection, so nothing hangs/errors
// on missing creds. Same pattern as knightage-identity's Program.cs.
if (string.IsNullOrWhiteSpace(builder.Configuration["Smtp:Host"]))
{
    builder.Services.AddScoped<IEmailSender, NullEmailSender>();
}
else
{
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
}

// Same fallback pattern as IEmailSender above: no service account configured (a fresh
// checkout, or prod until real Firebase credentials are set) -> log instead of trying
// to call FCM without credentials.
if (string.IsNullOrWhiteSpace(builder.Configuration["Fcm:ServiceAccountKeyPath"]))
{
    builder.Services.AddScoped<IPushSender, NullPushSender>();
}
else
{
    // Must run exactly once per process (throws on a second call), so it lives here in
    // the startup path rather than inside FcmPushSender's constructor.
    var serviceAccountCredential = Google.Apis.Auth.OAuth2.CredentialFactory
        .FromFile<Google.Apis.Auth.OAuth2.ServiceAccountCredential>(builder.Configuration["Fcm:ServiceAccountKeyPath"]);
    FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions
    {
        Credential = serviceAccountCredential.ToGoogleCredential()
    });
    builder.Services.AddScoped<IPushSender, FcmPushSender>();
}

builder.Services.AddSingleton<ReminderDigestGate>();
builder.Services.AddScoped<IReminderDigestService, ReminderDigestService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
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

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevClients", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Security headers missing entirely before this: no clickjacking protection, no MIME-sniffing
// protection, no HSTS. Not going further into a full script-src/style-src CSP -- the app still
// uses href="javascript:void(0)" for a few click handlers (app.html, login.html) and Angular's
// own [style.width.%]-style bindings write inline styles, both of which a strict CSP without
// 'unsafe-inline' would silently break. frame-ancestors covers the actual clickjacking risk
// without touching either of those.
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    context.Response.Headers.Append("Content-Security-Policy", "frame-ancestors 'none'");
    await next();
});

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("DevClients");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantResolutionMiddleware>();
app.MapControllers();

// Angular client-side routes (e.g. /leads) aren't real server routes -- fall back to
// index.html so the Angular router can handle them. Must come after MapControllers so API
// routes still resolve normally.
app.MapFallbackToFile("index.html");

app.Run();
