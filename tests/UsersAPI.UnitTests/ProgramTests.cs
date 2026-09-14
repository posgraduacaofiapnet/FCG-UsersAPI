using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace UsersAPI.UnitTests;

public sealed class ProgramTests : IClassFixture<UsersApiFactory>
{
    private readonly HttpClient _client;

    public ProgramTests(UsersApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task HealthAndMetricsEndpoints_AreAvailable()
    {
        (await _client.GetAsync("/health")).EnsureSuccessStatusCode();
        var metrics = await _client.GetAsync("/metrics");
        metrics.EnsureSuccessStatusCode();
        Assert.Contains("# HELP", await metrics.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RegisterAndLoginFlow_WorksThroughHttpPipeline()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var registration = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Grace Hopper",
            email,
            password = "Senha@123"
        });
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Senha@123"
        });

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.True(login.Headers.Contains(CorrelationId.HeaderName));
    }

    [Fact]
    public async Task InvalidRegistration_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "",
            email = "invalid",
            password = "weak"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed class UsersApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"users-api-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=unused",
                ["Jwt:Key"] = "integration-test-key-with-at-least-thirty-two-characters",
                ["Jwt:Issuer"] = "UsersAPI",
                ["Jwt:Audience"] = "FCG",
                ["Admin:Email"] = "admin-test@fcg.com",
                ["Admin:Password"] = "AdminSenha@123"
            }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<UsersDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<UsersDbContext>>();
            services.RemoveAll<IDatabaseProvider>();
            services.RemoveAll<UsersDbContext>();
            services.AddDbContext<UsersDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
