using Microsoft.AspNetCore.Identity;

namespace VibeCoding.Api.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
        = null;
    public bool IsActive { get; set; } = true;

    public ICollection<ScenarioAttempt> Attempts { get; set; } = new List<ScenarioAttempt>();
}
