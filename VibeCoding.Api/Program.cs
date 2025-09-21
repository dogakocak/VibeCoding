using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using VibeCoding.Api.Domain.Constants;
using VibeCoding.Api.Infrastructure.Configurations;
using VibeCoding.Api.Infrastructure.Identity;
using VibeCoding.Api.Infrastructure.Options;
using VibeCoding.Api.Infrastructure.Redis;
using VibeCoding.Api.Validators;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddApplicationOptions(configuration);
builder.Services.AddApplicationPersistence(configuration);
builder.Services.AddApplicationCore(configuration);

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .Select(x => new { Field = x.Key, Messages = x.Value!.Errors.Select(e => e.ErrorMessage) });
        return new BadRequestObjectResult(new { Errors = errors });
    };
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<AuthRegisterRequestValidator>();

builder.Services.AddOpenApi();

var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration missing");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole(SystemRoles.Admin));
    options.AddPolicy("RequireTrainer", policy => policy.RequireRole(SystemRoles.Trainer, SystemRoles.Admin));
});

builder.Services.AddProblemDetails();

var app = builder.Build();

await IdentitySeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<RedisRateLimitMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();