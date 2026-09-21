using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using Serilog;
using System.Threading.RateLimiting;
using VerusLLC.Api.Filters;
using VerusLLC.Api.Middleware;
using VerusLLC.Application.Common.Correlation;
using VerusLLC.Persistence.Companies;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Services.AddSerilog((services, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "VerusLLC.Api"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICorrelationIdAccessor, HttpCorrelationIdAccessor>();

builder.Services.AddApplication();
builder.Services.AddPersistence();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = new HeaderApiVersionReader("x-api-version");
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
}).AddOpenApi();

builder.Services.AddControllersWithViews(options =>
            options.Filters.Add<ApiExceptionFilterAttribute>());

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };

        problem.Extensions["correlationId"] = context.HttpContext.GetCorrelationId();

        return new BadRequestObjectResult(problem);
    };
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminSite", policyBuilder =>
    {
        // TODO: AllowAnyOrigin for local testing support. In production, this should be restricted to the admin site domain.
        //policyBuilder.WithOrigins(builder.Configuration.GetSection("Cors").Get<string[]>() ?? []);
        policyBuilder.AllowAnyOrigin();
        policyBuilder.AllowAnyHeader();
        policyBuilder.AllowAnyMethod();
        policyBuilder.AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 2,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var httpsRedirectionEnabled = (builder.Configuration["ASPNETCORE_URLS"] ?? string.Empty)
    .Contains("https://", StringComparison.OrdinalIgnoreCase);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

app.MapOpenApi().WithDocumentPerVersion();

app.MapScalarApiReference(options =>
{
    var descriptions = app.DescribeApiVersions();

    for (var i = 0; i < descriptions.Count; i++)
    {
        var description = descriptions[i];
        var isDefault = i == descriptions.Count - 1;

        options.AddDocument(description.GroupName, description.GroupName, isDefault: isDefault);
    }
});

if (httpsRedirectionEnabled)
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

if (app.Configuration.GetValue("Companies:SeedSampleData", true))
{
    await app.Services.GetRequiredService<CompanySeeder>().SeedAsync();
}

app.Run();

public partial class Program;
