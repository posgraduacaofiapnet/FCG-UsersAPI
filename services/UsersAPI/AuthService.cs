using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace UsersAPI;

public sealed class AuthService(
    UsersDbContext dbContext,
    JwtTokenService tokenService)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IResult> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        if (await dbContext.Users.AnyAsync(user => user.Email == request.Email, cancellationToken))
        {
            return Results.UnprocessableEntity(new { error = "Users.EmailAlreadyRegistered" });
        }

        var createdAt = DateTimeOffset.UtcNow;
        var user = new UserAccount
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = "User",
            CreatedAt = createdAt.UtcDateTime
        };

        var notification = new UserCreatedNotification(user.Id, user.Name, user.Email, createdAt);
        var outboxMessage = OutboxMessage.Create(NotificationEventTypes.UserCreated, notification, JsonOptions);

        dbContext.Users.Add(user);
        dbContext.OutboxMessages.Add(outboxMessage);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return Results.Created($"/api/users/{user.Id}", new { user.Id, user.Name, user.Email, user.Role });
    }

    public async Task<IResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(user => user.Email == request.Email, cancellationToken);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new AuthResponse(tokenService.Generate(user), user.Id, user.Name, user.Email, user.Role));
    }
}
