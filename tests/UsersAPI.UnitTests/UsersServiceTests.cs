using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UsersAPI;

namespace UsersAPI.UnitTests;

public sealed class UsersFixture
{
    public Faker Faker { get; } = new("pt_BR");

    public UsersDbContext CreateDbContext() => new(new DbContextOptionsBuilder<UsersDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    public JwtTokenService CreateTokenService() => new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "unit-test-key-with-at-least-thirty-two-characters-long",
            ["Jwt:Issuer"] = "UsersAPI",
            ["Jwt:Audience"] = "FCG"
        })
        .Build());
}

public sealed class UsersServiceTests(UsersFixture fixture) : IClassFixture<UsersFixture>
{
    [Theory]
    [InlineData("curta1!", false)]
    [InlineData("somenteletras!", false)]
    [InlineData("Senha123", false)]
    [InlineData("Senha@123", true)]
    public void PasswordPolicy_ValidatesStrength(string password, bool expected) =>
        Assert.Equal(expected, PasswordPolicy.IsStrong(password));

    [Fact]
    public void PasswordHasher_HashesAndVerifiesWithoutStoringPlainText()
    {
        const string password = "Senha@123";
        var hash = PasswordHasher.Hash(password);

        Assert.NotEqual(password, hash);
        Assert.True(PasswordHasher.Verify(password, hash));
        Assert.False(PasswordHasher.Verify("Outra@123", hash));
    }

    [Fact]
    public async Task RegisterAsync_PersistsUserAndOutboxInSameUnitOfWork()
    {
        await using var db = fixture.CreateDbContext();
        var service = new AuthService(db, fixture.CreateTokenService());
        var request = new RegisterUserRequest(fixture.Faker.Name.FullName(), fixture.Faker.Internet.Email(), "Senha@123");

        await service.RegisterAsync(request, CancellationToken.None);

        var user = Assert.Single(await db.Users.ToListAsync());
        Assert.Equal(request.Email, user.Email);
        Assert.True(PasswordHasher.Verify(request.Password, user.PasswordHash));
        var outbox = Assert.Single(await db.OutboxMessages.ToListAsync());
        Assert.Equal(NotificationEventTypes.UserCreated, outbox.EventType);
        Assert.Contains(user.Id.ToString(), outbox.Payload);
        Assert.False(outbox.IsSuccessful);
        Assert.Null(outbox.NextAttemptAt);
        Assert.Equal(0, outbox.Attempts);
    }

    [Fact]
    public async Task RegisterValidator_RejectsInvalidRequest()
    {
        var result = await new RegisterUserRequestValidator().ValidateAsync(
            new RegisterUserRequest("", "invalid-email", "weak"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterUserRequest.Email));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterUserRequest.Password));
    }

    [Fact]
    public void CorrelationId_PreservesValidValueAndRegeneratesInvalidValue()
    {
        Assert.Equal("lesson-123", CorrelationId.Normalize(" lesson-123 "));
        Assert.Equal(32, CorrelationId.Normalize(new string('x', 129)).Length);
    }
}
