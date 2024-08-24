using GerundOrInfinitiveBot.DataBaseObjects;
using GerundOrInfinitiveBot.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GerundOrInfinitiveBot.Services.Database;

public class DatabaseService : DbContext
{
    private readonly string _connectionString;
    private readonly ILogger _logger;

    public DbSet<Example> Examples { get; private set; }
    public DbSet<UserData> UserData { get; private set; }
    
    public DatabaseService(IOptions<ConnectionSettings> options)
    {
        _connectionString = options.Value.SqlServerConnection;
        //_logger = logger;
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