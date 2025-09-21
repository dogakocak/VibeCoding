using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VibeCoding.Api.Domain.Constants;
using VibeCoding.Api.Domain.Entities;

namespace VibeCoding.Api.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");

        foreach (var roleName in SystemRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole { Name = roleName, NormalizedName = roleName.ToUpperInvariant(), Description = $"System role: {roleName}" };
                var result = await roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to create role {Role}: {Errors}", roleName, string.Join(',', result.Errors.Select(e => e.Description)));
                }
            }
        }

        var adminEmail = configuration["Admin:Email"];
        var adminPassword = configuration["Admin:Password"];

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser is null)
            {
                adminUser = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = adminEmail,
                    NormalizedUserName = adminEmail.ToUpperInvariant(),
                    Email = adminEmail,
                    NormalizedEmail = adminEmail.ToUpperInvariant(),
                    DisplayName = "Administrator",
                    EmailConfirmed = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (!createResult.Succeeded)
                {
                    logger.LogError("Failed to create admin user: {Errors}", string.Join(',', createResult.Errors.Select(e => e.Description)));
                }
            }

            if (adminUser is not null && !await userManager.IsInRoleAsync(adminUser, SystemRoles.Admin))
            {
                var roleResult = await userManager.AddToRoleAsync(adminUser, SystemRoles.Admin);
                if (!roleResult.Succeeded)
                {
                    logger.LogError("Failed to assign admin role: {Errors}", string.Join(',', roleResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}