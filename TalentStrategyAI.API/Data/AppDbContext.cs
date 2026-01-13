using Microsoft.EntityFrameworkCore;
using TalentStrategyAI.API.Models;

namespace TalentStrategyAI.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<EmployeeProfile> EmployeeProfiles { get; set; }
}

