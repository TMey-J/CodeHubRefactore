using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeHub.Api.Common;
using Microsoft.OpenApi;
using CodeHub.Api.Endpoints;
using CodeHub.Application;
using CodeHub.Application.Common;
using CodeHub.Infrastructure;
using CodeHub.Infrastructure.Persistence;
using CodeHub.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>() ?? throw new InvalidOperationException("Jwt configuration is missing.");

        options.MapInboundClaims = false;
        options.TokenHandlers.Add(new JwtSecurityTokenHandler());
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var jsonOptions = context.HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;
                var envelope = ApiResponse<object>.Fail(["token invalid"]);
                await context.Response.WriteAsJsonAsync(envelope, jsonOptions.SerializerOptions);
            }
        };
    });

builder.Services.AddAuthorization();

// Wire-compatible JSON: snake_case property names and lowercase enum values ("public"/"private").
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddOpenApi();

// Swagger UI (like the legacy FastAPI /docs) with a Bearer scheme so
// protected endpoints can be tested from the UI itself.
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CodeHub",
        Version = "v1",
        Description = "CodeHub API — ASP.NET Core 10 Minimal API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token returned by /auth/login."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseCors();

// Serve Swagger in every environment (the legacy app exposed /docs the same way).
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CodeHub v1");
});

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGroup("/api/v1")
    .MapAuthEndpoints()
    .MapRepositoryEndpoints()
    .MapFileEndpoints()
    .MapDashboardEndpoints()
    .MapUserEndpoints();

await app.ApplyMigrationsAsync();

app.Run();

public partial class Program;
