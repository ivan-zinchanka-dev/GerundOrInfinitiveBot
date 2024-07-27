using GerundOrInfinitiveBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GerundOrInfinitiveBot;

public class DatabaseService : DbContext
{
    private const string SqlServerConnectionKey = "SqlServerConnection";
    
    private readonly IConfigurationRoot _configurationRoot;
    public DbSet<Example> Examples { get; private set; }
    public DbSet<UserData> UserData { get; private set; }
    
    public DatabaseService(IConfigurationRoot configurationRoot)
    {
        _configurationRoot = configurationRoot;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.LogTo(Console.WriteLine, LogLevel.Debug);
        optionsBuilder.UseSqlServer(_configurationRoot.GetConnectionString(SqlServerConnectionKey));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Example>()
            .HasKey(example => example.Id);
        
        modelBuilder.Entity<UserData>()
            .HasKey(user=>user.UserId);
            
        modelBuilder.Entity<UserData>()
            .HasOne(user => user.CurrentExample)
            .WithMany(example => example.CurrentUsers)
            .HasForeignKey(user => user.CurrentExampleId);
    }
}