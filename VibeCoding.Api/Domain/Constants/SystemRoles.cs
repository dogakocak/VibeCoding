namespace VibeCoding.Api.Domain.Constants;

public static class SystemRoles
{
    public const string Admin = "Admin";
    public const string Trainer = "Trainer";
    public const string Learner = "Learner";

    public static readonly string[] All = { Admin, Trainer, Learner };
}