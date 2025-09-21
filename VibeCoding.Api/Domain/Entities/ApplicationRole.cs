using Microsoft.AspNetCore.Identity;

namespace VibeCoding.Api.Domain.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
