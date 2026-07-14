using Microsoft.EntityFrameworkCore;

namespace UsersAPI;

public static class DatabaseSeeder
{
    public static async Task SeedAdminAsync(UsersDbContext dbContext, IConfiguration configuration, ILogger logger)
    {
        var email = configuration["Admin:Email"] ?? "admin@fcg.com";
        var password = configuration["Admin:Password"] ?? "AdminSenha@123";

        if (!PasswordPolicy.IsStrong(password))
        {
            logger.LogWarning("A senha configurada para o seed do Admin é fraca e não atende aos requisitos de segurança. O Seed foi abortado.");
            return;
        }

        var adminExists = await dbContext.Users.AnyAsync(u => u.Email == email);
        if (adminExists)
        {
            return;
        }

        var admin = new UserAccount
        {
            Id = Guid.NewGuid(),
            Name = "FCG Admin",
            Email = email,
            PasswordHash = PasswordHasher.Hash(password),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Administrador padrão seedado com sucesso: {Email}", email);
    }
}
