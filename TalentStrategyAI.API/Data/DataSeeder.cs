using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TalentStrategyAI.API.Models;

namespace TalentStrategyAI.API.Data;

public static class DataSeeder
{
    private const string SampleLoginsPath = "TestData/sample-logins.json";

    public static async Task SeedAsync(AppDbContext db, ILogger logger, bool syncPasswordsInDev = false)
    {
        try
        {
            await db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Migration failed (database may not exist yet). Seeding skipped.");
            return;
        }

        // Prefer output directory (when TestData is copied to output), then project directory
        var path = Path.Combine(AppContext.BaseDirectory, SampleLoginsPath);
        if (!System.IO.File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), SampleLoginsPath);
        }
        if (!System.IO.File.Exists(path))
        {
            logger.LogWarning("Sample logins file not found (tried BaseDirectory and CurrentDirectory). Skipping seed.");
            return;
        }

        string json;
        try
        {
            json = await System.IO.File.ReadAllTextAsync(path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read sample-logins.json. Skipping seed.");
            return;
        }

        List<SampleLogin>? logins;
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var arr = root.TryGetProperty("logins", out var l) ? l : default;
            if (arr.ValueKind != JsonValueKind.Array)
            {
                logger.LogWarning("sample-logins.json has no 'logins' array. Skipping seed.");
                return;
            }
            logins = new List<SampleLogin>();
            foreach (var item in arr.EnumerateArray())
            {
                var email = item.TryGetProperty("email", out var e) ? e.GetString() : null;
                var password = item.TryGetProperty("password", out var p) ? p.GetString() : null;
                var role = item.TryGetProperty("role", out var r) ? r.GetString() : "employee";
                var displayName = item.TryGetProperty("displayName", out var d) ? d.GetString() : email ?? "";
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                    continue;
                logins.Add(new SampleLogin { Email = email!, Password = password!, Role = role ?? "employee", DisplayName = displayName ?? email! });
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not parse sample-logins.json. Skipping seed.");
            return;
        }

        var added = 0;
        var synced = 0;
        foreach (var login in logins)
        {
            var email = login.Email.Trim().ToLowerInvariant();
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (existing != null)
            {
                if (syncPasswordsInDev)
                {
                    existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(login.Password.Trim(), BCrypt.Net.BCrypt.GenerateSalt(10));
                    existing.DisplayName = string.IsNullOrWhiteSpace(login.DisplayName) ? email : login.DisplayName.Trim();
                    await db.SaveChangesAsync();
                    synced++;
                }
                continue;
            }

            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(login.Password.Trim(), BCrypt.Net.BCrypt.GenerateSalt(10)),
                Role = login.Role == "manager" ? "manager" : "employee",
                DisplayName = string.IsNullOrWhiteSpace(login.DisplayName) ? email : login.DisplayName.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            if (user.Role == "employee")
            {
                db.EmployeeProfiles.Add(new EmployeeProfile
                {
                    UserId = user.Id,
                    Name = user.DisplayName,
                    Position = null,
                    Department = null
                });
                await db.SaveChangesAsync();
            }
            added++;
        }

        if (added > 0 || synced > 0)
            logger.LogInformation("Sample logins: added {Added}, password-synced {Synced} ({Total} in file).", added, synced, logins.Count);
    }

    private class SampleLogin
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "employee";
        public string DisplayName { get; set; } = "";
    }
}
