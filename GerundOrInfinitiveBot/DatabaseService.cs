using GerundOrInfinitiveBot.DataBaseObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GerundOrInfinitiveBot;

public class DatabaseService : DbContext
{
    private readonly string _connectionString;

    public DbSet<Example> Examples { get; private set; }
    public DbSet<UserData> UserData { get; private set; }
    
    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.LogTo(Console.WriteLine, LogLevel.Debug);
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.UseSqlServer(_connectionString);
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