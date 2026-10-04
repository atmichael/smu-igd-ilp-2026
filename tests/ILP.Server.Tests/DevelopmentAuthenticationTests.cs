using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ILP.Server.Tests;

public class DevelopmentAuthenticationTests : IClassFixture<EvidenceApiFactory>
{
    private readonly EvidenceApiFactory _factory;

    public DevelopmentAuthenticationTests(EvidenceApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Startup_OutsideDevelopment_FailsUntilRealAuthenticationExists()
    {
        using var production = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        var exception = Assert.ThrowsAny<Exception>(() => production.CreateClient());

        Assert.Contains("Feature 17", exception.ToString());
    }

    [Fact]
    public async Task Request_WithNonTestAuthorizationScheme_IsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "anything");

        var response = await client.GetAsync("/api/evidence-packages?caseId=any");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
