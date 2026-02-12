using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TalentStrategyAI.API.Data;
using TalentStrategyAI.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework Core (SQLite in Development, SQL Server otherwise)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
if (connectionString.TrimStart().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) || connectionString.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// JWT authentication
var jwtKey = builder.Configuration["Jwt:SecretKey"] ?? "fallback-secret-at-least-32-chars!!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TalentStrategyAI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TalentStrategyAI";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// Add CORS policy (allowing any origin for development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register services
builder.Services.AddScoped<IOpenAIService, OpenAIService>();
builder.Services.AddScoped<IResumeTextService, ResumeTextService>();

// HttpClient for resume-api.campbellthompson.com (chat, jobs, recommendations)
var resumeApiBase = builder.Configuration["ResumeApi:BaseUrl"];
if (!string.IsNullOrWhiteSpace(resumeApiBase))
{
    builder.Services.AddHttpClient("ResumeApi", client =>
    {
        client.BaseAddress = new Uri(resumeApiBase.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Enable static files and default files (for serving index.html at root)
app.UseStaticFiles();
app.UseDefaultFiles();

// Map controllers
app.MapControllers();

// Seed test users from TestData/sample-logins.json if database is empty
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DataSeeder.SeedAsync(db, logger);
}

app.Run();
