using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ILP.Server.Config;
using ILP.Server.Endpoints.EvidencePackages;
using ILP.Server.Endpoints.SourceDocuments;
using ILP.Server.Features.EvidenceStorage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    // The only sign-in is the development test scheme until Feature 17 adds real authentication.
    throw new InvalidOperationException(
        "No production authentication is configured (Feature 17). Run with ASPNETCORE_ENVIRONMENT=Development.");
}

builder.Services.AddOpenApi();
builder.Services.AddAuthentication(TestAuthHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SourceDocumentIntakePolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy(EvidenceStorageServiceCollectionExtensions.AuthorizationPolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});

builder.Services.AddEvidenceStorage(builder.Configuration);

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
        ["https://localhost:5173"];

    options.AddPolicy("ClientApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

OpenRouterConfig.Initialize(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("ClientApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapEndpoints();
app.MapEvidencePackagesEndpoints();

app.Run();

public partial class Program { }

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header)
            || !string.Equals(header.Scheme, SchemeName, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "camera-capture-test-user")],
            Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

